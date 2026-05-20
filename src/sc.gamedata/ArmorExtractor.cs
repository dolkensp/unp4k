using System;
using System.Collections.Generic;
using System.Xml;
using unforge;

namespace sc.gamedata
{
	internal static class ArmorExtractor
	{
		private const String PathPrefix = "libs/foundry/records/entities/scitem/ships/armor/";

		public static List<ArmorRecord> Extract(DataForge df, IDictionary<String, String> loc)
		{
			var result = new List<ArmorRecord>();
			foreach (var path in df.PathToRecordMap.Keys)
			{
				if (!path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)) continue;
				// Skip nested subfolders if any — direct children only.
				var rest = path.Substring(PathPrefix.Length);
				if (rest.Contains('/')) continue;

				var root = df.ReadRecordByPathAsXml(path);
				if (root == null) continue;
				var entityId = XmlHelpers.EntityIdFromRoot(root);
				var armorGuid = XmlHelpers.Attr(root, "__ref");

				var armor = XmlNav.FindFirst(root, "SCItemVehicleArmorParams");
				if (armor == null) continue;

				var deflectionWrap = XmlNav.FindFirst(armor, "armorDeflection");
				var deflectionValue = deflectionWrap != null ? XmlNav.FindFirst(deflectionWrap, "deflectionValue") : null;
				if (deflectionValue == null) continue;

				var multiplierWrap = XmlNav.FindFirst(armor, "damageMultiplier");
				var multiplierInfo = multiplierWrap != null ? XmlNav.FindFirst(multiplierWrap, "DamageInfo") : null;

				// Resistance multipliers come from SHealthComponentParams. Six
				// per-axis children, each with a Multiplier attribute.
				var resistance = new ResistanceProfile();
				var damageResistance = XmlNav.FindFirst(root, "DamageResistance");
				if (damageResistance != null)
				{
					resistance.phys = ReadResistance(damageResistance, "PhysicalResistance");
					resistance.energy = ReadResistance(damageResistance, "EnergyResistance");
					resistance.dist = ReadResistance(damageResistance, "DistortionResistance");
					resistance.therm = ReadResistance(damageResistance, "ThermalResistance");
					resistance.bio = ReadResistance(damageResistance, "BiochemicalResistance");
					resistance.stun = ReadResistance(damageResistance, "StunResistance");
				}

				// Localization Name: SAttachableComponentParams/AttachDef/Localization
				XmlElement? attach = XmlNav.FindFirst(root, "AttachDef");
				String? locKey = null;
				if (attach != null)
				{
					foreach (XmlNode c in attach.ChildNodes)
						if (c is XmlElement e && e.LocalName == "Localization") { locKey = XmlHelpers.Attr(e, "Name"); break; }
				}

				var fallback = NameResolver.PrettifyEntityId(entityId, "ARMR_");
				var name = NameResolver.Resolve(locKey, loc, fallback, entityId);
				// Trim CIG's redundant suffixes — same cleanup the Python
				// extractor does so the dropdown reads naturally.
				if (name.EndsWith(" Ship Armor", StringComparison.Ordinal))
					name = name[..^" Ship Armor".Length].TrimEnd();
				else if (name.EndsWith(" Armor", StringComparison.Ordinal))
					name = name[..^" Armor".Length].TrimEnd();
				if (String.IsNullOrEmpty(name)) name = fallback;

				// Hull HP: SHealthComponentParams/@Health on the armor item is
				// what players see as "hull HP" — the spaceship record's own
				// SHealthComponentParams is always 1 (just a container).
				Double? hullHp = null;
				var health = XmlNav.FindFirst(root, "SHealthComponentParams");
				if (health != null)
				{
					var h = XmlHelpers.AttrDouble(health, "Health", -1);
					if (h > 0) hullHp = h;
				}

				result.Add(new ArmorRecord
				{
					id = entityId,
					name = name,
					deflection = XmlHelpers.DamageFrom(deflectionValue),
					multiplier = multiplierInfo != null ? XmlHelpers.DamageFrom(multiplierInfo) : new DamageProfile(),
					resistance = resistance,
					hull_hp = hullHp,
					_guid = armorGuid,
				});
			}
			return result;
		}

		private static Double ReadResistance(XmlElement parent, String tag)
		{
			foreach (XmlNode c in parent.ChildNodes)
			{
				if (c is XmlElement e && e.LocalName == tag)
					return XmlHelpers.AttrDouble(e, "Multiplier", 1.0);
			}
			return 1.0;
		}
	}
}
