using System;
using System.IO;
using HarmonyLib;
using Logger = Silk.Logger;
using UnityEngine;
using UnityEngine.UI;

namespace Jukebox.Patches
{
    // Silk opens a mod's settings page with Announcer.ModsPopup(<mod name>); this fills in Jukebox's.
    [HarmonyPatch(typeof(Announcer), nameof(Announcer.ModsPopup))]
    public static class MenuPatches
    {
        public static void Postfix(string title, ref ModsMenuPopup __result)
        {
            if (title == JukeboxMod.ModName && __result != null)
            {
                AddJukeboxInfo(__result);
            }
        }

        private static void AddJukeboxInfo(ModsMenuPopup menu)
        {
            bool enabled = JukeboxConfig.Enabled;

            menu.CreateParagraph("<size=200%>\n<b><color=#FFD700>Jukebox</color></b></size>");
            menu.CreateParagraph($"<size=115%><b>Status:</b> {(enabled ? "<color=#00FF00>Enabled</color>" : "<color=#FF5555>Disabled</color>")}</size>");
            menu.CreateParagraph("<size=95%><color=#AAAAAA>Choose which battle tracks play, and add your own songs from a folder.</color></size>");

            AddButton(menu, enabled ? "Disable Jukebox" : "Enable Jukebox", () =>
            {
                JukeboxConfig.SetEnabled(!JukeboxConfig.Enabled);
                RefreshMenuLater(menu);
            });

            Spacer(menu);

            menu.CreateParagraph($"<size=100%><b>Music folder:</b> <noparse>{JukeboxConfig.MusicFolder}</noparse></size>");
            AddButton(menu, "Open Music Folder", OpenFolder);
            AddButton(menu, "Reload Music Folder", () => ReloadFolder(menu));

            Spacer(menu);

            // Game tracks
            menu.CreateParagraph("<size=130%><color=#AAAAAA>Game tracks</color></size>");
            var nativeTracks = AudioHelper.GetBattleTracks();
            int nativeOn = 0;
            if (nativeTracks.Count == 0)
            {
                menu.CreateParagraph("<size=95%><color=#666666>(none detected yet)</color></size>");
            }
            else
            {
                foreach (var clip in nativeTracks)
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    string name = clip.name;
                    bool on = !JukeboxConfig.IsNativeDisabled(name);
                    if (on)
                    {
                        nativeOn++;
                    }

                    AddButton(menu, $"{(on ? "[on]  " : "[off] ")}<noparse>{name}</noparse>", () =>
                    {
                        JukeboxConfig.ToggleNative(name);
                        RefreshMenuLater(menu);
                    });
                }
            }

            Spacer(menu);

            // Custom tracks
            menu.CreateParagraph("<size=130%><color=#AAAAAA>Your tracks</color></size>");
            int customOn = 0;
            if (!MusicLoader.Loaded)
            {
                menu.CreateParagraph("<size=95%><color=#666666>(still loading...)</color></size>");
            }
            else if (MusicLoader.CustomTracks.Count == 0)
            {
                menu.CreateParagraph("<size=95%><color=#666666>(no .wav, .mp3 or .ogg files in the music folder)</color></size>");
            }
            else
            {
                foreach (var track in MusicLoader.CustomTracks)
                {
                    string key = track.Key;
                    bool on = !JukeboxConfig.IsCustomDisabled(key);
                    if (on)
                    {
                        customOn++;
                    }

                    string length = track.Clip != null ? MusicLoader.FormatLength(track.Clip.length) : "?";
                    AddButton(menu, $"{(on ? "[on]  " : "[off] ")}<noparse>{track.Name}</noparse>  <color=#888888>{length}</color>", () =>
                    {
                        JukeboxConfig.ToggleCustom(key);
                        RefreshMenuLater(menu);
                    });
                }
            }

            if (enabled && nativeTracks.Count > 0 && nativeOn == 0 && customOn == 0)
            {
                Spacer(menu);
                menu.CreateParagraph("<size=95%><color=#FFAA00>Every battle track is off, so battle music is silent.</color></size>");
            }

            Spacer(menu);
            menu.CreateParagraph("<size=95%><color=#AAAAAA>Put .wav, .mp3 or .ogg files in the folder, then press Reload. Tracks set to [off] are left out of the shuffle.</color></size>");
        }

        private static void ReloadFolder(ModsMenuPopup menu)
        {
            // Rebuild after the scan, not inside this click: destroying the clicked button from its
            // own onClick throws a NullReferenceException and DOTween "NULL target" warnings.
            MusicLoader.ScanCompleted += () => RefreshMenu(menu);
            MusicLoader.RequestReload();
        }

        private static void OpenFolder()
        {
            try
            {
                string folder = JukeboxConfig.MusicFolder;
                Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Could not open the music folder: {ex.Message}");
            }
        }

        private static void AddButton(ModsMenuPopup menu, string label, Action onClick)
        {
            int before = GetChildCount(menu);
            menu.CreateButton(label, () => onClick());
            ResizeLastCreatedButton(menu, before);
        }

        private static void Spacer(ModsMenuPopup menu)
        {
            menu.CreateParagraph("<size=100%>\n </size>");
        }

        private static int GetChildCount(ModsMenuPopup menu)
        {
            return menu.objectParent != null ? menu.objectParent.childCount : 0;
        }

        private static void RefreshMenuLater(ModsMenuPopup menu)
        {
            MusicLoader.RunNextFrame(() => RefreshMenu(menu));
        }

        private static void RefreshMenu(ModsMenuPopup menu)
        {
            if (menu == null || menu.objectParent == null)
            {
                return;
            }

            for (int i = menu.objectParent.childCount - 1; i >= 0; i--)
            {
                var child = menu.objectParent.GetChild(i);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            AddJukeboxInfo(menu);
        }

        private static void ResizeLastCreatedButton(ModsMenuPopup menu, int childCountBefore)
        {
            if (menu == null || menu.objectParent == null)
            {
                return;
            }

            if (menu.objectParent.childCount <= childCountBefore)
            {
                return;
            }

            var buttonTransform = menu.objectParent.GetChild(menu.objectParent.childCount - 1);
            if (buttonTransform == null)
            {
                return;
            }

            var button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x * 1.1f, rect.sizeDelta.y * 1.45f);
            }

            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
            {
                label.fontSize *= 1.2f;
            }
        }
    }
}
