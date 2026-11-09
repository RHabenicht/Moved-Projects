# NSRCG II — Visual Basic .NET rewrite

A standalone **Shadowrun Third Edition** character-editor modernization of the original NSRCG (legacy Visual Basic 6 installer from 2005). This is a **new VB.NET Windows Forms project**, not the separate Go/Python-port project now named RunnerForge.

**Development status: initial source implementation, not a tested release.** The original ZIP contains installer binaries and data files, not VB6 forms/source code. This project is therefore a source rewrite using the original `.DAT` and `.sr3` formats for reference; it is not a direct recompilation of the historic program.

## Current capabilities

- Windows Forms GUI with DPI-aware layout, resizable/maximized main window, tabs and scrollable controls.
- Shadowrun Third Edition character record: name, metatype, six base attributes, priorities, skills, equipment, notes.
- Save and open native JSON character files with an atomic-replacement write.
- Import original `.sr3` characters, retaining all raw source text for later import expansion; reads identity, base attributes, skills and some equipment from known INI sections.
- Browse and search selected entries from legacy `.DAT` files when supplied, with excluded book codes filtered.
- Local HTML character-sheet export.
- Local PDF character-sheet export without needing Edge or extra packages.

Neither of the two abandoned UI stubs is included. In particular, **Ally Spirit creation is absent**. Excel export and GM-tool screens are also absent.

## Prerequisites

Install Visual Studio with the **.NET desktop development** workload and .NET 10 SDK, or just the .NET 10 SDK for command-line building.

Microsoft documents `net10.0-windows` and `<UseWindowsForms>true</UseWindowsForms>` for current Windows Forms projects. Windows 11 is supported; Microsoft currently limits *supported Windows 10 installations* for .NET 10 to eligible Enterprise/LTSC versions. Running on out-of-support Windows 10 Home/Pro is not validated here.

## Build

From a Windows PowerShell session in the repository folder:

```powershell
dotnet build .\src\NSRCGII.vbproj -c Release
.\publish-win-x64.ps1
```

The self-contained `publish/NSRCG-II.exe` should not require the separate .NET Desktop Runtime. The `publish/LegacyData` folder contains the curated data copied during publishing. Actual compilation and Windows execution have **not** been performed in the preparation environment; no executable or installer is claimed here.

## Catalog data

The `LegacyData` directory is a filtered working copy recovered from the user's original installer. Entries bearing known excluded source codes (`sr1`, `sr2`, `cb1–cb4`, `cp`, `pw`, `n/sl`, `nagee*`, `adh`, `bjf`, `ray`, `r3`) are excluded by a simple source-code scan. The **Rigger 3** records remain excluded pending a verified **Rigger 3 Revised** dataset. This is not a comprehensive source-book rules audit. Data is provided for local review, not as independently licensed redistribution material.

The editor can also browse another original NSRCG data directory via **File → Load legacy DAT folder**. The same banned-code filter applies.

## Known limitations

- Full SR3 priority, point-buy, skill specialization, magic, bioware, cyberware, character advancement and costs are **not yet implemented or verified**. The basic value checker is deliberately limited.
- Legacy `.sr3` import is **partial**, though the complete original text is preserved as a field in saved JSON. It does not yet reconstitute all original gear, cyberware, spells or derived stats.
- Original character-sheet styles and the full VB6 interface are not reconstructed.
- PDFs use an ASCII-limited standard font and simple text layout; non-ASCII characters are substituted on output. HTML supports Unicode.
- No Windows machine, .NET SDK or VB.NET compiler was available for compilation or GUI acceptance testing while preparing this version.
- No legacy companion EXE files are run or installed.

## GitHub destination

The Visual Basic source and filtered original catalog files are published at the **private repository** `RHabenicht/NSRCG-II`. GitHub Actions produces a Windows x64 build artifact for each main-branch commit. `RHabenicht/RunnerForge` is a separate port and is not modified by this repository. See `GITHUB_PUBLISH.md` for the repository status.

## Windows installer (.exe)

The GitHub Actions [Windows build workflow](https://github.com/RHabenicht/NSRCG-II/actions/workflows/windows-build.yml) builds two downloadable artifacts for each commit to `main`:

- **NSRCG-II-Setup-EXE** contains the `NSRCG-II-Setup-0.1.0-win-x64.exe` Windows installer, built with Inno Setup 6.3+. Download the artifact from the workflow run, extract the GitHub artifact ZIP, then run the setup executable. The installer includes the self-contained .NET 10 Windows x64 program and the original filtered `LegacyData` files, creates a Start Menu entry, offers a desktop shortcut, and supports removal through Windows Installed Apps.
- **NSRCG-II-Windows-x64** contains a portable build for manual use without installing. The `NSRCG-II.exe` must remain alongside the `LegacyData` folder.

The installer installs per-user into `%LOCALAPPDATA%\\Programs\\NSRCG II` and does not request administrator privileges. It targets Windows 10/11 x64 and x64-compatible Windows 11 Arm64 systems. It is not digitally signed; Windows SmartScreen may display a reputation warning. The source and catalog are **work in progress**, and successfully packaging the app does not establish complete Shadowrun 3 rules correctness or interactive Windows 10/11 acceptance.

For a local setup build on Windows with Inno Setup 6.3+ installed:

```powershell
.\\publish-win-x64.ps1
& "${env:ProgramFiles(x86)}\\Inno Setup 6\\ISCC.exe" .\\installer\\NSRCG-II.iss
```

The resulting installer is written under `installer-output/`. The GitHub workflow also performs unattended installation, checks the executable and data files, and then uninstalls as a packaging smoke test.
