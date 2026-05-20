using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using unforge;

namespace sc.gamedata
{
	// CLI entry point. Reads a Star Citizen Data.p4k (or pre-extracted Game2.dcb)
	// and emits a battlestations-shaped game_data.json built from the unforge
	// library's DataForge.ReadRecordByPathAsXml() — no disk-XML round-trip,
	// because writing ~30k tiny files is the bottleneck unforge.cli pays today.
	internal static class Program
	{
		// Same Star Citizen AES key that unp4k.exe uses (see src/unp4k/Program.cs).
		// Kept in sync intentionally — if CIG ever rotates this, all three tools
		// (unp4k, unp4k.fs, sc.gamedata) need the same update.
		private static readonly Byte[] StarCitizenAesKey = new Byte[]
		{
			0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A,
			0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47
		};

		private static int Main(String[] args)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

			var opts = CliOptions.Parse(args);
			if (opts == null) return 1;

			using var dcbStream = OpenDcbStream(opts);
			if (dcbStream == null) return 1;

			var ctorSw = System.Diagnostics.Stopwatch.StartNew();
			var df = new DataForge(dcbStream);
			ctorSw.Stop();
			Console.WriteLine($"DataForge ready ({ctorSw.Elapsed.TotalSeconds:F2}s, {df.PathToRecordMap.Count} records)");

			var loc = LoadLocalization(opts.BaseIniPath);
			Console.WriteLine($"Localization keys: {loc.Count}");

			// Pass 1: ammo records → guid → damage profile (used by guns).
			var extractSw = System.Diagnostics.Stopwatch.StartNew();
			var ammoMap = AmmoExtractor.Extract(df);
			Console.WriteLine($"Ammo records: {ammoMap.Count}");

			// Pass 2: gun + missile entities. Each carries its own attach
			// metadata (size, manufacturer, port type) but pulls damage from
			// the ammoMap by GUID for guns, or from explosion params inline
			// for missiles.
			var (guns, missiles) = WeaponExtractor.Extract(df, loc, ammoMap);
			Console.WriteLine($"Weapons: {guns.Count + missiles.Count} ({guns.Count} guns + {missiles.Count} missiles)");

			// Pass 3: armor entries. Indexed both by id and by GUID so the
			// vehicle pass can resolve `hardpoint_armor` references either
			// way (Carrack and similar reference armor by GUID, most others
			// by entityClassName).
			var armors = ArmorExtractor.Extract(df, loc);
			Console.WriteLine($"Armor records: {armors.Count}");

			// Pass 4a + 4b: per-ship IFCS controllers + thrusters. Each ship
			// references one controller (hardpoint_controller_flight) and
			// multiple thrusters in its loadout; both carry physics fields
			// (scm_speed, agility, thrust_capacity) that the DataForge tree
			// alone doesn't surface on the spaceship record.
			var ifcsRecords = IfcsExtractor.Extract(df);
			Console.WriteLine($"IFCS controllers: {ifcsRecords.Count}");
			var thrusterRecords = ThrusterExtractor.Extract(df);
			Console.WriteLine($"Thrusters: {thrusterRecords.Count}");

			// Pass 5: vehicles. Walks each ship's default loadout, looks up
			// weapons by GUID/className, collects size_class hints from CIG's
			// port-name 'classN' tokens, and aggregates per-ship physics from
			// the resolved IFCS + thruster + armor items via PhysicsAggregator.
			var weaponByGuid = guns.Concat(missiles).Where(w => w._guid != null)
				.ToDictionary(w => w._guid!, w => w);
			var weaponById = guns.Concat(missiles).ToDictionary(w => w.id, w => w);
			var armorById = armors.ToDictionary(a => a.id, a => a);
			var armorByGuid = armors.Where(a => a._guid != null)
				.ToDictionary(a => a._guid!, a => a);
			var ifcsById = ifcsRecords.ToDictionary(i => i.id, i => i);
			var ifcsByGuid = ifcsRecords.Where(i => i._guid != null)
				.ToDictionary(i => i._guid!, i => i);
			var thrusterById = thrusterRecords.ToDictionary(t => t.id, t => t);
			var thrusterByGuid = thrusterRecords.Where(t => t._guid != null)
				.ToDictionary(t => t._guid!, t => t);

