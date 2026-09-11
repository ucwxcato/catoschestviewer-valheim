# CatosChestViewer — Development Plan

> **Status:** Phase 0 and the Phase 1 scaffold are complete. Phase 2 targeting,
> read, formatting, and native-HUD integration are implemented, compiled, and
> deployed to the client profile. Gameplay behavior is not yet verified.
>
> **Purpose:** Show the contents of the chest currently under the local
> player's crosshair as simple text without opening the chest.
>
> **Authority:** This is the canonical implementation plan for CatosChestViewer.
> The sibling `ProgressionGater` project is a reference for build versions,
> deployment, test-server isolation, and Valheim 1.0 admin-list formatting.
>
> **Target:** Valheim 1.0.7 / network 39, BepInExPack Valheim 5.4.2350,
> BepInEx 5.4.23.5, Unity 6000.0.75.2503836, .NET Framework 4.8. These
> values are inherited from the sibling project and must be revalidated from
> installed assemblies before implementation.

### Phase 0 evidence

The following facts were inspected in the installed
`Valheim/valheim_Data/Managed/assembly_valheim.dll` using Mono.Cecil. This is
API evidence only; it does not prove that the plugin has loaded or that the
feature works in gameplay.

- `Player.GetHoverObject()` returns the object selected by Valheim's own
  `Player.UpdateHover()` path.
- `Player.FindHoverObject(...)` uses `GameCamera.instance`, a forward
  `Physics.RaycastNonAlloc` over `Player.m_interactMask`, and the native
  `Player.m_maxInteractDistance` plus `Hoverable.GetHoverOffset()` distance
  rule. The native hover path already resolves collider parents through
  `GetComponentInParent<Hoverable>()`.
- `Hud.UpdateCrosshair(Player, float)` reads the player's hover object,
  resolves `Hoverable`, and writes the normal hover string to public
  `Hud.m_hoverName` (`TMPro.TextMeshProUGUI`). A Harmony postfix can replace
  that text after vanilla has performed its normal update. The compile probe
  required and verified an explicit `Unity.TextMeshPro.dll` reference.
- `Container.GetInventory()` returns the chest's `Inventory` without opening
  it. `Inventory.GetAllItemsInGridOrder()` returns the slot-ordered item list.
- Display fields are available as `ItemDrop.ItemData.m_shared.m_name` and
  `ItemDrop.ItemData.m_stack`.
- `Container.CheckAccess(long)` is private but present and enforces the
  container privacy setting. `Container.m_checkGuardStone` is public and
  `PrivateArea.CheckAccess(Vector3, float, bool, bool)` is public, so the
  reader can fail closed for both private and guard-stone access before
  reading contents.
- `Container.GetHoverText()` is not sufficient as an access decision: its
  inspected body handles guard-stone hover text but does not call the private
  privacy `CheckAccess(long)` method. The implementation must therefore use a
  cached reflection delegate or equivalent verified access bridge for that
  private method.
- The inspected assembly contains no built-in subclasses of `Container`.
  Modded storage that uses the native `Container` component can be supported;
  unrelated custom storage remains out of scope for MVP.

**Phase 0 design decision:** use Valheim's native hover selection and native
HUD hover text rather than a second camera raycast and custom Canvas. This
keeps target selection aligned with the game's interaction rules and avoids a
second overlay lifecycle. The planned Harmony hook is a postfix on
`Hud.UpdateCrosshair`; the feature remains client-only and read-only.

## 0. Outcome

When the player aims at a chest, the mod performs a read-only local lookup and
renders a compact text panel containing the chest's current item names and
stack counts. The panel disappears or updates as the target changes. Looking
at a chest alone never opens it, moves items, sends an inventory mutation, or
requires an admin role.

Complete when:

```text
local player in range -> crosshair targets a chest -> contents are read ->
readable item/count text appears -> target changes or becomes invalid ->
display clears safely
```

## 1. Locked decisions

- The MVP is read-only and local-client rendered. It does not open the chest
  or alter inventory state.
- The plugin is client-only. The dedicated server does not install, load, or
  require `CatosChestViewer.dll`; other players do not need the mod.
- Targeting is based on the local player's look ray/crosshair and a bounded
  interaction range, using Valheim's already-computed
  `Player.GetHoverObject()` result rather than a second periodic raycast.
- The MVP supports vanilla `Container` chests and any compatible chest that
  exposes Valheim's normal `Container` inventory. Unsupported custom storage
  types are ignored safely.
- The display shows item display names and stack quantities; empty slots are
  omitted. Ordering follows inventory slot order for stability.
- The display replaces the normal native `Hud.m_hoverName` hover text after
  `Hud.UpdateCrosshair` runs. It is not chat spam, world-space floating text,
  or a chest UI replacement.
