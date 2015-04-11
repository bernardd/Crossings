using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ColossalFramework;

namespace Crossings
{
	public class CrossingsNode
	{
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
			var methods = typeof(NetNode).GetMethods (allFlags); //.Single(c => c.Name == "RenderInstance" && c.GetParameters().Length == 3); // No idea why this doesn't work. Do it the old fashioned way:
			foreach (MethodInfo m in methods) {
				if (m.Name == "RenderInstance" && m.GetParameters().Length == 3) {
					redirects.Add (m, RedirectionHelper.RedirectCalls (m, typeof(CrossingsNode).GetMethod ("RenderInstance", allFlags)));
					break;
				}
		//		if (m.Name == "RenderInstance" && m.GetParameters ().Length == 7) {
		//			redirects.Add (m, RedirectionHelper.RedirectCalls (m, typeof(CrossingsNode).GetMethod ("RenderInstanceInner", allFlags)));
		//		}
			}

			hooked = true;
			/*
			 * var allFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
Debug.Log(typeof(NetNode).GetMethods(allFlags).Single(c => c.Name == "RenderInstance" && c.GetParameters().Length == 3));
*/
		}

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
				//Debug.Log ("Here1 " + num);
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
				Debug.Log ("Here1");
				data.m_dirty = false;
				if (iter == 0)
				{
					if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None)
					{
						//thisNode.RefreshJunctionData(nodeID, info, instanceIndex);
						MethodInfo[] dynMethods = thisNode.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
						foreach (MethodInfo m in dynMethods) {
							if (m.Name == "RefreshJunctionData" && m.GetParameters ().Length == 3) {
								m.Invoke (thisNode, new object[] { nodeID, info, instanceIndex });
								break;
							}
						}
					}
					else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None)
					{
					//	thisNode.RefreshBendData(nodeID, info, instanceIndex, ref data);
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
										Graphics.DrawMesh(node2.m_nodeMesh, data.m_position, data.m_rotation, node2.m_nodeMaterial, node2.m_layer, null, 0, instance2.m_materialBlock);
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
	}
}

