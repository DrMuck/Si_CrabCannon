# Si_CrabCannon — Changelog

## v2.7.0 (2026-04-12) [CURRENT] — Multi-alien-team + range-based aim

### Multi-alien-team support (self-contained, not 4way-dependent)
- `FindAlienTeam` kept as legacy (returns the highest-tier alien-side team); new `FindAllAlienTeams` returns every team whose name contains "Alien", "Wildlife", or "Worm". Scans `Team.Teams` directly so it finds teams with no players.
- `UpdateSuperWeapon` rewritten to track qualification per team in `HashSet<int> _superTeamsContributed` keyed by `Team.Index`. Each alien-side team that reaches `SuperTier` adds `SuperMaxCharges` to the shared pool and gets its own announcement.
- Recharge refills to `SuperMaxCharges × contributingTeamCount` (so a 2-team pool refills to 10, a 1-team pool to 5).
- `CheckCannonTierAnnouncement` iterates all alien-side teams and announces cannon-unlock per team.
- Works transparently in standard HvA (1 alien team, unchanged 5-charge behavior) and in 4-way mode (2 alien-side teams, additive pool).

### Range-based aim
- `/ccaim range <meters>` — players/commanders set aim by target range; speed is computed from the current super angle via inverse ballistics (`range = spd² · K(angle)` → `spd = sqrt(range/K)`). Commander sets super defaults; players get personal aim override.
- `/cc range <m>` — admin, sets `LaunchSpeed` for default cannon at current `LaunchAngle`.
- `/cc superrange <m>` — admin, sets `_superSpeed` for super cannon at current `_superAngle`.
- All paths validate speed ≤ 800 and range ≤ `MAX_RANGE` (6000m), with helpful rejection messages suggesting angle change.
- New `ComputeSpeedForRange()` helper in `Helpers.cs`, derived from the existing `ComputeBallisticStats()` formula to stay consistent (same `G_EFF`, `DRAG_H`).

### Status/UI
- `/cc` help text updated to list `range` and `superrange` subcommands.
- `SuperLaunch` messaging now reports the **pool max** (`SuperMaxCharges × contributingTeams`) instead of the single-team max.

---

## v2.1.0 (2026-04-02)
- Fix: disable `CreatureDecapod` script during flight — its FixedUpdate was zeroing `rb.linearVelocity`
- Fix: disable `SphereCollider` during flight — prevents physics contacts from decelerating
- Both restored on landing
- Real physics approach (from v2.0.0) now works correctly with constant horizontal speed

## v2.0.0 (2026-04-02)
- **Major rewrite: real physics approach**
- Set launch velocity once (`rb.linearVelocity = launchVel`), let Unity physics handle the arc
- Gravity stays ON, no per-frame position/velocity override
- No more fighting between mod code and physics/network sync
- Landing detection: monitors vy sign flip + height return
- Layer 2 (IgnoreRaycast) during flight, safety timeout 2x flight time
- Issue: horizontal speed decayed due to CreatureDecapod script interference (fixed in v2.1.0)

## v1.8.0 (2026-04-02)
- Attempted position pre-compensation: `transform.position = arcPos - vel * dt`
- Fixed horizontal overshoot (hspd_ratio ~1.02x) but vertical still off
- Root cause found: client's `OrderedFixedUpdate` adds `(target - pos) * tickRate` to rigidbody velocity, causing compound accumulation. Server-side compensation cannot fix client-side interpolation behavior.

## v1.7.0 (2026-04-02)
- Based on v1.3.1 with vertical speed compensation: `arcVel.y -= 92f`
- Offset analysis showed ~92 m/s constant vertical offset at speed=100
- Compensation insufficient — offset not truly constant across speeds (50: ~140-330, 200: ~92-115)

## v1.6.0 (2026-04-02)
- Based on v1.3.1 with predictive velocity instead of analytical derivative
- `velocity = (nextFramePos - currentPos) / dt`
- Same overshoot as analytical — predictive velocity converges to derivative for small dt

## v1.5.0 (2026-04-02)
- Kinematic rigidbody + direct `net_VelocityLinear` field write via reflection
- Stuttery — kinematic bodies don't integrate well with client interpolation
- Client needs real rigidbody velocity for smooth interpolation

## v1.4.0 (2026-04-02)
- Kinematic teleport at FixedUpdate rate (50Hz) — back to QueueTeleport approach
- Coupled height formula: `peakHeight = speed² × sin²(angle) / (2g)`
- Removed tick rate setting
- Smooth enough on server but client saw PowerPoint-style jumps

## v1.3.1 (2026-04-02) — "the smooth one"
- Non-kinematic rigidbody, useGravity=false, analytical velocity derivative
- Per-frame: `transform.position = arcPos`, `rb.linearVelocity = arcVelocity`
- Smoothest client experience but heights wildly incorrect (2-600x overshoot)
- Root cause: physics engine applies velocity*dt ON TOP of position set, AND client adds velocity-based correction forces in `OrderedFixedUpdate`
- Coupled height: peakHeight derived from speed + angle

## v1.3.0 (2026-04-02)
- Non-kinematic rigidbody, useGravity=false, analytical velocity
- Same as v1.3.1 but with old hardcoded ArcHeight=100 (pre-coupled formula)

## v1.2.0 (2026-04-02)
- Added `/cc` chat command system (via AdminMod `PlayerMethods.RegisterPlayerCommand`)
- Commands: on/off/speed/angle/height/tick/cooldown/radius/status
- Fixed command registration: register as `"cc"` not `"!cc"` (AdminMod strips prefix)
- Fixed args parsing: AdminMod passes full chat text, need to skip first token

## v1.1.0 (2026-04-02)
- Added `!cc` admin command (Harmony patch — didn't work, wrong API)
- LaunchAngle: 45° → 22.5°, LaunchSpeed: 120 → 180 m/s
- Attempted tick rate multiplier (broken — gated elapsed time accumulation)

## v1.0.0 (2026-04-02)
- Initial version: proximity trigger, parabolic teleport arc, AdminMod AudioHelper sound
- QueueTeleport approach — worked on server but client-authoritative physics overrode it
- Fixed by stripping `NetworkComponent.SetPlayerOwner(null)` during flight
- Launch direction: nest center → crab position (approach vector)
- Cannon sound: `UserData/sounds/cannon_boom.wav` (generated 3.5s boom)

## Pre-v1.0 issues resolved
- HarmonyLib patch on `GameMode.OnGameEnded` failed — it's a delegate, not a method. Switched to `GameEvents.OnGameEnded += handler`
- `AccessTools` required HarmonyLib using — replaced with plain `AppDomain` assembly scanning
- `NetworkGameServer.GetServerIsRunning()` doesn't exist — use `GetServerStarted()`

## Architecture notes
- Server-side mod, no client mod needed
- AdminMod `AudioHelper.PlaySoundFile()` for cannon sound (voice packet injection)
- `NetworkComponent.SetPlayerOwner(null)` strips client physics authority during flight
- Replay logging mod (`Si_ReplayLogging`) used for trajectory analysis at 0.2s tick rate

## Key findings from trajectory analysis
- Client `NetworkTransformComponent.OrderedFixedUpdate` adds `(target - currentPos) * tickRateHz * dt` to rigidbody velocity — compound accumulation
- Client `ReadPhysics` ignores server updates when `GetPhysicsLocal()` is true (client thinks it owns physics)
- `WritePackedVector3` encodes velocity as magnitude (u16) + direction (3 bytes) — low precision but not the main issue
- CreatureDecapod FixedUpdate zeros rigidbody velocity when not grounded — must disable during flight
