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
