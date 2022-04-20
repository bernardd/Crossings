using HarmonyLib;

namespace Crossings
{
  [HarmonyPatch(typeof(RoadBaseAI))]
	[HarmonyPatch(nameof(RoadBaseAI.UpdateNodeFlags))]
	class UpdateNodeFlags
	{
		static bool Prefix(ref RoadBaseAI __instance, ref NetNode data)
		{
			var andFlags = 
				Crossings.CrossingFlag 
				| NetNode.Flags.TrafficLights
				| NetNode.Flags.CustomTrafficLights
				;
			var checkFlags =
				Crossings.CrossingFlag 
				| NetNode.Flags.TrafficLights
				;

			// Crossing & TrafficLights, but no CustomTrafficLights
			if((data.m_flags & andFlags) == checkFlags)
			{
				data.m_flags |= NetNode.Flags.CustomTrafficLights;
			}

			// return true -> execute UpdateNodeFlags
			return true;
		}
	}
}