- The plugin never trusts client input as an authority for inventory changes.
  There are no admin commands in the MVP, so `adminlist.txt` is only part of
  the shared test-server harness and not an authorization dependency.
- The sibling test-server conventions are adopted: isolated `TEST_SERVER/`,
  client DLL deployment, active-save `adminlist.txt`, and `V_<steamid64>`
  entries. The test identity is `V_76561198062587799` unless the local owner
  changes it. The admin list is harness parity only and is not an MVP
  authorization dependency.
- The test fixture world is copied into
  `TEST_SERVER/world/worlds_local/Dedicated.db/.fwl` and launched as
  `-world "Dedicated"` from that repository-local save directory. The launcher
  must not depend on the original Downloads location.

## 2. Goals and non-goals

### Goals

- Detect the chest under the local crosshair at a safe, configurable cadence.
- Read and format the current inventory without opening or mutating it.
- Update quickly when the target chest or its contents change.
- Clear stale text when the target is invalid, out of range, destroyed, or no
  longer a supported container.
- Work in single-player and multiplayer with the same client-side behavior.
- Provide a reproducible net48 build and isolated dedicated-server smoke test.
- Provide configurable enable/update/display settings with safe defaults;
  interaction range remains Valheim-native.

### Explicitly out of scope

- Opening, remotely looting, locking, sorting, transferring, or editing chests.
- Server-side inventory replication or a new network protocol.
- Admin-only visibility, admin commands, or bypasses.
- Other containers such as player inventory, carts, boats, tombstones,
  crafting stations, or custom mod storage unless compatibility is explicitly
  added after the MVP.
- Item icons, rarity colors, search, filtering, localization authoring,
  pagination, minimaps, ESP, wallhack behavior, or distance bypasses.
- Persistent chest indexing, database storage, analytics, or Discord output.

## 3. User experience / operational flow

1. Valheim's `Player.UpdateHover` computes the native crosshair target; the
   `Hud.UpdateCrosshair` postfix receives the same local `Player` context.
2. The controller reads `Player.GetHoverObject()`, resolves a `Container`
   from the hit object or its parent, and relies on Valheim's native range and
   hoverable filtering. Non-chest targets and invalid objects clear naturally
   through vanilla HUD behavior.
3. The reader checks the container's guard-stone and privacy access rules
   before reading its inventory. Inaccessible chests retain vanilla hover text
   and never expose contents.
4. The controller reads the container inventory on the main Unity thread,
   groups nothing by default, and formats non-empty slots as `Name xCount`.
5. A stable target identity plus a lightweight inventory fingerprint prevents
   unnecessary redraws while still refreshing when contents change.
6. The native `Hud.m_hoverName` text is replaced atomically with the chest
   listing. On target loss,
   destruction, exception, or scene transition it is cleared and the cached
   target is discarded.

Proposed default presentation:

```text
Chest
Wood x20
Stone x8
Amber x1
```

## 4. Architecture and ownership

The file/type names below are proposed implementation names; the referenced
Valheim APIs were verified in Phase 0 against the installed client assembly.

```text
src/CatosChestViewer/
  Plugin.cs                 BepInEx entry point and lifecycle
  ModConfig.cs              Configuration bindings and defaults
  ChestTargetController.cs  Native hover target, identity, invalidation, cadence
  ChestInventoryReader.cs   Safe Container/inventory read and fingerprint
  ChestTextFormatter.cs     Slot ordering, names, counts, truncation
  ChestOverlay.cs           Native Hud.m_hoverName patch and text ownership
  CatosChestViewer.csproj   net48 references and assembly metadata
scripts/
  setup-references.ps1      Copy game/core references, never commit DLLs
  build.ps1                 Release build
  package.ps1               Validated Thunderstore archive
TEST_SERVER/                Ignored local launcher, config, admin list, state
```

Ownership boundaries:

- `Plugin` owns BepInEx registration, logging, config initialization, and
  disposal. Harmony patches are optional and must not be used when the normal
  update/UI lifecycle is sufficient.
- `ChestTargetController` owns only target selection and cache invalidation;
  it must not format UI or mutate inventories.
- `ChestInventoryReader` owns read-only extraction from the confirmed Valheim
  `Container`/`Inventory` API. It must not call open, remove, add, or RPC
  methods.
- `ChestOverlay` owns only the native `Hud.m_hoverName` replacement and
  restoration behavior. It must not discover world objects or read inventory
  directly.
- The game/server owns chest state and multiplayer authority. This client-only
  plugin observes the locally replicated chest state and must never duplicate
  server inventory logic or introduce a network protocol.
- The dedicated-server test harness owns only world launch and save-directory
  setup. It must never receive or require this DLL.

