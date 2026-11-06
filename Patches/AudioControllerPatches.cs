using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Logger = Silk.Logger;

namespace Jukebox.Patches
{
    // AudioController.Update calls PlayBattleTrack every frame while battle music is on. Vanilla
    // starts a new track whenever the playing clip is not in its own battleTrackList, so a custom
    // clip handed over by patching PickNextTrack was replaced (restarted) on the very next frame
    // and never audible. This prefix takes over that check with Jukebox's own track pool, and
    // leaves the game alone when nothing is customised.
    [HarmonyPatch(typeof(AudioController), nameof(AudioController.PlayBattleTrack))]
    public static class AudioControllerPatches
    {
        private static AccessTools.FieldRef<AudioController, AudioSource> _musicSource;
        private static AccessTools.FieldRef<AudioController, List<AudioClip>> _battleTracks;
        private static bool _failed;

        public static bool Prefix(AudioController __instance)
        {
            if (_failed)
            {
                return true;
            }

            try
            {
                if (_musicSource == null)
                {
                    _musicSource = AccessTools.FieldRefAccess<AudioController, AudioSource>("musicSource");
                    _battleTracks = AccessTools.FieldRefAccess<AudioController, List<AudioClip>>("battleTrackList");
                }

                if (!TrackPool.TryGet(_battleTracks(__instance), out var pool))
                {
                    return true;
                }

                var source = _musicSource(__instance);
                if (source == null)
                {
                    return true;
                }

                // Same as vanilla: never switch tracks while the game is paused.
                if (AudioListener.pause)
                {
                    return false;
                }

                if (pool.Count == 0)
                {
                    // Every battle track is switched off, so battle music stays silent.
                    if (source.isPlaying)
                    {
                        source.Stop();
                    }
                    return false;
                }

                if (source.isPlaying && pool.Contains(source.clip))
                {
                    return false;
                }

                source.clip = TrackPool.PickNext(pool, source.clip);
                source.Play();
                return false;
            }
            catch (Exception ex)
            {
                _failed = true;
                Logger.LogError($"[Jukebox] Battle music patch failed; the game's own music plays from now on. {ex}");
                return true;
            }
        }
    }
}
