using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace sc.gamedata
{
	// Copies CryXML-physics fields (size_class, mass, agility, thrust_capacity,
	// scm_speed, etc.) from a previously-built LIVE game_data.json onto the
	// fresh PTU vehicle records by id. The DataForge tree alone doesn't
	// contain these — they're in per-ship `.xml` files inside the .p4k
	// (CryEngine entity definitions, not DataForge records). Until that
	// extraction is wired up, an overlay from a recent LIVE build is the
	// pragmatic source. Hull dimensions don't change between LIVE → PTU for
	// the same id, so the values stay accurate for existing ships.
	internal static class OverlayApplier
	{
		// Fields that ship physics knows but our DataForge pass leaves null.
		// Anything already populated on the PTU record (length/width/height/
		// crew_size/career/role/armor profile) is left alone.
		private static readonly String[] FieldsToCopy = new[]
		{
			"size_class",
			"scm_speed",
			"boost_speed",
			"nav_speed",
			"mass",
			"mass_loadout",
			"mass_total",
			"hull_hp",
			"agility",
			"acceleration",
			"thrust_capacity",
			"cross_section",
		};

		public static Int32 ApplyFromLive(String overlayPath, List<VehicleRecord> vehicles)
		{
			if (!File.Exists(overlayPath))
			{
				Console.WriteLine($"WARN: overlay not found at {overlayPath} — skipping.");
				return 0;
			}
			using var doc = JsonDocument.Parse(File.ReadAllText(overlayPath));
			if (!doc.RootElement.TryGetProperty("vehicles", out var liveVehicles)
				|| liveVehicles.ValueKind != JsonValueKind.Array) return 0;

			var byId = new Dictionary<String, JsonElement>(StringComparer.Ordinal);
			foreach (var v in liveVehicles.EnumerateArray())
			{
				if (v.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
					byId[idEl.GetString()!] = v;
			}

			var updated = 0;
			var fallbackFieldFills = 0;
			foreach (var v in vehicles)
			{
				if (!byId.TryGetValue(v.id, out var live)) continue;
				var touched = false;
				foreach (var field in FieldsToCopy)
				{
					if (!live.TryGetProperty(field, out var val) || val.ValueKind == JsonValueKind.Null) continue;
					// Skip-when-populated: physics extraction takes precedence.
					// Only fill from overlay if the fresh extraction left the
					// field null. Stops a stale overlay from silently clobbering
					// freshly-extracted values.
					if (IsAlreadyPopulated(v, field)) continue;
					if (CopyField(v, field, val))
					{
						touched = true;
						fallbackFieldFills++;
					}
				}
				if (touched) updated++;
			}
			if (fallbackFieldFills > 0)
			{
				Console.WriteLine($"WARN: overlay backfilled {fallbackFieldFills} field(s) across {updated} vehicle(s). "
					+ "Extraction couldn't resolve these — investigate per-ship coverage.");
			}
			return updated;
		}

		// True when the fresh extraction has already populated this field on v.
		// Used to make overlay strictly a fallback, never a clobber.
		private static Boolean IsAlreadyPopulated(VehicleRecord v, String field) => field switch
		{
			"size_class"     => v.size_class != null,
			"scm_speed"      => v.scm_speed != null,
			"boost_speed"    => v.boost_speed != null,
			"nav_speed"      => v.nav_speed != null,
			"mass"           => v.mass != null,
			"mass_loadout"   => v.mass_loadout != null,
			"mass_total"     => v.mass_total != null,
			"hull_hp"        => v.hull_hp != null,
			"agility"        => v.agility != null,
			"acceleration"   => v.acceleration != null,
			"thrust_capacity" => v.thrust_capacity != null,
			"cross_section"  => v.cross_section != null,
			_                => false,
		};

		// Reflection-style copy. Only the explicit field set above is touched;
		// any unknown field is silently skipped so an old overlay file with
		// extras doesn't poison PTU output.
		private static Boolean CopyField(VehicleRecord v, String field, JsonElement val)
		{
			switch (field)
			{
				case "size_class":
					if (val.ValueKind == JsonValueKind.Number) { v.size_class = val.GetInt32(); return true; }
					return false;
				case "scm_speed": v.scm_speed = AsDouble(val); return v.scm_speed != null;
				case "boost_speed": v.boost_speed = AsDouble(val); return v.boost_speed != null;
				case "nav_speed": v.nav_speed = AsDouble(val); return v.nav_speed != null;
				case "mass": v.mass = AsDouble(val); return v.mass != null;
				case "mass_loadout": v.mass_loadout = AsDouble(val); return v.mass_loadout != null;
				case "mass_total": v.mass_total = AsDouble(val); return v.mass_total != null;
				case "hull_hp": v.hull_hp = AsDouble(val); return v.hull_hp != null;
				case "agility":          v.agility          = Deserialize<AgilityProfile>(val);     return v.agility != null;
				case "acceleration":     v.acceleration     = Deserialize<AccelerationProfile>(val); return v.acceleration != null;
				case "thrust_capacity":  v.thrust_capacity  = Deserialize<ThrustCapacity>(val);     return v.thrust_capacity != null;
				case "cross_section":    v.cross_section    = Deserialize<CrossSection>(val);       return v.cross_section != null;
			}
			return false;
		}

		private static Double? AsDouble(JsonElement v)
		{
			if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
			return null;
		}

		// Deserialize a JsonElement into a typed physics shape. Falls back to
		// null on any malformed object so a bad overlay can't crash the build.
		private static T? Deserialize<T>(JsonElement v) where T : class
		{
			if (v.ValueKind != JsonValueKind.Object) return null;
			try
			{
				return JsonSerializer.Deserialize<T>(v.GetRawText());
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
