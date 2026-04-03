using MelonLoader;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        // --- Config JSON model ---
        class CrabCannonConfig
        {
            public bool Enabled = true;
            public float TriggerRadius = 15f;
            public float LaunchSpeed = 180f;
            public float LaunchAngle = 22.5f;
            public float CooldownSeconds = 10f;
            public int MinTier = 1;

            public bool CrabEnabled = true;
            public bool GoliathEnabled = false;
            public bool BehemothEnabled = false;
            public bool ScorpionEnabled = false;
            public bool HunterEnabled = false;

            public bool SuperEnabled = true;
            public int SuperTier = 8;
            public int SuperMaxCharges = 5;
            public float SuperRechargeTime = 360f;
            public float SuperCountdownInterval = 60f;
            public float SuperAngle = 20f;
            public float SuperSpeed = 180f;
            public bool PlayerAimAllowed = true;
            public bool CommanderAimAllowed = true;
            public float CannonCountdown = 3f;
        }

        static string _configPath = "";

        // --- Config fields (adjustable via /cc commands) ---
        static float TriggerRadius = 15f;
        static float LaunchSpeed = 180f;
        static float LaunchAngle = 22.5f;
        static float CooldownSeconds = 10f;
        static int MinTier = 1;
        static string CannonSoundFile = "sounds/cannon_boom.wav";

        // --- Unit type toggles ---
        static bool CrabEnabled = true;
        static bool GoliathEnabled = false;
        static bool BehemothEnabled = false;
        static bool ScorpionEnabled = false;
        static bool HunterEnabled = false;

        // --- Super Weapon config ---
        static bool SuperEnabled = true;
        static int SuperTier = 8;
        static int SuperMaxCharges = 5;
        static float SuperRechargeTime = 360f;
        static float SuperCountdownInterval = 60f;
        static bool PlayerAimAllowed = true;
        static bool CommanderAimAllowed = true;
        static float CannonCountdown = 3f;

        static void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var cfg = JsonConvert.DeserializeObject<CrabCannonConfig>(File.ReadAllText(_configPath));
                    if (cfg != null)
                    {
                        _enabled = cfg.Enabled;
                        TriggerRadius = cfg.TriggerRadius;
                        LaunchSpeed = cfg.LaunchSpeed;
                        LaunchAngle = cfg.LaunchAngle;
                        CooldownSeconds = cfg.CooldownSeconds;
                        MinTier = cfg.MinTier;
                        CrabEnabled = cfg.CrabEnabled;
                        GoliathEnabled = cfg.GoliathEnabled;
                        BehemothEnabled = cfg.BehemothEnabled;
                        ScorpionEnabled = cfg.ScorpionEnabled;
                        HunterEnabled = cfg.HunterEnabled;
                        SuperEnabled = cfg.SuperEnabled;
                        SuperTier = cfg.SuperTier;
                        SuperMaxCharges = cfg.SuperMaxCharges;
                        SuperRechargeTime = cfg.SuperRechargeTime;
                        SuperCountdownInterval = cfg.SuperCountdownInterval;
                        _superAngle = cfg.SuperAngle;
                        _superSpeed = cfg.SuperSpeed;
                        PlayerAimAllowed = cfg.PlayerAimAllowed;
                        CommanderAimAllowed = cfg.CommanderAimAllowed;
                        CannonCountdown = cfg.CannonCountdown;
                        MelonLogger.Msg("CrabCannon: Config loaded from " + _configPath);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("CrabCannon: Failed to load config: " + ex.Message);
            }
            SaveConfig();
            MelonLogger.Msg("CrabCannon: Default config saved to " + _configPath);
        }

        static void SaveConfig()
        {
            try
            {
                var cfg = new CrabCannonConfig
                {
                    Enabled = _enabled,
                    TriggerRadius = TriggerRadius,
                    LaunchSpeed = LaunchSpeed,
                    LaunchAngle = LaunchAngle,
                    CooldownSeconds = CooldownSeconds,
                    MinTier = MinTier,
                    CrabEnabled = CrabEnabled,
                    GoliathEnabled = GoliathEnabled,
                    BehemothEnabled = BehemothEnabled,
                    ScorpionEnabled = ScorpionEnabled,
                    HunterEnabled = HunterEnabled,
                    SuperEnabled = SuperEnabled,
                    SuperTier = SuperTier,
                    SuperMaxCharges = SuperMaxCharges,
                    SuperRechargeTime = SuperRechargeTime,
                    SuperCountdownInterval = SuperCountdownInterval,
                    SuperAngle = _superAngle,
                    SuperSpeed = _superSpeed,
                    PlayerAimAllowed = PlayerAimAllowed,
                    CommanderAimAllowed = CommanderAimAllowed,
                    CannonCountdown = CannonCountdown
                };
                string dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_configPath, JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("CrabCannon: Failed to save config: " + ex.Message);
            }
        }
    }
}
