using MelonLoader;
using SilicaAdminMod;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Si_CrabCannon
{
    public partial class CrabCannon
    {
        // --- Empirical correction parameters ---
        const float G_EFF = 21.506f;
        const float DRAG_H = 2.747f;
        const float MAX_RANGE = 6000f;

        // --- Per-player aim overrides ---
        static readonly Dictionary<int, float[]> _playerAim = new Dictionary<int, float[]>();

        /// <summary>
        /// Inverse of ComputeBallisticStats: given angle + desired range, compute the required speed.
        /// Returns -1f if the angle cannot achieve any positive range.
        /// range = spd² · [sin(2a)/g − 2·DRAG_H·sin²(a)/g²]
        /// </summary>
        static float ComputeSpeedForRange(float angleDeg, float rangeMeters, out float achievedRange)
        {
            achievedRange = 0f;
            float rad = angleDeg * Mathf.Deg2Rad;
            float sin2a = Mathf.Sin(2f * rad);
            float sinA = Mathf.Sin(rad);
            float K = sin2a / G_EFF - 2f * DRAG_H * sinA * sinA / (G_EFF * G_EFF);
            if (K <= 0f) return -1f;
            float spd = Mathf.Sqrt(rangeMeters / K);
            float[] stats = ComputeBallisticStats(spd, angleDeg);
            achievedRange = stats[1];
            return spd;
        }

        static float[] ComputeBallisticStats(float spd, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float v0v = spd * Mathf.Sin(rad);
            float v0h = spd * Mathf.Cos(rad);
            float peakH = (v0v * v0v) / (2f * G_EFF);
            float flightT = (2f * v0v) / G_EFF;
            float range = v0h * flightT - 0.5f * DRAG_H * flightT * flightT;
            if (range < 0f) range = 0f;
            return new float[] { peakH, range, flightT };
        }

        static void Reply(Player? player, string msg)
        {
            if (player != null) HelperMethods.SendChatMessageToPlayer(player, msg);
            MelonLogger.Msg("CrabCannon: " + msg);
        }

        static bool TryParseFloat(string s, out float val)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }

        static void SendTeamChat(Team team, string msg)
        {
            if (team == null) return;
            for (int i = 0; i < Player.Players.Count; i++)
            {
                var p = Player.Players[i];
                if (p != null && p.Team == team && p != NetworkGameServer.GetServerPlayer())
                    HelperMethods.SendChatMessageToPlayer(p, msg);
            }
            MelonLogger.Msg("CrabCannon [TeamChat]: " + msg);
        }

        void PlayCannonSound(Player player)
        {
            try { _ = AudioHelper.PlaySoundFile(CannonSoundFile, player); }
            catch (Exception ex) { MelonLogger.Warning("CrabCannon sound failed: " + ex.Message); }
        }

        bool IsLaunchableUnit(Unit unit)
        {
            if (unit == null || unit.ObjectInfo == null) return false;
            string name = unit.ObjectInfo.name;
            if (CrabEnabled && name.Contains("Crab")) return true;
            if (GoliathEnabled && name.Contains("Goliath")) return true;
            if (BehemothEnabled && name.Contains("Behemoth")) return true;
            if (ScorpionEnabled && name.Contains("Scorpion")) return true;
            if (HunterEnabled && name.Contains("Hunter")) return true;
            return false;
        }

        bool IsNest(Structure structure)
        {
            if (structure == null || structure.ObjectInfo == null) return false;
            return structure.ObjectInfo.name.Contains("Nest");
        }

        // Returns true if a team name looks alien-side (Alien, Wildlife, Worm, etc.)
        static bool IsAlienSideTeamName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Alien") || name.Contains("Wildlife") || name.Contains("Worm");
        }

        // Legacy: returns a single alien-side team (highest-tier if multiple).
        // Kept for backward compatibility with announcements that target one team.
        static Team FindAlienTeam()
        {
            Team best = null;
            var teams = FindAllAlienTeams();
            for (int i = 0; i < teams.Count; i++)
            {
                if (best == null || teams[i].TechnologyTier > best.TechnologyTier)
                    best = teams[i];
            }
            return best;
        }

        // Returns all alien-side teams (Alien + Wildlife in 4way mode, just Alien in standard).
        static List<Team> FindAllAlienTeams()
        {
            var result = new List<Team>();
            // Scan Team.Teams directly — covers teams with no players too
            for (int i = 0; i < Team.Teams.Count; i++)
            {
                var t = Team.Teams[i];
                if (t == null) continue;
                if (IsAlienSideTeamName(t.TeamName))
                    result.Add(t);
            }
            return result;
        }

        void CheckCannonTierAnnouncement()
        {
            if (_cannonTierAnnounced || MinTier <= 0) return;

            var teams = FindAllAlienTeams();
            if (teams.Count == 0) return;

            // Announce to every alien-side team that has reached MinTier
            bool anyAnnounced = false;
            foreach (var t in teams)
            {
                if (t.TechnologyTier < MinTier) continue;
                float[] cs = ComputeBallisticStats(LaunchSpeed, LaunchAngle);
                SendTeamChat(t,
                    string.Format("[CANNON] Crab Cannon unlocked! Walk a crab to the Nest to launch. Use /ccaim <angle> <speed> to aim (peak={0:F0}m range={1:F0}m).",
                        cs[0], cs[1]));
                anyAnnounced = true;
            }
            if (anyAnnounced) _cannonTierAnnounced = true;
        }

        bool IsNearNest(Unit unit, List<Structure> structures)
        {
            Vector3 pos = unit.transform.position;
            for (int s = 0; s < structures.Count; s++)
            {
                var st = structures[s];
                if (st == null || st.IsDestroyed) continue;
                if (st.Team != unit.Team) continue;
                if (!IsNest(st)) continue;
                if (Vector3.Distance(pos, st.transform.position) <= TriggerRadius)
                    return true;
            }
            return false;
        }
    }
}
