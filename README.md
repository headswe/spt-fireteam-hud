# Fireteam HUD

An STHUD-style squad HUD for SPT 4.0 + Fika. It's a client-side BepInEx plugin, so
there is nothing to install on the server.

- **Radar:** turns with your view, so up is where you're looking. Every teammate is a dot
  in their own colour. A teammate beyond the range is pinned to the rim as a hollow ring.
  Small ▲/▼ arrows show when someone is above or below you, such as upstairs or in a
  basement. The arrow in the centre is you.
- **Name list:** a list next to the radar with a colour swatch, name and distance for each
  teammate. Teammates who die show `KIA` and stay greyed out. Teammates who extract or
  disconnect show `—`.
- **Per-player colours:** the first four are STHUD's red, green, blue and yellow, then
  orange, purple, cyan, pink and white. Each nickname gets its own colour entry the first
  time you raid together, so your friends keep their colours from raid to raid. You can
  change any of them.
- **Configurable area:** all settings are live in the BepInEx Configuration Manager
  (F12): anchor to any of 9 screen positions, X/Y offset, scale, size, range, opacity,
  background, and which side the list goes on.

The HUD only appears in raid while you have teammates. Press **Home** to hide it.

## Nametags

Battlefield-style teammate markers that replace Fika's name plates. They use the same
colours as the HUD: both read one shared roster and the same per-player colour settings,
so a player's colour always matches.

- **A marker over every teammate.** It's a chevron by default, or a diamond or dot, in
  the player's colour with a dark outline, so it stands out against bright skies, foliage
  and dark interiors. It shrinks with distance down to a set minimum ("Far scale"), so it
  stays readable at 300 m instead of disappearing.
- **Names only when you look.** The name and distance fade in for the teammate near your
  crosshair, and everyone else is just a marker. You can set names to Always or Never.
- **Downed teammates:** a pulsing red cross with `DOWNED` and the distance, always shown,
  so you know who needs a revive. This is the only status shown. There are no effect
  icons, limb skeletons or faction badges.
- **Behind cover**, the tag fades smoothly to a set opacity instead of popping in and out.
- **Off-screen arrows** at the screen edge point to teammates behind or beside you, with
  their distance.
- **Optional:** tags dim while you aim, project correctly through magnified scopes (or can
  be hidden there), fade when a teammate is right next to you, and can show a thin health
  bar (off by default).

Press **End** to hide the nametags. On startup the plugin turns off Fika's own name
plates. Turn them back on under *Fika → Coop | Name Plates* if you remove this plugin.
If the Fika host disables name plates for the raid, these nametags respect that and
stay hidden.

## Install

1. Build it (see below).
2. Copy `FireteamHud.dll` to `<SPT>\BepInEx\plugins\FireteamHud\`.
3. Every player who wants the HUD installs it on their own client. It isn't needed on
   the Fika headless client or on the server.

## Setting it up

1. Open F12 → *Fireteam HUD* and turn on **Preview**. This shows four dummy teammates
   circling you, in the main menu as well as in raid. One is above you, one below, one is
   beyond range, and one keeps dying.
2. Adjust the **Layout** settings until it sits where you want it, for example the
   default *BottomCenter* with Offset Y `-90` above the stance and health indicators.
   In a raid, the dummies also get nametags. One of them is downed, so you can tune the
   look of the tags as well.
3. Turn Preview off before you play.

| Section | Settings |
| --- | --- |
| General | Enabled, toggle key, Preview, teammate source |
| Layout | Anchor (9 positions), Offset X/Y, Scale, Opacity, Background opacity |
| Radar | Show, Size, Range (m), Dot size, out-of-range rings, elevation arrow threshold, self arrow, half-range ring |
| Name list | Show, Side (left/right), Width, Font size, Distance, keep dead/gone |
| Colours | Your own arrow colour |
| Player colours | One entry per teammate nickname, added automatically. Used by the HUD and the nametags |
| Nametags | Enabled, toggle key, turn off Fika plates, marker shape and size, name mode, uppercase, font size, distance, health bar, focus radius, max distance, far scale, height, opacity behind cover and while aiming, optics, near fade, off-screen arrows |

## Who counts as a teammate

`General → Teammates`:

- **Auto** (default): uses Fika's `CoopHandler.HumanPlayers`, which holds every human in
  your Fika raid. Without Fika it falls back to *SameGroup*.
- **FikaHumans**: uses Fika's list only.
- **SameGroup**: human players with the same group id as you.

Fika is found by reflection at runtime. The plugin doesn't reference it, so the plugin
still loads without Fika, and Preview works in plain SPT.

The HUD only shows your own squad. It never shows bots or other players.

## Build

```sh
dotnet build src/FireteamHud.Plugin -c Release -p:SptDir="C:\SPT"
dotnet test tests/FireteamHud.Tests   # radar maths, layout and roster; no game files needed
```

Layout:

- `src/FireteamHud.Core`: the game-independent parts, with unit tests.
  - radar projection, nametag scaling, focus fade and off-screen arrows
  - placing the HUD on screen
  - the roster that tracks alive, dead and gone teammates
  - the palette
- `src/FireteamHud.Plugin`: the BepInEx plugin.
  - `SquadSource.cs` and `NametagRenderer.cs`: the files that touch EFT types
  - `FikaCompat.cs`: reads Fika's config through BepInEx, with no Fika reference
  - `SquadTracker.cs`: the shared roster behind the HUD and the nametags
  - `HudRenderer.cs`, `NametagRenderer.cs`: IMGUI drawing
  - `HudConfig.cs`: settings
  - `Textures.cs`: generated shapes, so the plugin needs no asset bundle

## Status

- The HUD has been tested live with Fika. **The nametags have not been tested in-game
  yet.** The core has unit tests. The plugin compiles against the
  real BepInEx 5.4 and Unity 2022.3 APIs, but EFT's classes were stubbed for the check.
- The EFT members it uses are `GameWorld.MainPlayer`, `GameWorld.AllAlivePlayersList`,
  `Player.Position`, `Player.Rotation`, `Player.ProfileId`, `Player.Profile.Nickname`,
  `Player.Profile.Info.GroupId`, `Player.IsAI` and `Player.HealthController.IsAlive`. If
  your SPT version renames one of them, the build fails in `SquadSource.cs`.
- The nametags also use `CameraManager`, `PlayerBones.Neck`, `ProceduralWeaponAnimation`
  and `HealthController.GetBodyPartHealth`. Fika's own name plates use the same members.
- The view direction comes from EFT's `CameraManager` camera, falling back to
  `Camera.main` and then to the player's yaw.
