using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        // --- Super Weapon runtime (shared pool across alien-side teams) ---
        static int _superCharges = 0;
        static bool _superReady = false;
        static bool _superAnnounced = false;
        static float _superRechargeTimer = 0f;
        static bool _superRecharging = false;
        static float _superLastCountdown = 0f;

        // Tracks which alien-side teams have already been announced as contributing
        // to the super pool. Each contributes SuperMaxCharges to the shared pool.
        static readonly HashSet<int> _superTeamsContributed = new HashSet<int>();

        // --- Super Weapon aim ---
        static float _superAngle = 20f;
        static float _superSpeed = 180f;

        /// <summary>
        /// Per-team qualification: when an alien-side team first reaches SuperTier, it adds
        /// SuperMaxCharges to the shared pool and gets an announcement. Works for both
        /// standard HvA (1 team) and 4-way mode (Alien + Wildlife).
        /// </summary>
        void UpdateSuperWeapon()
        {
            var teams = FindAllAlienTeams();
            if (teams.Count == 0) return;

            // Check each alien-side team for new tier-8 qualification
            foreach (var t in teams)
            {
                if (t.TechnologyTier < SuperTier) continue;
                int key = t.Index;
                if (_superTeamsContributed.Contains(key)) continue;

                _superTeamsContributed.Add(key);
                _superCharges += SuperMaxCharges;
                _superReady = true;
                _superRecharging = false;
                _superAnnounced = true;

                SendTeamChat(t,
                    string.Format("[SUPER WEAPON] ONLINE! {0} Goliath cannons added to the pool (total {1}). Commander: use /ccaim <angle> <speed> to aim.",
                        SuperMaxCharges, _superCharges));
                MelonLogger.Msg($"CrabCannon: Super weapon activated for {t.TeamShortName} (pool now {_superCharges}).");
            }

            if (!_superAnnounced) return;

            // Recharge logic: when pool is depleted, timer runs, then refills to SuperMaxCharges * contributingTeams
            if (_superRecharging)
            {
                _superRechargeTimer -= Time.deltaTime;
                _superLastCountdown -= Time.deltaTime;

                if (_superLastCountdown <= 0f)
                {
                    _superLastCountdown = SuperCountdownInterval;
                    int remaining = Mathf.CeilToInt(_superRechargeTimer);
                    int mins = remaining / 60;
                    int secs = remaining % 60;
                    foreach (var t in teams)
                        if (_superTeamsContributed.Contains(t.Index))
                            SendTeamChat(t, string.Format("[SUPER WEAPON] Recharging... {0}:{1:D2} remaining", mins, secs));
                }

                if (_superRechargeTimer <= 0f)
                {
                    _superRecharging = false;
                    int total = SuperMaxCharges * _superTeamsContributed.Count;
                    _superCharges = total;
                    _superReady = true;
                    float[] rs = ComputeBallisticStats(_superSpeed, _superAngle);
                    foreach (var t in teams)
                        if (_superTeamsContributed.Contains(t.Index))
                            SendTeamChat(t, string.Format("[SUPER WEAPON] RECHARGED! {0} Goliath cannons ready! Aim: angle={1} speed={2} (peak={3:F0}m range={4:F0}m). Use /ccaim <angle> <speed> to adjust.",
                                total, _superAngle, _superSpeed, rs[0], rs[1]));
                }
            }
        }

        void SuperLaunch(Player player, Unit unit, Structure nest)
        {
            int pid = player.GetInstanceID();
            float aimAngle = _superAngle;
            float aimSpeed = _superSpeed;
            if (_playerAim.TryGetValue(pid, out float[] playerAim))
            {
                aimAngle = playerAim[0];
                aimSpeed = playerAim[1];
            }

            MelonLogger.Msg(string.Format("SUPER LAUNCH: player={0} aimSpeed={1} aimAngle={2} (team: {3}/{4}, playerOverride: {5})",
                player.PlayerName, aimSpeed, aimAngle, _superSpeed, _superAngle,
                _playerAim.ContainsKey(pid) ? "YES" : "no"));

            LaunchUnit(player, unit, nest, aimSpeed, aimAngle);

            _superCharges--;
            float[] bstats = ComputeBallisticStats(aimSpeed, aimAngle);
            int poolMax = SuperMaxCharges * System.Math.Max(1, _superTeamsContributed.Count);

            SendTeamChat(player.Team,
                string.Format("[SUPER WEAPON] {0} launched Goliath! {1}/{2} remaining | spd={3} ang={4} peak={5:F0}m range={6:F0}m",
                    player.PlayerName, _superCharges, poolMax, aimSpeed, aimAngle, bstats[0], bstats[1]));

            if (_superCharges <= 0)
            {
                _superReady = false;
                _superRecharging = true;
                _superRechargeTimer = SuperRechargeTime;
                _superLastCountdown = SuperCountdownInterval;

                int mins = (int)SuperRechargeTime / 60;
                SendTeamChat(player.Team,
                    string.Format("[SUPER WEAPON] All charges spent! Recharging in {0} minutes...", mins));
            }
        }
    }
}
