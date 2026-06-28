# Si_CrabCannon

> Long-range ballistic super-weapon for the **Alien faction** in [Silica](https://store.steampowered.com/app/504900/Silica/).

When the cannon is unlocked (team reaches the configured tech tier), an alien player walks a Crab — or any other enabled creature — into the Nest's trigger radius and gets launched as a ballistic projectile. The Crab arcs across the map, lands at the configured range, and unleashes the creature on the target. Combined with the optional **Super Weapon** mode it becomes a per-team strategic strike with charges, recharge time, and team-wide aiming.

Acts as the alien equivalent of artillery or a nuke.

## Features

- **Ballistic launch** — drop a Crab (or Goliath / Behemoth / Scorpion / Hunter) into the Nest area and it arcs across the map
- **Range-aware tuning** — admins set speed + angle, server previews peak height and ground range, rejects shots that exceed the map's safe limit
- **Per-team Super Weapon** — separate trajectory and charge pool, aimable by the team commander (or any player, if the admin allows it)
- **Player / commander aim opt-ins** — admins can let regular players or just the commander set their own aim with `/ccaim`
- **Pre-fire countdown broadcast** — configurable warning before the launch
- **Per-creature toggles** — enable / disable each payload type independently

## Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader) on your Silica dedicated server
2. Install the [Silica Admin Mod](https://github.com/data-bomb/Silica) (required dependency)
3. Copy `Si_CrabCannon.dll` to your server's `Mods/` folder
4. (Optional) Copy `cannon_boom.wav` (or your own) to `UserData/sounds/`

Config is auto-created at `UserData/CrabCannon_cfg/Si_CrabCannon_Config.json` on first run.

## Admin commands (`/cc`)

All `/cc` subcommands require admin (`CanAdminExecute(Power.Generic)`).

### Core

| Command | Effect |
|--|--|
| `/cc` | Toggle ON / OFF |
| `/cc on` / `/cc off` | Explicit on/off |
| `/cc status` | One-line summary: mode, speed, angle, peak/range, cooldown, per-creature toggles, super state |
| `/cc speed <m/s>` | Launch speed (0–800). Previews new peak + range. Rejected if range exceeds map limit |
| `/cc angle <0–90>` | Launch angle in degrees |
| `/cc range <m>` | Sets speed so range hits `<m>` at the current angle (convenience) |
| `/cc cooldown <s>` | Reload between shots |
| `/cc radius <m>` | Trigger radius around the Nest |
| `/cc tier <N>` | Minimum team tech tier before cannon unlocks |
| `/cc countdown <s>` | Pre-fire warning countdown broadcast in team chat |

### Payload toggles

| Command | Effect |
|--|--|
| `/cc crab` | Toggle Crab payload on/off |
| `/cc goliath` | Toggle Goliath payload on/off |
| `/cc behemoth` | Toggle Behemoth payload on/off |
| `/cc scorpion` | Toggle Scorpion payload on/off |
| `/cc hunter` | Toggle Hunter payload on/off |

### Super weapon

| Command | Effect |
|--|--|
| `/cc super` | Toggle the Super Weapon subsystem on/off |
| `/cc superrange <m>` | Set super-weapon speed for the target range at the super-weapon's angle |
| `/cc supercharges <N>` | Max simultaneous super-weapon charges per team |
| `/cc supercd <s>` | Recharge time between super shots |

### Aim permissions

| Command | Effect |
|--|--|
| `/cc playeraim` | Toggle: can regular players aim the cannon? |
| `/cc commanderaim` | Toggle: can the commander aim the cannon? |

## Player / commander commands (`/ccaim`)

Available depending on what the admin allows:
- **Admins** — always
- **Commanders** — only if `CommanderAimAllowed` is ON (`/cc commanderaim`)
- **Regular players** — only if `PlayerAimAllowed` is ON (`/cc playeraim`)

If you don't have permission, `/ccaim` returns "Aim is locked".

| Command | Effect |
|--|--|
| `/ccaim` | Show your current personal aim (or the cannon + super defaults if you haven't set one) |
| `/ccaim <angle> <speed>` | Set aim manually. Angle 1–89°, speed 1–800. Range checked against `MAX_RANGE`. Commander → updates super-weapon defaults team-wide and broadcasts to team chat. Regular player → personal override |
| `/ccaim range <meters>` | Compute the speed needed to land at `<meters>` at your current angle (convenience) |

## Configuration

`UserData/CrabCannon_cfg/Si_CrabCannon_Config.json` — auto-created on first run.

Key fields:
- `LaunchSpeed`, `LaunchAngle` — default cannon ballistics
- `CooldownSeconds` — reload between standard shots
- `TriggerRadius` — Nest radius that triggers launch
- `MinTier` — tech tier required before cannon unlocks
- `CrabEnabled`, `GoliathEnabled`, `BehemothEnabled`, `ScorpionEnabled`, `HunterEnabled` — payload toggles
- `SuperEnabled` — Super Weapon master toggle
- `SuperAngle`, `SuperSpeed` — super-weapon defaults
- `SuperMaxCharges`, `SuperRechargeTime` — super-weapon charge economy
- `PlayerAimAllowed`, `CommanderAimAllowed` — `/ccaim` permissions
- `CannonCountdown` — pre-fire warning

## Building from source

```bash
cd Si_CrabCannon
dotnet build -c Release
```

Targets `netstandard2.1`. Reference DLLs in `include/netstandard2.1/`:
- `SilicaCore.dll`, `MelonLoader.dll`, `Si_AdminMod.dll`, `0Harmony.dll`
- `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.PhysicsModule.dll`

Build output: `Si_CrabCannon/bin/Release/netstandard2.1/Si_CrabCannon.dll`.

## License

GPL-3.0 (matches Silica modding ecosystem).
