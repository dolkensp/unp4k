using System;
using System.Collections.Generic;
using System.Xml;
using unforge;

namespace sc.gamedata
{
	// Sweeps per-ship thruster items from DataForge so VehicleExtractor can
	// resolve loadout refs (ports like `hardpoint_thruster_main_*`,
	// `hardpoint_mav_*`, `hardpoint_retro_*`, `hardpoint_vtol_*`) to typed
	// ThrusterRecords without re-reading XML during the vehicle pass.
	//
	// Each thruster carries its own thrustCapacity (Newtons) and a thrusterType
	// string ("Main" | "Maneuver" | "Retro"). VTOL units are encoded as
	// onlyActiveInVTOL=1 on otherwise-Maneuver thrusters; PhysicsAggregator
	// uses that flag to redirect them into the VTOL bucket.
	internal static class ThrusterExtractor
	{
		private const String PathPrefix = "libs/foundry/records/entities/scitem/ships/thrusters/";

		public static List<ThrusterRecord> Extract(DataForge df)
		{
			var result = new List<ThrusterRecord>();
			foreach (var path in df.PathToRecordMap.Keys)
			{
				if (!path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)) continue;
				// Direct children only — skip nested subfolders just in case.
				var rest = path.Substring(PathPrefix.Length);
				if (rest.Contains('/')) continue;

				var root = df.ReadRecordByPathAsXml(path);
				if (root == null) continue;
				var entityId = XmlHelpers.EntityIdFromRoot(root);
				var guid = XmlHelpers.Attr(root, "__ref");

				var thruster = XmlNav.FindFirst(root, "SCItemThrusterParams");
				if (thruster == null) continue;

				var record = new ThrusterRecord
				{
					id = entityId,
					_guid = guid,
					thrust_capacity = XmlHelpers.AttrDouble(thruster, "thrustCapacity", 0),
					thruster_type = XmlHelpers.Attr(thruster, "thrusterType") ?? "",
					only_active_in_vtol = XmlHelpers.AttrDouble(thruster, "onlyActiveInVTOL", 0) > 0.5,
				};

				// Base mass from SAttachableComponentParams/AttachDef@Mass.
				// Used in mass_loadout aggregation.
				var attach = XmlNav.FindFirst(root, "AttachDef");
				if (attach != null)
				{
					record.mass = XmlHelpers.AttrDouble(attach, "Mass", 0);
				}

				result.Add(record);
			}
			return result;
		}
	}
}
