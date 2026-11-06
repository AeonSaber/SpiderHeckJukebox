using System.Collections.Generic;
using UnityEngine;

namespace Jukebox
{
    // The battle tracks Jukebox may play: the enabled game tracks plus the enabled custom tracks.
    // Built on demand and cached, because the game asks for it every frame during a match.
    public static class TrackPool
    {
        private static List<AudioClip> _pool = new List<AudioClip>();
        private static bool _customised;
        private static bool _dirty = true;
        private static object _builtFor; // the game's battle track list the cache was built from

        // Call after any change to the settings or to the custom track list.
        public static void Invalidate()
        {
            _dirty = true;
        }

        // Returns false when Jukebox should leave the game's music alone: the mod is off,
        // or nothing differs from vanilla (no custom tracks on and no game track turned off).
        public static bool TryGet(List<AudioClip> nativeAll, out List<AudioClip> pool)
        {
            if (_dirty || !ReferenceEquals(_builtFor, nativeAll))
            {
                Rebuild(nativeAll);
            }

            pool = _pool;
            return _customised;
        }

        private static void Rebuild(List<AudioClip> nativeAll)
        {
            _dirty = false;
            _builtFor = nativeAll;
            _pool = new List<AudioClip>();
            _customised = false;

            if (!JukeboxConfig.Enabled)
            {
                return;
            }

            var disabledNative = JukeboxConfig.DisabledNative;
            var disabledCustom = JukeboxConfig.DisabledCustom;

            bool anyNativeDisabled = false;
            if (nativeAll != null)
            {
                foreach (var clip in nativeAll)
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    if (disabledNative.Contains(clip.name))
                    {
                        anyNativeDisabled = true;
                        continue;
                    }

                    _pool.Add(clip);
                }
            }

            bool anyCustom = false;
            foreach (var track in MusicLoader.CustomTracks)
            {
                if (track.Clip == null || disabledCustom.Contains(track.Key))
                {
                    continue;
                }

                _pool.Add(track.Clip);
                anyCustom = true;
            }

            _customised = anyNativeDisabled || anyCustom;
        }

        // A random track from the pool, never the one that just played (unless it is the only one).
        public static AudioClip PickNext(List<AudioClip> pool, AudioClip current)
        {
            if (pool.Count == 1)
            {
                return pool[0];
            }

            int currentIndex = current != null ? pool.IndexOf(current) : -1;
            if (currentIndex < 0)
            {
                return pool[Random.Range(0, pool.Count)];
            }

            int index = Random.Range(0, pool.Count - 1);
            if (index >= currentIndex)
            {
                index++;
            }

            return pool[index];
        }
    }
}
