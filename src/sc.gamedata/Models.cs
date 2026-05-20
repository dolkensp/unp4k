using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sc.gamedata
{
	// ── Output records ────────────────────────────────────────────────
	// All field names match what battlestations expects in game_data.json.
	// Lower-case underscored names are intentional (matches the existing
	// LIVE-channel game_data.json shape produced by zandbox + enrich pipeline).

	internal sealed class DamageProfile
	{
		public Double phys { get; set; }
		public Double energy { get; set; }
		public Double dist { get; set; }
		public Double therm { get; set; }
		public Double bio { get; set; }
		public Double stun { get; set; }
	}

	internal sealed class ResistanceProfile
	{
		public Double phys { get; set; } = 1.0;
		public Double energy { get; set; } = 1.0;
		public Double dist { get; set; } = 1.0;
		public Double therm { get; set; } = 1.0;
		public Double bio { get; set; } = 1.0;
		public Double stun { get; set; } = 1.0;
	}

	internal sealed class WeaponRecord
	{
		public String id { get; set; } = "";
		public String name { get; set; } = "";
		public Int32 size { get; set; }
		public String kind { get; set; } = "gun";  // "gun" | "missile"
		public DamageProfile damage { get; set; } = new();

		// Gun-only enrichment fields (null for missiles).
		// pellet_count: projectiles per trigger pull. 1 for normal weapons,
		// >1 for scatterguns. Sourced from
		// SCItemWeaponComponentParams/fireActions/SWeaponActionFire*Params/launchParams/SProjectileLauncher@pelletCount.
		// Battlestations multiplies per-pellet damage / DPS by this when
		// classifying penetration vs deflection threshold.
		public Int32? pellet_count { get; set; }
		public Double? rate_of_fire { get; set; }
		public Double? projectile_velocity { get; set; }
		public Double? projectile_lifetime { get; set; }
		public Double? range { get; set; }
		public Double? alpha_damage { get; set; }
		public Double? alpha_phys { get; set; }
		public Double? alpha_energy { get; set; }
		public Double? alpha_dist { get; set; }
		public Double? dps_burst { get; set; }
		public Int32? magazine_capacity { get; set; }
		public Double? base_penetration_distance { get; set; }
		public Double? heat_capacity { get; set; }
		public Double? heat_per_shot { get; set; }
		public Double? cooling_delay { get; set; }
		public Double? cooling_per_second { get; set; }
		public Double? overheat_fix_time { get; set; }
		public Int32? shots_to_overheat { get; set; }
		public Double? time_to_overheat { get; set; }

		// Missile-only fields.
		public Double? lock_time { get; set; }
		public Double? lock_range_max { get; set; }
		public Double? health { get; set; }
		public String? missile_subtype { get; set; }

		// Internal — used to resolve weapon refs by GUID during vehicle pass,
		// stripped before JSON serialization via [JsonIgnore].
		[JsonIgnore]
		public String? _guid { get; set; }
	}

	internal sealed class ArmorRecord
	{
		public String id { get; set; } = "";
		public String name { get; set; } = "";
		public DamageProfile deflection { get; set; } = new();
		public DamageProfile multiplier { get; set; } = new();
		public ResistanceProfile resistance { get; set; } = new();

		// Hull HP from SHealthComponentParams/@Health on the armor item.
		// Surfaces as vehicle.hull_hp after VehicleExtractor copies it.
		public Double? hull_hp { get; set; }

		[JsonIgnore]
		public String? _guid { get; set; }
	}

	// ── Per-ship physics shapes (output JSON) ─────────────────────────
	// Match the existing physics_overlay.json field set so battlestations
	// keeps reading the same shape. Note the CIG-style inconsistency:
	// thrust_capacity uses "maneuvering" but acceleration uses "maneuver" —
	// preserved from the legacy overlay format.

	internal sealed class AgilityProfile
	{
		public Double pitch { get; set; }
		public Double yaw { get; set; }
		public Double roll { get; set; }
		public Double pitch_boosted { get; set; }
		public Double yaw_boosted { get; set; }
		public Double roll_boosted { get; set; }
	}

	internal sealed class AccelerationProfile
	{
		public Double main { get; set; }
		public Double retro { get; set; }
		public Double vtol { get; set; }
		public Double maneuver { get; set; }
		public Double main_boosted { get; set; }
		public Double maneuver_boosted { get; set; }
	}

	internal sealed class ThrustCapacity
	{
		public Double main { get; set; }
		public Double retro { get; set; }
		public Double vtol { get; set; }
		public Double maneuvering { get; set; }
	}

	internal sealed class CrossSection
	{
		public Double x { get; set; }
		public Double y { get; set; }
		public Double z { get; set; }
	}

	// ── Internal-only records used during vehicle pass ────────────────
	// Not serialized to game_data.json. Index by id + by GUID, like weapons
	// and armor, so VehicleExtractor can resolve loadout refs cheaply.

	internal sealed class IfcsRecord
	{
		public String id { get; set; } = "";
		public Double scm_speed { get; set; }
		public Double boost_speed_forward { get; set; }
		public Double max_speed { get; set; }      // CIG's "max" = the nav/quantum cap
		// Per-axis angular velocity (deg/s). x=pitch, y=yaw, z=roll.
		public Double angular_velocity_x { get; set; }
		public Double angular_velocity_y { get; set; }
		public Double angular_velocity_z { get; set; }
		// Afterburn multipliers applied to the baseline angular velocity for
		// the boosted variants. Per-axis.
		public Double afterburn_ang_velocity_mult_x { get; set; } = 1.0;
		public Double afterburn_ang_velocity_mult_y { get; set; } = 1.0;
		public Double afterburn_ang_velocity_mult_z { get; set; } = 1.0;
		// Forward (positive y) linear afterburn multiplier — boosts main acceleration.
		public Double afterburn_lin_accel_mult_forward { get; set; } = 1.0;
		// Base mass of the IFCS item itself (contributes to mass_loadout).
		public Double mass { get; set; }

		public String? _guid { get; set; }
	}

	internal sealed class ThrusterRecord
	{
		public String id { get; set; } = "";
		public Double thrust_capacity { get; set; }
		// "Main" | "Maneuver" | "Retro" — direct from SCItemThrusterParams/@thrusterType.
		public String thruster_type { get; set; } = "";
		// When true, this thruster only fires in atmospheric VTOL mode; it
		// counts toward the VTOL bucket regardless of thruster_type (which
		// is typically "Maneuver" for VTOL units).
		public Boolean only_active_in_vtol { get; set; }
		public Double mass { get; set; }

		public String? _guid { get; set; }
	}

	internal sealed class SlotRecord
	{
		public String label { get; set; } = "";
		public Int32 size { get; set; }
		public String kind { get; set; } = "gun";  // "gun" | "missile" | "missile_rack"
		public String? stock_weapon_id { get; set; }

		// Missile-rack fields (only when kind == "missile_rack").
		public Int32? min_size { get; set; }
		public Int32? max_size { get; set; }
		public String? stock_rack_id { get; set; }
		public List<String?>? stock_missile_ids { get; set; }
	}

	internal sealed class VehicleRecord
	{
		public String id { get; set; } = "";
		public String name { get; set; } = "";
		public String? armor_id { get; set; }
		public List<SlotRecord> slots { get; set; } = new();

		// Copied from the resolved armor record. Optional because some
		// vehicles legitimately have no armor entry (test rigs, racing snubs).
		public DamageProfile? deflection { get; set; }
		public DamageProfile? multiplier { get; set; }
		public ResistanceProfile? resistance { get; set; }

		// Bounding-box dimensions (from VehicleComponentParams/maxBoundingBoxSize).
		public Double? length { get; set; }
		public Double? width { get; set; }
		public Double? height { get; set; }

		// Crew + role metadata (loc-resolved where possible).
		public Int32? crew_size { get; set; }
		public String? career { get; set; }
		public String? role { get; set; }

		// Physics fields. Populated by PhysicsAggregator (from IFCS / thrusters
		// / armor / vehicle XML) during VehicleExtractor.Extract. Nullable so
		// OverlayApplier can backfill anything extraction couldn't resolve.
		public Int32? size_class { get; set; }
		public Double? scm_speed { get; set; }
		public Double? boost_speed { get; set; }
		public Double? nav_speed { get; set; }
		public Double? mass { get; set; }
		public Double? mass_loadout { get; set; }
		public Double? mass_total { get; set; }
		public Double? hull_hp { get; set; }
		public AgilityProfile? agility { get; set; }
		public AccelerationProfile? acceleration { get; set; }
		public ThrustCapacity? thrust_capacity { get; set; }
		public CrossSection? cross_section { get; set; }
	}

	internal sealed class GameDataOutput
	{
		public Int32 schema_version { get; set; }
		public String generated_at { get; set; } = "";
		public String? channel { get; set; }
		public Int32 dcb_version { get; set; }
		public List<WeaponRecord> weapons { get; set; } = new();
		public List<ArmorRecord> ships { get; set; } = new();
		public List<VehicleRecord> vehicles { get; set; } = new();
		public List<RackRecord> racks { get; set; } = new();
	}
}
