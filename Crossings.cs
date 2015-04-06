using System;
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
		public override void OnLevelLoaded (LoadMode mode) {
			UIView.GetAView().AddUIComponent(typeof(CrossingsUIToggle));
			Debug.Log("Crossings loaded\n");
		}
	}

	public class crossing : PedestrianPathAI {
		public override ToolBase.ToolErrors CheckBuildPosition (bool test, bool visualize, bool overlay, bool autofix, ref NetTool.ControlPoint startPoint, ref NetTool.ControlPoint middlePoint, ref NetTool.ControlPoint endPoint, out BuildingInfo ownerBuilding, out Vector3 ownerPosition, out Vector3 ownerDirection, out int productionRate)
		{
			Debug.Log("CheckBuildPosition called\n");
			return base.CheckBuildPosition (test, visualize, overlay, autofix, ref startPoint, ref middlePoint, ref endPoint, out ownerBuilding, out ownerPosition, out ownerDirection, out productionRate);
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
