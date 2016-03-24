using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using System;

namespace Crossings
{
	public class CrossingsNode
	{
		public static int CrossingFlag = (int)NetNode.Flags.Electricity << 1; // Largest item in the enum moved one bit
		static bool hooked = false;
		static private Dictionary<MethodInfo, RedirectCallsState> redirects = new Dictionary<MethodInfo, RedirectCallsState>();

		public static void Hook()
		{
			if (hooked) {
				foreach (var kvp in redirects)
				{
					RedirectionHelper.RevertRedirect(kvp.Key, kvp.Value);
				}
				redirects.Clear ();
			}

			var allFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
			var methods = typeof(NetNode).GetMethods (allFlags); //.Single(c => c.Name == "RenderInstance" && c.GetParameters().Length == 3); // No idea why this doesn't work. Do it the old fashioned way:
			foreach (MethodInfo m in methods) {
				if (m.Name == "RefreshJunctionData" && m.GetParameters ().Length == 6)
				{
					Debug.Log ("[Crossings] Hooking RefreshJunctionData");
					redirects.Add (m, RedirectionHelper.RedirectCalls (m, typeof(CrossingsNode).GetMethod ("RefreshJunctionData", allFlags)));
				}
				else
				{
					Debug.Log ("[Crossings] Not hooking: " + m.Name + "/" + m.GetParameters().Length);
				}
						
			}

			Debug.Log ("[Crossings] Hooking UpdateNodeFlags");
			MethodInfo method = typeof(RoadBaseAI).GetMethod("UpdateNodeFlags", allFlags);
			redirects.Add (method, RedirectionHelper.RedirectCalls (method, typeof(CrossingsNode).GetMethod ("UpdateNodeFlags", allFlags)));

			Debug.Log ("[Crossings] Hooking CalculateNode");
			method = typeof(NetNode).GetMethod("CalculateNode", allFlags);
			redirects.Add (method, RedirectionHelper.RedirectCalls (method, typeof(CrossingsNode).GetMethod ("CalculateNode", allFlags)));

			hooked = true;

		}

