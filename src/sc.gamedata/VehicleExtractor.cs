using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using unforge;

namespace sc.gamedata
{
	internal static class VehicleExtractor
	{
		private const String PathPrefix = "libs/foundry/records/entities/spaceships/";

		// Matches CIG's `_PU_Pirate`, `_Wreck`, `_NPC`, `_Hijacked`, etc. on
		// the entity id so mission/AI/event variants don't clutter the player
		// vehicle roster. Mirrors the regex in
		// battlestations/src/utils/penetration/damageRules.ts and
		// zandbox/extract_game_data.py — keep all three in sync if updated.
		private static readonly Regex NonPlayerIdRe = new(
			@"(^Orbital_Sentry"
			+ @"|_Hijacked(?:_|$)"
			+ @"|_PU_(Pirate|UEE|NineTails|Criminal|HOS)"
			+ @"|_Pirate$"
			+ @"|_Showdown$|_ShipShowdown$"
			+ @"|_BIS\d+"
			+ @"|_Drug_"
			+ @"|_Unmanned$|_Crewless$|_Bombless$"
			+ @"|_Wreck(?:_|$)"
			+ @"|_NPC(?:_|$)|_AI(?:_|$)"
			+ @"|_Boarded$"
			+ @"|_Mission_PIR|_EA_PIR|_EA_Outlaws$"
			+ @"|_Collector_(Stealth|Military|Mod)"
			+ @"|_Exec_(Stealth|Military|StealthIndustrial)"
			+ @"|_Stealth$"
			+ @"|_S3Bombs$"
			+ @"|_FW22NFZ|_Fleetweek"
			+ @"|_Swarm$|_Civilian$"
			+ @"|_Arena_Commander|_Derelict|_Tutorial)",
			RegexOptions.Compiled);

