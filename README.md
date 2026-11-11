# Aura Island

A native Windows 11 Dynamic Island-style desktop widget built with WPF and .NET 8.

## Current prototype
- Always-on-top floating island window
- Click to expand/collapse with animation
- Drag the island anywhere on screen
- Live clock and date
- CPU and memory usage monitoring
- Light/dark theme toggle
- No third-party NuGet dependencies

## Requirements
- Windows 10/11 (Windows 11 recommended)
- .NET 8 SDK

## Run
Open `AuraIsland.sln` in Visual Studio 2022 (17.8+) and run the project, or from this directory:

```powershell
dotnet run --project src/AuraIsland/AuraIsland.csproj
```

## Build
```powershell
dotnet publish src/AuraIsland/AuraIsland.csproj -c Release -r win-x64 --self-contained true
```

The published application will be in `src/AuraIsland/bin/Release/net8.0-windows/win-x64/publish/`.

## Roadmap
1. Prototype UI, clock, CPU/RAM, drag, themes.
2. Windows media session controls.
3. Wi-Fi and Bluetooth controls (with Windows permission/API constraints handled explicitly).
4. Weather, notifications, settings and startup integration.

This is an independent implementation. It does not include or modify the third-party Island application.
