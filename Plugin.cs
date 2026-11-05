using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;

namespace SongCoreJsonCacheFix
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public sealed class Plugin
    {
        private readonly Harmony _harmony = new Harmony("satodoge.songcore.jsoncachefix");
        private readonly Logger _log;

        [Init]
        public Plugin(Logger log, Config config)
        {
            _log = log;
            JsonCache.Log = log;
            PluginConfig.Instance = config.Generated<PluginConfig>();
        }

        [OnStart]
        public void Start()
        {
            StatusBanner.RegisterSettings();
            Assembly songCore = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "SongCore")
                {
                    songCore = assembly;
                    break;
                }
            }
            if (songCore == null)
            {
                _log.Error("SongCore assembly not found");
                return;
            }
            if (songCore.GetName().Version.ToString() != "3.16.0.0")
            {
                _log.Error("Unsupported SongCore version: " + songCore.GetName().Version + ". This patch targets 3.16.0.0.");
                return;
            }
            Type cache = songCore.GetType("SongCore.Hooks.BeatmapLevelCache.BeatmapLevelCache");
            Type io = songCore.GetType("SongCore.Hooks.BeatmapLevelCache.IOBlacklistHooks");
            if (cache == null || io == null)
            {
                _log.Error("SongCore internal types not found");
                return;
            }
            PropertyInfo allow = io.GetProperty("AllowIO", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (allow == null || !(allow.GetValue(null, null) is AsyncLocal<bool> allowIO))
            {
                _log.Error("SongCore AllowIO is unavailable; no patches installed");
                return;
            }
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo asyncMethod = cache.GetMethod("GetJsonDataAsync", flags);
            MethodInfo syncMethod = cache.GetMethod("GetJsonData", flags);
            MethodInfo invalidateLevel = cache.GetMethod("InvalidateLevel", flags);
            MethodInfo invalidateDifficulty = cache.GetMethod("InvalidateDifficulty", flags);
            MethodInfo levelMatches = cache.GetMethod("LevelMatches", flags);
            if (asyncMethod == null || syncMethod == null || invalidateLevel == null || invalidateDifficulty == null || levelMatches == null)
            {
                _log.Error("Cache methods not found");
                return;
            }
            JsonCache.AllowIO = allowIO;
            JsonCache.LevelMatchesMethod = levelMatches;
            try
            {
                _harmony.Patch(asyncMethod, prefix: new HarmonyMethod(typeof(JsonCache).GetMethod(nameof(JsonCache.AsyncPrefix))));
                _harmony.Patch(syncMethod, prefix: new HarmonyMethod(typeof(JsonCache).GetMethod(nameof(JsonCache.SyncPrefix))));
                _harmony.Patch(invalidateLevel, postfix: new HarmonyMethod(typeof(JsonCache).GetMethod(nameof(JsonCache.AfterInvalidateLevel))));
                _harmony.Patch(invalidateDifficulty, postfix: new HarmonyMethod(typeof(JsonCache).GetMethod(nameof(JsonCache.AfterInvalidateDifficulty))));
                StatusBanner.Install(_harmony);
                JsonCache.Installed = true;
                _log.Info("SongCore 3.16.0 JSON cache now keeps each cached string and in-flight task with its BeatmapKey.");
            }
            catch (Exception e)
            {
                _harmony.UnpatchSelf();
                JsonCache.Installed = false;
                _log.Error("Unable to patch SongCore: " + e);
            }
        }

        [OnExit]
        public void Exit()
        {
            JsonCache.Installed = false;
            _harmony.UnpatchSelf();
        }
    }
}
