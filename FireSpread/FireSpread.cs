using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Math;
using ICities;
using UnityEngine;
using Random = System.Random;

namespace FireSpread
{
    public class FireSpread : ThreadingExtensionBase
    {
        private readonly Random _rand = new Random(Environment.TickCount);

        private ushort _updateIndex;

        public override void OnAfterSimulationTick()
        {
            DoFireSpread();
            base.OnAfterSimulationTick();
        }

        private static float GetBuildingFireSpreadChance(ref Building building)
        {
            var chance = ModSettings.Instance.BaseFireSpreadChance;
            if ((building.m_problems & Notification.Problem.WaterNotConnected) != Notification.Problem.None)
            {
                chance += ModSettings.Instance.NoWaterFireSpreadAdditional;
            }
            if ((building.m_problems & Notification.Problem.NoEducatedWorkers) != Notification.Problem.None)
            {
                chance += ModSettings.Instance.UneducatedFireSpreadAdditional;
            }
            if (building.Info.m_buildingAI as PowerPlantAI)
            {
                chance += ModSettings.Instance.PowerPlantFireSpreadAdditional;
            }
            else if (building.Info.m_buildingAI is IndustrialExtractorAI || building.Info.m_buildingAI is IndustrialBuildingAI)
            {
                chance += ModSettings.Instance.IndustrialFireSpreadAdditional;
            }
            return Math.Min(chance * ModSettings.Instance.FireSpreadModifier, 1.0f);
        }

        private static float DistanceSqr(ref Vector3 a, ref Vector3 b)
        {
            Vector3 vector3 = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
            return vector3.x * vector3.x + vector3.y * vector3.y + vector3.z * vector3.z;
        }

        private IEnumerable<ushort> GetNearestBuildings(Vector3 position, float radius)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            float num = Mathf.Min(position.x - radius, position.x + radius);
            float num2 = Mathf.Min(position.z - radius, position.y + radius);
            float num3 = Mathf.Max(position.x - radius, position.x + radius);
            float num4 = Mathf.Max(position.z - radius, position.y + radius);
            int num5 = Mathf.Max((int)(num / 64f + 135f), 0);
            int num6 = Mathf.Max((int)(num2 / 64f + 135f), 0);
            int num7 = Mathf.Min((int)(num3 / 64f + 135f), 269);
            int num8 = Mathf.Min((int)(num4 / 64f + 135f), 269);
            for (int i = num6; i < num8; i++)
            {
                for (int j = num5; j < num7; j++)
                {
                    int num9 = 0;
                    ushort num10 = instance.m_buildingGrid[i * 270 + j];
                    while (num10 != 0)
                    {
                        float num11 = DistanceSqr(ref instance.m_buildings.m_buffer[num10].m_position, ref position);
                        if (num11 < radius * radius)
                        {
                            yield return num10;
                        }
                        num10 = instance.m_buildings.m_buffer[num10].m_nextGridBuilding;
                        if (++num9 >= 49152)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private float GetSphereOfInfluence(ref Building building)
        {
            return (Mathf.Abs(building.Info.m_generatedInfo.m_max.x - building.Info.m_generatedInfo.m_min.x) + Mathf.Abs(building.Info.m_generatedInfo.m_max.z - building.Info.m_generatedInfo.m_min.z)) * 1.5f;
        }

        public void DoFireSpread()
        {
            if (!Singleton<BuildingManager>.exists || !Singleton<SimulationManager>.exists)
            {
                return;
            }
            var bm = Singleton<BuildingManager>.instance;
            if (bm.m_firesDisabled)
            {
                return;
            }
            if (!Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.FireDepartment))
            {
                return;
            }
            SimulationManager sim = Singleton<SimulationManager>.instance;
            if (sim == null)
            {
                return;
            }
            _updateIndex += 1;
            if (_updateIndex >= 1000)
            {
                _updateIndex = 0;
            }
            var firesHandledThisTick = 0;
            var i = _updateIndex;
            while (i < bm.m_buildings.m_buffer.Length)
            {
                if (!(bm.m_buildings.m_buffer[i].Info == null))
                {
                    if (bm.m_buildings.m_buffer[i].Info.m_placementMode == BuildingInfo.PlacementMode.Roadside)
                    {
                        if ((bm.m_buildings.m_buffer[i].m_problems & Notification.Problem.Fire) != Notification.Problem.None)
                        {
                            var influence = GetSphereOfInfluence(ref bm.m_buildings.m_buffer[i]);
                            double fireSpreadChance =
                                GetBuildingFireSpreadChance(ref bm.m_buildings.m_buffer[i]);
                            foreach (var nearbyId in GetNearestBuildings(bm.m_buildings.m_buffer[i].m_position, influence)
                                .Where(nearbyId => (bm.m_buildings.m_buffer[nearbyId].m_problems & Notification.Problem.Fire) == Notification.Problem.None)
                                .Where(nearbyId => _rand.NextDouble() < fireSpreadChance))
                            {
                                LightBuildingOnFire(nearbyId, ref bm.m_buildings.m_buffer[nearbyId]);
                            }
                            firesHandledThisTick++;
                            if (firesHandledThisTick >= 20)
                            {
                                break;
                            }
                        }
                    }
                }
                i += 1000;
            }
        }

        private static float RandomFloat()
        {
            return new Randomizer(Environment.TickCount).Int32(0, 2147483647) / 2.14748365E+09f;
        }

        private void LightBuildingOnFire(ushort buildingId, ref Building data)
        {
            var bm = Singleton<BuildingManager>.instance;
            if ((bm.m_buildings.m_buffer[buildingId].m_problems & Notification.Problem.Fire) !=
                Notification.Problem.None)
            {
                return;
            }
            int fireHazard;
            int fireSize;
            int fireTolerance;
            bm.m_buildings.m_buffer[buildingId].Info.m_buildingAI.GetFireParameters(buildingId, ref data, out fireHazard, out fireSize, out fireTolerance);
            if (fireHazard == 0 ||
                (data.m_flags & (Building.Flags.Completed | Building.Flags.Abandoned)) != Building.Flags.Completed ||
                data.m_fireIntensity != 0 || data.GetLastFrameData().m_fireDamage != 0)
            {
                return;
            }
            var waterLevel = Singleton<TerrainManager>.instance.WaterLevel(new Vector2(data.m_position.x, data.m_position.z));
            if (!(waterLevel <= data.m_position.y))
            {
                return;
            }
            var preDeactivateFlags = data.m_flags;
            data.m_fireIntensity = (byte)fireSize;
            data.Info.m_buildingAI.BuildingDeactivated(buildingId, ref data);
            var postDeactivateFlags = data.m_flags;
            Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingId, true);
            if (postDeactivateFlags != preDeactivateFlags)
            {
                Singleton<BuildingManager>.instance.UpdateFlags(buildingId, postDeactivateFlags ^ preDeactivateFlags);
            }
        }
    }
}
