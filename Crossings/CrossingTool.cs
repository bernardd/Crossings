using System.Collections;
using UnityEngine;
using System.Threading;
using ColossalFramework;

namespace Crossings
{
	public class CrossingTool : ToolBase
	{
		NetTool.ControlPoint m_controlPoint;
		NetTool.ControlPoint m_cachedControlPoint;
		ToolErrors m_buildErrors;
		ToolErrors m_cachedErrors;
		Ray m_mouseRay;
		float m_mouseRayLength;
		bool m_mouseRayValid;
		NetInfo m_prefab;
		ushort m_currentNodeID, m_currentSegmentID;

		private object m_cacheLock;

		protected override void Awake()
		{
			m_toolController = GameObject.FindObjectOfType<ToolController>();
			m_cacheLock = new object();
		}

		private void GetNetAtCursor()
		{
			Vector3 mousePosition = Input.mousePosition;
			RaycastInput input = new RaycastInput(Camera.main.ScreenPointToRay(mousePosition), Camera.main.farClipPlane);
			RaycastOutput output;
			input.m_netService = new RaycastService(ItemClass.Service.Road, ItemClass.SubService.None, ItemClass.Layer.Default);
			input.m_ignoreSegmentFlags = NetSegment.Flags.None;
			input.m_ignoreNodeFlags = NetNode.Flags.None;
			input.m_ignoreTerrain = true;
			if (RayCast(input, out output))
			{
				//Debug.Log("Found segment " + output.m_netSegment);
				//Debug.Log("Found node " + output.m_netNode);
				m_currentNodeID = output.m_netNode;
				m_currentSegmentID = output.m_netSegment;
			}
			else
			{
				m_currentNodeID = 0;
				m_currentSegmentID = 0;
			}
		}

		protected override void OnToolGUI()
		{
			bool isInsideUI = this.m_toolController.IsInsideUI;
			Event current = Event.current;
			if (current.type == EventType.MouseDown && !isInsideUI)
			{
				if (current.button == 0) { // LMB
					if (this.m_cachedErrors == ToolBase.ToolErrors.None && m_currentSegmentID != 0) {
						SimulationManager.instance.AddAction(this.CreateCrossing());
					} else {
						//	Singleton<SimulationManager>.instance.AddAction(this.CreateFailed());
					}
				} else if (current.button == 1) { // RMB
					if (this.m_cachedErrors == ToolBase.ToolErrors.None && m_currentSegmentID != 0) {
						Debug.Log("[Crossings] Trying to remove crossing " + m_currentNodeID);	
						SimulationManager.instance.AddAction(this.RemoveCrossing());
					} else {
						//	Singleton<SimulationManager>.instance.AddAction(this.CreateFailed());
					}
				}

			}
		}

		override protected void OnToolUpdate()
		{
			while (!Monitor.TryEnter(this.m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) {}
			try {
				this.m_cachedControlPoint = this.m_controlPoint;
				this.m_cachedErrors = this.m_buildErrors;
			} finally {
				Monitor.Exit(this.m_cacheLock);
			}

			base.OnToolUpdate();
			// Cast ray, find node
			GetNetAtCursor ();

			if (m_currentSegmentID != 0) {
				NetSegment segment = NetManager.instance.m_segments.m_buffer [m_currentSegmentID];
				m_prefab = PrefabCollection<NetInfo>.GetPrefab (segment.m_infoIndex);
			} else {
				m_prefab = null;
			}
		}

		protected override void OnToolLateUpdate()
		{
			if (m_prefab == null)
			{
				return;
			}
			Vector3 mousePosition = Input.mousePosition;
			this.m_mouseRay = Camera.main.ScreenPointToRay(mousePosition);
			this.m_mouseRayLength = Camera.main.farClipPlane;
			this.m_mouseRayValid = (!this.m_toolController.IsInsideUI && Cursor.visible);
			base.ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.NormalPower);
		}

