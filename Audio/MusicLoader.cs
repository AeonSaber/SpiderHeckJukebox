using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Silk.Logger;

namespace Jukebox
{
    // Lives on a DontDestroyOnLoad object. Scans the music folder at startup and on request,
    // and runs small deferred jobs for the mod menu.
    public class MusicLoader : MonoBehaviour
    {
        public static MusicLoader Instance { get; private set; }

        public static readonly List<CustomTrack> CustomTracks = new List<CustomTrack>();

        public static bool Loaded { get; private set; }

        // Fired after each scan finishes. Subscribers are one-shot: the list is cleared each time.
        public static event Action ScanCompleted;

        private static readonly string[] Extensions = { ".wav", ".mp3", ".ogg" };

        private static bool _reloadRequested;
        private AudioClip _asyncClip;

        public static void EnsureRunning()
        {
            if (Instance == null)
            {
                new GameObject("SpiderHeckJukebox.MusicLoader").AddComponent<MusicLoader>();
            }
        }

        public static void Shutdown()
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
            }

            foreach (var track in CustomTracks)
            {
                if (track.Clip != null)
                {
                    Destroy(track.Clip);
                }
            }

            CustomTracks.Clear();
            Loaded = false;
            TrackPool.Invalidate();
        }

        public static void RequestReload()
        {
            _reloadRequested = true;
            Logger.LogInfo("[Jukebox] Music folder reload requested.");
            EnsureRunning();
        }

        // Runs an action on the next frame. The menu uses this so it is never rebuilt from inside
        // a button's own click handler.
        public static void RunNextFrame(Action action)
        {
            EnsureRunning();
            Instance.StartCoroutine(NextFrame(action));
        }

        public static string FormatLength(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
        }

        private static IEnumerator NextFrame(Action action)
        {
            yield return null;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Deferred menu update failed: {ex.Message}");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Created during mod Initialize, before the first scene loads. Without this, Unity
            // destroys the object with that scene and the folder is never scanned.
            DontDestroyOnLoad(gameObject);
            StartCoroutine(RunLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private IEnumerator RunLoop()
        {
            // Let the game's AudioController finish starting up first.
            yield return new WaitForSeconds(0.5f);

            _reloadRequested = false;
            yield return Scan();
            Loaded = true;
            Logger.LogInfo($"[Jukebox] Loaded {CustomTracks.Count} custom track(s) from {JukeboxConfig.MusicFolder}");
            RaiseScanCompleted();

            while (true)
            {
                yield return new WaitForSeconds(0.25f);
                if (!_reloadRequested)
                {
                    continue;
                }

                _reloadRequested = false;
                yield return Scan();
                Logger.LogInfo($"[Jukebox] Reloaded: {CustomTracks.Count} custom track(s).");
                RaiseScanCompleted();
            }
        }

        private static void RaiseScanCompleted()
        {
            var handlers = ScanCompleted;
            ScanCompleted = null;
            if (handlers == null)
            {
                return;
            }

            try
            {
                handlers();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Menu refresh after scan failed: {ex.Message}");
            }
        }

        // Builds the new track list, reusing clips for files that have not changed, then swaps it
        // in at once so playback never sees a half-built list.
        private IEnumerator Scan()
        {
            string[] files = ListAudioFiles(JukeboxConfig.MusicFolder);

            var previous = new Dictionary<string, CustomTrack>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in CustomTracks)
            {
                previous[track.Path] = track;
            }

            var next = new List<CustomTrack>();
            foreach (string file in files)
            {
                FileInfo info;
                try
                {
                    info = new FileInfo(file);
                }
                catch (Exception)
                {
                    continue;
                }

                if (previous.TryGetValue(file, out var existing) && existing.Clip != null
                    && existing.LastWriteUtc == info.LastWriteTimeUtc && existing.Size == info.Length)
                {
                    next.Add(existing);
                    previous.Remove(file);
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(file);
                string ext = Path.GetExtension(file).ToLowerInvariant();
                AudioClip clip;

                if (ext == ".wav")
                {
                    clip = WavParser.ParseFile(file, name);
                }
                else
                {
                    yield return LoadAsyncClip(file, ext == ".mp3" ? AudioType.MPEG : AudioType.OGGVORBIS, name);
                    clip = _asyncClip;
                }

                if (clip == null)
                {
                    continue; // the loader already logged why
                }

                if (clip.length <= 0f || clip.samples <= 0)
                {
                    Logger.LogWarning($"[Jukebox] '{info.Name}' decoded to an empty clip, so it is skipped.");
                    Destroy(clip);
                    continue;
                }

                Logger.LogInfo($"[Jukebox] Track '{info.Name}': {FormatLength(clip.length)}, {clip.frequency} Hz, {clip.channels} channel(s)");
                next.Add(new CustomTrack(info.Name, name, file, info.LastWriteTimeUtc, info.Length, clip));
            }

            // Clips for files that were removed or changed since the last scan.
            foreach (var gone in previous.Values)
            {
                if (gone.Clip != null)
                {
                    Destroy(gone.Clip);
                }
            }

            CustomTracks.Clear();
            CustomTracks.AddRange(next);
            TrackPool.Invalidate();
        }

        private static string[] ListAudioFiles(string folder)
        {
            try
            {
                if (string.IsNullOrEmpty(folder))
                {
                    return new string[0];
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    Logger.LogInfo($"[Jukebox] Created music folder: {folder}");
                    return new string[0];
                }

                var files = new List<string>();
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (Array.IndexOf(Extensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
                    {
                        continue;
                    }

                    if ((File.GetAttributes(file) & FileAttributes.Hidden) != 0)
                    {
                        continue;
                    }

                    files.Add(file);
                }

                files.Sort(StringComparer.OrdinalIgnoreCase);
                return files.ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Could not read the music folder {folder}: {ex.Message}");
                return new string[0];
            }
        }

        private IEnumerator LoadAsyncClip(string file, AudioType type, string clipName)
        {
            _asyncClip = null;
            string fileName = Path.GetFileName(file);

            UnityWebRequest request;
            try
            {
                request = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, type);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Could not start loading '{fileName}': {ex.Message}");
                yield break;
            }

            using (request)
            {
                yield return request.SendWebRequest();

                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Logger.LogWarning($"[Jukebox] Could not decode '{fileName}': {request.error}");
                    }
                    else
                    {
                        _asyncClip = DownloadHandlerAudioClip.GetContent(request);
                        if (_asyncClip != null)
                        {
                            _asyncClip.name = clipName;
                        }
                        else
                        {
                            Logger.LogWarning($"[Jukebox] No audio data in '{fileName}'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[Jukebox] Error loading '{fileName}': {ex.Message}");
                    _asyncClip = null;
                }
            }
        }
    }
}
