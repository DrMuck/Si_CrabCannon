using MelonLoader;
using System;
using System.Globalization;

namespace Si_CrabCannon
{
    // Si_ModUI export: mirrors the /cc and /ccaim chat commands only; chat stays fully usable without the UI.
    public partial class CrabCannon
    {
        public static string ModUIData()
        {
            try
            {
                string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
                string R(float v) => v.ToString("F0", CultureInfo.InvariantCulture);
                float[] st = ComputeBallisticStats(LaunchSpeed, LaunchAngle);
                float[] ss = ComputeBallisticStats(_superSpeed, _superAngle);
                string units = "crab" + (CrabEnabled ? "+" : "-") + " goliath" + (GoliathEnabled ? "+" : "-") + " behemoth" + (BehemothEnabled ? "+" : "-") +
                               " scorpion" + (ScorpionEnabled ? "+" : "-") + " hunter" + (HunterEnabled ? "+" : "-");
                return ModUI.ModUIExport.New()
                    .Enabled(_enabled)
                    .Info("Crab Cannon " + (_enabled ? "ON" : "OFF") + " | speed " + F(LaunchSpeed) + " angle " + F(LaunchAngle) + " -> peak " + R(st[0]) + " m, range " + R(st[1]) + " m | cooldown " + F(CooldownSeconds) + " s | radius " + F(TriggerRadius) + " m | tier >= " + MinTier + " | " + units)
                    .Info("Super " + (SuperEnabled ? "ON" : "OFF") + " | max " + SuperMaxCharges + " charges | recharge " + F(SuperRechargeTime) + " s | aim " + F(_superAngle) + " / " + F(_superSpeed) + " -> range " + R(ss[1]) + " m | player aim " + (PlayerAimAllowed ? "allowed" : "locked") + ", commander aim " + (CommanderAimAllowed ? "allowed" : "locked"))
                    .Action("/ccaim", "Show my aim", "Your cannon aim and the resulting peak height and range.")
                    .Value("/ccaim angle", "Launch angle (1-89)", "Your launch angle; the speed stays. Commanders set the team default instead.", admin: false, current: F(_superAngle))
                    .Value("/ccaim speed", "Launch speed (1-800)", "Your launch speed; the angle stays. Commanders set the team default instead.", admin: false, current: F(_superSpeed))
                    .Value("/ccaim range", "Aim by range (m)", "Sets your speed so the shot lands at this distance at your current angle.", admin: false, current: R(ss[1]))
                    .Action("/cc status", "Status", "Cannon and super weapon settings.", admin: true)
                    .OnOff("/cc enabled", "Crab Cannon", "Master switch for the cannon.", admin: true, on: _enabled)
                    .Value("/cc speed", "Launch speed", "Default launch speed, 1 to 800.", admin: true, current: F(LaunchSpeed))
                    .Value("/cc angle", "Launch angle", "Default launch angle in degrees, above 0 and below 90.", admin: true, current: F(LaunchAngle))
                    .Value("/cc range", "Default range (m)", "Sets the default speed so the shot lands at this range (shows the current range).", admin: true, current: R(st[1]))
                    .Value("/cc cooldown", "Cooldown (s)", "Seconds between cannon shots.", admin: true, current: F(CooldownSeconds))
                    .Value("/cc radius", "Trigger radius (m)", "Distance around a Nest that triggers a launch.", admin: true, current: F(TriggerRadius))
                    .Value("/cc tier", "Minimum tier", "Tech tier a team needs before the cannon unlocks.", admin: true, current: MinTier.ToString(CultureInfo.InvariantCulture))
                    .Value("/cc countdown", "Countdown (s)", "Seconds of warning before a launch.", admin: true, current: F(CannonCountdown))
                    .OnOff("/cc crab", "Crab payload", "Crabs can be launched.", admin: true, on: CrabEnabled)
                    .OnOff("/cc goliath", "Goliath payload", "Goliaths can be launched.", admin: true, on: GoliathEnabled)
                    .OnOff("/cc behemoth", "Behemoth payload", "Behemoths can be launched.", admin: true, on: BehemothEnabled)
                    .OnOff("/cc scorpion", "Scorpion payload", "Scorpions can be launched.", admin: true, on: ScorpionEnabled)
                    .OnOff("/cc hunter", "Hunter payload", "Hunters can be launched.", admin: true, on: HunterEnabled)
                    .OnOff("/cc super", "Super weapon", "Master switch for the super weapon.", admin: true, on: SuperEnabled)
                    .Value("/cc superrange", "Super range (m)", "Sets the super weapon speed for this range at its angle (shows the current range).", admin: true, current: R(ss[1]))
                    .Value("/cc supercharges", "Super charges", "Maximum super weapon charges.", admin: true, current: SuperMaxCharges.ToString(CultureInfo.InvariantCulture))
                    .Value("/cc supercd", "Super recharge (s)", "Seconds to recharge the super weapon.", admin: true, current: F(SuperRechargeTime))
                    .OnOff("/cc playeraim", "Player aim", "Regular players may set their own aim with ccaim.", admin: true, on: PlayerAimAllowed)
                    .OnOff("/cc commanderaim", "Commander aim", "Commanders may set the team aim with ccaim.", admin: true, on: CommanderAimAllowed)
                    .Build();
            }
            catch (Exception exception) { MelonLogger.Warning("CrabCannon ModUIData: " + exception.Message); return null; }
        }
    }
}
