# unp4k
Unp4k utilities for Star Citizen

# Installation:
1. Download `unp4k-tools-3.2.0.zip`
2. Right click the zip and select *Properties*
3. Under the *General* tab, check the *Unblock* checkbox at the bottom if it exists, and click ok
4. Extract the selected zip to your desired installation directory

# Quickstart:

1. Drag `Data.p4k` from the `Starcitizen\LIVE` folder, directly onto `unp4k.exe`

# Advanced Command Line Usage:

1. Launch a command line, and navigate to the unp4k directory
2. Execute `unp4k.exe c:\path\to\data.p4k [filter]` where filter is a keyword used to filter the results (the default filter is `*.*`)

NOTE: This version of unp4k offloads decryption of encrypted files to a remote webserver.
NOTE: The filter does not fully support wildcards. To extract files of a certain type, you may use `*.ext` as the filter, but no further wildcard functionality exists.

# Basic GUI Usage:

1. Launch `unp4k.gui.exe`
2. Select `File` > `Open` and browse to your chosen `Data.p4k`
3. Browse the file structure
4. Right click to extract/open files

NOTE: unp4k.gui is early alpha, and has many crashes, and unfinished features. Use at your own risk