		private static readonly Regex ClassRe = new(@"class[_]?(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex MissileAttachRe = new(@"^missile +0*(\d+)( +attach)?$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static List<VehicleRecord> Extract(
			DataForge df,
			IDictionary<String, String> loc,
			IReadOnlyDictionary<String, WeaponRecord> weaponByGuid,
			IReadOnlyDictionary<String, WeaponRecord> weaponById,
			IReadOnlyDictionary<String, ArmorRecord> armorById,
			IReadOnlyDictionary<String, ArmorRecord> armorByGuid,
			IReadOnlyDictionary<String, IfcsRecord> ifcsById,
			IReadOnlyDictionary<String, IfcsRecord> ifcsByGuid,
			IReadOnlyDictionary<String, ThrusterRecord> thrusterById,
			IReadOnlyDictionary<String, ThrusterRecord> thrusterByGuid)
		{
			var result = new List<VehicleRecord>();
			foreach (var path in df.PathToRecordMap.Keys)
			{
				if (!path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)) continue;
				var rest = path.Substring(PathPrefix.Length);
				if (rest.Contains('/')) continue;

				var root = df.ReadRecordByPathAsXml(path);
				if (root == null) continue;
				var entityId = XmlHelpers.EntityIdFromRoot(root);
				if (NonPlayerIdRe.IsMatch(entityId)) continue;

				var loadout = XmlNav.FindFirst(root, "SEntityComponentDefaultLoadoutParams");
				var topLoadout = loadout != null ? XmlNav.FindFirst(loadout, "loadout") : null;
				if (topLoadout == null) continue;

				// Hardpoint armor — both spellings (US/UK) appear across CIG's
				// data, and a few ships reference armor by GUID instead of
				// className (Carrack and friends).
				String? armorId = null;
				foreach (var entry in DirectEntries(topLoadout))
				{
					var portName = XmlHelpers.Attr(entry, "itemPortName") ?? "";
					if (portName == "hardpoint_armor" || portName == "hardpoint_armour")
					{
						var cn = XmlHelpers.Attr(entry, "entityClassName");
						if (!String.IsNullOrEmpty(cn))
						{
							armorId = cn;
						}
						else
						{
							var refGuid = XmlHelpers.Attr(entry, "entityClassReference");
							if (!String.IsNullOrEmpty(refGuid)
								&& armorByGuid.TryGetValue(refGuid, out var resolved))
								armorId = resolved.id;
						}
						break;
					}
				}

				// DFS the loadout tree: every chain that resolves to a known
				// weapon (gun OR missile) becomes a slot. The walker only
				// yields when the leaf entry's classRef/className matches
				// our weapon index, so dashboards/coolers/thrusters get
				// filtered automatically.
				var slots = new List<SlotRecord>();
				var seenChains = new HashSet<String>();
				foreach (var (chain, weapon) in WalkForWeapons(topLoadout, new List<String>(), weaponByGuid, weaponById))
				{
					var chainKey = String.Join('/', chain);
					if (!seenChains.Add(chainKey)) continue;
					var size = weapon.size != 0 ? weapon.size : SlotSizeFromChain(chain);
					slots.Add(new SlotRecord
					{
						label = FormatSlotLabel(chain),
						size = size,
						kind = weapon.kind,
						stock_weapon_id = weapon.id,
					});
				}

				// Collapse consecutive missile slots that share a rack-prefix
				// label (`bay door > #1`, `bay door > #2`) into a single
				// missile_rack entry. This matches the shape the analyzer's
				// rack picker expects (min/max size + stock missile list).
				var collapsedSlots = CollapseMissileRacks(slots);

				ArmorRecord? armor = null;
				if (!String.IsNullOrEmpty(armorId)) armorById.TryGetValue(armorId, out armor);

				if (collapsedSlots.Count == 0 && armor == null) continue;

				// Display name fix-ups: drop the leading manufacturer word
				// when the loc entry includes it (Aegis Avenger Titan →
				// Avenger Titan, RSI Aurora MR → Aurora MR).
				XmlElement? attach = XmlNav.FindFirst(root, "AttachDef");
				String? locKey = null;
				if (attach != null)
				{
					foreach (XmlNode c in attach.ChildNodes)
						if (c is XmlElement e && e.LocalName == "Localization") { locKey = XmlHelpers.Attr(e, "Name"); break; }
				}
				var fallback = NameResolver.PrettifyEntityId(entityId, "");
				var name = NameResolver.Resolve(locKey, loc, fallback, entityId);
				var nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (nameWords.Length > 1
					&& !String.IsNullOrEmpty(fallback)
					&& !fallback.StartsWith(nameWords[0], StringComparison.Ordinal))
					name = String.Join(' ', nameWords.Skip(1));

				collapsedSlots.Sort((a, b) =>
				{
					var bySize = b.size.CompareTo(a.size);
					return bySize != 0 ? bySize : String.Compare(a.label, b.label, StringComparison.Ordinal);
				});

				var vehicle = new VehicleRecord
				{
					id = entityId,
					name = name,
					armor_id = armorId,
					slots = collapsedSlots,
				};
				if (armor != null)
				{
					vehicle.deflection = armor.deflection;
					vehicle.multiplier = armor.multiplier;
					vehicle.resistance = armor.resistance;
				}

				// Walk the loadout AGAIN for physics references. We can't reuse
				// WalkForWeapons — its early-yield-on-leaf semantics emit
				// chains, not individual entries, and would miss thrusters
				// that aren't leaves. Sibling walker WalkAllEntries hands us
				// every entry; we categorize against the new dicts here.
				IfcsRecord? ifcs = null;
				var thrustersList = new List<ThrusterRecord>();
				foreach (var entry in WalkAllEntries(topLoadout))
				{
					var entryRefGuid = XmlHelpers.Attr(entry, "entityClassReference");
					var entryCn = XmlHelpers.Attr(entry, "entityClassName");

					if (ifcs == null)
					{
						if (!String.IsNullOrEmpty(entryRefGuid) && ifcsByGuid.TryGetValue(entryRefGuid, out var byGuid))
							ifcs = byGuid;
						else if (!String.IsNullOrEmpty(entryCn) && ifcsById.TryGetValue(entryCn, out var byId))
							ifcs = byId;
					}

					if (!String.IsNullOrEmpty(entryRefGuid) && thrusterByGuid.TryGetValue(entryRefGuid, out var tByGuid))
						thrustersList.Add(tByGuid);
					else if (!String.IsNullOrEmpty(entryCn) && thrusterById.TryGetValue(entryCn, out var tById))
						thrustersList.Add(tById);
				}

				PhysicsAggregator.Aggregate(vehicle, root, ifcs, thrustersList, armor);

				// Bounding-box dims + crew + role/career. Cross-section is
				// already populated by PhysicsAggregator from the same
				// maxBoundingBoxSize element; the meters-rounded length /
				// width / height fields below stay for backward-compat with
				// game_data.json consumers that read those keys directly.
				var vehicleComponent = XmlNav.FindFirst(root, "VehicleComponentParams");
				if (vehicleComponent != null)
				{
					vehicle.crew_size = XmlHelpers.AttrIntNullable(vehicleComponent, "crewSize");
					var careerKey = XmlHelpers.Attr(vehicleComponent, "vehicleCareer");
					var roleKey = XmlHelpers.Attr(vehicleComponent, "vehicleRole");
					if (!String.IsNullOrEmpty(careerKey))
						vehicle.career = NameResolver.Resolve(careerKey, loc, "");
					if (!String.IsNullOrEmpty(roleKey))
						vehicle.role = NameResolver.Resolve(roleKey, loc, "");

					foreach (XmlNode c in vehicleComponent.ChildNodes)
					{
						if (c is XmlElement e && e.LocalName == "maxBoundingBoxSize")
						{
							vehicle.length = XmlHelpers.Round(XmlHelpers.AttrDoubleNullable(e, "y"), 2);
							vehicle.width = XmlHelpers.Round(XmlHelpers.AttrDoubleNullable(e, "x"), 2);
							vehicle.height = XmlHelpers.Round(XmlHelpers.AttrDoubleNullable(e, "z"), 2);
							break;
						}
					}
				}

				result.Add(vehicle);
			}
			return result;
		}

