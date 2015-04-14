using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;

namespace Crossings
{
	public class CrossingsNode
	{
		public static int CrossingFlag = (int)NetNode.Flags.OneWayIn << 1; // Largest item in the enum moved one bit
		static bool hooked = false;
		static private Dictionary<MethodInfo, RedirectCallsState> redirects = new Dictionary<MethodInfo, RedirectCallsState>();

		public CrossingsNode ()
		{
		}

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
	/*		var methods = typeof(NetNode).GetMethods (allFlags); //.Single(c => c.Name == "RenderInstance" && c.GetParameters().Length == 3); // No idea why this doesn't work. Do it the old fashioned way:
			foreach (MethodInfo m in methods) {
				if (m.Name == "RenderInstance" && m.GetParameters().Length == 3) {
					redirects.Add (m, RedirectionHelper.RedirectCalls (m, typeof(CrossingsNode).GetMethod ("RenderInstance", allFlags)));
					break;
				}
		//		if (m.Name == "RenderInstance" && m.GetParameters ().Length == 7) {
		//			redirects.Add (m, RedirectionHelper.RedirectCalls (m, typeof(CrossingsNode).GetMethod ("RenderInstanceInner", allFlags)));
		//		}
			}
*/
			MethodInfo method = typeof(RoadBaseAI).GetMethod("UpdateNodeFlags", allFlags);
			redirects.Add (method, RedirectionHelper.RedirectCalls (method, typeof(CrossingsNode).GetMethod ("UpdateNodeFlags", allFlags)));

			method = typeof(NetNode).GetMethod("CalculateNode", allFlags);
			redirects.Add (method, RedirectionHelper.RedirectCalls (method, typeof(CrossingsNode).GetMethod ("CalculateNode", allFlags)));

			hooked = true;

		}