			var vehicles = VehicleExtractor.Extract(df, loc,
				weaponByGuid, weaponById,
				armorById, armorByGuid,
				ifcsById, ifcsByGuid,
				thrusterById, thrusterByGuid);
			Console.WriteLine($"Vehicles: {vehicles.Count} ({vehicles.Count(v => v.slots.Count > 0)} with weapon hardpoints, "
				+ $"{vehicles.Count(v => v.size_class != null)} with physics extracted)");

			var racks = RackExtractor.Extract(df, loc);
			Console.WriteLine($"Missile racks: {racks.Count}");

			extractSw.Stop();
			Console.WriteLine($"Extraction: {extractSw.Elapsed.TotalSeconds:F2}s");

			// Optional overlay: copy fields like size_class / mass / agility /
			// thrust_capacity from a previously-built game_data.json onto the
			// matching vehicle ids. These derive from per-ship physics XMLs
			// outside the DataForge tree, so PTU output stays sparse on those
			// axes unless overlaid from a recent LIVE build.
			if (!String.IsNullOrEmpty(opts.OverlayPath))
			{
				var overlayed = OverlayApplier.ApplyFromLive(opts.OverlayPath!, vehicles);
				Console.WriteLine($"Overlay applied: {overlayed} vehicles updated from {opts.OverlayPath}");
			}

			var output = new GameDataOutput
			{
				schema_version = 1,
				generated_at = DateTime.UtcNow.ToString("o"),
				channel = opts.Channel,
				dcb_version = df.FileVersion,
				weapons = guns.Concat(missiles).OrderBy(w => w.size).ThenBy(w => w.name).ToList(),
				ships = armors.OrderBy(a => a.name).ToList(),
				vehicles = vehicles.OrderBy(v => v.name).ToList(),
				racks = racks,
			};

