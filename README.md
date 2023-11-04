# unp4k
The go to tools which allow everyone and anyone to open, descrypt as well as extract any data from Star Citizen and Squadron 42!

**Currently Squadron 42 is not out nor is there official Linux support for Star Citizen so, there are either no instructions for or few/supplimentary instructions for them which will be updated in future!**

- [Any 64-bit OS which supports .NET 8 is therefore supported.](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md)
- For non-Windows, you will need to download install [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- For Windows, if you dont use Win11 or havent updated it, an automatic installer may prompt you about .NET 8.

## Usage (GUI & Command Line)
1. Download the latest [unp4k](https://github.com/dolkensp/unp4k/releases) and unzip it anywhere within its own folder.
2. If you are using Windows, you can simply run unp4k.exe.

## Usage (GUI)


## Usage (Command Line)
**Tip: Use double quotation marks around a file/folder path if it contains spaces or else is will split it into multiple arguments. This also applies to the filter when using paths!**
### Windows
#### Powershell (Terminal app default)
    .\unp4ck -d -i InFilePath -o OutDirectoryPath
#### Command Line
    unp4ck -d -i InFilePath -o OutDirectoryPath
#### Example
    unp4ck -i "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" -o "C:\Windows\SC" -f "*.png" -d
### macOS
### Debian Linux
#### Bash
    ./unp4ck -d -i InFilePath -o OutDirectoryPath
#### Example
    unp4ck -i /home/USERNAME/unp4k/Data.p4k -o /home/USERNAME/unp4k/output -f *.png -d

# File Format Overview
## p4k
The p4k format is used by Star Citizen and Squadron 42 as a means of archiving all game files into a single place, simplifying as well as potentially speeding up many of the games systems.

The format is seemingly a custom PKZip archiving format which supports modes including STORE, DEFLATE and supports ZSTD. It supports bespoke encryption which covers some entries within the archive, obviously depending on what CIG encrypt per release; all the encrypted entries can be decrypted using CryEngine's public key used in its games for various encryption runtimes within the engine itself.

## dcb
The dcb format is a bespoke database format which has similarities to CryXML. dcb is the resulting file from a system known internally at CIG as 'DataForge' and is able to be converted/extracted by putting it through unp4k's unforger.

## CryXML
CryXML is a type of the serialised standard XML created for CryEngine and still exists in Star Citizen today! It can be deserialised to standard XML and then to other formats.