		// NetNode override
		public void CalculateNode(ushort nodeID)
		{
			NetNode thisNode = NetManager.instance.m_nodes.m_buffer [nodeID];

			if (thisNode.m_flags == NetNode.Flags.None)
			{
				return;
			}

			bool isCrossing = (thisNode.m_flags & (NetNode.Flags)CrossingFlag) != NetNode.Flags.None;
			if (isCrossing)
				Debug.Log ("GOT A CROSSING NODE! " + nodeID);
			
			NetManager instance = Singleton<NetManager>.instance;
			Vector3 vector = Vector3.zero;
			int segmentCount = 0;
			int connections = 0;
			bool hasSegments = false;
			bool makeMiddle = false;
			bool makeBend = false;
			bool makeJunction = false;
			bool isBend = false;
			bool isStraight = false;
			bool flag7 = false;
			bool notRamp = true;
			bool flag9 = Singleton<TerrainManager>.instance.HasDetailMapping(thisNode.m_position);
			NetInfo with = null;
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
			}
			bool startNodeIsFirst = false;

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
								bool isKStartNode = nodeID == instance.m_segments.m_buffer[(int)segment3].m_startNode;
								Vector3 dirKFromNode = (!isKStartNode) ? instance.m_segments.m_buffer[(int)segment3].m_endDirection : instance.m_segments.m_buffer[(int)segment3].m_startDirection;
								float num4 = dirFromNode.x * dirKFromNode.x + dirFromNode.z * dirKFromNode.z;
								float num5 = 0.01f - Mathf.Min(info2.m_maxTurnAngleCos, info3.m_maxTurnAngleCos);
								if (num4 < num5)
								{
									connections++;
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
					else if (segmentCount == 2 && info2.IsCombatible(with) && info2.IsCombatible(netInfo))
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
			if (isCrossing)
			{
				makeJunction = true;
			}
			NetNode.Flags flags = thisNode.m_flags & ~(NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable);
			if ((flags & NetNode.Flags.Outside) != NetNode.Flags.None)
			{
				thisNode.m_flags = flags;
			}
			else if (makeJunction)
			{
				thisNode.m_flags = (flags | NetNode.Flags.Junction);
			}
			else if (makeBend)
			{
				thisNode.m_flags = (flags | NetNode.Flags.Bend);
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
				thisNode.m_flags = (flags | NetNode.Flags.End);
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
			NetInfo thisInfo = PrefabCollection<NetInfo>.GetPrefab(data.m_infoIndex);

			RoadBaseAI thisAI = thisInfo.m_netAI as RoadBaseAI;

			bool isCrossing = (data.m_flags & (NetNode.Flags)CrossingFlag) != NetNode.Flags.None;
			if (isCrossing)
				Debug.Log ("GOT A CROSSING NODE! " + nodeID);

			// Luckily NetAI.UpdateNodeFlags() is a noop for now, so we don't have to fnangle a way to call this
			// base.UpdateNodeFlags(nodeID, ref data);

			NetNode.Flags flags = data.m_flags;
			uint levelsSeen = 0u;
			int levelsSeenCount = 0;
			NetManager instance = Singleton<NetManager>.instance;
			int incomingSegments = 0;
			int incomingLanes = 0;
			int attachedSegmentsWithLanes = 0;
			bool wantTrafficLights = thisAI.WantTrafficLights();
			// For each segment
			for (int i = 0; i < 8; i++)
			{
				ushort segment = data.GetSegment(i);
				if (segment != 0)
				{
					NetInfo info = instance.m_segments.m_buffer[(int)segment].Info;
					uint levelBit = 1u << (int)info.m_class.m_level;
					if ((levelsSeen & levelBit) == 0u)
					{
						levelsSeen |= levelBit;
						levelsSeenCount++;
					}
					if (info.m_netAI.WantTrafficLights())
					{
						wantTrafficLights = true;
					}
					int forwardLanes = 0;
					int backLanes = 0;
					instance.m_segments.m_buffer[(int)segment].CountLanes(segment, NetInfo.LaneType.Vehicle, VehicleInfo.VehicleType.Car, ref forwardLanes, ref backLanes);
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
				}
			}
			if (levelsSeenCount >= 2)
			{
				flags |= NetNode.Flags.Transition;
			}
			else
			{
				flags &= ~NetNode.Flags.Transition;
			}
			// Logic for traffic light setting
			if (wantTrafficLights && (isCrossing || incomingSegments > 2 || (incomingSegments >= 2 && attachedSegmentsWithLanes >= 3 && incomingLanes > 6)) && (flags & NetNode.Flags.Junction) != NetNode.Flags.None)
			{
				flags |= NetNode.Flags.TrafficLights;
			}
			else
			{
				flags &= ~NetNode.Flags.TrafficLights;
			}
			data.m_flags = flags;
		}

		#if false
		public void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, int layerMask)
		{
			//if (nodeID > 32768) return;
			//Debug.Log ("NodeID: " + nodeID);
			NetNode thisNode = NetManager.instance.m_nodes.m_buffer [nodeID];
			if (thisNode.m_flags == NetNode.Flags.None) {
				return;
			}

			NetInfo info = thisNode.Info;
			if (!cameraInfo.Intersect(thisNode.m_bounds))
			{
				return;
			}
			if (thisNode.m_problems != Notification.Problem.None && (layerMask & 1 << Singleton<NotificationManager>.instance.m_notificationLayer) != 0)
			{
				Vector3 position = thisNode.m_position;
				position.y += Mathf.Max(5f, info.m_maxHeight);
				Notification.RenderInstance(cameraInfo, thisNode.m_problems, position, 1f);
			}
				
			if ((layerMask & info.m_netLayers) == 0)
			{
				return;
			}
			if ((thisNode.m_flags & (NetNode.Flags.End | NetNode.Flags.Bend | NetNode.Flags.Junction)) == NetNode.Flags.None)
			{
				return;
			}
			if ((thisNode.m_flags & NetNode.Flags.Bend) != NetNode.Flags.None)
			{
				if (info.m_segments == null || info.m_segments.Length == 0)
				{
					return;
				}
			}
			else if (info.m_nodes == null || info.m_nodes.Length == 0)
			{
				return;
			}
			//uint count = (uint)CalculateRendererCount(ref node, info);
			MethodInfo dynMethod = thisNode.GetType().GetMethod("CalculateRendererCount",
				BindingFlags.NonPublic | BindingFlags.Instance);
			uint count = (uint)(int)dynMethod.Invoke(thisNode, new object[] {info});

			RenderManager instance = Singleton<RenderManager>.instance;
			uint num;
			if (instance.RequireInstance(65536u + (uint)nodeID, count, out num))
			{
				int num2 = 0;
				while (num != 65535u)
				{
					RenderInstanceInner(ref thisNode, cameraInfo, nodeID, info, num2, thisNode.m_flags, ref num, ref instance.m_instances[(int)((UIntPtr)num)]);
					if (++num2 > 36)
					{
	//					CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}

			// Copy the node back when we're done rendering
			NetManager.instance.m_nodes.m_buffer [nodeID] = thisNode;
		}
			
		private void RenderInstanceInner(ref NetNode thisNode, RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
		{
			if (data.m_dirty)
			{
				data.m_dirty = false;
				if (iter == 0)
				{
					if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None)
					{
						//thisNode.RefreshJunctionData(nodeID, info, instanceIndex);
					/*
					   MethodInfo[] dynMethods = thisNode.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
						foreach (MethodInfo m in dynMethods) {
							if (m.Name == "RefreshJunctionData" && m.GetParameters ().Length == 3) {
								m.Invoke (thisNode, new object[] { nodeID, info, instanceIndex });
								break;
							}
						}
						*/
						RefreshJunctionData (ref thisNode, nodeID, info, instanceIndex);
					}
					else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None)
					{
						//thisNode.RefreshBendData(nodeID, info, instanceIndex, ref data);
						MethodInfo dynMethod = thisNode.GetType().GetMethod("RefreshBendData",
							BindingFlags.NonPublic | BindingFlags.Instance);
						object[] p = new object[] { nodeID, info, instanceIndex, data };
						dynMethod.Invoke(thisNode, p);
						data = (RenderManager.Instance)p [3];
					}
					else if ((flags & NetNode.Flags.End) != NetNode.Flags.None)
					{
						//thisNode.RefreshEndData(nodeID, info, instanceIndex, ref data);
						MethodInfo dynMethod = thisNode.GetType().GetMethod("RefreshEndData",
							BindingFlags.NonPublic | BindingFlags.Instance);
						object[] p = new object[] { nodeID, info, instanceIndex, data };
						dynMethod.Invoke(thisNode, p);
						data = (RenderManager.Instance)p [3];
					}
				}
			}
			if (data.m_initialized)
			{
				if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None)
				{
					if ((data.m_dataInt0 & 8) != 0)
					{
						ushort segment = thisNode.GetSegment(data.m_dataInt0 & 7);
						if (segment != 0)
						{
							NetManager instance = Singleton<NetManager>.instance;
							info = instance.m_segments.m_buffer[(int)segment].Info;
							for (int i = 0; i < info.m_nodes.Length; i++)
							{
								NetInfo.Node node = info.m_nodes[i];
								if (node.CheckFlags(flags) && node.m_directConnect)
								{
									if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance))
									{
										instance.m_materialBlock.Clear();
										instance.m_materialBlock.AddMatrix(instance.ID_LeftMatrix, data.m_dataMatrix0);
										instance.m_materialBlock.AddMatrix(instance.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
										instance.m_materialBlock.AddVector(instance.ID_MeshScale, data.m_dataVector0);
										instance.m_materialBlock.AddVector(instance.ID_ObjectIndex, data.m_dataVector3);
										instance.m_materialBlock.AddColor(instance.ID_Color, data.m_dataColor0);
										if (info.m_requireSurfaceMaps && data.m_dataTexture1 != null)
										{
											instance.m_materialBlock.AddTexture(instance.ID_SurfaceTexA, data.m_dataTexture0);
											instance.m_materialBlock.AddTexture(instance.ID_SurfaceTexB, data.m_dataTexture1);
											instance.m_materialBlock.AddVector(instance.ID_SurfaceMapping, data.m_dataVector1);
										}
										NetManager expr_1F9_cp_0 = instance;
										expr_1F9_cp_0.m_drawCallData.m_defaultCalls = expr_1F9_cp_0.m_drawCallData.m_defaultCalls + 1;
										Graphics.DrawMesh(node.m_nodeMesh, data.m_position, data.m_rotation, node.m_nodeMaterial, node.m_layer, null, 0, instance.m_materialBlock);

									}
									else
									{
										if (info.m_requireSurfaceMaps && data.m_dataTexture0 != node.m_surfaceTexA)
										{
											if (node.m_combinedCount != 0)
											{
												NetNode.RenderLod(cameraInfo, info, node);
											}
											node.m_surfaceTexA = data.m_dataTexture0;
											node.m_surfaceTexB = data.m_dataTexture1;
											node.m_surfaceMapping = data.m_dataVector1;
										}
										node.m_leftMatrices[node.m_combinedCount] = data.m_dataMatrix0;
										node.m_rightMatrices[node.m_combinedCount] = data.m_extraData.m_dataMatrix2;
										node.m_meshScales[node.m_combinedCount] = data.m_dataVector0;
										node.m_objectIndices[node.m_combinedCount] = data.m_dataVector3;
										node.m_meshLocations[node.m_combinedCount] = data.m_position;
										node.m_lodMin = Vector3.Min(node.m_lodMin, data.m_position);
										node.m_lodMax = Vector3.Max(node.m_lodMax, data.m_position);
										if (++node.m_combinedCount == node.m_leftMatrices.Length)
										{
											NetNode.RenderLod(cameraInfo, info, node);
										}
									}
								}
							}
						}
					}
					else
					{
						ushort segment2 = thisNode.GetSegment(data.m_dataInt0 & 7);
						if (segment2 != 0)
						{
							NetManager instance2 = Singleton<NetManager>.instance;
							info = instance2.m_segments.m_buffer[(int)segment2].Info;
							for (int j = 0; j < info.m_nodes.Length; j++)
							{
								NetInfo.Node node2 = info.m_nodes[j];
								if (node2.CheckFlags(flags) && !node2.m_directConnect)
								{
									if (cameraInfo.CheckRenderDistance(data.m_position, node2.m_lodRenderDistance))
									{
										instance2.m_materialBlock.Clear();
										instance2.m_materialBlock.AddMatrix(instance2.ID_LeftMatrix, data.m_dataMatrix0);
										instance2.m_materialBlock.AddMatrix(instance2.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
										instance2.m_materialBlock.AddMatrix(instance2.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
										instance2.m_materialBlock.AddMatrix(instance2.ID_RightMatrixB, data.m_dataMatrix1);
										instance2.m_materialBlock.AddVector(instance2.ID_MeshScale, data.m_dataVector0);
										instance2.m_materialBlock.AddVector(instance2.ID_CenterPos, data.m_dataVector1);
										instance2.m_materialBlock.AddVector(instance2.ID_SideScale, data.m_dataVector2);
										instance2.m_materialBlock.AddVector(instance2.ID_ObjectIndex, data.m_extraData.m_dataVector4);
										instance2.m_materialBlock.AddColor(instance2.ID_Color, data.m_dataColor0);
										if (info.m_requireSurfaceMaps && data.m_dataTexture1 != null)
										{
											instance2.m_materialBlock.AddTexture(instance2.ID_SurfaceTexA, data.m_dataTexture0);
											instance2.m_materialBlock.AddTexture(instance2.ID_SurfaceTexB, data.m_dataTexture1);
											instance2.m_materialBlock.AddVector(instance2.ID_SurfaceMapping, data.m_dataVector3);
										}
										NetManager expr_594_cp_0 = instance2;
										expr_594_cp_0.m_drawCallData.m_defaultCalls = expr_594_cp_0.m_drawCallData.m_defaultCalls + 1;
										Graphics.DrawMesh (node2.m_nodeMesh, data.m_position, data.m_rotation, node2.m_nodeMaterial, node2.m_layer, null, 0, instance2.m_materialBlock);
									}
									else
									{
										if (info.m_requireSurfaceMaps && data.m_dataTexture0 != node2.m_surfaceTexA)
										{
											if (node2.m_combinedCount != 0)
											{
												NetNode.RenderLod(cameraInfo, info, node2);
											}
											node2.m_surfaceTexA = data.m_dataTexture0;
											node2.m_surfaceTexB = data.m_dataTexture1;
											node2.m_surfaceMapping = data.m_dataVector3;
										}
										node2.m_leftMatrices[node2.m_combinedCount] = data.m_dataMatrix0;
										node2.m_leftMatricesB[node2.m_combinedCount] = data.m_extraData.m_dataMatrix3;
										node2.m_rightMatrices[node2.m_combinedCount] = data.m_extraData.m_dataMatrix2;
										node2.m_rightMatricesB[node2.m_combinedCount] = data.m_dataMatrix1;
										node2.m_meshScales[node2.m_combinedCount] = data.m_dataVector0;
										node2.m_centerPositions[node2.m_combinedCount] = data.m_dataVector1;
										node2.m_sideScales[node2.m_combinedCount] = data.m_dataVector2;
										node2.m_objectIndices[node2.m_combinedCount] = data.m_extraData.m_dataVector4;
										node2.m_meshLocations[node2.m_combinedCount] = data.m_position;
										node2.m_lodMin = Vector3.Min(node2.m_lodMin, data.m_position);
										node2.m_lodMax = Vector3.Max(node2.m_lodMax, data.m_position);
										if (++node2.m_combinedCount == node2.m_leftMatrices.Length)
										{
											NetNode.RenderLod(cameraInfo, info, node2);
										}
									}
								}
							}
						}
					}
				}
				// TODO: Delegate below code back to original NetNode
				else if ((flags & NetNode.Flags.End) != NetNode.Flags.None)
				{
					NetManager instance3 = Singleton<NetManager>.instance;
					for (int k = 0; k < info.m_nodes.Length; k++)
					{
						NetInfo.Node node3 = info.m_nodes[k];
						if (node3.CheckFlags(flags) && !node3.m_directConnect)
						{
							if (cameraInfo.CheckRenderDistance(data.m_position, node3.m_lodRenderDistance))
							{
								instance3.m_materialBlock.Clear();
								instance3.m_materialBlock.AddMatrix(instance3.ID_LeftMatrix, data.m_dataMatrix0);
								instance3.m_materialBlock.AddMatrix(instance3.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
								instance3.m_materialBlock.AddMatrix(instance3.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
								instance3.m_materialBlock.AddMatrix(instance3.ID_RightMatrixB, data.m_dataMatrix1);
								instance3.m_materialBlock.AddVector(instance3.ID_MeshScale, data.m_dataVector0);
								instance3.m_materialBlock.AddVector(instance3.ID_CenterPos, data.m_dataVector1);
								instance3.m_materialBlock.AddVector(instance3.ID_SideScale, data.m_dataVector2);
								instance3.m_materialBlock.AddVector(instance3.ID_ObjectIndex, data.m_extraData.m_dataVector4);
								instance3.m_materialBlock.AddColor(instance3.ID_Color, data.m_dataColor0);
								if (info.m_requireSurfaceMaps && data.m_dataTexture1 != null)
								{
									instance3.m_materialBlock.AddTexture(instance3.ID_SurfaceTexA, data.m_dataTexture0);
									instance3.m_materialBlock.AddTexture(instance3.ID_SurfaceTexB, data.m_dataTexture1);
									instance3.m_materialBlock.AddVector(instance3.ID_SurfaceMapping, data.m_dataVector3);
								}
								NetManager expr_9AB_cp_0 = instance3;
								expr_9AB_cp_0.m_drawCallData.m_defaultCalls = expr_9AB_cp_0.m_drawCallData.m_defaultCalls + 1;
								Graphics.DrawMesh(node3.m_nodeMesh, data.m_position, data.m_rotation, node3.m_nodeMaterial, node3.m_layer, null, 0, instance3.m_materialBlock);
							}
							else
							{
								if (info.m_requireSurfaceMaps && data.m_dataTexture0 != node3.m_surfaceTexA)
								{
									if (node3.m_combinedCount != 0)
									{
										NetNode.RenderLod(cameraInfo, info, node3);
									}
									node3.m_surfaceTexA = data.m_dataTexture0;
									node3.m_surfaceTexB = data.m_dataTexture1;
									node3.m_surfaceMapping = data.m_dataVector3;
								}
								node3.m_leftMatrices[node3.m_combinedCount] = data.m_dataMatrix0;
								node3.m_leftMatricesB[node3.m_combinedCount] = data.m_extraData.m_dataMatrix3;
								node3.m_rightMatrices[node3.m_combinedCount] = data.m_extraData.m_dataMatrix2;
								node3.m_rightMatricesB[node3.m_combinedCount] = data.m_dataMatrix1;
								node3.m_meshScales[node3.m_combinedCount] = data.m_dataVector0;
								node3.m_centerPositions[node3.m_combinedCount] = data.m_dataVector1;
								node3.m_sideScales[node3.m_combinedCount] = data.m_dataVector2;
								node3.m_objectIndices[node3.m_combinedCount] = data.m_extraData.m_dataVector4;
								node3.m_meshLocations[node3.m_combinedCount] = data.m_position;
								node3.m_lodMin = Vector3.Min(node3.m_lodMin, data.m_position);
								node3.m_lodMax = Vector3.Max(node3.m_lodMax, data.m_position);
								if (++node3.m_combinedCount == node3.m_leftMatrices.Length)
								{
									NetNode.RenderLod(cameraInfo, info, node3);
								}
							}
						}
					}
				}
				else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None)
				{
					NetManager instance4 = Singleton<NetManager>.instance;
					for (int l = 0; l < info.m_segments.Length; l++)
					{
						NetInfo.Segment segment3 = info.m_segments[l];
						bool flag;
						if (segment3.CheckFlags(NetSegment.Flags.None, out flag))
						{
							if (cameraInfo.CheckRenderDistance(data.m_position, segment3.m_lodRenderDistance))
							{
								instance4.m_materialBlock.Clear();
								instance4.m_materialBlock.AddMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
								instance4.m_materialBlock.AddMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
								instance4.m_materialBlock.AddVector(instance4.ID_MeshScale, data.m_dataVector0);
								instance4.m_materialBlock.AddVector(instance4.ID_ObjectIndex, data.m_dataVector3);
								instance4.m_materialBlock.AddColor(instance4.ID_Color, data.m_dataColor0);
								if (info.m_requireSurfaceMaps && data.m_dataTexture1 != null)
								{
									instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
									instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
									instance4.m_materialBlock.AddVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
								}
								NetManager expr_D45_cp_0 = instance4;
								expr_D45_cp_0.m_drawCallData.m_defaultCalls = expr_D45_cp_0.m_drawCallData.m_defaultCalls + 1;
								Graphics.DrawMesh(segment3.m_segmentMesh, data.m_position, data.m_rotation, segment3.m_segmentMaterial, segment3.m_layer, null, 0, instance4.m_materialBlock);
							}
							else
							{
								if (info.m_requireSurfaceMaps && data.m_dataTexture0 != segment3.m_surfaceTexA)
								{
									if (segment3.m_combinedCount != 0)
									{
										NetSegment.RenderLod(cameraInfo, info, segment3);
									}
									segment3.m_surfaceTexA = data.m_dataTexture0;
									segment3.m_surfaceTexB = data.m_dataTexture1;
									segment3.m_surfaceMapping = data.m_dataVector1;
								}
								segment3.m_leftMatrices[segment3.m_combinedCount] = data.m_dataMatrix0;
								segment3.m_rightMatrices[segment3.m_combinedCount] = data.m_extraData.m_dataMatrix2;
								segment3.m_meshScales[segment3.m_combinedCount] = data.m_dataVector0;
								segment3.m_objectIndices[segment3.m_combinedCount] = data.m_dataVector3;
								segment3.m_meshLocations[segment3.m_combinedCount] = data.m_position;
								segment3.m_lodMin = Vector3.Min(segment3.m_lodMin, data.m_position);
								segment3.m_lodMax = Vector3.Max(segment3.m_lodMax, data.m_position);
								if (++segment3.m_combinedCount == segment3.m_leftMatrices.Length)
								{
									NetSegment.RenderLod(cameraInfo, info, segment3);
								}
							}
						}
					}
				}
			}
			instanceIndex = (uint)data.m_nextInstance;
		}

		// NetNode
		private void RefreshJunctionData(ref NetNode thisNode, ushort nodeID, NetInfo info, uint instanceIndex)
		{
			NetManager instance = Singleton<NetManager>.instance;
			Vector3 vector = thisNode.m_position;
			for (int i = 0; i < 8; i++)
			{
				ushort segment = thisNode.GetSegment(i);
				if (segment != 0)
				{
					NetInfo info2 = instance.m_segments.m_buffer[(int)segment].Info;
					ItemClass connectionClass = info2.GetConnectionClass();
					Vector3 a = (nodeID != instance.m_segments.m_buffer[(int)segment].m_startNode) ? instance.m_segments.m_buffer[(int)segment].m_endDirection : instance.m_segments.m_buffer[(int)segment].m_startDirection;
					float num = -1f;
					for (int j = 0; j < 8; j++)
					{
						ushort segment2 = thisNode.GetSegment(j);
						if (segment2 != 0 && segment2 != segment)
						{
							NetInfo info3 = instance.m_segments.m_buffer[(int)segment2].Info;
							ItemClass connectionClass2 = info3.GetConnectionClass();
							if (connectionClass.m_service == connectionClass2.m_service)
							{
								Vector3 vector2 = (nodeID != instance.m_segments.m_buffer[(int)segment2].m_startNode) ? instance.m_segments.m_buffer[(int)segment2].m_endDirection : instance.m_segments.m_buffer[(int)segment2].m_startDirection;
								float num2 = a.x * vector2.x + a.z * vector2.z;
								num = Mathf.Max(num, num2);
								if (j > i && info.m_requireDirectRenderers)
								{
									float num3 = 0.01f - Mathf.Min(info2.m_maxTurnAngleCos, info3.m_maxTurnAngleCos);
									if (num2 < num3 && instanceIndex != 65535u)
									{
										float nodeInfoPriority = info2.m_netAI.GetNodeInfoPriority(segment, ref instance.m_segments.m_buffer[(int)segment]);
										float nodeInfoPriority2 = info3.m_netAI.GetNodeInfoPriority(segment2, ref instance.m_segments.m_buffer[(int)segment2]);
										MethodInfo dynMethod = Util.GetMethod (typeof(NetNode), "RefreshJunctionData", 7);
										if (nodeInfoPriority >= nodeInfoPriority2) {
										/*	object[] p = new object[] { nodeID, i, info2, segment, segment2, instanceIndex, Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)] };
											dynMethod.Invoke(thisNode, p);
											Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)] = (RenderManager.Instance)p [6];
											instanceIndex = (uint)p [5];*/
											RefreshJunctionDataImpl (ref thisNode, nodeID, i, info2, segment, segment2, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)]);
										} else {
										/*	object[] p = new object[] { nodeID, j, info3, segment2, segment, instanceIndex, Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)] };
											dynMethod.Invoke(thisNode, p);
											Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)] = (RenderManager.Instance)p [6];
											instanceIndex = (uint)p [5];*/
											RefreshJunctionDataImpl (ref thisNode, nodeID, j, info3, segment2, segment, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)]);
										}

									}
								}
							}
						}
					}
					vector += a * (2f + num * 2f);
				}
			}
			vector.y = thisNode.m_position.y + (float)thisNode.m_heightOffset * 0.015625f;
			if (info.m_requireSegmentRenderers)
			{
				// Update adjacent segments
				for (int k = 0; k < 8; k++)
				{
					ushort segment3 = thisNode.GetSegment(k);
					if (segment3 != 0 && instanceIndex != 65535u)
					{
					/*	MethodInfo dynMethod = GetMethod (typeof(NetNode), "RefreshJunctionData", 6);
						object[] p = new object[] { nodeID, k, segment3, vector, instanceIndex, Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)] };
						dynMethod.Invoke(thisNode, p);
						Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)] = (RenderManager.Instance)p [5];
						instanceIndex = (uint)p [4];
						*/
						RefreshJunctionDataImpl (ref thisNode, nodeID, k, segment3, vector, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances [(int)((UIntPtr)instanceIndex)]);
					}
				}
			}
		}

		private void RefreshJunctionDataImpl(ref NetNode thisNode, ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
		{
			NetManager instance = Singleton<NetManager>.instance;
			data.m_position = thisNode.m_position;
			data.m_rotation = Quaternion.identity;
			data.m_initialized = true;
			float vScale = 0.05f;
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
				Debug.Log ("Here 1 " + nodeID);
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
				data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
				data.m_dataVector2 = new Vector4(num6, y, num8, w);
				data.m_extraData.m_dataVector4 = RenderManager.GetColorLocation(65536u + (uint)nodeID);
			}
			else
			{
				Debug.Log ("Here 2");
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
				data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
				data.m_dataVector2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
				data.m_extraData.m_dataVector4 = RenderManager.GetColorLocation(65536u + (uint)nodeID);
			}
			data.m_dataInt0 = segmentIndex;
			data.m_dataColor0 = info.m_color;
			data.m_dataColor0.a = 0f;
			if (info.m_requireSurfaceMaps)
			{
				Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector3);
			}
			instanceIndex = (uint)data.m_nextInstance;
		}


		private void RefreshJunctionDataImpl(ref NetNode thisNode, ushort nodeID, int segmentIndex, NetInfo info, ushort nodeSegment, ushort nodeSegment2, ref uint instanceIndex, ref RenderManager.Instance data)
		{
			Debug.Log ("HereAlso");
			data.m_position = thisNode.m_position;
			data.m_rotation = Quaternion.identity;
			data.m_initialized = true;
			float vScale = 0.05f;
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			Vector3 zero3 = Vector3.zero;
			Vector3 zero4 = Vector3.zero;
			Vector3 zero5 = Vector3.zero;
			Vector3 zero6 = Vector3.zero;
			Vector3 zero7 = Vector3.zero;
			Vector3 zero8 = Vector3.zero;
			bool start = Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].m_startNode == nodeID;
			bool flag;
			Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].CalculateCorner(nodeSegment, true, start, false, out zero, out zero5, out flag);
			Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].CalculateCorner(nodeSegment, true, start, true, out zero2, out zero6, out flag);
			start = (Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].m_startNode == nodeID);
			Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].CalculateCorner(nodeSegment2, true, start, true, out zero3, out zero7, out flag);
			Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].CalculateCorner(nodeSegment2, true, start, false, out zero4, out zero8, out flag);
			Vector3 vector;
			Vector3 vector2;
			NetSegment.CalculateMiddlePoints(zero, -zero5, zero3, -zero7, true, true, out vector, out vector2);
			Vector3 vector3;
			Vector3 vector4;
			NetSegment.CalculateMiddlePoints(zero2, -zero6, zero4, -zero8, true, true, out vector3, out vector4);
			data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector, vector2, zero3, zero2, vector3, vector4, zero4, thisNode.m_position, vScale);
			data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector3, vector4, zero4, zero, vector, vector2, zero3, thisNode.m_position, vScale);
			data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
			data.m_dataVector3 = RenderManager.GetColorLocation(65536u + (uint)nodeID);
			data.m_dataInt0 = (8 | segmentIndex);
			data.m_dataColor0 = info.m_color;
			data.m_dataColor0.a = 0f;
			if (info.m_requireSurfaceMaps)
			{
				Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector1);
			}
			instanceIndex = (uint)data.m_nextInstance;
		}



		#endif


		//return PrefabCollection<NetInfo>.GetPrefab((uint)this.m_infoIndex);









	}
}

