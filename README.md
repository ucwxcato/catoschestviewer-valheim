# CatosChestViewer

CatosChestViewer is a client-side Valheim mod that shows the contents of the
container under the player's crosshair without opening it. Its hover view shows
used slots out of total slots and lists item stacks alphabetically. Optional
**Stash Sense** highlights things you are carrying that belong in that chest.

![CatosChestViewer showing localized chest contents](thunderstore/catoschestviewer.png)

## Client-side only

This mod only runs in `valheim.exe`. It does **not** need to be installed on a
dedicated server, and other players do not need it either. Each client renders
its own local hover display from the container it is looking at.

## Installation

1. Install BepInExPack Valheim `5.4.2350`.
2. Copy `CatosChestViewer.dll` into the client's
   `BepInEx/plugins` directory.
3. Start Valheim and look at a chest or other supported container.

The generated configuration is located at
`BepInEx/config/com.catosaur.catoschestviewer.cfg`.

## Stash Sense (optional)

Stash Sense is enabled by default and gives matching chest rows a bright
highlight whenever you carry the same item type. This makes it easy to spot
where a stack belongs without opening every chest.

```ini
[Stash Sense]
Enabled = true
```

Stash Sense only changes the local hover text; it never moves, sorts, opens, or
otherwise changes items. It does not need to be installed on a server or by
other players.

## Development

See [AGENTS.md](AGENTS.md) for the repository's compatibility, reference,
test-server, and world setup rules. Build a release DLL with:

```powershell
dotnet build src/CatosChestViewer/CatosChestViewer.csproj -c Release
```

Create and validate the Thunderstore archive with:

```powershell
.\scripts\package.ps1
```
