# unp4k [![Release](https://github.com/dolkensp/unp4k/actions/workflows/release.yaml/badge.svg)](https://github.com/dolkensp/unp4k/actions/workflows/release.yaml)
These tools allow users to open, decrypt, and extract data from Star Citizen `.p4k` files.

# Installation:
1. Download `unp4k-suite-v3.3.x.zip`
2. Right click the zip and select *Properties*
3. Under the *General* tab, check the *Unblock* checkbox at the bottom if it exists, and click ok
4. Extract the selected zip to your desired installation directory

NOTE: unforge currently requires [.Net Framework 4.6.2](https://www.microsoft.com/net/download/thank-you/net462) or greater to run. If you receive the error `Method not found: '!!0[] System.Array.Empty()'.` you will need to install this framework.

# Quickstart:

1. Drag `Data.p4k` from the `Starcitizen\LIVE` folder, directly onto `unp4k.exe`

# Advanced Command Line Usage:

1. Launch a command line, and navigate to the unp4k directory
2. Execute `unp4k.exe c:\path\to\data.p4k [filter]` where filter is a keyword used to filter the results (the default filter is `*.*`)

NOTE: The filter does not fully support wildcards. To extract files of a certain type, you may use `*.ext` as the filter, but no further wildcard functionality exists.

# unp4k.fs - Virtual Filesystem

`unp4k.fs` mounts a `.p4k` or `.dcb` file as a read-only virtual drive using [Dokan](https://github.com/dokan-dev/dokany), letting you browse and read Star Citizen game assets directly in Windows Explorer without extracting anything to disk.

**Prerequisites:** [Dokan](https://github.com/dokan-dev/dokany/releases) must be installed.

## Usage:

```
unp4k.fs.exe [path-to-file] [mount-point]
```

- `path-to-file` — path to a `.p4k` or `.dcb` file. Defaults to `Data.p4k` in the current directory.
- `mount-point` — drive letter or empty directory to mount to (e.g. `S:` or `C:\sc-data`). Defaults to `<filename>.unp4k` next to the input file.

**Examples:**

```
unp4k.fs.exe
unp4k.fs.exe "D:\Roberts Space Industries\StarCitizen\LIVE\Data.p4k"
unp4k.fs.exe "D:\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" S:
unp4k.fs.exe game.dcb X:\virtual
```

Once mounted, CryXML files are served as standard XML and DataForge records are extracted as XML on demand. Press `Q` or `Esc` to unmount and exit.

## Interactive options (while mounted):

| Key | Option | Default | Range |
|-----|--------|---------|-------|
| `1` | Max reference depth — how deeply nested DataForge references are followed | 1 | 1–1000 |
| `2` | Max pointer depth — recursion safety limit for DataForge structures | library default | 10–1000 |
| `3` | Max nodes — node count safety limit per DataForge record | library default | 1000–1000000 |

# File Format Overview:

The p4k files used by Star Citizen are Zip archives.

Star Citizen supports data in multiple modes inside the archive, including STORE, DEFLATE, and custom support for ZSTD.

Star Citizen also implements bespoke encryption over *some* of the data inside the archive - this can all be decrypted with the same public key that is utilized by CryEngine based games for various encryption routines within the engine.

Inside the p4k file, XML files are often stored as CryXML rather than raw XML.

CryXML is a basic serialized XML format which `unforge.exe` is able to deserialize.

Inside the p4k file, there is also a `game.dcb` file, which is a bespoke database format, with similarities to CryXML.

This is the product of what is known internally as "DataForge", and is also able to be converted/extracted using the `unforge.exe` tool.
