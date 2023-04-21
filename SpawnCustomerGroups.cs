using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace KitchenCustomerGroupSpawner
{
    public enum SpawnerState
    {
        Disabled,
        PracticeOnly,
        Enabled
    }

    public struct SSpawnerSettings : IComponentData
    {
        public SpawnerState State;
        public int TotalGroups;
        public int GroupsSpawnedThisDay;
        public int MaxQueueLength;
        public float SpawnInterval;
        public bool FixedCustomerModel;
        public bool IsCat;
        public int MinGroupSize;
        public int MaxGroupSize;
        public int CustomerType;
    }

    internal class CustomerGroupSpawner : RestaurantSystem
    {

        EntityQuery Queuers;
        MethodInfo mNewGroup;
        EntityQuery SpawnerSettings;

        float nextSpawnTime = 0f;
        float spawnInterval = 3f;

        bool _isInit = false;
        bool _systemFound = false;
        float _intervalRemainingTime = 0f;
        CreateCustomerGroup _createCustomerGroupSystem;

        protected override void Initialise()
        {
            base.Initialise();
            Queuers = GetEntityQuery(typeof(CQueuePosition));
            SpawnerSettings = GetEntityQuery(typeof(SSpawnerSettings));
            mNewGroup = ReflectionUtils.GetMethod<CreateCustomerGroup>("NewGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                EntityManager.DestroyEntity(SpawnerSettings);
                _isInit = false;
                return;
            }

            if (!TryGetSingleton(out SSpawnerSettings spawnerSettings))
            {
                SpawnerState state;
                switch (Main.PrefManager.Get<int>(Main.SPAWNER_ACTIVE_ID))
                {
                    case 0:
                        state = SpawnerState.PracticeOnly;
                        break;
                    case 1:
                        state = SpawnerState.Enabled;
                        break;
                    case -1:
                    default:
                        state = SpawnerState.Disabled;
                        break;

                }
                spawnerSettings = new SSpawnerSettings()
                {
                    State = state,
                    GroupsSpawnedThisDay = 0,
                    TotalGroups = Main.PrefManager.Get<int>(Main.GROUP_TOTAL_ID),
                    MaxQueueLength = Main.PrefManager.Get<int>(Main.GROUP_LIMIT_ID),
                    SpawnInterval = Main.PrefManager.Get<int>(Main.SPAWN_INTERVAL_ID) / 10f,
                    FixedCustomerModel = Main.PrefManager.Get<int>(Main.IS_CAT_ID) > -1,
                    IsCat = Main.PrefManager.Get<int>(Main.IS_CAT_ID) == 1,
                    MinGroupSize = Main.PrefManager.Get<int>(Main.MIN_GROUP_SIZE_ID),
                    MaxGroupSize = Main.PrefManager.Get<int>(Main.MAX_GROUP_SIZE_ID),
                    CustomerType = Main.PrefManager.Get<int>(Main.CUSTOMER_TYPE_ID)
                };
            }

            SpawnerState spawnerState = spawnerSettings.State;
            if (spawnerSettings.TotalGroups > -1 && spawnerSettings.GroupsSpawnedThisDay >= spawnerSettings.TotalGroups)
            {
                spawnerState = SpawnerState.Disabled;
            }

            switch (spawnerState)
            {
                case SpawnerState.Disabled:
                    _isInit = false;
                    return;
                case SpawnerState.PracticeOnly:
                    if (!Has<SPracticeMode>())
                        return;
                    break;
                case SpawnerState.Enabled:
                default:
                    break;
            }

            float currentTime = base.Time.TotalTime;

            if (!_isInit)
            {
                try
                {
                    _createCustomerGroupSystem = World.GetExistingSystem<CreateCustomerGroup>();
                    _systemFound = true;
                    Main.LogInfo("Spawner enabled!");
                    SetNextSpawnTime(spawnerSettings.SpawnInterval);
                    Main.LogInfo($"Spawning New Group every {spawnInterval} seconds.");
                }
                catch (NullReferenceException)
                {
                    _systemFound = false;
                    Main.LogInfo("Cannot find instance of Kitchen.CreateCustomerGroup system");
                }
                _isInit = true;
            }

            if (!Require(out SGameTime time) || time.IsPaused)
            {
                if (_intervalRemainingTime == 0f)
                {
                    spawnInterval = spawnerSettings.SpawnInterval;
                    _intervalRemainingTime = Mathf.Clamp(nextSpawnTime - currentTime, 0f, spawnInterval);
                }
                return;
            }

            if (_intervalRemainingTime > 0f)
            {
                nextSpawnTime = currentTime + _intervalRemainingTime;
                _intervalRemainingTime = 0f;
                return;
            }

            if (_systemFound && (spawnerSettings.MaxQueueLength < 1 || (Queuers.CalculateEntityCount() < spawnerSettings.MaxQueueLength)))
            {
                UnityEngine.Random.InitState(Mathf.RoundToInt(currentTime * 1000) % 1000);

                if (nextSpawnTime < currentTime)
                {
                    spawnerSettings.GroupsSpawnedThisDay++;
                    if (!spawnerSettings.FixedCustomerModel)
                        spawnerSettings.IsCat = UnityEngine.Random.Range(0, 2) == 1;
                    Set(spawnerSettings);

                    Main.LogWarning($"Spawning Group {spawnerSettings.GroupsSpawnedThisDay}/{spawnerSettings.TotalGroups}");
                    SetNextSpawnTime(spawnerSettings.SpawnInterval);
                    CreateCustomerGroup(spawnerSettings.MinGroupSize, spawnerSettings.MaxGroupSize, spawnerSettings.CustomerType, spawnerSettings.IsCat);
                }
            }
        }

        private void CreateCustomerGroup(int minGroupSize, int maxGroupSize, int customerTypeId, bool? isCat = null)
        {
            if (maxGroupSize < 1)
                maxGroupSize = 1;
            minGroupSize = Mathf.Clamp(minGroupSize, 1, maxGroupSize);

            bool found = false;
            if (Main.CustomerTypeRegistered)
            {
                if (GameData.Main.TryGet<CustomerType>(customerTypeId, out var customerType))
                {
                    found = true;
                    mNewGroup.Invoke(_createCustomerGroupSystem, new object[] { customerType, UnityEngine.Random.Range(minGroupSize, maxGroupSize + 1), isCat });
                }
            }
            
            if (!found)
            {
                mNewGroup.Invoke(_createCustomerGroupSystem, new object[] { CustomerTypeReferences.GenericCustomer, UnityEngine.Random.Range(minGroupSize, maxGroupSize + 1), isCat });
            }
        }

        private void SetNextSpawnTime(float spawnInterval)
        {
            nextSpawnTime = base.Time.TotalTime + spawnInterval;
        }
    }
}