		// NetNode override
		public void CalculateNode(ushort nodeID)
		{
			Debug.Log ("[Crossings] CalculateNode");
			// MODIFICATION //
			NetNode thisNode = NetManager.instance.m_nodes.m_buffer [nodeID];
			// END MODIFICATON //

			if (thisNode.m_flags == NetNode.Flags.None)
			{
				return;
			}

			// MODIFICATION //
			bool isCrossing = (thisNode.m_flags & (NetNode.Flags)CrossingFlag) != NetNode.Flags.None;
			if (isCrossing)
				Debug.Log ("[Crossings] GOT A CROSSING NODE! " + nodeID);
			// END MODIFICATON //
			
			NetManager instance = Singleton<NetManager>.instance;
			Vector3 vector = Vector3.zero;
			int segmentCount = 0; // num
			int connections = 0; // num2
			bool hasSegments = false; // flag1
			bool makeMiddle = false; // flag2
			bool makeBend = false; // flag4
			bool makeJunction = false; // flag4
			bool makeCrossing = false; // MODIFICATION
			bool isBend = false;  // flag5
			bool isStraight = false; // flag6
			bool flag7 = false;
			bool notRamp = true; // flag8
			bool flag9 = Singleton<TerrainManager>.instance.HasDetailMapping(thisNode.m_position);
			NetInfo with = null;
			bool flag10 = false;
			bool flag11 = false;
			NetInfo netInfo = null;
			float num3 = -1E+07f;
			for (int i = 0; i < 8; i++)
			{
				ushort segment = thisNode.GetSegment(i);
				if (segment != 0)
				{
					NetInfo info = instance.m_segments.m_buffer[(int)segment].Info;
					float nodeInfoPriority = info.m_netAI.GetNodeInfoPriority(segment, ref instance.m_segments.m_buffer[(int)segment]);
					if (nodeInfoPriority > num3)
					{
						netInfo = info;
						num3 = nodeInfoPriority;
					}
				}
			}
			if (netInfo == null)
			{
				netInfo = thisNode.Info;
			}
			if (netInfo != thisNode.Info)
			{
				thisNode.Info = netInfo;
				Singleton<NetManager>.instance.UpdateNodeColors(nodeID);
				if (!netInfo.m_canDisable)
				{
					thisNode.m_flags &= ~NetNode.Flags.Disabled;
				}
			}
			bool startNodeIsFirst = false; // flag12

			// For each attached segment
			for (int j = 0; j < 8; j++)
			{
				ushort segment2 = thisNode.GetSegment(j);
				if (segment2 != 0)
				{
					segmentCount++;
					ushort startNode = instance.m_segments.m_buffer[(int)segment2].m_startNode;
					ushort endNode = instance.m_segments.m_buffer[(int)segment2].m_endNode;
					Vector3 startDirection = instance.m_segments.m_buffer[(int)segment2].m_startDirection;
					Vector3 endDirection = instance.m_segments.m_buffer[(int)segment2].m_endDirection;
					bool isStartNode = nodeID == startNode;
					Vector3 dirFromNode = (!isStartNode) ? endDirection : startDirection;
					NetInfo info2 = instance.m_segments.m_buffer[(int)segment2].Info;
					ItemClass connectionClass = info2.GetConnectionClass();
					bool flag14;
					bool flag15;
					if (isStartNode == ((instance.m_segments.m_buffer[(int)segment2].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
					{
						flag14 = info2.m_hasBackwardVehicleLanes;
						flag15 = info2.m_hasForwardVehicleLanes;
					}
					else {
						flag14 = info2.m_hasForwardVehicleLanes;
						flag15 = info2.m_hasBackwardVehicleLanes;
					}

					// For each segment after the one we're looking at
					for (int k = j + 1; k < 8; k++)
					{
						ushort segment3 = thisNode.GetSegment(k);
						if (segment3 != 0)
						{
							NetInfo info3 = instance.m_segments.m_buffer[(int)segment3].Info;
							ItemClass connectionClass2 = info3.GetConnectionClass();

							// If both segments are the same type
							if (connectionClass2.m_service == connectionClass.m_service)
							{
								bool isKStartNode = nodeID == instance.m_segments.m_buffer[(int)segment3].m_startNode; // flag16
								Vector3 dirKFromNode = (!isKStartNode) ? instance.m_segments.m_buffer[(int)segment3].m_endDirection : instance.m_segments.m_buffer[(int)segment3].m_startDirection; // vector3
								float num4 = dirFromNode.x * dirKFromNode.x + dirFromNode.z * dirKFromNode.z;
								float num5 = 0.01f - Mathf.Min(info2.m_maxTurnAngleCos, info3.m_maxTurnAngleCos);
								if (num4 < num5)
								{
									if ((info2.m_requireDirectRenderers && (info2.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info2.m_nodeConnectGroups & info3.m_connectGroup) != NetInfo.ConnectGroup.None)) || (info3.m_requireDirectRenderers && (info3.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info3.m_nodeConnectGroups & info2.m_connectGroup) != NetInfo.ConnectGroup.None)))
									{
										connections++;
									}
								}
								else
								{
									makeJunction = true;
								}
							}
							else
							{
								makeJunction = true;
							}
						}
					}
					if (instance.m_nodes.m_buffer[(int)startNode].m_elevation != instance.m_nodes.m_buffer[(int)endNode].m_elevation)
					{
						notRamp = false;
					}
					Vector3 position = instance.m_nodes.m_buffer[(int)startNode].m_position;
					Vector3 position2 = instance.m_nodes.m_buffer[(int)endNode].m_position;
					if (isStartNode)
					{
						flag9 = (flag9 && Singleton<TerrainManager>.instance.HasDetailMapping(position2));
					}
					else
					{
						flag9 = (flag9 && Singleton<TerrainManager>.instance.HasDetailMapping(position));
					}
					if (NetSegment.IsStraight(position, startDirection, position2, endDirection))
					{
						isStraight = true;
					}
					else
					{
						isBend = true;
					}
					if (segmentCount == 1)
					{
						startNodeIsFirst = isStartNode;
						vector = dirFromNode;
						hasSegments = true;
					}
					else if (segmentCount == 2 && info2.IsCombatible(with) && info2.IsCombatible(netInfo) && flag14 == flag11 && flag15 == flag10)
					{
						float num6 = vector.x * dirFromNode.x + vector.z * dirFromNode.z;
						if (num6 < -0.999f)
						{
							makeMiddle = true;
						}
						else
						{
							makeBend = true;
						}
						flag7 = (isStartNode != startNodeIsFirst);
					}
					else
					{
						makeJunction = true;
					}
					with = info2;
					flag10 = flag14;
					flag11 = flag15;
				}
			}
			if (!netInfo.m_enableMiddleNodes & makeMiddle)
			{
				makeBend = true;
			}
			if (!netInfo.m_enableBendingNodes & makeBend)
			{
				makeJunction = true;
			}
			if (netInfo.m_requireContinuous && (thisNode.m_flags & NetNode.Flags.Untouchable) != NetNode.Flags.None)
			{
				makeJunction = true;
			}
			if (netInfo.m_requireContinuous && !flag7 && (makeMiddle || makeBend))
			{
				makeJunction = true;
			}
			// MODIFICATION //
			if (isCrossing)
			{
				makeCrossing = true;
			}
			// END MODIFICATION //
				
			NetNode.Flags flags = thisNode.m_flags & ~(NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable);
			if ((flags & NetNode.Flags.Outside) != NetNode.Flags.None)
			{
				thisNode.m_flags = flags;
			}
			else if (makeJunction)
			{
				// MODIFICATION //
				thisNode.m_flags = (flags | NetNode.Flags.Junction) & ~(NetNode.Flags)CrossingFlag;
			}
			else if (makeCrossing) {
				thisNode.m_flags = flags | NetNode.Flags.Junction;
			}
			else if (makeBend)
			{
				thisNode.m_flags = (flags | NetNode.Flags.Bend) & ~(NetNode.Flags)CrossingFlag;
				// END MODIFICATION //
			}
			else if (makeMiddle)
			{
				if ((!isBend || !isStraight) && (thisNode.m_flags & (NetNode.Flags.Untouchable | NetNode.Flags.Double)) == NetNode.Flags.None && notRamp && netInfo.m_netAI.CanModify())
				{
					flags |= NetNode.Flags.Moveable;
				}
				thisNode.m_flags = (flags | NetNode.Flags.Middle);
			}
			else if (hasSegments)
			{
				if ((thisNode.m_flags & NetNode.Flags.Untouchable) == NetNode.Flags.None && notRamp && netInfo.m_netAI.CanModify() && netInfo.m_enableMiddleNodes)
				{
					flags |= NetNode.Flags.Moveable;
				}
				thisNode.m_flags = (flags | NetNode.Flags.End) & ~(NetNode.Flags)CrossingFlag;
			}


			thisNode.m_heightOffset = (byte)((!flag9) ? 64 : 0);
			thisNode.m_connectCount = (byte)connections;
			BuildingInfo newBuilding;
			float heightOffset;
			netInfo.m_netAI.GetNodeBuilding(nodeID, ref thisNode, out newBuilding, out heightOffset);
			thisNode.UpdateBuilding(nodeID, newBuilding, heightOffset);

			// Copy back to the canonical node buffer
			NetManager.instance.m_nodes.m_buffer [nodeID] = thisNode;
		}

		// RoadBaseAI override
		public void UpdateNodeFlags(ushort nodeID, ref NetNode data)
		{
			Debug.Log ("[Crossings] UpdateNodeFlags");
			// MODIFICATION //
			NetInfo thisInfo = PrefabCollection<NetInfo>.GetPrefab(data.m_infoIndex);

			RoadBaseAI thisAI = thisInfo.m_netAI as RoadBaseAI;

			bool isCrossing = (data.m_flags & (NetNode.Flags)CrossingFlag) != NetNode.Flags.None;

			// Luckily NetAI.UpdateNodeFlags() is a noop for now, so we don't have to fnangle a way to call this
			// base.UpdateNodeFlags(nodeID, ref data);
			// [VN, 02/18/2016] But we could also move this method into a new class that inherits from RoadBaseAI.
			// END MODIFICATON //

			NetNode.Flags flags = data.m_flags;
			uint levelsSeen = 0u; // num
			int levelsSeenCount = 0; // num2
			NetManager instance = Singleton<NetManager>.instance;
			int incomingSegments = 0; // num3
			int incomingLanes = 0; // num4
			int attachedSegmentsWithLanes = 0; // num5
			bool wantTrafficLights = thisAI.WantTrafficLights(); // flag
			bool flag2 = false;
			int roadLinks = 0; // num6
			int trainLinks = 0; // num7

			// For each segment
			for (int i = 0; i < 8; i++)
			{
				ushort segment = data.GetSegment(i);
				if (segment != 0)
				{
					NetInfo info = instance.m_segments.m_buffer[(int)segment].Info;
					if (info != null)
					{
						uint levelBit = 1u << (int)info.m_class.m_level; // num8
						if ((levelsSeen & levelBit) == 0u)
						{
							levelsSeen |= levelBit;
							levelsSeenCount++;
						}
						if (info.m_netAI.WantTrafficLights())
						{
							wantTrafficLights = true;
						}
						if ((info.m_vehicleTypes & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None != ((thisInfo.m_vehicleTypes & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None))
						{
							flag2 = true;
						}
						int forwardLanes = 0; // num9
						int backLanes = 0; // num10
						instance.m_segments.m_buffer[(int)segment].CountLanes(segment, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Tram, ref forwardLanes, ref backLanes);
						if (instance.m_segments.m_buffer[(int)segment].m_endNode == nodeID)
						{
							if (forwardLanes != 0)
							{
								incomingSegments++;
								incomingLanes += forwardLanes;
							}
						}
						else if (backLanes != 0)
						{
							incomingSegments++;
							incomingLanes += backLanes;
						}
						if (forwardLanes != 0 || backLanes != 0)
						{
							attachedSegmentsWithLanes++;
						}
						if (info.m_class.m_service == ItemClass.Service.Road)
						{
							roadLinks++;
						}
						else if (info.m_class.m_service == ItemClass.Service.PublicTransport)
						{
							trainLinks++;
						}
					}
				}
			}
			if (roadLinks >= 2 && trainLinks >= 2)
			{
				flags |= (NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights);
			}
			else
			{
				if (levelsSeenCount >= 2 || flag2)
				{
					flags |= NetNode.Flags.Transition;
				}
				else
				{
					flags &= ~NetNode.Flags.Transition;
				}
				// MODIFICATON //
				// Logic for traffic light setting
				if (wantTrafficLights && (isCrossing || incomingSegments > 2 || (incomingSegments >= 2 && attachedSegmentsWithLanes >= 3 && incomingLanes > 6)) && (flags & NetNode.Flags.Junction) != NetNode.Flags.None)
				// END MODIFICATION //
				{
					flags |= NetNode.Flags.TrafficLights;
				}
				else
				{
					flags &= ~NetNode.Flags.TrafficLights;
				}
			}
			data.m_flags = flags;
		}

		// NetNode override
		private void RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
		{
			Debug.Log ("[Crossings] RefreshJunctionData6");
			// MODIFICATION //
			NetNode thisNode = NetManager.instance.m_nodes.m_buffer [nodeID];
			// END MODIFICATION //
			NetManager instance = Singleton<NetManager>.instance;
			data.m_position = thisNode.m_position;
			data.m_rotation = Quaternion.identity;
			data.m_initialized = true;
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			Vector3 zero3 = Vector3.zero;
			Vector3 zero4 = Vector3.zero;
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			Vector3 a = Vector3.zero;
			Vector3 a2 = Vector3.zero;
			Vector3 zero5 = Vector3.zero;
			Vector3 zero6 = Vector3.zero;
			Vector3 zero7 = Vector3.zero;
			Vector3 zero8 = Vector3.zero;
			NetSegment netSegment = instance.m_segments.m_buffer[(int)nodeSegment];
			NetInfo info = netSegment.Info;
			float vScale = info.m_netAI.GetVScale();
			ItemClass connectionClass = info.GetConnectionClass();
			Vector3 vector3 = (nodeID != netSegment.m_startNode) ? netSegment.m_endDirection : netSegment.m_startDirection;
			float num = -4f;
			float num2 = -4f;
			ushort num3 = 0;
			ushort num4 = 0;
			for (int i = 0; i < 8; i++)
			{
				ushort segment = thisNode.GetSegment(i);
				if (segment != 0 && segment != nodeSegment)
				{
					NetInfo info2 = instance.m_segments.m_buffer[(int)segment].Info;
					ItemClass connectionClass2 = info2.GetConnectionClass();
					if (connectionClass.m_service == connectionClass2.m_service)
					{
						NetSegment netSegment2 = instance.m_segments.m_buffer[(int)segment];
						Vector3 vector4 = (nodeID != netSegment2.m_startNode) ? netSegment2.m_endDirection : netSegment2.m_startDirection;
						float num5 = vector3.x * vector4.x + vector3.z * vector4.z;
						if (vector4.z * vector3.x - vector4.x * vector3.z < 0f)
						{
							if (num5 > num)
							{
								num = num5;
								num3 = segment;
							}
							num5 = -2f - num5;
							if (num5 > num2)
							{
								num2 = num5;
								num4 = segment;
							}
						}
						else
						{
							if (num5 > num2)
							{
								num2 = num5;
								num4 = segment;
							}
							num5 = -2f - num5;
							if (num5 > num)
							{
								num = num5;
								num3 = segment;
							}
						}
					}
				}
			}
			bool start = netSegment.m_startNode == nodeID;
			bool flag;
			netSegment.CalculateCorner(nodeSegment, true, start, false, out zero, out zero3, out flag);
			netSegment.CalculateCorner(nodeSegment, true, start, true, out zero2, out zero4, out flag);
			if (num3 != 0 && num4 != 0)
			{

				float num6 = info.m_pavementWidth / info.m_halfWidth * 0.5f;
				float y = 1f;
				if (num3 != 0)
				{
					NetSegment netSegment3 = instance.m_segments.m_buffer[(int)num3];
					NetInfo info3 = netSegment3.Info;
					start = (netSegment3.m_startNode == nodeID);
					netSegment3.CalculateCorner(num3, true, start, true, out vector, out a, out flag);
					netSegment3.CalculateCorner(num3, true, start, false, out vector2, out a2, out flag);
					float num7 = info3.m_pavementWidth / info3.m_halfWidth * 0.5f;
					num6 = (num6 + num7) * 0.5f;
					y = 2f * info.m_halfWidth / (info.m_halfWidth + info3.m_halfWidth);
				}
				float num8 = info.m_pavementWidth / info.m_halfWidth * 0.5f;
				float w = 1f;
				if (num4 != 0)
				{
					NetSegment netSegment4 = instance.m_segments.m_buffer[(int)num4];
					NetInfo info4 = netSegment4.Info;
					start = (netSegment4.m_startNode == nodeID);
					netSegment4.CalculateCorner(num4, true, start, true, out zero5, out zero7, out flag);
					netSegment4.CalculateCorner(num4, true, start, false, out zero6, out zero8, out flag);
					float num9 = info4.m_pavementWidth / info4.m_halfWidth * 0.5f;
					num8 = (num8 + num9) * 0.5f;
					w = 2f * info.m_halfWidth / (info.m_halfWidth + info4.m_halfWidth);
				}
				Vector3 vector5;
				Vector3 vector6;
				NetSegment.CalculateMiddlePoints(zero, -zero3, vector, -a, true, true, out vector5, out vector6);
				Vector3 vector7;
				Vector3 vector8;
				NetSegment.CalculateMiddlePoints(zero2, -zero4, vector2, -a2, true, true, out vector7, out vector8);
				Vector3 vector9;
				Vector3 vector10;
				NetSegment.CalculateMiddlePoints(zero, -zero3, zero5, -zero7, true, true, out vector9, out vector10);
				Vector3 vector11;
				Vector3 vector12;
				NetSegment.CalculateMiddlePoints(zero2, -zero4, zero6, -zero8, true, true, out vector11, out vector12);

				data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector5, vector6, vector, zero, vector5, vector6, vector, thisNode.m_position, vScale);
				data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector7, vector8, vector2, zero2, vector7, vector8, vector2, thisNode.m_position, vScale);
				data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(zero, vector9, vector10, zero5, zero, vector9, vector10, zero5, thisNode.m_position, vScale);
				data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(zero2, vector11, vector12, zero6, zero2, vector11, vector12, zero6, thisNode.m_position, vScale);
				data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
				data.m_dataVector1 = centerPos - data.m_position;

				// **** MODIFICATION **** //
				if ((thisNode.m_flags & (NetNode.Flags)CrossingFlag) == NetNode.Flags.None)
					// Original code
					data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
				else
					data.m_dataVector1.w = 0.01f;
				// **** END MODIFICATION **** //

				data.m_dataVector2 = new Vector4(num6, y, num8, w);
			}
			else
			{
				centerPos.x = (zero.x + zero2.x) * 0.5f;
				centerPos.z = (zero.z + zero2.z) * 0.5f;
				vector = zero2;
				vector2 = zero;
				a = zero4;
				a2 = zero3;
				float d = Mathf.Min(info.m_halfWidth * 1.33333337f, 16f);
				Vector3 vector13 = zero - zero3 * d;
				Vector3 vector14 = vector - a * d;
				Vector3 vector15 = zero2 - zero4 * d;
				Vector3 vector16 = vector2 - a2 * d;
				Vector3 vector17 = zero + zero3 * d;
				Vector3 vector18 = vector + a * d;
				Vector3 vector19 = zero2 + zero4 * d;
				Vector3 vector20 = vector2 + a2 * d;
				data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector13, vector14, vector, zero, vector13, vector14, vector, thisNode.m_position, vScale);
				data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector19, vector20, vector2, zero2, vector19, vector20, vector2, thisNode.m_position, vScale);
				data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(zero, vector17, vector18, vector, zero, vector17, vector18, vector, thisNode.m_position, vScale);
				data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(zero2, vector15, vector16, vector2, zero2, vector15, vector16, vector2, thisNode.m_position, vScale);
				data.m_dataMatrix0.SetRow(3, data.m_dataMatrix0.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
				data.m_extraData.m_dataMatrix2.SetRow(3, data.m_extraData.m_dataMatrix2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
				data.m_extraData.m_dataMatrix3.SetRow(3, data.m_extraData.m_dataMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
				data.m_dataMatrix1.SetRow(3, data.m_dataMatrix1.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
				data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
				data.m_dataVector1 = centerPos - data.m_position;

				// **** MODIFICATION **** //
				if ((thisNode.m_flags & (NetNode.Flags)CrossingFlag) == NetNode.Flags.None)
					// Original code
					data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
				else
					data.m_dataVector1.w = 0.01f;
				// **** END MODIFICATION **** //
				
				data.m_dataVector2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
			}
			Vector4 colorLocation;
			Vector4 vector21;
			// **** MODIFICATION **** //
			if (NetNode.BlendJunction(nodeID) || (thisNode.m_flags & (NetNode.Flags)CrossingFlag) == NetNode.Flags.None)
			// **** END MODIFICATION **** //
			{
				colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
				vector21 = colorLocation;
			}
			else {
				colorLocation = RenderManager.GetColorLocation((uint)(49152 + nodeSegment));
				vector21 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
			}
			data.m_extraData.m_dataVector4 = new Vector4(colorLocation.x, colorLocation.y, vector21.x, vector21.y);
			data.m_dataInt0 = segmentIndex;
			data.m_dataColor0 = info.m_color;
			data.m_dataColor0.a = 0f;
			data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
			if (info.m_requireSurfaceMaps)
			{
				Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector3);
			}
			instanceIndex = (uint)data.m_nextInstance;
		}
	}
}

