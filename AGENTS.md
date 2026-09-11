# CatosChestViewer development

## Scope and source of truth

CatosChestViewer is a client-only Valheim/BepInEx mod whose MVP displays a
chest's contents while the local player is looking at that chest. The
canonical implementation plan is [DEVPLAN.md](DEVPLAN.md). Keep source,
configuration, deployment scripts, and documentation consistent with that
plan.

The dedicated server does not need this mod installed. Other players also do
not need it; each client independently renders its own local chest hover view.
The `[BepInProcess("valheim.exe")]` guard must remain on the plugin so an
accidental server-side copy cannot load it in `valheim_server.exe`.

The repository contains a bootstrap project and documentation. The chest
display is not implemented or gameplay-verified yet. Do not claim that the mod
is implemented, installed, or gameplay-verified until the relevant source,
artifact, runtime log, and manual test evidence exists.

## Compatibility and references

Follow the validated environment used by the sibling ProgressionGater project:

- Valheim `1.0.7`, network version `39`, Unity `6000.0.75.2503836`.
- BepInExPack Valheim `5.4.2350` / BepInEx `5.4.23.5`.
- .NET Framework `4.8`.
- Client: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`.
- Dedicated server: `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server`.
- Client managed assemblies: `valheim_Data\Managed`.
- BepInEx core assemblies: the installed client profile's `BepInEx\core` (the
  base Steam client install may not contain client-side BepInEx core DLLs).
- The native HUD text type requires `Unity.TextMeshPro.dll` in addition to the
  usual Unity and Valheim references.

These versions are inherited from the sibling project and must be rechecked
against the installed files before implementation. Refresh local references
with separate game-managed and BepInEx-core source directories, as in:

```powershell
.\scripts\setup-references.ps1 `
  -GameManagedDirectory "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed" `
  -BepInExCoreDirectory "C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosChestViewer\BepInEx\core"
```

Never commit reference DLLs under `lib/`; ignore `bin/`, `obj/`, runtime
server state, and local credentials.

## Build

The project is a net48 class library with references to BepInEx,
Harmony, Unity, and the Valheim assemblies in `lib/`:

```powershell
dotnet build src/CatosChestViewer/CatosChestViewer.csproj -c Release
```

Planned output:

```text
src/CatosChestViewer/bin/Release/net48/net48/CatosChestViewer.dll
```

Use a repository build script once the project is scaffolded. A release build
is not gameplay verification; record both separately.

## Local test server

Use an ignored `TEST_SERVER/` harness based on
`ProgressionGater/TEST_SERVER/start_progressiongater_test.bat`:

- Rebuild on every launch.
- Deploy the newest `CatosChestViewer.dll` only to the clean r2modman client
  profile named `CatosChestViewer`.
- Do not copy the CatosChestViewer DLL to the dedicated server; the server is
  only the multiplayer test endpoint.
- Refuse to launch when Valheim is running and locking the client DLL.
- Refuse to launch when the server game assembly is older than the refreshed
  local reference.
- Use an isolated save directory and world, port `2462`, password `696969`,
  and `-public 0` unless the plan is deliberately changed.
- For the current test setup, use the repository-local copied world at
  `TEST_SERVER\world` with `-world "Dedicated"`; do not depend on
  `C:\Users\magni\Downloads\Dedicated` at runtime.
- Copy `TEST_SERVER/adminlist.txt` into the active save directory before
  launch. For Valheim 1.0, every entry must use `V_<steamid64>`, for example
  `V_76561198062587799`.

The authoritative admin file is the `adminlist.txt` in the directory passed by
`-savedir`, not a client-local config. The test directory, worlds, logs,
configs, and identity files must remain uncommitted.

CatosChestViewer is not allowed to trust client-supplied admin state for its
MVP; the chest display itself is not admin-only. The test admin list is
operational parity with the sibling harness, not a mod dependency. If future
server features or commands are added, they must be split into a separate
server plugin and authenticate against Valheim's loaded admin list, normalizing
both `V_<id>` and raw IDs only at the comparison boundary.

## Gameplay verification

Test with a clean client profile containing BepInExPack and the mod, connected
to the matching local dedicated server. Verify at minimum:

1. Looking at a loaded chest shows a stable, readable contents list.
2. Looking away, moving out of range, opening the chest, or targeting a
   non-chest clears or updates the display safely.
3. Empty, inaccessible, destroyed, and rapidly changing chests do not throw
   exceptions or show stale contents.
4. Multiplayer clients see only their own local hover display and no
   unauthorized inventory mutation occurs.
5. Server and client BepInEx logs show successful load and no repeated update
   errors.

Do not package for Thunderstore until the clean client/server smoke test and
the failure-path checks pass.