## 5. Data, lifecycle, and failure handling

No persistent data is required. Runtime state is transient:

| State | Entry | Exit |
|---|---|---|
| Hidden | plugin start, invalid target, scene change | valid supported chest hit |
| Tracking | valid target cached | target changes, range loss, destruction, disable |
| Rendered | successful read/format | next changed fingerprint or tracking exit |
| Faulted-read recovery | read/format exception | clear cache, log throttled warning, retry next cadence |

Rules:

- All Unity and Valheim object access occurs on the main thread.
- Treat destroyed Unity objects as invalid even if the managed reference is
  non-null.
- Snapshot only the fields needed for display during one read. Do not retain
  `ItemDrop.ItemData` references across frames.
- Bound maximum displayed lines and total text length to protect the UI from
  pathological/custom inventories; the exact defaults are a Phase 0 tuning
  point.
- Throttle repeated identical exceptions so a broken custom container cannot
  flood `LogOutput.log`.
- Dispose/unregister the overlay on plugin destroy and clear it on scene
  transitions and player absence.

## 6. Configuration, permissions, and integrations

Proposed config file: `BepInEx/config/com.catosaur.catoschestviewer.cfg`.

```ini
[General]
Enabled = true
UpdateIntervalMs = 100
MaxLines = 30
MaxTextCharacters = 1200

[Display]
ShowHeader = true
ShowEmptyMessage = true
EmptyMessage = Empty
```

The native interaction distance and HUD placement are confirmed in Phase 0;
the MVP should not introduce an independent `MaxDistance` or custom UI anchor.
`UpdateIntervalMs` is a proposed redraw/fingerprint throttle only. Invalid
values clamp to safe bounds and are logged once. Config reload is optional for
MVP; if added, it must marshal changes to the main thread and restore native
hover text safely.

There is no permission gate for the read-only display. The client-only test
server still uses the sibling project's admin setup for operational
consistency, but the mod does not read or depend on it:

```text
TEST_SERVER/adminlist.example.txt: V_YOUR_STEAM_ID
TEST_SERVER/adminlist.txt:         V_76561198062587799
```

The launcher copies that file to the `-savedir` directory before starting the
server. It must not put admin IDs in a client config or commit live identity,
world, or log files.

## 7. Safety, security, and product constraints

- Looking at a chest is observational only: no `Open`, `RPC`, inventory
  transfer, item removal, or write operation is permitted in the reader path.
- Enforce the same bounded ray distance client-side for UX; never advertise
  visibility beyond normal interaction range.
- Do not enumerate all nearby chests or retain a global chest cache; this keeps
  performance predictable and avoids an ESP-like feature.
- Never display an old chest's contents under a new target. Cache identity must
  include the resolved chest object and the target's current validity.
- Handle locked/private/custom chests by the confirmed game access model. If
  local read access is not authoritative or safe for a container type, omit it
  and document the result in verification rather than bypassing the check.
- Avoid logging item contents or player-sensitive inventory data in normal
  operation.
- Fail closed on null, destroyed, malformed, or unsupported objects: clear the
  overlay and continue the game without throwing into the update loop.

## 8. Verification matrix

| Scenario | Expected result | Evidence required |
|---|---|---|
| Aim at a populated vanilla chest | Names/counts appear without opening it | Manual client smoke test/video or notes; log shows plugin loaded |
| Aim at an empty chest | Empty message or header-only result per config | Manual check |
| Look at ground/player/non-container | Overlay is hidden/cleared | Manual check |
| Move beyond configured range | Overlay clears | Manual check |
| Change contents while tracking | Text refreshes without opening/closing overlay | Manual multiplayer or controlled test |
| Destroy chest/leave scene | No exception; overlay clears | Manual check plus clean BepInEx log |
| Unsupported/custom container | Safely ignored or explicitly supported | Test result and log review |
| Two clients target chests | Each client sees only its own local target | Two-client test; no new RPCs |
| Repeated malformed/read failures | Game continues; warning is throttled | Log inspection |
| Server launch/admin format | Server starts with active-save `V_...` admin file | Launcher output and save-dir file evidence |
| Version mismatch | Launcher refuses stale server/client setup | Negative launcher test |
| Package contents | DLL/manifest/icon/docs only; no references or live state | Archive listing and SHA-256 |

## 9. Phased checklist

### Phase 0 — Design lock and API reconnaissance

- [x] Inspect installed Valheim managed assemblies and confirm the
  `Container`, `Inventory`, item-name, native hover, and HUD text APIs.
- [x] Confirm that Valheim's target resolves child colliders through
  `GetComponentInParent<Hoverable>()` and enforces native interaction distance.
