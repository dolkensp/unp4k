# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build / Run

This is a .NET solution. All projects multi-target `net6.0;net8.0;net10.0`; the release pipeline (`.github/workflows/release.yaml`) builds against `net10.0`. There are no automated tests in this repo.

```pwsh
dotnet restore
dotnet build unp4k.sln -c Release
# Single-file self-contained publish (matches CI):
dotnet publish src\unp4k\unp4k.csproj          -c Release -r win-x64 -f net10.0
dotnet publish src\unp4k.fs\unp4k.fs.csproj    -c Release -r win-x64 -f net10.0
dotnet publish src\unforge.cli\unforge.cli.csproj -c Release -r win-x64 -f net10.0
```

Linux builds **must** pass `--no-self-contained -p:UseAppHost=false -p:PublishSingleFile=false` and skip `unp4k.fs` (Dokan is Windows-only) — see the matrix in `.github/workflows/release.yaml`.

`appveyor.yml` is legacy (targets `net47`, `master` branch, AppVeyor). The active CI is GitHub Actions on `main`.

## Architecture

Six projects, layered roughly: SharpZipLib (fork) → unforge (library) → unp4k / unp4k.fs / unforge.cli (executables).

- **`src/unp4k`** — Console extractor. Opens a `.p4k` as an encrypted Zip via the SharpZipLib fork and streams entries to disk under the cwd. Filter is a substring match, not a glob (`*.ext` is special-cased to mean `.ext`). Catches per-entry exceptions and POSTs them to `https://herald.holoxplor.space/p4k/exception/...` — this is intentional telemetry, not stray code.
- **`src/unp4k.fs`** — Mounts a `.p4k` or `.dcb` as a read-only virtual drive via Dokan (`DokanNet` 2.3.0.3). Builds an in-memory `VirtualNode` tree once at startup; `CompressedFileSystem` walks the zip entries (and recurses into any `.dcb` it finds), `DataForgeFileSystem` walks DataForge record paths. CryXML and DataForge records are converted to XML lazily on read via `GetContent` lambdas. The interactive console adjusts `DataForge.MaxReferenceDepth` / `MaxPointerDepth` / `MaxNodes` (statics on `unforge.DataForge`) and clears the VFS cache on change. **Windows-only** — Dokan must be installed on the host.
- **`src/unforge`** — Library. Two formats live here:
  - **DataForge** (`DataForge.cs`, `DataForgeTypeReader.cs`, `ComplexTypes/`, `SimpleTypes/`) — bespoke binary DB found inside `.p4k` as `game.dcb`. The on-disk layout (header, definition tables, value arrays, two string tables) is documented in `spec.md` at the repo root; consult it before touching the reader. `FileVersion` and `IsLegacy` gate field widths and which tables exist. Static `Max*` knobs guard against recursion blowups when records reference each other.
  - **CryXML** (`CryXmlB/`) — CryEngine's serialized XML; `CryXmlSerializer.ReadFile` returns a real `XmlDocument`.
- **`src/unforge.cli`** — Wraps `unforge` as a CLI. `Smelter` dispatches by extension: `.dcb` → `DataForge.Save(...xml)`, anything else → `CryXmlSerializer.ReadFile`, with the original renamed to `.raw` when not overwriting.
- **`src/ICSharpCode.SharpZipLib`** — Local fork, **not** the NuGet package. The fork adds Star Citizen's AES decryption to the Zip reader; `ZipFile.Key` takes the 16-byte key. Both executables hardcode the same key (`unp4k/Program.cs:13`, `unp4k.fs/Program.cs:175`) — keep them in sync if it ever rotates.
- **`src/Zstd.Net`** — Zstd bindings. In the solution but **not currently referenced** by any executable; SC's ZSTD entries are decoded through the SharpZipLib fork. Don't assume it's wired up.

## Conventions

- `.editorconfig` mandates **tabs** with `indent_size = 4` for C# (2 for `.csproj`/XML). Match the existing tab-indented style.
- `DebugType=none` in Release on every project — don't expect PDBs in release output; the CI scripts also explicitly strip any that slip through.
