﻿using HarmonyLib;
using KitchenData;
using KitchenMods;
using PreferenceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenCustomerGroupSpawner
{
    public class Main : IModInitializer
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.CustomerGroupSpawner";
        public const string MOD_NAME = "Customer Group Spawner";
        public const string MOD_VERSION = "0.1.4";

        internal static PreferenceSystemManager PrefManager;
        internal const string SPAWNER_ACTIVE_ID = "spawnerActive";
        internal const string MIN_GROUP_SIZE_ID = "minGroupSize";
        internal const string MAX_GROUP_SIZE_ID = "maxGroupSize";
        internal const string IS_CAT_ID = "isCat";
        internal const string SPAWN_INTERVAL_ID = "spawnInterval";
        internal const string GROUP_LIMIT_ID = "groupLimit";
        internal const string GROUP_TOTAL_ID = "groupTotal";
        internal const string CUSTOMER_TYPE_ID = "customerType";
        internal static bool CustomerTypeRegistered = false;
        public Main()
        {
        }

        public void PostActivate(Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        public void PreInject()
        {
            if (PrefManager == null)
            {
                CreatePreferences();
            }
        }

        public void PostInject() { }

        private int[] GenerateIntArray(string input, out string[] stringRepresentation, int[] addValuesBefore = null, int[] addValuesAfter = null, string prefix = "", string postfix = "")
        {
            List<string> stringOutput = new List<string>();
            List<int> output = new List<int>();
            string[] ranges = input.Split(',');
            foreach (string range in ranges)
            {
                string[] extents = range.Split('|');
                int min = Convert.ToInt32(extents[0]);
                int max;
                int step;
                switch (extents.Length)
                {
                    case 1:
                        output.Add(min);
                        stringOutput.Add($"{prefix}{min}{postfix}");
                        continue;
                    case 2:
                        max = Convert.ToInt32(extents[1]);
                        step = 1;
                        break;
                    case 3:
                        max = Convert.ToInt32(extents[1]);
                        step = Convert.ToInt32(extents[2]);
                        break;
                    default:
                        continue;
                }
                for (int i = min; i <= max; i += step)
                {
                    output.Add(i);
                    stringOutput.Add($"{prefix}{i}{postfix}");
                }
            }
            stringRepresentation = stringOutput.ToArray();
            if (addValuesBefore == null)
                addValuesBefore = new int[0];
            if (addValuesAfter == null)
                addValuesAfter = new int[0];
            return addValuesBefore.AddRangeToArray(output.ToArray()).AddRangeToArray(addValuesAfter);
        }

        private void CreatePreferences()
        {
            string[] strings;

            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);
            PrefManager
                .AddLabel("Customer Group Spawner")
                .AddOption<int>(
                    SPAWNER_ACTIVE_ID,
                    -1,
                    new int[] { -1, 0, 1 },
                    new string[] { "Disabled", "Practice Mode Only", "Practice Mode and Day" })
                .AddLabel("Spawn Interval (deciseconds)")
                .AddOption<int>(
                    SPAWN_INTERVAL_ID,
                    30,
                    GenerateIntArray("5|100", out strings),  //Decisecond
                    strings)
                .AddLabel("Min Group Size")
                .AddOption<int>(
                    MIN_GROUP_SIZE_ID,
                    1,
                    GenerateIntArray("1|80", out strings),
                    strings)
                .AddLabel("Max Group Size")
                .AddOption<int>(
                    MAX_GROUP_SIZE_ID,
                    2,
                    GenerateIntArray("1|80", out strings),
                    strings)
                .AddLabel("Max Queue Length")
                .AddOption<int>(
                    GROUP_LIMIT_ID,
                    50,
                    GenerateIntArray("5|1000|5", out strings, new int[] { -1 }, null),
                    new string[] { $"Uncapped" }.AddRangeToArray(strings))

                .AddLabel("Total Number of Groups")
                .AddOption<int>(
                    GROUP_TOTAL_ID,
                    50,
                    GenerateIntArray("0|1000|5", out strings, new int[] { -1 }, null),
                    new string[] { $"Uncapped" }.AddRangeToArray(strings));

            if (TryGetCustomerTypes(out int[] ids, out string[] names))
            {
                PrefManager
                    .AddLabel("Customer Type")
                    .AddOption<int>(
                        CUSTOMER_TYPE_ID,
                        ids[0],
                        ids,
                        names);
                CustomerTypeRegistered = true;
            }

            PrefManager
                .AddLabel("Model")
                .AddOption<int>(
                    IS_CAT_ID,
                    -1,
                    new int[] { -1, 0, 1 },
                    new string[] { "Random", "Morphman", "Kitty Cat" });

            PrefManager.AddSpacer();
            PrefManager.AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        private bool TryGetCustomerTypes(out int[] ids, out string[] names)
        {
            try
            {
                IEnumerable<CustomerType> customerTypes = GameData.Main.Get<CustomerType>();
                ids = customerTypes.Select(x => x.ID).ToArray();
                names = customerTypes.Select(x => x.name).ToArray();
                return true;
            }
            catch (Exception e)
            {
                Main.LogError($"TryGetCustomerTypes error.\n{e.Message}\n{e.StackTrace}");
                ids = new int[0];
                names = new string[0];
                return false;
            }
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