		override public void SimulationStep()
		{
			ServiceTypeGuide optionsNotUsed = Singleton<NetManager>.instance.m_optionsNotUsed;
			if (optionsNotUsed != null && !optionsNotUsed.m_disabled)
			{
				optionsNotUsed.Disable();
			}

			Vector3 position = this.m_controlPoint.m_position;
			bool failed = false;

			NetTool.ControlPoint controlPoint = default(NetTool.ControlPoint);
			NetNode.Flags ignoreNodeFlags;
			NetSegment.Flags ignoreSegmentFlags;

			ignoreNodeFlags = NetNode.Flags.None;
			ignoreSegmentFlags = NetSegment.Flags.None;

			Building.Flags ignoreBuildingFlags = Building.Flags.All;

			if (m_prefab != null) {
				if (this.m_mouseRayValid && NetTool.MakeControlPoint (this.m_mouseRay, this.m_mouseRayLength, m_prefab, false, ignoreNodeFlags, ignoreSegmentFlags, ignoreBuildingFlags, 0, false, out controlPoint)) {
					if (controlPoint.m_node == 0 && controlPoint.m_segment == 0 && !controlPoint.m_outside) {
						controlPoint.m_position.y = NetSegment.SampleTerrainHeight (m_prefab, controlPoint.m_position, false, 0) + controlPoint.m_elevation;
					}
				} else {
					failed = true;
				}
			}
			this.m_controlPoint = controlPoint;

			this.m_toolController.ClearColliding();

			ToolBase.ToolErrors toolErrors = ToolBase.ToolErrors.None;
			if (failed)
			{
				toolErrors |= ToolBase.ToolErrors.RaycastFailed;
			}
			while (!Monitor.TryEnter(this.m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
			{
			}
			try
			{
				this.m_buildErrors = toolErrors;
			}
			finally
			{
				Monitor.Exit(this.m_cacheLock);
			}

		}

		public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
		{
			//Debug.Log ("Render Overlay");
			base.RenderOverlay(cameraInfo);

			if (!this.m_toolController.IsInsideUI && Cursor.visible) {  /* && (this.m_cachedErrors & (ToolBase.ToolErrors.RaycastFailed | ToolBase.ToolErrors.Pending)) == ToolBase.ToolErrors.None*/
				NetTool.ControlPoint controlPoint = this.m_cachedControlPoint;

				BuildingInfo buildingInfo;
				Vector3 vector;
				Vector3 vector2;
				int num3;

				if (m_prefab != null) {
					m_prefab.m_netAI.CheckBuildPosition (false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out buildingInfo, out vector, out vector2, out num3);

					Color colour;
					if (CanBuild (m_currentSegmentID, m_currentNodeID))
						colour = base.GetToolColor (false, false);
					else
						colour = base.GetToolColor (true, false);

					ToolManager toolManager = Singleton<ToolManager>.instance;
					toolManager.m_drawCallData.m_overlayCalls++;
					Singleton<RenderManager>.instance.OverlayEffect.DrawCircle (cameraInfo, colour, m_controlPoint.m_position, m_prefab.m_halfWidth * 2f, -1f, 1280f, false, false);
				}
			}
		}

		// I am not sure why the tool controller needs to be injected here.
		// ToolBase suggests this should happen on its own, but it does not.
		protected override void OnEnable()
		{
			m_toolController.ClearColliding ();
			m_buildErrors = ToolBase.ToolErrors.Pending;
			m_cachedErrors = ToolBase.ToolErrors.Pending;
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.ToolCursor = null;
			m_buildErrors = ToolBase.ToolErrors.Pending;
			m_cachedErrors = ToolBase.ToolErrors.Pending;
			m_mouseRayValid = false;
			base.OnDisable();
		}
			
		private IEnumerator CreateCrossing()
		{
			if (CanBuild (m_currentSegmentID, m_currentNodeID)) {
				ushort newSegment, newNode;
				int cost, productionRate;
				ToolBase.ToolErrors errors = NetTool.CreateNode (m_prefab, m_controlPoint, m_controlPoint, m_controlPoint, NetTool.m_nodePositionsSimulation, 0, true, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
				Debug.Log ("[Crossings] CreateNode test result: " + errors + " " + newNode + " " + newSegment + " " + cost + " " + productionRate);
				if (errors != ToolBase.ToolErrors.None)
					yield return null;
				if (newNode == 0) {
					NetTool.CreateNode (m_prefab, m_controlPoint, m_controlPoint, m_controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out newNode, out newSegment, out cost, out productionRate);
					NetManager.instance.m_nodes.m_buffer [newNode].m_flags |= (NetNode.Flags)CrossingsNode.CrossingFlag;
					Debug.Log ("[Crossings] CreateNode real result: " + errors + " " + newNode + " " + newSegment + " " + cost + " " + productionRate);
				} else {
					NetManager.instance.m_nodes.m_buffer [newNode].m_flags |= (NetNode.Flags)CrossingsNode.CrossingFlag;
					NetManager.instance.UpdateNode (newNode, 0, 0);
					Debug.Log ("[Crossings] Existing Node: " + newNode + " " + NetManager.instance.m_nodes.m_buffer [newNode].m_flags);
				}
			}

			yield return null;
		}


		private IEnumerator RemoveCrossing()
		{
			if (m_currentNodeID != 0) {
				NetManager.instance.m_nodes.m_buffer [m_currentNodeID].m_flags &= ~(NetNode.Flags)CrossingsNode.CrossingFlag;
				NetManager.instance.UpdateNode (m_currentNodeID, 0, 0);
				m_currentNodeID = 0;
			}
			yield return null;
		}

		private bool CanBuild(ushort segmentID, ushort nodeID)
		{
			if (segmentID == 0)
				return false;
			
			NetSegment segment = NetManager.instance.m_segments.m_buffer [m_currentSegmentID];
			NetInfo info = segment.Info;
			ItemClass.Level level = info.m_class.m_level;
			if (!(info.m_netAI is RoadBaseAI))
				return false; // No crossings on non-roads
			
			if (level == ItemClass.Level.Level5 || level == ItemClass.Level.Level1)
				return false; // No crossings on freeways or dirt roads

			if (level != ItemClass.Level.Level2) {
				ushort[] nodes;
				if (nodeID == 0)
					nodes = new ushort[] { segment.m_startNode, segment.m_endNode };
				else
					nodes = new ushort[] { nodeID };

				//Debug.Log ("Segment flags: " + segment.m_flags);
				foreach (ushort n in nodes) {
					NetNode node = NetManager.instance.m_nodes.m_buffer [n];
					//Debug.Log ("Node Flags: " + node.m_flags);
					float groundHeight = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(node.m_position);
					if (Mathf.Abs(node.m_position.y - groundHeight) > 11f) // Bridge
						return false; // 4/6 lane bridges get their bridgework mucked up by crossings
				}
			}

			if (nodeID == 0)
				return true; // No node means we can create one

			NetNode.Flags flags = NetManager.instance.m_nodes.m_buffer [nodeID].m_flags;
			if ((flags & (NetNode.Flags)CrossingsNode.CrossingFlag) != NetNode.Flags.None)
				return true; // Already a crossing - can remove it

			// Can't add crossing to the end, a junction (except a crossing one) or a bend
			return (flags & (NetNode.Flags.End | NetNode.Flags.Junction | NetNode.Flags.Bend)) == NetNode.Flags.None;
		}
	}
}


