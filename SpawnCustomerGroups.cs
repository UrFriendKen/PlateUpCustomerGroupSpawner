using Kitchen;
using KitchenLib.Utils;
using System;
using System.Media;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace KitchenCustomerGroupSpawner
{
    internal class CustomerGroupSpawner : RestaurantSystem
    {
        EntityQuery Queuers;
        MethodInfo mNewGroup;

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
            mNewGroup = ReflectionUtils.GetMethod<CreateCustomerGroup>("NewGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                _isInit = false;
                return;
            }

            int spawnerState = Main.PrefManager.Get<int>(Main.SPAWNER_ACTIVE_ID);
            switch (spawnerState)
            {
                case -1:
                    _isInit = false;
                    return;
                case 0:
                    if (!Has<SPracticeMode>())
                        return;
                    break;
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
                    SetNextSpawnTime();
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
                    spawnInterval = Main.PrefManager.Get<int>(Main.SPAWN_INTERVAL_ID) / 10f;
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

            if (_systemFound && (Queuers.CalculateEntityCount() < Main.PrefManager.Get<int>(Main.GROUP_LIMIT_ID)))
            {
                UnityEngine.Random.InitState(Mathf.RoundToInt(currentTime * 1000) % 1000);

                if (nextSpawnTime < currentTime)
                {
                    spawnInterval = Main.PrefManager.Get<int>(Main.SPAWN_INTERVAL_ID) / 10f;
                    SetNextSpawnTime();
                    if (Main.PrefManager.Get<int>(Main.IS_CAT_ID) == -1)
                    {
                        CreateCustomerGroup(Main.PrefManager.Get<int>(Main.MIN_GROUP_SIZE_ID), Main.PrefManager.Get<int>(Main.MAX_GROUP_SIZE_ID));
                    }
                    else
                    {
                        CreateCustomerGroup(Main.PrefManager.Get<int>(Main.MIN_GROUP_SIZE_ID), Main.PrefManager.Get<int>(Main.MAX_GROUP_SIZE_ID), Main.PrefManager.Get<int>(Main.IS_CAT_ID) == 1);
                    }
                }
            }
        }

        private void CreateCustomerGroup(int minGroupSize, int maxGroupSize, bool? isCat = null)
        {
            if (maxGroupSize < 1)
                maxGroupSize = 1;
            minGroupSize = Mathf.Clamp(minGroupSize, 1, maxGroupSize);

            if (!isCat.HasValue)
            {
                isCat = UnityEngine.Random.Range(0, 2) == 1;
            }

            mNewGroup.Invoke(_createCustomerGroupSystem, new object[] { UnityEngine.Random.Range(minGroupSize, maxGroupSize + 1), isCat });
        }

        private void SetNextSpawnTime()
        {
            nextSpawnTime = base.Time.TotalTime + (Main.PrefManager.Get<int>(Main.SPAWN_INTERVAL_ID) / 10f);
        }
    }
}
