using HarmonyLib;
using UnityEngine;

namespace Crossings
{
	[HarmonyPatch(typeof(NetNode))]
	[HarmonyPatch(nameof(NetNode.CalculateNode))]
	class CalculateNode
	{
		static void Postfix(ref NetNode __instance)
		{
			if ((__instance.m_flags & NetNode.Flags.Outside) != NetNode.Flags.None)
			{
				// Do nothing
			}
			else if ((__instance.m_flags & NetNode.Flags.Junction) != NetNode.Flags.None)
			{
				// This is already a junction - clear out any crossing flag we might have set
				__instance.m_flags &= ~(NetNode.Flags)Crossings.CrossingFlag;
			}
			else if ((__instance.m_flags & (NetNode.Flags)Crossings.CrossingFlag) != NetNode.Flags.None)
			{
				__instance.m_flags |= NetNode.Flags.Junction;
				__instance.m_flags &= ~(NetNode.Flags.Moveable | NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
			}
		}
	}
}