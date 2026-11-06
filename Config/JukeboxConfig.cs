using System;
using System.Collections.Generic;
using System.IO;
using Silk;
using Logger = Silk.Logger;

namespace Jukebox
{
    public static class JukeboxConfig
    {
        private const string ModId = JukeboxMod.ModId;

        public static bool Enabled => Config.GetModConfigValue(ModId, "Enabled", true);

        public static string MusicFolder
        {
            get
            {
                string value = Config.GetModConfigValue(ModId, "MusicFolder", "");
                if (string.IsNullOrEmpty(value))
                {
                    value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "SpiderHeck Jukebox");
                }
                return value;
            }
        }

        public static string DisabledNativeRaw => Config.GetModConfigValue(ModId, "DisabledNativeTracks", "");
        public static string DisabledCustomRaw => Config.GetModConfigValue(ModId, "DisabledCustomTracks", "");

        public static HashSet<string> DisabledNative
        {
            get { return ParseSet(DisabledNativeRaw); }
        }

        public static HashSet<string> DisabledCustom
        {
            get { return ParseSet(DisabledCustomRaw); }
        }

        public static bool IsNativeDisabled(string trackName)
        {
            return trackName != null && DisabledNative.Contains(trackName);
        }

        public static bool IsCustomDisabled(string trackName)
        {
            return trackName != null && DisabledCustom.Contains(trackName);
        }

        public static void SetEnabled(bool value)
        {
            Config.SetModConfigValue(ModId, "Enabled", value);
            TrackPool.Invalidate();
            Logger.LogInfo($"[Jukebox] Jukebox {(value ? "enabled" : "disabled")}");
        }

        public static void ToggleNative(string trackName)
        {
            var set = DisabledNative;
            if (set.Contains(trackName))
            {
                set.Remove(trackName);
            }
            else
            {
                set.Add(trackName);
            }
            Config.SetModConfigValue(ModId, "DisabledNativeTracks", JoinSet(set));
            TrackPool.Invalidate();
            Logger.LogInfo($"[Jukebox] Game track '{trackName}' {(set.Contains(trackName) ? "disabled" : "enabled")}");
        }

        public static void ToggleCustom(string trackName)
        {
            var set = DisabledCustom;
            if (set.Contains(trackName))
            {
                set.Remove(trackName);
            }
            else
            {
                set.Add(trackName);
            }
            Config.SetModConfigValue(ModId, "DisabledCustomTracks", JoinSet(set));
            TrackPool.Invalidate();
            Logger.LogInfo($"[Jukebox] Custom track '{trackName}' {(set.Contains(trackName) ? "disabled" : "enabled")}");
        }

        // Names are stored separated by '|', which Windows does not allow in file names, so a
        // song called "Part 1, Part 2.mp3" survives. Version 0.1.0 used ',', which is still read.
        private const char Separator = '|';

        private static HashSet<string> ParseSet(string raw)
        {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(raw))
            {
                return set;
            }

            char separator = raw.IndexOf(Separator) >= 0 ? Separator : ',';
            foreach (var part in raw.Split(separator))
            {
                var name = part.Trim();
                if (name.Length > 0)
                {
                    set.Add(name);
                }
            }

            return set;
        }

        private static string JoinSet(HashSet<string> set)
        {
            return string.Join(Separator.ToString(), set);
        }
    }
}
