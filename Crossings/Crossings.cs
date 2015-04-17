using ICities;
using ColossalFramework.UI;
using UnityEngine;
using System;
using System.Reflection;


namespace Crossings {
	public class CrossingsInfo : IUserMod {
		public string Name {
			get { return "Pedestrian Crossings"; }
		} 

		public string Description {
			get { return "Adds placeable pedestrian (zebra) crossings"; }
		}
	}
	 
	public class Loader : LoadingExtensionBase {

		public override void OnCreated(ILoading loading)
		{
			Debug.Log ("OnCreated()");
			base.OnCreated (loading);

			CrossingsNode.Hook ();

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
			base.OnLevelLoaded (mode);
			Debug.Log ("Crossings loading...");


			Debug.Log("Crossings loaded");
		}

	}

	public static class Util
	{
		public static MethodInfo GetMethod(Type type, string name, int pCount)
		{
			MethodInfo[] methods = type.GetMethods (BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (MethodInfo m in methods) {
				if (m.Name == name && m.GetParameters ().Length == pCount)
					return m;
			}
			return null;
		}
	}

	public class ThreadingExtension : ThreadingExtensionBase {
		public static ThreadingExtension Instance { get; private set; }

		string[] twowayNames = { "Basic Road", "Large Road" };
		string[] onewayNames = { "Oneway Road", "Large Oneway" };

		NetTool netTool = null;
		ToolMode toolMode = ToolMode.Off;

		UIPanel roadsPanel = null; 

		CrossingsUI ui = new CrossingsUI();
		bool loadingLevel = false;

		CrossingTool buildTool = null;

		public void OnLevelUnloading() {
			ui.DestroyView();
			toolMode = ToolMode.Off;
			loadingLevel = true;
		}

		public void OnLevelLoaded(LoadMode mode) {
			loadingLevel = false;
			Debug.Log("OnLevelLoaded, UI visible: " + ui.isVisible);
		}

		public override void OnCreated(IThreading threading) {
			Instance = this;
			ui.selectedToolModeChanged += (ToolMode newMode) => {
				SetToolMode(newMode);
			};
		}

		public override void OnReleased() {
			ui.DestroyView();
			DestroyBuildTool();
		}

		void CreateBuildTool() {
			if (buildTool == null) {
				buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<CrossingTool>();
				if (buildTool == null) {  
					buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<CrossingTool>();
					Debug.Log("Tool created: " + buildTool);
				}
				else {
					Debug.Log("Found existing tool: " + buildTool);
				}
			} 
		}

		void DestroyBuildTool() {
			if (buildTool != null) {
				Debug.Log("Tool destroyed");
				CrossingTool.Destroy(buildTool);
				buildTool = null;
			}
		}

		void SetToolMode(ToolMode mode, bool resetNetToolModeToStraight = false) {
			if (mode == toolMode) return;

			if (mode != ToolMode.Off) {
				CreateBuildTool();
				ToolsModifierControl.toolController.CurrentTool = buildTool;

				if (mode == ToolMode.On) {
					Debug.Log("Crossing placement mode activated");
					toolMode = ToolMode.On;
				}
		
				ui.toolMode = toolMode;
			}
			else {
				Debug.Log("Tool disabled");
				toolMode = ToolMode.Off;

				if (ToolsModifierControl.toolController.CurrentTool == buildTool || ToolsModifierControl.toolController.CurrentTool == null) {
					ToolsModifierControl.toolController.CurrentTool = netTool;
				}

				DestroyBuildTool();

				ui.toolMode = toolMode;

				if (resetNetToolModeToStraight) {
					netTool.m_mode = NetTool.Mode.Straight;
					Debug.Log("Reseted netTool mode: " + netTool.m_mode);
				}
			}
		}

		public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
			if (loadingLevel)
				return;

			if (roadsPanel == null) {
				roadsPanel = UIView.Find<UIPanel> ("RoadsPanel");
			}

			if (roadsPanel == null || !roadsPanel.isVisible) {
				if (toolMode != ToolMode.Off) {
					Debug.Log ("Roads panel no longer visible");
					SetToolMode (ToolMode.Off, true);
				}
				return;
			}

			if (netTool == null) {
				foreach (var tool in ToolsModifierControl.toolController.Tools) {
					NetTool nt = tool as NetTool;
					if (nt != null && nt.m_prefab != null) {
						Debug.Log ("NetTool found: " + nt.name);
						netTool = nt;
						break;
					}
				}

				if (netTool == null)
					return;

				Debug.Log ("UI visible: " + ui.isVisible);
			}

			if (!ui.isVisible) {
				ui.Show ();
			}

			if (toolMode != ToolMode.Off) {
				if (ToolsModifierControl.toolController.CurrentTool != buildTool) {
					Debug.Log ("Another tool selected");
					SetToolMode (ToolMode.Off);
				}
			} else {
				ui.toolMode = ToolMode.Off;

				if (ToolsModifierControl.toolController.CurrentTool == buildTool) {
					ToolsModifierControl.toolController.CurrentTool = netTool;
				}
			}
		}

	}
}
