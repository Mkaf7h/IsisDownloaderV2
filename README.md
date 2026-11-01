<div align="center">

<!-- First Image (Top Header) -->
<img src="https://i.pinimg.com/736x/6c/f0/21/6cf021b41941e1e25bf48ea548be27cf.jpg" alt="IsisDownloader Banner Graphic" width="100%" style="border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.5);">

<br><br>



#  IsisDownloaderV2



[![.NET Version](https://img.shields.io/badge/.NET-8.0--windows-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows)](https://www.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

</div>

# Features



- **Payload Obfuscation Engine:** Obfuscates PowerShell cmdlets and encodes launcher commands using Base64 (`-EncodedCommand`).
- **Hidden Execution & File Attribute:** Executes payload in background window (`-WindowStyle Hidden`) and sets Windows `Hidden` file attribute on disk.
- **Dropper & Customizable Drop Locations:** Drops files into randomized hidden folders inside choice of system directories:
  - `%TEMP%`
  - `%APPDATA%`
  - `%LOCALAPPDATA%`
  - `%PUBLIC%`
  - `%USERPROFILE%`
  - `%ProgramData%`
- **Delete After :** Automatically wipes the downloaded file and dropper directory immediately after execution completes.
- **Triple-Redundant Upload Pipeline:** Automatic failover between Catbox (`catbox.moe`), Litterbox (`litterbox.catbox.moe`), and Tmpfiles (`tmpfiles.org`).
- **Command History & Export:** Keeps full history log with one-click export to `IsisDownloader_Codes.txt`.

---

##  Overview

**IsisDownloader** is an advanced C# desktop application built on **.NET 8.0** that provides a seamless, fast solution for uploading files and instantly generating automated **PowerShell** execution commands (`powershell "irm <URL> | iex"`).

Features a sleek dark UI, multi-server upload fallback system (Catbox, Litterbox, Tmpfiles), automatic clipboard copying, and complete command history logging.

---

##  Screenshots

<div align="center">

###  Main
<img src="https://i.imgur.com/fopOqII.png" alt="Main Interface" width="85%" style="border-radius: 10px; margin-bottom: 20px;">

<br><br>

###  Command History
<img src="https://i.imgur.com/4BYdbr1.png" alt="History & Settings" width="85%" style="border-radius: 10px;">

<br><br>

# About
<!-- Second Image (Below top header) -->
<img src="https://i.imgur.com/jcKwbEv.png" alt="IsisDownloader Banner" width="100%">

</div>

---

##  System Requirements

- **Operating System:** Windows 10 / 11 (64-bit)
- **Framework:** [.NET 8.0 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

##  Building & Running

### 1️⃣ Clone Repository
```bash
git clone https://github.com/Mkaf7h/IsisDownloader.git
cd IsisDownloader
```

### 2️⃣ Run Application
```bash
dotnet run --project IsisDownloader.csproj
```

### 3️⃣ Build Binary
```bash
dotnet build IsisDownloader.csproj -c Release
```

---

## ⚖️ Disclaimer

> ⚠️ **Notice:** This tool is designed strictly for educational and authorized security testing purposes. The developer holds no responsibility for any misuse or illegal activities conducted with this software.

---

<div align="center">

**NukersecGroup**

</div>
