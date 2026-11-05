using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IPA.Logging;

namespace SongCoreJsonCacheFix
{
    public static class JsonCache
    {
        private const int Audio = 0;
        private const int TypeCount = 3;

        internal static Logger Log;
        internal static AsyncLocal<bool> AllowIO;
        internal static MethodInfo LevelMatchesMethod;
        internal static bool Installed;

        private static readonly ConditionalWeakTable<object, SlotList> Slots = new ConditionalWeakTable<object, SlotList>();

        public static bool AsyncPrefix(object __instance, object __0, object __1, object __2, object __3, ref Task<string> __result)
        {
            __result = ReadAsync(__instance, __0, __1, Convert.ToInt32(__2), (Func<Task<string>>)__3);
            return false;
        }

        public static bool SyncPrefix(object __instance, object __0, object __1, object __2, object __3, ref string __result)
        {
            object levelData = __0;
            object key = __1;
            int type = Convert.ToInt32(__2);
            var original = (Func<string>)__3;
            JsonCacheEntry cached = MatchingEntry(__instance, levelData, key, type);
            if (cached != null && cached.Data != null)
            {
                Log.Debug("Returning " + type + " JSON data from cache for request " + key);
                __result = cached.Data;
                return false;
            }
            JsonCacheEntry entry = CreateEntry(__instance, levelData, key, type);
            bool previous = AllowIO.Value;
            AllowIO.Value = true;
            try
            {
                __result = original();
                Store(__instance, type, entry, __result);
            }
            finally
            {
                AllowIO.Value = previous;
            }
            return false;
        }

        public static void AfterInvalidateLevel(object __instance)
        {
            if (Slots.TryGetValue(__instance, out SlotList slots))
            {
                Array.Clear(slots.Entries, 0, slots.Entries.Length);
            }
        }

        public static void AfterInvalidateDifficulty(object __instance)
        {
            if (Slots.TryGetValue(__instance, out SlotList slots))
            {
                Array.Clear(slots.Entries, 1, slots.Entries.Length - 1);
            }
        }

        private static async Task<string> ReadAsync(object cache, object levelData, object key, int type, Func<Task<string>> original)
        {
            JsonCacheEntry cached = MatchingEntry(cache, levelData, key, type);
            if (cached != null && cached.Data != null)
            {
                Log.Debug("Returning " + type + " JSON data from cache for request " + key);
                return cached.Data;
            }
            Task<string> cachedTask = cached != null ? cached.Task : null;
            if (cachedTask != null)
            {
                Log.Debug("Returning " + type + " JSON task from cache for request " + key);
                return await cachedTask;
            }
            JsonCacheEntry entry = CreateEntry(cache, levelData, key, type);
            bool previous = AllowIO.Value;
            AllowIO.Value = true;
            try
            {
                Task<string> originalTask = original();
                if (entry != null)
                {
                    entry.Task = originalTask;
                }
                string data = await originalTask;
                Store(cache, type, entry, data);
                return data;
            }
            finally
            {
                AllowIO.Value = previous;
                if (entry != null)
                {
                    entry.Task = null;
                }
            }
        }

        private static JsonCacheEntry MatchingEntry(object cache, object levelData, object key, int type)
        {
            JsonCacheEntry entry = List(cache).Entries[type];
            if (LevelMatches(cache, levelData) && entry != null && ReferenceEquals(entry.LevelData, levelData) && (type == Audio || (IsValid(key) && KeysEqual(entry.Key, key))))
            {
                return entry;
            }
            return null;
        }

        private static JsonCacheEntry CreateEntry(object cache, object levelData, object key, int type)
        {
            if (!LevelMatches(cache, levelData) || (type != Audio && !IsValid(key)))
            {
                return null;
            }
            var entry = new JsonCacheEntry(levelData, key);
            List(cache).Entries[type] = entry;
            return entry;
        }

        private static void Store(object cache, int type, JsonCacheEntry entry, string data)
        {
            SlotList slots = List(cache);
            if (entry != null && slots.Entries[type] == entry && LevelMatches(cache, entry.LevelData))
            {
                Log.Debug("Storing " + type + " JSON data in cache for request " + entry.Key);
                entry.Data = data;
            }
        }

        private static SlotList List(object cache)
        {
            return Slots.GetValue(cache, _ => new SlotList());
        }

        private static bool LevelMatches(object cache, object levelData)
        {
            return (bool)LevelMatchesMethod.Invoke(cache, new[] { levelData });
        }

        private static bool IsValid(object key)
        {
            if (key == null)
            {
                return false;
            }
            MethodInfo method = key.GetType().GetMethod("IsValid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null && (bool)method.Invoke(key, null);
        }

        private static bool KeysEqual(object stored, object requested)
        {
            if (stored == null || requested == null)
            {
                return false;
            }
            MethodInfo equality = stored.GetType().GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (equality != null)
            {
                return (bool)equality.Invoke(null, new[] { stored, requested });
            }
            return stored.Equals(requested);
        }

        private sealed class SlotList
        {
            internal readonly JsonCacheEntry[] Entries = new JsonCacheEntry[TypeCount];
        }

        private sealed class JsonCacheEntry
        {
            internal readonly object LevelData;
            internal readonly object Key;
            internal string Data;
            internal Task<string> Task;

            internal JsonCacheEntry(object levelData, object key)
            {
                LevelData = levelData;
                Key = key;
            }
        }
    }
}
