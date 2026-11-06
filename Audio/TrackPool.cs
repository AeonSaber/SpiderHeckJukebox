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

        private static AudioClip _lastPicked;

        // A random track from the pool, never the one that played last (unless it is the only one).
        // Between rounds the music source holds the victory theme, so "the one that played last" is
        // remembered here rather than read from the source.
        public static AudioClip PickNext(List<AudioClip> pool, AudioClip current)
        {
            if (pool.Count == 1)
            {
                _lastPicked = pool[0];
                return _lastPicked;
            }

            AudioClip avoid = current != null && pool.Contains(current) ? current : _lastPicked;
            int avoidIndex = avoid != null ? pool.IndexOf(avoid) : -1;

            int index;
            if (avoidIndex < 0)
            {
                index = Random.Range(0, pool.Count);
            }
            else
            {
                index = Random.Range(0, pool.Count - 1);
                if (index >= avoidIndex)
                {
                    index++;
                }
            }

            _lastPicked = pool[index];
            return _lastPicked;
        }
    }
}
