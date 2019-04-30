# unp4k [![Build status](https://ci.appveyor.com/api/projects/status/hkufa3njtl0x9v79/branch/master?svg=true)](https://ci.appveyor.com/project/dolkensp/unp4k/branch/master)
Unp4k utilities for Star Citizen

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

# Basic GUI Usage:

1. Launch `unp4k.gui.exe`
2. Select `File` > `Open` and browse to your chosen `Data.p4k`
3. Browse the file structure
4. Right click to extract/open files

NOTE: unp4k.gui is early alpha, and has many crashes, and unfinished features. Use at your own risk
