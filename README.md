# NSRCG II — Visual Basic .NET rewrite

A Windows Forms rewrite of the original NSRCG character editor for **Shadowrun Third Edition**. The original application shipped as a Visual Basic 6 installer in 2005. This repository holds the Visual Basic .NET implementation; the Go/Python port is maintained separately as RunnerForge.

**Status: initial source implementation.** The original ZIP contains installer binaries and data files. It has no VB6 forms or source code. This project rebuilds the editor around the original `.DAT` and `.sr3` formats.

## Current capabilities

- Windows Forms GUI with DPI-aware layout, resizable/maximized main window, tabs and scrollable controls.
- Shadowrun Third Edition character record: name, metatype, six base attributes, priorities, skills, equipment, notes.
- Save and open native JSON character files with an atomic-replacement write.
- Import original `.sr3` characters, retaining all raw source text for later import expansion; reads identity, base attributes, skills and some equipment from known INI sections.
- Browse and search selected entries from legacy `.DAT` files when supplied, with excluded book codes filtered.
- Local HTML character-sheet export.
- Local PDF character-sheet export without needing Edge or extra packages.

## Prerequisites

Install Visual Studio with the **.NET desktop development** workload and .NET 10 SDK, or just the .NET 10 SDK for command-line building.

Microsoft documents `net10.0-windows` and `<UseWindowsForms>true</UseWindowsForms>` for current Windows Forms projects. Windows 11 is supported; Microsoft currently limits *supported Windows 10 installations* for .NET 10 to eligible Enterprise/LTSC versions. Running on out-of-support Windows 10 Home/Pro is not validated here.

## Build

From a Windows PowerShell session in the repository folder:

```powershell
dotnet build .\src\NSRCGII.vbproj -c Release
.\publish-win-x64.ps1
```

The self-contained `publish/NSRCG-II.exe` runs without a separate .NET Desktop Runtime. The `publish/LegacyData` folder contains the curated data copied during publishing.

The November 9, 2026 Windows Actions run built and launched the application, compiled the installer, and passed the unattended install/uninstall smoke test. It uploaded the `NSRCG-II-Setup-EXE` and `NSRCG-II-Windows-x64` artifacts. [View the run](https://github.com/RHabenicht/NSRCG-II/actions/runs/42608537540). Physical Windows 10/11 interactive acceptance remains outstanding.

## Catalog data

The `LegacyData` directory is a filtered working copy recovered from the user's original installer. A source-code scan excludes entries marked `sr1`, `sr2`, `cb1–cb4`, `cp`, `pw`, `n/sl`, `nagee*`, `adh`, `bjf`, `ray`, or `r3`. **Rigger 3** entries remain excluded until a verified **Rigger 3 Revised** dataset is available. This filter does not verify every retained entry against its sourcebook. The data is provided for local review; no separate redistribution license is claimed.

The editor can also browse another original NSRCG data directory through **File → Load legacy DAT folder**. The same excluded-code filter applies.

## Known limitations

- Full SR3 priority, point-buy, skill specialization, magic, bioware, cyberware, character advancement and costs are **not yet implemented or verified**. The basic value checker is deliberately limited.
- Legacy `.sr3` import is **partial**, though the complete original text is preserved as a field in saved JSON. It does not yet reconstitute all original gear, cyberware, spells or derived stats.
- The editor uses its own layout and does not reproduce every original character-sheet style or screen.
- PDFs use an ASCII-limited standard font and simple text layout; non-ASCII characters are substituted on output. HTML supports Unicode.
- The hosted Windows workflow covers compilation, launch, and installer smoke tests. Interactive acceptance on physical Windows 10/11 systems remains outstanding.

## GitHub destination

The Visual Basic source and filtered catalog are maintained in the **private repository** `RHabenicht/NSRCG-II`. GitHub Actions builds Windows x64 installer and portable artifacts for main-branch commits. The Go/Python port is maintained separately as `RHabenicht/RunnerForge`. See `GITHUB_PUBLISH.md` for the repository status.

## Windows installer (.exe)

The GitHub Actions [Windows build workflow](https://github.com/RHabenicht/NSRCG-II/actions/workflows/windows-build.yml) builds two downloadable artifacts for each commit to `main`:

- **NSRCG-II-Setup-EXE** contains the `NSRCG-II-Setup-0.1.0-win-x64.exe` Windows installer, built with Inno Setup 6.3+. Download the artifact from the workflow run, extract the GitHub artifact ZIP, then run the setup executable. The installer includes the self-contained .NET 10 Windows x64 program and the original filtered `LegacyData` files, creates a Start Menu entry, offers a desktop shortcut, and supports removal through Windows Installed Apps.
- **NSRCG-II-Windows-x64** contains a portable build for manual use without installing. The `NSRCG-II.exe` must remain alongside the `LegacyData` folder.

The installer installs per-user into `%LOCALAPPDATA%\\Programs\\NSRCG II` and does not request administrator privileges. It targets Windows 10/11 x64 and x64-compatible Windows 11 Arm64 systems. The installer is unsigned, so Windows SmartScreen may display a reputation warning. The source and catalog remain in development. Packaging checks do not complete Shadowrun Third Edition rules validation or interactive Windows 10/11 acceptance.

For a local setup build on Windows with Inno Setup 6.3+ installed:

```powershell
.\\publish-win-x64.ps1
& "${env:ProgramFiles(x86)}\\Inno Setup 6\\ISCC.exe" .\\installer\\NSRCG-II.iss
```

The resulting installer is written under `installer-output/`. The GitHub workflow also performs unattended installation, checks the executable and data files, and then uninstalls as a packaging smoke test.
