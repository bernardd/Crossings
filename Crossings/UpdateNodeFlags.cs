using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace Crossings
{
	[HarmonyPatch(typeof(RoadBaseAI))]
	[HarmonyPatch(nameof(RoadBaseAI.UpdateNodeFlags))]
	class UpdateNodeFlags
	{
		static void Postfix(ref RoadBaseAI __instance, ref NetNode data)
		{
			bool wantTrafficLights = __instance.WantTrafficLights();
			NetManager netManager = Singleton<NetManager>.instance;

			//Debug.Log("isCrossing: " + isCrossing);
			//Debug.Log("wantTrafficLights: " + wantTrafficLights);

			if (!wantTrafficLights)
			{
				for (int i = 0; i < 8; i++)
				{
					ushort segment = data.GetSegment(i);
					if (segment != 0)
					{
						NetInfo info = netManager.m_segments.m_buffer[(int)segment].Info;
						if (info != null)
						{
							if ((info.m_vehicleTypes & VehicleInfo.VehicleType.Train) != VehicleInfo.VehicleType.None)
							{
								// No crossings allowed where there's a railway intersecting
								return;
							}

							if (info.m_netAI.WantTrafficLights())
							{
								wantTrafficLights = true;
							}
						}
					}
				}
			}

			bool isCrossing = (data.m_flags & (NetNode.Flags)Crossings.CrossingFlag) != NetNode.Flags.None;

			if (wantTrafficLights && isCrossing)
			{
				data.m_flags |= NetNode.Flags.TrafficLights;
			}
		}
	}
}
