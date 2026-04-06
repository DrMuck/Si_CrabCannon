using MelonLoader;
using UnityEngine;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        // --- Super Weapon runtime (per alien team) ---
        static int _superCharges = 0;
        static bool _superReady = false;
        static bool _superAnnounced = false;
        static float _superRechargeTimer = 0f;
        static bool _superRecharging = false;
        static float _superLastCountdown = 0f;

        // --- Super Weapon aim ---
        static float _superAngle = 20f;
        static float _superSpeed = 180f;

        void UpdateSuperWeapon()
        {
            Team alienTeam = FindAlienTeam();
            if (alienTeam == null) return;

            if (alienTeam.TechnologyTier >= SuperTier)
            {
                if (!_superAnnounced)
                {
                    _superAnnounced = true;
                    _superCharges = SuperMaxCharges;
                    _superReady = true;
                    _superRecharging = false;
                    SendTeamChat(alienTeam,
                        string.Format("[SUPER WEAPON] ONLINE! {0} Goliath cannons ready at the Nest! Commander: use /ccaim <angle> <speed> to aim.", SuperMaxCharges));
                    MelonLogger.Msg("CrabCannon: Super weapon activated for alien team.");
                }

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
                        SendTeamChat(alienTeam,
                            string.Format("[SUPER WEAPON] Recharging... {0}:{1:D2} remaining", mins, secs));
                    }

                    if (_superRechargeTimer <= 0f)
                    {
                        _superRecharging = false;
                        _superCharges = SuperMaxCharges;
                        _superReady = true;
                        float[] rs = ComputeBallisticStats(_superSpeed, _superAngle);
                        SendTeamChat(alienTeam,
                            string.Format("[SUPER WEAPON] RECHARGED! {0} Goliath cannons ready! Aim: angle={1} speed={2} (peak={3:F0}m range={4:F0}m). Use /ccaim <angle> <speed> to adjust.",
                                SuperMaxCharges, _superAngle, _superSpeed, rs[0], rs[1]));
                    }
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

            SendTeamChat(player.Team,
                string.Format("[SUPER WEAPON] {0} launched Goliath! {1}/{2} remaining | spd={3} ang={4} peak={5:F0}m range={6:F0}m",
                    player.PlayerName, _superCharges, SuperMaxCharges, aimSpeed, aimAngle, bstats[0], bstats[1]));

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
