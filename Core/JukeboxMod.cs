using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Silk;
using Logger = Silk.Logger;

namespace Jukebox
{
    [SilkMod(ModName, new string[] { "AeonS" }, "0.2.0", "0.7.0", ModName, 1)]
    public class JukeboxMod : SilkMod
    {
        // Silk shows this name in its mod list and passes it to Announcer.ModsPopup as the menu
        // title. It is also the settings file name: Silk\Config\Mods\SpiderHeckJukebox.yaml.
        public const string ModName = "SpiderHeckJukebox";
        public const string ModId = ModName;
        private const string HarmonyId = "com.AeonS.SpiderHeckJukebox";

        public static JukeboxMod Instance { get; private set; }

        private static string _version;
        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    _version = $"{version.Major}.{version.Minor}.{version.Build}";
                }
                return _version;
            }
        }

        public override void Initialize()
        {
            Instance = this;
            Logger.LogInfo($"Initializing {ModName} v{Version}...");

            SetupConfiguration();

            MusicLoader.EnsureRunning();

            new Harmony(HarmonyId).PatchAll();
            Logger.LogInfo("Harmony patches applied.");
        }

        private void SetupConfiguration()
        {
            var defaultConfig = new Dictionary<string, object>
            {
                { "Enabled", true },
                { "MusicFolder", "" },
                { "DisabledNativeTracks", "" },
                { "DisabledCustomTracks", "" },
            };

            Config.LoadModConfig(ModId, defaultConfig);
            Logger.LogInfo("Configuration loaded.");
        }

        public override void Unload()
        {
            Logger.LogInfo($"Unloading {ModName}...");

            new Harmony(HarmonyId).UnpatchSelf();
            MusicLoader.Shutdown();
            Instance = null;
        }
    }
}
