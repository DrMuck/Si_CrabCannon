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

        void PlayCannonSound()
        {
            try { _ = AudioHelper.PlaySoundFile(CannonSoundFile); }
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