			var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
			});

			Directory.CreateDirectory(Path.GetDirectoryName(opts.OutputPath)!);
			File.WriteAllText(opts.OutputPath, json + Environment.NewLine, new UTF8Encoding(false));
			Console.WriteLine($"Wrote {opts.OutputPath} ({new FileInfo(opts.OutputPath).Length / 1024.0:F1} KiB)");
			return 0;
		}

		// Resolves --p4k or --dcb into a seekable stream over Game2.dcb. For
		// .p4k inputs we open the archive via SharpZipLib (which has the SC
		// AES decryptor wired in) and copy the first .dcb entry into memory —
		// MemoryStream is far cheaper than the temp-disk dance unforge.cli
		// does, and DataForge needs random seeks anyway so we need it in RAM
		// regardless.
		private static Stream? OpenDcbStream(CliOptions opts)
		{
			if (opts.DcbPath != null)
			{
				if (!File.Exists(opts.DcbPath)) { Console.Error.WriteLine($"DCB not found: {opts.DcbPath}"); return null; }
				return File.OpenRead(opts.DcbPath);
			}
			if (opts.P4kPath != null)
			{
				if (!File.Exists(opts.P4kPath)) { Console.Error.WriteLine($"P4K not found: {opts.P4kPath}"); return null; }
				Console.WriteLine($"Streaming Game2.dcb out of {opts.P4kPath}…");
				var sw = System.Diagnostics.Stopwatch.StartNew();
				using var pakStream = File.OpenRead(opts.P4kPath);
				var pak = new ZipFile(pakStream) { Key = StarCitizenAesKey };
				ZipEntry? dcbEntry = null;
				foreach (ZipEntry entry in pak)
				{
					if (entry.Name.EndsWith(".dcb", StringComparison.OrdinalIgnoreCase))
					{
						dcbEntry = entry;
						break;
					}
				}
				if (dcbEntry == null) { Console.Error.WriteLine("No .dcb found in p4k"); return null; }
				var ms = new MemoryStream((Int32)Math.Min(dcbEntry.Size, Int32.MaxValue));
				using (var input = pak.GetInputStream(dcbEntry))
				{
					input.CopyTo(ms);
				}
				ms.Position = 0;
				sw.Stop();
				Console.WriteLine($"Loaded {dcbEntry.Name} ({ms.Length / (1024.0 * 1024.0):F1} MB) in {sw.Elapsed.TotalSeconds:F2}s");
				return ms;
			}
			Console.Error.WriteLine("Either --p4k or --dcb is required.");
			return null;
		}

		// Reads CIG's localization INI (one `key=value` pair per line, UTF-8
		// with BOM). Used to resolve @item_NameXXX / @vehicle_NameXXX loc keys
		// the entity XMLs reference.
		private static Dictionary<String, String> LoadLocalization(String? baseIniPath)
		{
			var loc = new Dictionary<String, String>(StringComparer.Ordinal);
			if (String.IsNullOrEmpty(baseIniPath)) return loc;
			if (!File.Exists(baseIniPath))
			{
				Console.WriteLine($"WARN: base.ini not found at {baseIniPath} — names will fall back to prettified entity ids.");
				return loc;
			}
			foreach (var raw in File.ReadAllLines(baseIniPath, Encoding.UTF8))
			{
				var line = raw.TrimEnd('\r', '\n');
				var eq = line.IndexOf('=');
				if (eq <= 0) continue;
				loc[line.Substring(0, eq)] = line.Substring(eq + 1);
			}
			return loc;
		}
	}

	internal sealed class CliOptions
	{
		public String? P4kPath;
		public String? DcbPath;
		public String? BaseIniPath;
		public String OutputPath = "game_data.json";
		public String? OverlayPath;
		public String? Channel;

		public static CliOptions? Parse(String[] args)
		{
			if (args.Length == 0 || args.Any(a => a is "-h" or "--help"))
			{
				PrintUsage();
				return null;
			}
			var o = new CliOptions();
			for (var i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--p4k": o.P4kPath = args[++i]; break;
					case "--dcb": o.DcbPath = args[++i]; break;
					case "--base-ini": o.BaseIniPath = args[++i]; break;
					case "--output": o.OutputPath = args[++i]; break;
					case "--overlay": o.OverlayPath = args[++i]; break;
					case "--channel": o.Channel = args[++i]; break;
					default:
						Console.Error.WriteLine($"Unknown arg: {args[i]}");
						PrintUsage();
						return null;
				}
			}
			if (o.P4kPath == null && o.DcbPath == null)
			{
				Console.Error.WriteLine("Either --p4k or --dcb is required.");
				PrintUsage();
				return null;
			}
			return o;
		}

		private static void PrintUsage()
		{
			Console.Error.WriteLine("Usage: sc.gamedata --p4k <Data.p4k> | --dcb <Game2.dcb>");
			Console.Error.WriteLine("                   [--base-ini <base.ini>]");
			Console.Error.WriteLine("                   [--output <game_data.json>]");
			Console.Error.WriteLine("                   [--overlay <previous-game_data.json>]");
			Console.Error.WriteLine("                   [--channel LIVE|PTU|EPTU]");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Builds a battlestations-shaped game_data.json from CIG's DataForge");
			Console.Error.WriteLine("database. With --p4k, Game2.dcb is streamed out of the archive in");
			Console.Error.WriteLine("memory (no temp disk write). --overlay copies physics fields");
			Console.Error.WriteLine("(size_class, mass, agility, thrust_capacity, scm_speed, etc.) from a");
			Console.Error.WriteLine("previous game_data.json onto matching vehicle ids — useful for PTU");
			Console.Error.WriteLine("builds where the per-ship physics XMLs aren't yet parsed.");
		}
	}
}
