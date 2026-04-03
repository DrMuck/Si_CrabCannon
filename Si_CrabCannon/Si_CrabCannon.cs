/*
 Si_CrabCannon - v2.6.0

 Alien cannon: player-controlled alien units near a friendly Nest get
 launched across the map like a cannonball. Real physics approach.

 Super Weapon: At tier 8, 5 Goliaths can be launched from a Nest.
 After 5 launches, recharges over 6 minutes with team countdown.
 Commander or Goliath player sets aim via /ccaim <angle> <speed>.

 Admin commands (/cc ...):
   on|off, speed, angle, cooldown, radius, tier, status
   crab, goliath, behemoth, scorpion, hunter — toggle unit types
   super — toggle super weapon on/off
   supercharges N — set max charges (default 5)
   supercd N — set recharge time in seconds (default 360)

 Player command:
   /ccaim <angle> <speed> — set super weapon aim (commander or goliath player)
*/

using MelonLoader;
using SilicaAdminMod;
using System.IO;
using UnityEngine;

[assembly: MelonInfo(typeof(Si_CrabCannon.CrabCannon), "Crab Cannon", "2.6.0", "schwe")]
[assembly: MelonGame("Bohemia Interactive", "Silica")]

namespace Si_CrabCannon
{
    public partial class CrabCannon : MelonMod
    {
        static bool _enabled = true;
        static float _proximityCheckTimer = 0f;
        const float PROXIMITY_CHECK_INTERVAL = 0.5f;
        static bool _cannonTierAnnounced = false;

        public override void OnInitializeMelon()
        {
            _configPath = Path.Combine("UserData", "CrabCannon_Config.json");
            LoadConfig();
            MelonLogger.Msg("Crab Cannon v2.6.0 loaded! (real physics + super weapon)");
            GameEvents.OnGameEnded += OnGameEnded;
        }

        public override void OnLateInitializeMelon()
        {
            PlayerMethods.RegisterPlayerCommand("cc", OnCcCommand, true);
            PlayerMethods.RegisterPlayerCommand("ccaim", OnCcAimCommand, true);
            MelonLogger.Msg("CrabCannon: Registered /cc and /ccaim commands.");
        }

        static void OnGameEnded(GameMode mode, Team winner)
        {
            foreach (var kvp in _activeFlights)
                Land(kvp.Value);
            _activeFlights.Clear();
            _cooldowns.Clear();
            _countdowns.Clear();
            _cannonTierAnnounced = false;
            _superCharges = 0;
            _superReady = false;
            _superAnnounced = false;
            _superRecharging = false;
            _superRechargeTimer = 0f;
            _playerAim.Clear();
            MelonLogger.Msg("CrabCannon: Round ended, cleared state.");
        }

        public override void OnUpdate()
        {
            if (!NetworkGameServer.GetServerStarted()) return;

            if (_enabled)
            {
                _proximityCheckTimer -= Time.deltaTime;
                if (_proximityCheckTimer <= 0f)
                {
                    _proximityCheckTimer = PROXIMITY_CHECK_INTERVAL;
                    CheckProximityTriggers();
                }
            }

            MonitorFlights();

            if (SuperEnabled)
                UpdateSuperWeapon();
        }
    }
}
