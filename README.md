# unp4k
Unp4k utilities for Star Citizen

# Basic instructions:

1. Download the unp4k zip suitable for your platform (`unp4k-3-1-0-win-x64.zip` if you're unsure)
2. Right click the zip and select *Properties*
3. Under the *General* tab, check the *Unblock* checkbox at the bottom if it exists, and click ok
4. Extract the selected zip to your desired directory
5. Drag `Data.p4k` from the `Starcitizen\LIVE` folder, directly onto `unp4k.exe`.
6. This will extract all xml files from the Data.p4k. To access other assets, see **Advanced instructions**.

# Advanced instructions:

1. Download the unp4k zip suitable for your platform (`unp4k-3-1-0-win-x64.zip` if you're unsure)
2. Right click the zip and select *Properties*
3. Under the *General* tab, check the *Unblock* checkbox at the bottom if it exists, and click ok
4. Extract the selected zip to your desired directory
5. Launch a command line, and navigate to the unp4k directory
6. Execute `unp4k.exe c:\path\to\data.p4k [filter]` where filter is a keyword used to filter the results (the default filter is `xml`)

NOTE: This version of unp4k offloads decryption of encrypted files to a remote webserver.
NOTE: The filter does NOT support wildcards - if you want to search for xml files, use the filter `xml`
