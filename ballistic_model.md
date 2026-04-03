# Crab Cannon — Ballistic Model with Empirical Corrections

## Fitted from v2.2.0 replay data (2026-04-02)
- 11 launches, speed=180, angle=22.5 deg
- Averaged trajectories, fit window 0-6.1s
- Initial velocities fixed from settings: v0h=166.3 m/s, v0v=68.9 m/s

## Corrected Ballistic Equations

### Height (vertical)
```
h(t) = v0v * t - 0.5 * g_eff * t^2
```
- **g_eff = 21.506 m/s²** (±0.071)
- Standard gravity: g = 9.81 m/s²
- Extra vertical drag: 11.696 m/s²
- Ratio: g_eff / g = 2.192x

### Horizontal distance
```
d(t) = v0h * t - 0.5 * drag_h * t^2
```
- **drag_h = 2.747 m/s²** (±0.041)

### Derived quantities (using corrected model)
```
peak_height = v0v^2 / (2 * g_eff)
time_to_apex = v0v / g_eff
total_flight_time = 2 * v0v / g_eff
horizontal_range = v0h * total_flight_time - 0.5 * drag_h * total_flight_time^2
```

Where:
```
v0v = speed * sin(angle)
v0h = speed * cos(angle)
```

### Correction factor for launch speed
To achieve an intended peak height H with the corrected model:
```
speed_corrected = speed * sqrt(g_eff / g) = speed * 1.481
```

## Example: speed=180, angle=22.5 deg
| Quantity | Intended (g=9.81) | Corrected (g_eff=21.51) |
|---|---|---|
| v0v | 68.9 m/s | 68.9 m/s |
| v0h | 166.3 m/s | 166.3 m/s |
| Peak height | 242 m | **110 m** |
| Time to apex | 7.0 s | **3.2 s** |
| Flight time | 14.0 s | **6.4 s** |
| Horiz range | 2335 m | **1008 m** |

## Source of extra drag
- Vertical: likely game's custom gravity multiplier or creature physics system
  applying ~2.2x gravity even with CreatureDecapod disabled
- Horizontal: unknown source, possibly Unity physics or network correction forces
- Both present even with: linearDamping=0, all colliders disabled, 
  CreatureDecapod disabled, NoCollide layer, useGravity=true