		private static IEnumerable<XmlElement> DirectEntries(XmlElement loadoutElem)
		{
			// SItemPortLoadoutManualParams/entries/SItemPortLoadoutEntryParams
			XmlElement? manual = null;
			foreach (XmlNode c in loadoutElem.ChildNodes)
				if (c is XmlElement e && e.LocalName == "SItemPortLoadoutManualParams") { manual = e; break; }
			if (manual == null) yield break;
			XmlElement? entries = null;
			foreach (XmlNode c in manual.ChildNodes)
				if (c is XmlElement e && e.LocalName == "entries") { entries = e; break; }
			if (entries == null) yield break;
			foreach (XmlNode c in entries.ChildNodes)
				if (c is XmlElement e && e.LocalName == "SItemPortLoadoutEntryParams") yield return e;
		}

		// Yields every SItemPortLoadoutEntryParams in the loadout subtree,
		// regardless of whether the entry's ref resolves to a weapon. Used by
		// the physics pass to categorize entries against multiple dicts
		// (ifcs, thrusters, armor) without forcing them through WalkForWeapons'
		// weapon-leaf early-yield logic.
		private static IEnumerable<XmlElement> WalkAllEntries(XmlElement loadoutElem)
		{
			foreach (var entry in DirectEntries(loadoutElem))
			{
				yield return entry;
				XmlElement? nested = null;
				foreach (XmlNode c in entry.ChildNodes)
					if (c is XmlElement e && e.LocalName == "loadout") { nested = e; break; }
				if (nested != null)
					foreach (var nestedEntry in WalkAllEntries(nested))
						yield return nestedEntry;
			}
		}

