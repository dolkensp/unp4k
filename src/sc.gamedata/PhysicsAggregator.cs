using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace sc.gamedata
{
	// Pure aggregation: combines a vehicle's CryEngine entity XML, its
	// resolved IFCS controller, its resolved thrusters, and its armor record
	// into the 12 physics fields on VehicleRecord. No I/O — easy to unit-test.
	//
	// Field provenance:
	//   size_class        ← Vehicle@size on entity root
	//   cross_section     ← VehicleComponentParams/maxBoundingBoxSize @x@y@z
	//   scm_speed         ← IFCS scmSpeed
	//   boost_speed       ← IFCS boostSpeedForward
	//   nav_speed         ← IFCS maxSpeed (CIG's absolute cap = nav/quantum)
	//   agility.*         ← IFCS speedProfile/angularVelocity × afterburn mults
	//   mass              ← sum of Vehicle/Parts/Part@mass at root
	//   mass_loadout      ← sum of item masses across resolved loadout
	//   mass_total        ← mass + mass_loadout
	//   hull_hp           ← armor.hull_hp
	//   thrust_capacity   ← bucketed sum of thruster.thrust_capacity
	//   acceleration      ← thrust_capacity / mass_total per direction
	//
	// All fields null-safe — extraction failures leave the value null so
	// OverlayApplier can backfill from a known-good baseline.
	internal static class PhysicsAggregator
	{
		// Bucketed running sums for thrust_capacity, populated as we walk the
		// resolved-loadout list. Local struct so the categorization rule lives
		// next to the iteration.
		private struct ThrustBuckets
		{
			public Double Main;
			public Double Retro;
			public Double Vtol;
			public Double Maneuvering;
		}

		// Apply every available physics field to `v`. Each section is guarded;
		// missing inputs leave the corresponding fields null.
		public static void Aggregate(
			VehicleRecord v,
			XmlElement vehicleRoot,
			IfcsRecord? ifcs,
			IReadOnlyList<ThrusterRecord> thrusters,
			ArmorRecord? armor)
		{
			// size_class — first AttachDef@Size on the ship record. This is the
			// SAttachableComponentParams/AttachDef under the spaceship's own
			// Components block (each item port has its own AttachDef, but the
			// ship-level one appears first in document order).
			var shipAttach = XmlNav.FindFirst(vehicleRoot, "AttachDef");
			if (shipAttach != null)
			{
				var sizeAttr = XmlHelpers.Attr(shipAttach, "Size");
				if (Int32.TryParse(sizeAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeClass))
					v.size_class = sizeClass;
			}

			// cross_section — VehicleComponentParams/maxBoundingBoxSize.
			var vcp = XmlNav.FindFirst(vehicleRoot, "VehicleComponentParams");
			if (vcp != null)
			{
				var mbb = XmlNav.FindFirst(vcp, "maxBoundingBoxSize");
				if (mbb != null)
				{
					v.cross_section = new CrossSection
					{
						x = XmlHelpers.AttrDouble(mbb, "x", 0),
						y = XmlHelpers.AttrDouble(mbb, "y", 0),
						z = XmlHelpers.AttrDouble(mbb, "z", 0),
					};
				}
			}

			// Structural mass — sum of <Part mass="…"> children of <Parts>.
			// CryEngine vehicle entities define parts as a flat list under
			// <Parts>; physics-relevant parts (hull body, wings, etc.) carry
			// a mass attribute. Most ships have just one mass-bearing root
			// part; some split mass across a few. Recursive walk handles
			// either layout. Mass unit is kg (matches the existing overlay).
			var partsRoot = XmlNav.FindFirst(vehicleRoot, "Parts");
			Double structuralMass = 0;
			if (partsRoot != null)
			{
				foreach (XmlNode node in partsRoot.GetElementsByTagName("Part"))
				{
					if (node is not XmlElement part) continue;
					var m = XmlHelpers.AttrDouble(part, "mass", 0);
					if (m > 0) structuralMass += m;
				}
			}
			if (structuralMass > 0) v.mass = structuralMass;

			// IFCS-sourced speed + agility fields. IFCS is the single source of
			// truth for SCM / boost / nav speeds and per-axis rotation rates.
			if (ifcs != null)
			{
				if (ifcs.scm_speed > 0) v.scm_speed = ifcs.scm_speed;
				if (ifcs.boost_speed_forward > 0) v.boost_speed = ifcs.boost_speed_forward;
				if (ifcs.max_speed > 0) v.nav_speed = ifcs.max_speed;

				v.agility = new AgilityProfile
				{
					pitch = ifcs.angular_velocity_x,
					yaw = ifcs.angular_velocity_y,
					roll = ifcs.angular_velocity_z,
					pitch_boosted = ifcs.angular_velocity_x * ifcs.afterburn_ang_velocity_mult_x,
					yaw_boosted = ifcs.angular_velocity_y * ifcs.afterburn_ang_velocity_mult_y,
					roll_boosted = ifcs.angular_velocity_z * ifcs.afterburn_ang_velocity_mult_z,
				};
			}

			// Thrust capacity — sum thrusters by category. CIG encodes type as
			// a string ("Main" / "Maneuver" / "Retro"); VTOL units are
			// flagged via onlyActiveInVTOL=1 on otherwise-Maneuver thrusters,
			// so that flag wins when categorizing.
			var buckets = new ThrustBuckets();
			Double thrusterMass = 0;
			foreach (var t in thrusters)
			{
				thrusterMass += t.mass;
				if (t.only_active_in_vtol)
				{
					buckets.Vtol += t.thrust_capacity;
					continue;
				}
				switch (t.thruster_type)
				{
					case "Main":     buckets.Main += t.thrust_capacity; break;
					case "Retro":    buckets.Retro += t.thrust_capacity; break;
					case "Maneuver": buckets.Maneuvering += t.thrust_capacity; break;
					// Unknown types silently skip — keeps the build resilient
					// to new CIG categories landing in a future patch.
				}
			}
			// Always emit thrust_capacity even if all zero — the LPA wants the
			// field present so consumers can rely on `.main` etc. existing.
			v.thrust_capacity = new ThrustCapacity
			{
				main = buckets.Main,
				retro = buckets.Retro,
				vtol = buckets.Vtol,
				maneuvering = buckets.Maneuvering,
			};

			// Loadout mass — sum of ifcs + thrusters. v1 omits weapon and
			// armor mass (those models don't yet track Mass attrs); follow-up
			// can add them. Most weapons / armor weigh tiny vs the thruster
			// totals on capital ships, so undercount is small.
			Double loadoutMass = thrusterMass + (ifcs?.mass ?? 0);
			if (loadoutMass > 0) v.mass_loadout = loadoutMass;
			if (v.mass != null || v.mass_loadout != null)
				v.mass_total = (v.mass ?? 0) + (v.mass_loadout ?? 0);

			// Acceleration = thrust / mass_total per direction. Only meaningful
			// when we have both a thrust bucket AND a non-zero total mass.
			if (v.mass_total > 0 && v.thrust_capacity != null)
			{
				var totalMass = v.mass_total.Value;
				var mainBoostMult = ifcs?.afterburn_lin_accel_mult_forward ?? 1.0;
				// v1: use the forward-axis afterburn multiplier for both main
				// and maneuver boosted variants. Per-axis maneuver boost
				// would require afterburnAccelMultiplierPositive's x/z values
				// — left to v1.1 since the overlay's existing maneuver_boosted
				// uses a single ratio anyway.
				v.acceleration = new AccelerationProfile
				{
					main = buckets.Main / totalMass,
					retro = buckets.Retro / totalMass,
					vtol = buckets.Vtol / totalMass,
					maneuver = buckets.Maneuvering / totalMass,
					main_boosted = (buckets.Main / totalMass) * mainBoostMult,
					maneuver_boosted = (buckets.Maneuvering / totalMass) * mainBoostMult,
				};
			}

			// hull_hp — directly from the resolved armor item.
			if (armor?.hull_hp is { } hp && hp > 0) v.hull_hp = hp;
		}
	}
}
