using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = Silk.Logger;

namespace Jukebox
{
    public static class AudioHelper
    {
        private static readonly FieldInfo InstanceField =
            typeof(AudioController).GetField("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo BattleTrackListField =
            typeof(AudioController).GetField("battleTrackList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static List<AudioClip> GetBattleTracks()
        {
            var tracks = new List<AudioClip>();
            try
            {
                if (InstanceField == null || BattleTrackListField == null)
                {
                    return tracks;
                }

                var instance = InstanceField.GetValue(null) as AudioController;
                if (instance == null)
                {
                    return tracks;
                }

                var list = BattleTrackListField.GetValue(instance) as List<AudioClip>;
                if (list != null)
                {
                    tracks.AddRange(list);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Could not read battle track list: {ex.Message}");
            }

            return tracks;
        }
    }
}
