using MelonLoader;
using SilicaAdminMod;
using System.Collections.Generic;
using UnityEngine;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        class CountdownState
        {
            public float EnterTime;
            public int LastAnnounced;
            public int NestId;
            public bool IsSuperLaunch;
        }

        class FlightState
        {
            public Unit Unit;
            public Player Player;
            public float StartY;
            public float LaunchTime;
            public float MaxFlightTime;
            public bool PastApex;
            public int OrigLayer;
            public float DragOrig;
            public MonoBehaviour CreatureScript;
            public Collider[] DisabledColliders;
            public bool CollidersRestored;
            public Vector3 PendingVelocity;
            public bool Launched;
            public float OrigTeleportDist;
        }

        // --- Runtime state ---
        static readonly Dictionary<int, float> _cooldowns = new Dictionary<int, float>();
        static readonly Dictionary<int, FlightState> _activeFlights = new Dictionary<int, FlightState>();
        static readonly Dictionary<int, CountdownState> _countdowns = new Dictionary<int, CountdownState>();

        void CheckProximityTriggers()
        {
            var structures = Structure.Structures;
            if (structures == null || structures.Count == 0) return;

            var playersInZone = new HashSet<int>();

            for (int i = 0; i < Player.Players.Count; i++)
            {
                var player = Player.Players[i];
                if (player == null) continue;

                var unit = player.ControlledUnit;
                if (unit == null || unit.IsDestroyed) continue;

                int playerId = player.GetInstanceID();
                if (_activeFlights.ContainsKey(playerId)) continue;

                // Cooldown check
                if (_cooldowns.TryGetValue(playerId, out float cdEnd) && Time.time < cdEnd)
                {
                    if (IsNearNest(unit, structures))
                    {
                        int cdRemain = Mathf.CeilToInt(cdEnd - Time.time);
                        if (_countdowns.TryGetValue(playerId, out var cdState) && cdState.LastAnnounced != cdRemain)
                        {
                            cdState.LastAnnounced = cdRemain;
                            HelperMethods.SendChatMessageToPlayer(player,
                                string.Format("[CANNON] Cooldown: {0}s", cdRemain));
                        }
                        else if (!_countdowns.ContainsKey(playerId))
                        {
                            _countdowns[playerId] = new CountdownState { LastAnnounced = cdRemain };
                            HelperMethods.SendChatMessageToPlayer(player,
                                string.Format("[CANNON] Cooldown: {0}s", cdRemain));
                        }
                    }
                    continue;
                }

                // Determine launch type
                bool isSuperLaunch = false;
                bool isNormalLaunch = false;
                string unitName = unit.ObjectInfo != null ? unit.ObjectInfo.name : "";

                if (SuperEnabled && _superReady && _superCharges > 0 && unitName.Contains("Goliath"))
                {
                    if (unit.Team != null && unit.Team.TechnologyTier >= SuperTier)
                        isSuperLaunch = true;
                }

                if (!isSuperLaunch && IsLaunchableUnit(unit))
                {
                    if (MinTier > 0 && unit.Team != null && unit.Team.TechnologyTier < MinTier)
                        isNormalLaunch = false;
                    else
                        isNormalLaunch = true;
                }

                if (!isSuperLaunch && !isNormalLaunch) continue;

                // Find nearest nest in range
                Structure nearestNest = null;
                Vector3 unitPos = unit.transform.position;
                for (int s = 0; s < structures.Count; s++)
                {
                    var structure = structures[s];
                    if (structure == null || structure.IsDestroyed) continue;
                    if (structure.Team != unit.Team) continue;
                    if (!IsNest(structure)) continue;

                    float dist = Vector3.Distance(unitPos, structure.transform.position);
                    if (dist <= TriggerRadius)
                    {
                        nearestNest = structure;
                        break;
                    }
                }

                if (nearestNest == null)
                {
                    _countdowns.Remove(playerId);
                    continue;
                }

                playersInZone.Add(playerId);

                // Countdown system
                int nestId = nearestNest.GetInstanceID();
                if (!_countdowns.TryGetValue(playerId, out var state) || state.NestId != nestId)
                {
                    state = new CountdownState
                    {
                        EnterTime = Time.time,
                        LastAnnounced = -1,
                        NestId = nestId,
                        IsSuperLaunch = isSuperLaunch
                    };
                    _countdowns[playerId] = state;
                }

                float elapsed = Time.time - state.EnterTime;
                float remaining = CannonCountdown - elapsed;

                if (remaining <= 0f)
                {
                    _countdowns.Remove(playerId);
                    if (isSuperLaunch)
                    {
                        SuperLaunch(player, unit, nearestNest);
                    }
                    else
                    {
                        float useSpeed = LaunchSpeed;
                        float useAngle = LaunchAngle;
                        if (_playerAim.TryGetValue(playerId, out float[] paim))
                        {
                            useAngle = paim[0];
                            useSpeed = paim[1];
                        }
                        LaunchUnit(player, unit, nearestNest, useSpeed, useAngle);
                    }
                }
                else
                {
                    int secRemaining = Mathf.CeilToInt(remaining);
                    if (secRemaining != state.LastAnnounced)
                    {
                        state.LastAnnounced = secRemaining;
                        string launchType = isSuperLaunch ? "SUPER WEAPON" : "Cannon";
                        HelperMethods.SendChatMessageToPlayer(player,
                            string.Format("[{0}] Launching in {1}s...", launchType, secRemaining));
                    }
                }
            }

            // Clean up stale countdowns
            var staleIds = new List<int>();
            foreach (var kvp in _countdowns)
            {
                if (!playersInZone.Contains(kvp.Key) && !_cooldowns.ContainsKey(kvp.Key))
                    staleIds.Add(kvp.Key);
            }
            foreach (int id in staleIds)
                _countdowns.Remove(id);
        }

        void LaunchUnit(Player player, Unit unit, Structure nest, float speed, float angle)
        {
            int playerId = player.GetInstanceID();
            Vector3 startPos = unit.transform.position;

            Vector3 forward = startPos - nest.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = unit.transform.forward;
            forward.Normalize();

            float angleRad = angle * Mathf.Deg2Rad;
            float vHoriz = speed * Mathf.Cos(angleRad);
            float vVert = speed * Mathf.Sin(angleRad);
            Vector3 launchVel = forward * vHoriz + Vector3.up * vVert;

            float flightTime = (2f * vVert) / 9.81f;

            var netComp = unit.NetworkComponent;
            if (netComp != null)
                netComp.SetPlayerOwner(null);

            float origTeleportDist = 0f;

            var rb = unit.GetComponent<Rigidbody>();
            float dragOrig = 0f;
            if (rb != null)
            {
                dragOrig = rb.linearDamping;
                rb.linearDamping = 0f;
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
            }

            int origLayer = unit.gameObject.layer;
            unit.gameObject.layer = GamePhysics.LAYER_NOCOLLIDE;

            MonoBehaviour creatureScript = null;
            var decapod = unit.GetComponent<CreatureDecapod>();
            if (decapod != null)
            {
                creatureScript = decapod;
                decapod.enabled = false;
            }

            var allColliders = unit.GetComponentsInChildren<Collider>();
            var disabledColliders = new List<Collider>();
            foreach (var col in allColliders)
            {
                if (col.enabled)
                {
                    col.enabled = false;
                    disabledColliders.Add(col);
                }
            }

            float maxTime = Mathf.Max(flightTime * 2f, 5f);

            var flight = new FlightState
            {
                Unit = unit,
                Player = player,
                StartY = startPos.y,
                LaunchTime = Time.time,
                MaxFlightTime = maxTime,
                PastApex = false,
                OrigLayer = origLayer,
                DragOrig = dragOrig,
                CreatureScript = creatureScript,
                DisabledColliders = disabledColliders.ToArray(),
                PendingVelocity = launchVel,
                Launched = false,
                OrigTeleportDist = origTeleportDist
            };

            _activeFlights[playerId] = flight;
            _cooldowns[playerId] = Time.time + CooldownSeconds;

            unit.transform.rotation = Quaternion.LookRotation(forward + Vector3.up * 0.3f);

            PlayCannonSound();

            string unitName = unit.ObjectInfo != null ? unit.ObjectInfo.name : "?";
            float[] bstats = ComputeBallisticStats(speed, angle);
            MelonLogger.Msg(string.Format("CANNON! {0} ({1}) launched! Speed={2} Angle={3} ExpPeak={4:F0}m ExpRange={5:F0}m",
                player.PlayerName, unitName, speed, angle, bstats[0], bstats[1]));
        }

        void MonitorFlights()
        {
            if (_activeFlights.Count == 0) return;

            var finished = new List<int>();

            foreach (var kvp in _activeFlights)
            {
                var flight = kvp.Value;

                if (flight.Unit == null || flight.Unit.IsDestroyed)
                {
                    finished.Add(kvp.Key);
                    continue;
                }

                float elapsed = Time.time - flight.LaunchTime;

                // Apply velocity after delay (ownership transition)
                if (!flight.Launched && elapsed >= 0.3f)
                {
                    flight.Launched = true;
                    flight.StartY = flight.Unit.transform.position.y;
                    var rbLaunch = flight.Unit.GetComponent<Rigidbody>();
                    if (rbLaunch != null)
                        rbLaunch.linearVelocity = flight.PendingVelocity;
                }

                if (!flight.Launched) continue;

                float currentY = flight.Unit.transform.position.y;
                var rb = flight.Unit.GetComponent<Rigidbody>();
                float vy = rb != null ? rb.linearVelocity.y : 0f;

                if (!flight.PastApex && vy < 0f && elapsed > 0.8f)
                    flight.PastApex = true;

                // Re-enable colliders when descending near ground
                if (flight.PastApex && !flight.CollidersRestored && currentY <= flight.StartY + 50f)
                {
                    flight.CollidersRestored = true;
                    flight.Unit.gameObject.layer = flight.OrigLayer;
                    if (flight.DisabledColliders != null)
                        foreach (var col in flight.DisabledColliders)
                            if (col != null) col.enabled = true;
                }

                // Landing detection
                bool belowStart = currentY <= flight.StartY + 5f;
                bool hitGround = flight.PastApex && vy >= -1f && vy <= 1f && elapsed > 1.5f;
                bool landed = flight.PastApex && (belowStart || hitGround) && elapsed > 1f;
                bool timeout = elapsed > flight.MaxFlightTime;

                if (landed || timeout)
                {
                    Land(flight);
                    if (flight.Player != null)
                        _cooldowns[flight.Player.GetInstanceID()] = Time.time + CooldownSeconds;
                    finished.Add(kvp.Key);
                    MelonLogger.Msg(string.Format("Cannon {0}! Player: {1} ({2:F1}s)",
                        timeout ? "timeout" : "landed", flight.Player?.PlayerName ?? "?", elapsed));
                }
                else
                {
                    if (rb != null)
                    {
                        // Gravity compensation for network sync
                        Vector3 realVel = rb.linearVelocity;
                        rb.linearVelocity = realVel - Physics.gravity * Time.fixedDeltaTime;

                        if (flight.Unit.NetworkTransformComponent != null)
                            flight.Unit.NetworkTransformComponent.OnTeleport();

                        rb.linearVelocity = realVel;

                        Vector3 faceDir = realVel;
                        faceDir.y *= 0.3f;
                        if (faceDir.sqrMagnitude > 1f)
                            flight.Unit.transform.rotation = Quaternion.LookRotation(faceDir);
                    }
                }
            }

            foreach (int id in finished)
                _activeFlights.Remove(id);
        }

        static void Land(FlightState flight)
        {
            if (flight.Unit == null || flight.Unit.IsDestroyed) return;

            if (flight.CreatureScript != null)
                flight.CreatureScript.enabled = true;

            if (!flight.CollidersRestored)
            {
                if (flight.DisabledColliders != null)
                    foreach (var col in flight.DisabledColliders)
                        if (col != null) col.enabled = true;
                flight.Unit.gameObject.layer = flight.OrigLayer;
            }

            var rb = flight.Unit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearDamping = flight.DragOrig;
            }

            if (flight.Player != null)
            {
                var netComp = flight.Unit.NetworkComponent;
                if (netComp != null)
                    netComp.SetPlayerOwner(flight.Player);
            }
        }
    }
}
