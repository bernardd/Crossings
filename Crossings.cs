using ICities;
using UnityEngine;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ColossalFramework;


namespace Crossings {
	public class CrossingsInfo : IUserMod {
		public string Name {
			get { return "Pedestrian Crossings"; }
		} 

		public string Description {
			get { return "Adds placeable pedestrian (zebra) crossings"; }
		}
	}
	 
	public class loader : LoadingExtensionBase {
		UIComponent uiComponent;

		public override void OnCreated(ILoading loading)
		{
			Debug.Log ("OnCreated()");
			base.OnCreated (loading);
			Debug.Log ("OnCreated() complete");
		}

		public override void OnReleased()
		{
			Debug.Log ("OnReleased()");
			base.OnReleased ();
			Debug.Log ("OnReleased() complete");
		}

		public override void OnLevelLoaded (LoadMode mode)
		{
			Debug.Log("Crossings loading...");
			uiComponent = UIView.GetAView().AddUIComponent (typeof(CrossingsUIToggle));
			Debug.Log("Crossings loaded");
		}

	}

	public static class CrossingMaker
	{
		public static ushort MakeCrossing(ushort currentNode, NetInfo prefab, NetTool.ControlPoint controlPoint)
		{
			ushort node = 0;
			if (currentNode == 0) {
				ushort newNode, newSegment;
				int cost, productionRate;
				ToolBase.ToolErrors errors = NetTool.CreateNode (prefab, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, true, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
				Debug.Log ("CreateNode test result: " + errors + " " + newNode + " " + newSegment + " " + cost + " " + productionRate);
				if (errors != ToolBase.ToolErrors.None)
					return 0;
				
				NetTool.CreateNode (prefab, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
				Debug.Log ("CreateNode real result: " + errors + " " + newNode + " " + newSegment + " " + cost + " " + productionRate);
				Debug.Log ("Created Node: " + NetManager.instance.m_nodes.m_buffer [node].m_flags);
				return newNode;
			} else {
				if ((NetManager.instance.m_nodes.m_buffer [currentNode].m_flags &
				    (NetNode.Flags.End | NetNode.Flags.Junction | NetNode.Flags.Bend)) == NetNode.Flags.None) {
					node = currentNode;
					Debug.Log ("Existing Node: " + node + " " + NetManager.instance.m_nodes.m_buffer [node].m_flags);
					return node;
				} else {
					Debug.Log ("End, bend or intersection node - ignoring " + currentNode);
					return 0;
				}
			}
		}


			// UpdateNodeFlags
			// UpdateNodeRenderer

			// Maybe:
			/*
				 	if (prefab.m_class.m_service > ItemClass.Service.Office)
					{dd
						int num5 = prefab.m_class.m_service - ItemClass.Service.Office - 1;
						Singleton<GuideManager>.instance.m_serviceNotUsed[num5].Disable();
						Singleton<GuideManager>.instance.m_serviceNeeded[num5].Deactivate();
					}
					if (prefab.m_class.m_service == ItemClass.Service.Road)
					{
						Singleton<CoverageManager>.instance.CoverageUpdated(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
						Singleton<NetManager>.instance.m_roadsNotUsed.Disable();
					}
				*/


		public static void AddFlagsToNode(ushort node) {
			if (node != 0) {
				int checkCount = 0;
				for (int i=0; i<8; i++) {
					ushort segmentID = NetManager.instance.m_nodes.m_buffer [node].GetSegment (i);
					if (segmentID != 0) {
						NetSegment[] segBuf = NetManager.instance.m_segments.m_buffer;	
						if (segBuf[segmentID].m_startNode == node) {
							segBuf[segmentID].m_flags |= NetSegment.Flags.CrossingStart;
							checkCount++;
						} else if (segBuf[segmentID].m_endNode == node) {
							segBuf[segmentID].m_flags |= NetSegment.Flags.CrossingEnd;
							checkCount++;
						} else {
							Debug.Log("Not start or end node of contained segment...er...");
						}
					}
				}
				if (checkCount != 2)
					Debug.Log ("BAD: Not exactly two segments connecting to the new node");
			}
			NetManager.instance.m_nodes.m_buffer [node].m_flags |= NetNode.Flags.TrafficLights | NetNode.Flags.Junction | NetNode.Flags.Untouchable;
		}
	}

	/* 

	NetManager instance = Singleton<NetManager>.instance;
	instance.m_segments;
	instance.m_nodes;

	NetNode.Info.m_lanes[x].m_laneType = NetInfo.LaneType.Pedestrian 
	NetNode.Info.m_lanes[x].m_vehicleType = None
	BeautificationPanel
	public const NetNode.Flags LevelCrossing = 2097152;

	*/
}
