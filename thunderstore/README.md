# Catos Chest Viewer

By **Catosaur**.

Client-side mod to see what is inside a chest while looking at it, without
opening the container. Items are localized, alphabetized, and duplicate stacks
are combined into one row. The header shows used slots out of the container's
total slots, and rows can show how many source stacks contributed to the total.

![CatosChestViewer in game](https://raw.githubusercontent.com/ucwxcato/catoschestviewer-valheim/main/thunderstore/catoschestviewer.png)

## Client-side only

CatosChestViewer is a **client-side mod**. It does **not** need to be
installed on a dedicated server, and other players do not need to install it.
Each client independently renders its own local hover display.

## Supported containers

The viewer supports vanilla chests and compatible modded storage that exposes
Valheim's native `Container` component, inventory, and access behavior. This
includes native-compatible storage such as modded iron chests when they follow
that API. Unsupported custom storage systems are ignored safely.

Example output:

```text
Reinforced Chest 23/24
Bone Fragments x116 (3 stacks)
Feathers x168 (4 stacks)
Wood x240
```

The source-stack detail can be disabled with `ShowStackCount` in the generated
configuration file. Stack counts are shown only when an item occupies multiple
source stacks.

## Installation

Install the Thunderstore package with a Valheim mod manager, or place
`CatosChestViewer.dll` in the client profile's `BepInEx/plugins` directory.
BepInExPack Valheim `5.4.2350` is required.

The mod creates its configuration at
`BepInEx/config/com.catosaur.catoschestviewer.cfg`.