		private static IEnumerable<(List<String> chain, WeaponRecord weapon)> WalkForWeapons(
			XmlElement loadoutElem,
			List<String> chain,
			IReadOnlyDictionary<String, WeaponRecord> weaponByGuid,
			IReadOnlyDictionary<String, WeaponRecord> weaponById)
		{
			foreach (var entry in DirectEntries(loadoutElem))
			{
				var portName = XmlHelpers.Attr(entry, "itemPortName") ?? "";
				var newChain = new List<String>(chain) { portName };

				WeaponRecord? weapon = null;
				var refGuid = XmlHelpers.Attr(entry, "entityClassReference");
				var cn = XmlHelpers.Attr(entry, "entityClassName");
				if (!String.IsNullOrEmpty(refGuid) && weaponByGuid.TryGetValue(refGuid, out var byGuid)) weapon = byGuid;
				else if (!String.IsNullOrEmpty(cn) && weaponById.TryGetValue(cn, out var byId)) weapon = byId;

				XmlElement? nested = null;
				foreach (XmlNode c in entry.ChildNodes)
					if (c is XmlElement e && e.LocalName == "loadout") { nested = e; break; }
				if (nested != null)
					foreach (var pair in WalkForWeapons(nested, newChain, weaponByGuid, weaponById))
						yield return pair;

				if (weapon != null) yield return (newChain, weapon);
			}
		}

		private static Int32 SlotSizeFromChain(List<String> chain)
		{
			foreach (var port in chain)
			{
				var m = ClassRe.Match(port);
				if (m.Success && Int32.TryParse(m.Groups[1].Value, out var v)) return v;
			}
			return 0;
		}

		private static String FormatSlotLabel(List<String> chain)
		{
			var cleaned = new List<String>();
			foreach (var port in chain)
			{
				var s = port;
				foreach (var prefix in new[]
				{
					"hardpoint_weapon_gun_",
					"hardpoint_weapon_",
					"hardpoint_turret_",
					"hardpoint_",
				})
				{
					if (s.StartsWith(prefix, StringComparison.Ordinal))
					{
						s = s.Substring(prefix.Length);
						break;
					}
				}
				s = ClassRe.Replace(s, "").Replace("__", "_").Trim('_').Replace('_', ' ').Trim();
				var mAttach = MissileAttachRe.Match(s);
				if (mAttach.Success) s = "#" + mAttach.Groups[1].Value;
				if (!String.IsNullOrEmpty(s)) cleaned.Add(s);
			}
			return cleaned.Count > 0 ? String.Join(" > ", cleaned) : (chain.Count > 0 ? chain[^1] : "");
		}

		// Group consecutive missile slots that share a rack-prefix label into
		// one missile_rack entry. Slot labels at this point look like
		// `left bay door > #1`, `left bay door > #2`, … — we strip the
		// `> #N` suffix and collapse runs that share the same prefix.
		private static List<SlotRecord> CollapseMissileRacks(List<SlotRecord> slots)
		{
			var rackSuffix = new Regex(@"^(.+?)\s*>\s*#\d+$", RegexOptions.Compiled);
			var result = new List<SlotRecord>();
			var i = 0;
			while (i < slots.Count)
			{
				var slot = slots[i];
				if (slot.kind != "missile")
				{
					result.Add(slot);
					i++;
					continue;
				}
				var m = rackSuffix.Match(slot.label);
				var prefix = m.Success ? m.Groups[1].Value.Trim() : slot.label;
				var rackMembers = new List<SlotRecord>();
				var j = i;
				while (j < slots.Count && slots[j].kind == "missile")
				{
					var jm = rackSuffix.Match(slots[j].label);
					var jp = jm.Success ? jm.Groups[1].Value.Trim() : slots[j].label;
					if (jp != prefix) break;
					rackMembers.Add(slots[j]);
					j++;
				}
				result.Add(new SlotRecord
				{
					label = prefix,
					kind = "missile_rack",
					min_size = rackMembers.Min(s => s.size),
					max_size = rackMembers.Max(s => s.size),
					size = rackMembers[0].size,
					stock_missile_ids = rackMembers.Select(s => s.stock_weapon_id).ToList(),
				});
				i = j;
			}
			return result;
		}
	}
}
