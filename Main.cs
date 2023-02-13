using HarmonyLib;
using KitchenCustomerGroupSpawner.Preferences;
using KitchenLib;
using KitchenLib.Event;
using KitchenMods;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenCustomerGroupSpawner

{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.CustomerGroupSpawner";
        public const string MOD_NAME = "Customer Group Spawner";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.3";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        internal static PreferencesManager PrefManager;
        internal const string SPAWNER_ACTIVE_ID = "spawnerActive";
        internal const string MIN_GROUP_SIZE_ID = "minGroupSize";
        internal const string MAX_GROUP_SIZE_ID = "maxGroupSize";
        internal const string IS_CAT_ID = "isCat";
        internal const string SPAWN_INTERVAL_ID = "spawnInterval";
        internal const string GROUP_LIMIT_ID = "groupLimit";

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnPostActivate(Mod mod)
        {
            PrefManager = new PreferencesManager(MOD_GUID, MOD_NAME);
            CreatePreferences();
        }

        public override void PostActivate(Mod mod)
        {
            base.PostActivate(mod);
        }

        private void CreatePreferences()
        {
            PrefManager.AddLabel("Customer Group Spawner");
            PrefManager.AddOption<int>(SPAWNER_ACTIVE_ID, "", -1, new int[] { -1, 0, 1 }, new string[] { "Disabled", "Practice Mode Only", "Practice Mode and Day" });

            PrefManager.AddLabel("Spawn Interval (seconds)");
            List<int> values = new List<int>();
            List<string> strings = new List<string>();
            for (int i = 5; i < 100 + 1; i++)
            {
                values.Add(i);
                strings.Add($"{i / 10}.{i % 10}");
            }
            PrefManager.AddOption<int>(SPAWN_INTERVAL_ID, "Spawn Interval", 30, values.ToArray(), strings.ToArray()); //Decisecond
            
            values.Clear();
            strings.Clear();
            for (int i = 1; i < 80 + 1; i++)
            {
                values.Add(i);
                strings.Add($"{i}");
            }
            PrefManager.AddLabel("Min Group Size");
            PrefManager.AddOption<int>(MIN_GROUP_SIZE_ID, "Min Group Size", 1, values.ToArray(), strings.ToArray());
            PrefManager.AddLabel("Max Group Size");
            PrefManager.AddOption<int>(MAX_GROUP_SIZE_ID, "Max Group Size", 2, values.ToArray(), strings.ToArray());

            values.Clear();
            strings.Clear();
            for (int i = 5; i < 1000 + 1; i += 5)
            {
                values.Add(i);
                strings.Add($"{i}");
            }
            PrefManager.AddLabel("Number of Groups Limit");
            PrefManager.AddOption<int>(GROUP_LIMIT_ID, "Number of Groups Limit", 50, values.ToArray(), strings.ToArray());

            PrefManager.AddLabel("Customer Type");
            PrefManager.AddOption<int>(IS_CAT_ID, "Customer Type", -1, new int[] { -1, 0, 1 }, new string[] { "Random", "Morphman", "Kitty Cat" });



            
            PrefManager.AddSpacer();
            PrefManager.AddSpacer();

            PrefManager.RegisterMenu(PreferencesManager.MenuType.PauseMenu);
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