- [x] Choose the existing `Hud.m_hoverName`/`Hud.UpdateCrosshair` UI path rather
  than a second raycast and custom Canvas.
- [x] Confirm the privacy and guard-stone access APIs and lock inaccessible
  chests to vanilla hover text without contents.
- [x] Record the confirmed API names and decisions in this plan.
- [x] **Verify:** refreshed references and a temporary minimal net48 compile
  probe resolved every required type, including `Unity.TextMeshPro`; the
  probe was removed after the check. Gameplay load and behavior remain later
  verification gates.

### Phase 1 — Project and test-server scaffold

- [x] Add `src/CatosChestViewer/CatosChestViewer.csproj` with net48, assembly
  metadata, and non-copy-local references to `lib/`.
- [x] Add `Plugin.cs`, `ModConfig.cs`, `scripts/setup-references.ps1`, and
  `scripts/build.ps1` following the sibling conventions.
- [x] Add `.gitignore` entries for `TEST_SERVER/`, build output, references,
  save data, logs, and `adminlist.txt`.
- [x] Add ignored `TEST_SERVER/README.md`, client-only launcher, repository-local
  `TEST_SERVER/world` fixture, config template,
  `adminlist.example.txt`, and local `adminlist.txt` using `V_76561198062587799`.
- [x] Make the launcher rebuild, deploy the DLL only to the CatosChestViewer
  r2modman client profile, copy the admin list to the active `-savedir`, check
  the server assembly age, stop on a locked client, and launch the copied
  `Dedicated` world from `TEST_SERVER/world` by default.
- [x] Add the client-process guard and configure the launcher so the server
  never receives the plugin DLL.
- [x] Add the BepInEx config bindings and seed a missing client config from the
  repository test template without overwriting an existing client config.
- [ ] **Verify:** build the bootstrap and run the launcher; confirm only the
  client profile contains the deployed plugin.
- [x] **Verify:** clean Release build succeeds; local reference DLLs and live
  test-server state are ignored; static launcher checks show client-only
  deployment and repository-local world usage.

### Phase 2 — Read-only chest targeting and formatting

- [x] Implement `ChestTargetController` with bounded cadence, native
  `Player.GetHoverObject()` chest resolution, target identity, range checks,
  and invalidation.
- [x] Implement `ChestInventoryReader` using only confirmed read APIs; preserve
  slot order, omit empty slots, and return an immutable display snapshot.
- [x] Implement `ChestTextFormatter` with item display names, stack counts,
  header/empty text, line and character limits, and safe fallback names.
- [ ] Add unit-level tests for empty slots, malformed/null items, duplicate
  item names, truncation, and stable fingerprints where the API permits.
- [ ] **Verify:** populated and empty chests render in a local client while
  chest state remains unchanged.

### Phase 3 — Overlay lifecycle and integration hardening

- [x] Implement one local `ChestOverlay` instance with atomic text replacement,
  clear/hide behavior, and plugin/scene disposal.
- [x] Wire target changes and inventory fingerprints to redraw only when needed.
- [x] Add throttled diagnostics for unsupported objects and repeated read
  failures without logging contents.
- [ ] Verify multiplayer behavior with a matching but unmodded dedicated
  server and one or more modded clients; confirm other clients do not need the
  plugin, no custom network RPC is introduced, and no inventory mutation occurs.
- [ ] **Verify:** complete the verification matrix's gameplay, failure, and
  two-client scenarios with clean BepInEx logs.

### Phase 4 — Packaging and release gate

- [ ] Add `thunderstore/manifest.json`, icon, README, and changelog with the
  BepInExPack Valheim `5.4.2350` dependency.
- [ ] Add `scripts/package.ps1` to build, validate semantic version alignment,
  stage only release files, archive, and print SHA-256.
- [ ] Run the isolated test launcher from a clean r2modman profile and verify
  the active save directory contains the `V_...` admin entry while the server
  plugin directory does not contain `CatosChestViewer.dll`.
- [ ] Inspect the archive for absence of `lib/*.dll`, `TEST_SERVER/`, worlds,
  logs, configs containing secrets, and build intermediates.
- [ ] **Verify:** only then mark implementation checkboxes complete and publish
  the package.

## 10. Open tuning points

- [ ] Confirm final overlay anchor, font size, color, and whether the header
  includes the chest name.
- [ ] Tune update interval, distance, maximum lines, and character cap after
  profiling on a populated world.
- [ ] Decide whether localized item names should use Valheim's localization
  service in the first release or retain prefab/display-name fallback.
- [ ] Decide whether an empty chest should show `Empty` or only the header,
  based on the manual UX test.
- [ ] Revalidate inherited Valheim/BepInEx versions when implementation begins;
  update this plan and `AGENTS.md` if the installed runtime differs.
