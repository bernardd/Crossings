using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;


namespace Crossings
{
    [HarmonyPatch]
    class RefreshJunctionData
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
            typeof(NetNode),
                    "RefreshJunctionData",
            new Type[] {
                typeof(ushort),
                typeof(int),
                typeof(ushort),
                typeof(Vector3),
                typeof(uint).MakeByRefType(),
                typeof(RenderManager.Instance).MakeByRefType()
            }
        );
        }

        static void Postfix(ref NetNode __instance, ref RenderManager.Instance data)
        {
            if ((__instance.m_flags & (NetNode.Flags)Crossings.CrossingFlag) != NetNode.Flags.None)
            {
                data.m_dataVector1.w = 0.01f;
            }
        }
    }
}