using System;
using System.Collections.Generic;
using System.Xml;
using unforge;

namespace sc.gamedata
{
	// Sweeps the per-ship IFCS controller items from DataForge so VehicleExtractor
	// can resolve a ship's loadout reference (port `hardpoint_controller_flight`,
	// item class `Controller_Flight_{Ship_Id}`) to a typed IfcsRecord without
	// re-reading XML during the vehicle pass.
	//
	// Each ship has its own controller — CIG bakes scm_speed / boost_speed /
	// max_speed / per-axis angular velocity into the controller item, not the
	// ship entity. The IfcsRecord captures what PhysicsAggregator needs to
	// populate VehicleRecord.{scm_speed, boost_speed, nav_speed, agility}.
	internal static class IfcsExtractor
	{
		private const String PathPrefix = "libs/foundry/records/entities/scitem/ships/controller/";

		public static List<IfcsRecord> Extract(DataForge df)
		{
			var result = new List<IfcsRecord>();
			foreach (var path in df.PathToRecordMap.Keys)
			{
				if (!path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)) continue;
				// Direct children only — skip any nested subfolders if they appear.
				var rest = path.Substring(PathPrefix.Length);
				if (rest.Contains('/')) continue;

				var root = df.ReadRecordByPathAsXml(path);
				if (root == null) continue;
				var entityId = XmlHelpers.EntityIdFromRoot(root);
				var guid = XmlHelpers.Attr(root, "__ref");

				var ifcs = XmlNav.FindFirst(root, "IFCSParams");
				if (ifcs == null) continue;

				var record = new IfcsRecord
				{
					id = entityId,
					_guid = guid,
					scm_speed = XmlHelpers.AttrDouble(ifcs, "scmSpeed", 0),
					boost_speed_forward = XmlHelpers.AttrDouble(ifcs, "boostSpeedForward", 0),
					max_speed = XmlHelpers.AttrDouble(ifcs, "maxSpeed", 0),
				};

				// Per-axis angular velocity lives at
				// IFCSParams/speedProfile/angularVelocity @x@y@z (deg/s).
				var speedProfile = XmlNav.FindFirst(ifcs, "speedProfile");
				if (speedProfile != null)
				{
					var av = XmlNav.FindFirst(speedProfile, "angularVelocity");
					if (av != null)
					{
						record.angular_velocity_x = XmlHelpers.AttrDouble(av, "x", 0);
						record.angular_velocity_y = XmlHelpers.AttrDouble(av, "y", 0);
						record.angular_velocity_z = XmlHelpers.AttrDouble(av, "z", 0);
					}
				}

				// Afterburn (boost) per-axis multipliers live in afterburnerNew,
				// added in 4.x. Older ships may still have the legacy
				// `afterburner` block; we prefer New and fall back if missing.
				var afterburnerNew = XmlNav.FindFirst(ifcs, "afterburnerNew");
				if (afterburnerNew != null)
				{
					var angMult = XmlNav.FindFirst(afterburnerNew, "afterburnAngVelocityMultiplier");
					if (angMult != null)
					{
						record.afterburn_ang_velocity_mult_x = XmlHelpers.AttrDouble(angMult, "x", 1.0);
						record.afterburn_ang_velocity_mult_y = XmlHelpers.AttrDouble(angMult, "y", 1.0);
						record.afterburn_ang_velocity_mult_z = XmlHelpers.AttrDouble(angMult, "z", 1.0);
					}
					var linAccel = XmlNav.FindFirst(afterburnerNew, "afterburnAccelMultiplierPositive");
					if (linAccel != null)
					{
						// Y is the forward axis in CIG's local frame; use that as
						// the canonical "main_boosted" multiplier.
						record.afterburn_lin_accel_mult_forward = XmlHelpers.AttrDouble(linAccel, "y", 1.0);
					}
				}

				// IFCS item's own mass (contributes to mass_loadout). Found on
				// SAttachableComponentParams/AttachDef@Mass. Falls back to 0
				// when missing — the IFCS is usually light enough that 0 is
				// a fine default if CIG omits it.
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
