using SilicaAdminMod;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        // === /ccaim — players (if allowed) or admins/commanders ===
        static void OnCcAimCommand(Player? caller, string args)
        {
            if (caller == null) return;

            bool isAdmin = caller.CanAdminExecute(Power.Generic);
            bool isCommander = caller.IsCommander;

            if (!isAdmin)
            {
                if (isCommander && !CommanderAimAllowed)
                {
                    HelperMethods.SendChatMessageToPlayer(caller, "Commander aim is locked by admin.");
                    return;
                }
                if (!isCommander && !PlayerAimAllowed)
                {
                    HelperMethods.SendChatMessageToPlayer(caller, "Player aim is locked. Only commanders/admins can change.");
                    return;
                }
            }

            args = (args ?? "").Trim();
            string[] allParts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string angleStr = allParts.Length > 1 ? allParts[1] : "";
            string speedStr = allParts.Length > 2 ? allParts[2] : "";

            if (string.IsNullOrEmpty(angleStr))
            {
                int pid = caller.GetInstanceID();
                if (_playerAim.TryGetValue(pid, out float[] aim))
                {
                    float[] s = ComputeBallisticStats(aim[1], aim[0]);
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Your cannon aim: angle={0} speed={1} -> peak={2:F0}m range={3:F0}m", aim[0], aim[1], s[0], s[1]));
                }
                else
                {
                    float[] sn = ComputeBallisticStats(LaunchSpeed, LaunchAngle);
                    float[] ss = ComputeBallisticStats(_superSpeed, _superAngle);
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Cannon defaults: angle={0} speed={1} (peak={2:F0}m range={3:F0}m) | Super: angle={4} speed={5} (peak={6:F0}m range={7:F0}m) | /ccaim <angle> <speed> to set your own",
                            LaunchAngle, LaunchSpeed, sn[0], sn[1], _superAngle, _superSpeed, ss[0], ss[1]));
                }
                return;
            }

            // /ccaim range <meters> — compute speed from target range at the caller's current angle.
            // Angle source priority: caller's personal aim override → super default (_superAngle).
            if (angleStr.Equals("range", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseFloat(speedStr, out float reqRange) || reqRange <= 0f)
                {
                    HelperMethods.SendChatMessageToPlayer(caller, "Usage: /ccaim range <meters>");
                    return;
                }
                if (reqRange > MAX_RANGE)
                {
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Denied: range {0:F0}m exceeds {1}m limit.", reqRange, MAX_RANGE));
                    return;
                }

                // Determine angle to use: player's personal override (if any) takes precedence.
                int callerPid = caller.GetInstanceID();
                float usedAngle;
                string angleSource;
                if (_playerAim.TryGetValue(callerPid, out float[] existingAim) && existingAim != null && existingAim.Length >= 1)
                {
                    usedAngle = existingAim[0];
                    angleSource = "personal";
                }
                else
                {
                    usedAngle = _superAngle;
                    angleSource = "super default";
                }

                float needSpd = ComputeSpeedForRange(usedAngle, reqRange, out float actualRange);
                if (needSpd < 0f)
                {
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Angle {0}° cannot achieve that range. Change angle first via /ccaim <angle> <speed>.", usedAngle));
                    return;
                }
                if (needSpd > 800f)
                {
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Denied: range {0:F0}m at angle {1}° needs speed {2:F0} (max 800). Lower range or raise angle.",
                            reqRange, usedAngle, needSpd));
                    return;
                }

                if (caller.IsCommander)
                {
                    // Commander updates super defaults. Keep current super angle (don't overwrite with personal).
                    _superAngle = usedAngle;
                    _superSpeed = needSpd;
                    _playerAim.Remove(callerPid);
                    SaveConfig();
                    SendTeamChat(caller.Team,
                        string.Format("[SUPER WEAPON] Commander set aim: angle={0} speed={1:F0} -> range={2:F0}m ({3})", _superAngle, _superSpeed, actualRange, angleSource));
                }
                else
                {
                    // Player personal override — keep the angle they had (or super default), set new speed.
                    _playerAim[callerPid] = new float[] { usedAngle, needSpd };
                    HelperMethods.SendChatMessageToPlayer(caller,
                        string.Format("Your cannon aim set: angle={0} speed={1:F0} -> range={2:F0}m ({3})", usedAngle, needSpd, actualRange, angleSource));
                }
                return;
            }

            if (!TryParseFloat(angleStr, out float newAngle) || newAngle <= 0 || newAngle >= 90)
            {
                HelperMethods.SendChatMessageToPlayer(caller, "Usage: /ccaim <angle 1-89> <speed>  OR  /ccaim range <meters>");
                return;
            }

            float newSpeed = _superSpeed;
            if (!string.IsNullOrEmpty(speedStr))
            {
                if (!TryParseFloat(speedStr, out newSpeed) || newSpeed <= 0 || newSpeed > 800)
                {
                    HelperMethods.SendChatMessageToPlayer(caller, "Usage: /ccaim <angle 1-89> <speed 1-800>");
                    return;
                }
            }

            float[] rangeCheck = ComputeBallisticStats(newSpeed, newAngle);
            if (rangeCheck[1] > MAX_RANGE)
            {
                HelperMethods.SendChatMessageToPlayer(caller,
                    string.Format("Denied: range={0:F0}m exceeds {1}m limit. Lower speed or angle.", rangeCheck[1], MAX_RANGE));
                return;
            }

            if (caller.IsCommander)
            {
                _superAngle = newAngle;
                _superSpeed = newSpeed;
                // Clear personal override so team settings apply
                _playerAim.Remove(caller.GetInstanceID());
                SaveConfig();
                float[] s = ComputeBallisticStats(_superSpeed, _superAngle);
                SendTeamChat(caller.Team,
                    string.Format("[SUPER WEAPON] Commander set aim: angle={0} speed={1} -> peak={2:F0}m range={3:F0}m", _superAngle, _superSpeed, s[0], s[1]));
            }
            else
            {
                int pid = caller.GetInstanceID();
                _playerAim[pid] = new float[] { newAngle, newSpeed };
                float[] s = ComputeBallisticStats(newSpeed, newAngle);
                HelperMethods.SendChatMessageToPlayer(caller,
                    string.Format("Your cannon aim set: angle={0} speed={1} -> peak={2:F0}m range={3:F0}m", newAngle, newSpeed, s[0], s[1]));
            }
        }

        // === /cc — admin only ===
        static void OnCcCommand(Player? caller, string args)
        {
            if (caller != null && !caller.CanAdminExecute(Power.Generic))
            {
                HelperMethods.SendChatMessageToPlayer(caller, "Crab Cannon: admin only");
                return;
            }

            args = (args ?? "").Trim();
            string[] allParts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string sub = allParts.Length > 1 ? allParts[1].ToLower() : "";
            string valStr = allParts.Length > 2 ? allParts[2].Trim() : "";

            if (string.IsNullOrEmpty(sub))
            {
                _enabled = !_enabled;
                SaveConfig();
                Reply(caller, "Crab Cannon: " + (_enabled ? "ON" : "OFF"));
                return;
            }

            switch (sub)
            {
                case "speed":
                    if (TryParseFloat(valStr, out float speed) && speed > 0 && speed <= 800)
                    {
                        float[] sc = ComputeBallisticStats(speed, LaunchAngle);
                        if (sc[1] > MAX_RANGE)
                        { Reply(caller, string.Format("Denied: range={0:F0}m exceeds {1}m limit. Lower speed or angle.", sc[1], MAX_RANGE)); break; }
                        LaunchSpeed = speed;
                        SaveConfig();
                        Reply(caller, string.Format("Crab Cannon: speed={0} -> peak={1:F0}m range={2:F0}m", LaunchSpeed, sc[0], sc[1]));
                    }
                    else Reply(caller, "Crab Cannon: speed = " + LaunchSpeed + " (usage: /cc speed <number>)");
                    break;
                case "range":
                case "superrange":
                {
                    bool superMode = (sub == "superrange");
                    if (!TryParseFloat(valStr, out float reqRange) || reqRange <= 0f)
                    {
                        Reply(caller, string.Format("Usage: /cc {0} <meters>", sub));
                        break;
                    }
                    if (reqRange > MAX_RANGE)
                    { Reply(caller, string.Format("Denied: range {0:F0}m exceeds {1}m limit.", reqRange, MAX_RANGE)); break; }
                    float ang = superMode ? _superAngle : LaunchAngle;
                    float needSpd = ComputeSpeedForRange(ang, reqRange, out float actualRange);
                    if (needSpd < 0f)
                    { Reply(caller, string.Format("Angle {0}° cannot achieve any range. Change angle first.", ang)); break; }
                    if (needSpd > 800f)
                    { Reply(caller, string.Format("Denied: range {0:F0}m at angle {1}° needs speed {2:F0} (max 800). Raise angle or lower range.", reqRange, ang, needSpd)); break; }
                    if (superMode) _superSpeed = needSpd; else LaunchSpeed = needSpd;
                    SaveConfig();
                    string label = superMode ? "Super" : "Default";
                    Reply(caller, string.Format("Crab Cannon {0}: angle={1} speed={2:F0} -> range={3:F0}m", label, ang, needSpd, actualRange));
                    break;
                }
                case "angle":
                    if (TryParseFloat(valStr, out float angle) && angle > 0 && angle < 90)
                    {
                        float[] ac = ComputeBallisticStats(LaunchSpeed, angle);
                        if (ac[1] > MAX_RANGE)
                        { Reply(caller, string.Format("Denied: range={0:F0}m exceeds {1}m limit. Lower speed or angle.", ac[1], MAX_RANGE)); break; }
                        LaunchAngle = angle;
                        SaveConfig();
                        Reply(caller, string.Format("Crab Cannon: angle={0} -> peak={1:F0}m range={2:F0}m", LaunchAngle, ac[0], ac[1]));
                    }
                    else Reply(caller, "Crab Cannon: angle = " + LaunchAngle + " (usage: /cc angle <0-90>)");
                    break;
                case "cooldown":
                    if (TryParseFloat(valStr, out float cd) && cd >= 0)
                    { CooldownSeconds = cd; SaveConfig(); Reply(caller, "Crab Cannon: cooldown = " + CooldownSeconds + "s"); }
                    else Reply(caller, "Crab Cannon: cooldown = " + CooldownSeconds + "s");
                    break;
                case "radius":
                    if (TryParseFloat(valStr, out float radius) && radius > 0)
                    { TriggerRadius = radius; SaveConfig(); Reply(caller, "Crab Cannon: radius = " + TriggerRadius + "m"); }
                    else Reply(caller, "Crab Cannon: radius = " + TriggerRadius + "m");
                    break;
                case "tier":
                    if (int.TryParse(valStr, out int tier) && tier >= 0)
                    { MinTier = tier; SaveConfig(); Reply(caller, "Crab Cannon: min tier = " + MinTier); }
                    else Reply(caller, "Crab Cannon: min tier = " + MinTier);
                    break;
                case "crab": CrabEnabled = !CrabEnabled; SaveConfig(); Reply(caller, "crab = " + (CrabEnabled ? "ON" : "OFF")); break;
                case "goliath": GoliathEnabled = !GoliathEnabled; SaveConfig(); Reply(caller, "goliath = " + (GoliathEnabled ? "ON" : "OFF")); break;
                case "behemoth": BehemothEnabled = !BehemothEnabled; SaveConfig(); Reply(caller, "behemoth = " + (BehemothEnabled ? "ON" : "OFF")); break;
                case "scorpion": ScorpionEnabled = !ScorpionEnabled; SaveConfig(); Reply(caller, "scorpion = " + (ScorpionEnabled ? "ON" : "OFF")); break;
                case "hunter": HunterEnabled = !HunterEnabled; SaveConfig(); Reply(caller, "hunter = " + (HunterEnabled ? "ON" : "OFF")); break;
                case "super": SuperEnabled = !SuperEnabled; SaveConfig(); Reply(caller, "Super weapon = " + (SuperEnabled ? "ON" : "OFF")); break;
                case "countdown":
                    if (TryParseFloat(valStr, out float cdown) && cdown >= 0)
                    { CannonCountdown = cdown; SaveConfig(); Reply(caller, "Cannon countdown = " + CannonCountdown + "s"); }
                    else Reply(caller, "Cannon countdown = " + CannonCountdown + "s (usage: /cc countdown <seconds>)");
                    break;
                case "playeraim": PlayerAimAllowed = !PlayerAimAllowed; SaveConfig(); Reply(caller, "Player aim = " + (PlayerAimAllowed ? "ALLOWED" : "LOCKED")); break;
                case "commanderaim": CommanderAimAllowed = !CommanderAimAllowed; SaveConfig(); Reply(caller, "Commander aim = " + (CommanderAimAllowed ? "ALLOWED" : "LOCKED")); break;
                case "supercharges":
                    if (int.TryParse(valStr, out int ch) && ch > 0)
                    { SuperMaxCharges = ch; SaveConfig(); Reply(caller, "Super weapon charges = " + SuperMaxCharges); }
                    else Reply(caller, "Super weapon charges = " + SuperMaxCharges);
                    break;
                case "supercd":
                    if (TryParseFloat(valStr, out float scd) && scd > 0)
                    { SuperRechargeTime = scd; SaveConfig(); Reply(caller, "Super weapon recharge = " + SuperRechargeTime + "s"); }
                    else Reply(caller, "Super weapon recharge = " + SuperRechargeTime + "s");
                    break;
                case "on": _enabled = true; SaveConfig(); Reply(caller, "Crab Cannon: ON"); break;
                case "off": _enabled = false; SaveConfig(); Reply(caller, "Crab Cannon: OFF"); break;
                case "status":
                    float[] stats = ComputeBallisticStats(LaunchSpeed, LaunchAngle);
                    string unitFlags = string.Format("crab={0} gol={1} beh={2} scorp={3} hunt={4}",
                        CrabEnabled ? "+" : "-", GoliathEnabled ? "+" : "-",
                        BehemothEnabled ? "+" : "-", ScorpionEnabled ? "+" : "-",
                        HunterEnabled ? "+" : "-");
                    string superInfo = SuperEnabled
                        ? string.Format("SUPER: {0}/{1} charges, cd={2}s, aim={3}/{4}",
                            _superCharges, SuperMaxCharges, SuperRechargeTime, _superAngle, _superSpeed)
                        : "SUPER: OFF";
                    Reply(caller, string.Format("CC: {0} | spd={1} ang={2} pk={3:F0}m rng={4:F0}m cd={5}s r={6}m t>={7} | {8} | {9}",
                        _enabled ? "ON" : "OFF", LaunchSpeed, LaunchAngle, stats[0], stats[1],
                        CooldownSeconds, TriggerRadius, MinTier, unitFlags, superInfo));
                    break;
                default:
                    Reply(caller, "/cc [on|off|speed|angle|range|superrange|cooldown|radius|tier|status|crab|goliath|behemoth|scorpion|hunter|super|supercharges|supercd]");
                    break;
            }
        }
    }
}
