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

		public static CrossingsUI ui = new CrossingsUI();
		public static bool loadingLevel = false;
		public static CrossingTool buildTool = null;
		public static NetTool netTool = null;

		public override void OnCreated(ILoading loading)
		{
			Debug.Log ("OnCreated()");
			base.OnCreated (loading);

			CrossingsNode.Hook ();

			ui.selectedToolModeChanged += (bool enabled) => {
				SetToolEnabled(enabled);
			};

			Debug.Log ("OnCreated() complete");
		}

		public override void OnReleased()
		{
			Debug.Log ("[Crossings] OnReleased()");
			ui.DestroyView();
			DestroyBuildTool();

			base.OnReleased ();
		}

		public override void OnLevelLoaded (LoadMode mode)
		{
			base.OnLevelLoaded (mode);

			if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
				ui.DestroyView ();
			loadingLevel = false;

			Debug.Log("[Crossings] OnLevelLoaded, UI visible: " + ui.isVisible);
		}

		public override void OnLevelUnloading() {
			Debug.Log ("[Crossings] Unloading level");
			ui.DestroyView();
			loadingLevel = true;
		}
			
		public static void SetToolEnabled(bool enabled) {
			if (enabled == ui.toolEnabled)
				return;

			if (enabled) {
				Debug.Log ("[Crossings] Tool enabled");
				if (buildTool == null)
					CreateBuildTool ();

				ToolsModifierControl.toolController.CurrentTool = buildTool;
			}
			else {
				Debug.Log("[Crossings] Tool disabled");

				if (ToolsModifierControl.toolController.CurrentTool == buildTool || ToolsModifierControl.toolController.CurrentTool == null) {
					ToolsModifierControl.toolController.CurrentTool = netTool;
				}

				DestroyBuildTool();
			}
			ui.toolEnabled = enabled;
		}

		static void CreateBuildTool() {
			buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<CrossingTool>();
			if (buildTool == null) {  
				buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<CrossingTool>();
				Debug.Log("[Crossings] Tool created: " + buildTool);
			}
			else {
				Debug.Log("[Crossings] Found existing tool: " + buildTool);
			}
		}

		static void DestroyBuildTool() {
			if (buildTool != null) {
				Debug.Log("[Crossings] Tool destroyed");
				CrossingTool.Destroy(buildTool);
				buildTool = null;
			}
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

		UIPanel roadsPanel = null; 

		public override void OnCreated(IThreading threading) {

		}

		public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
			if (Loader.loadingLevel)
				return;

			if (roadsPanel == null) {
				roadsPanel = UIView.Find<UIPanel> ("RoadsPanel");
			}

			if (roadsPanel == null || !roadsPanel.isVisible) {
				if (Loader.ui.toolEnabled) {
					Debug.Log ("[Crossings] Roads panel no longer visible");
					Loader.SetToolEnabled (false);
				}
				return;
			}

			if (Loader.netTool == null) {
				foreach (var tool in ToolsModifierControl.toolController.Tools) {
					NetTool nt = tool as NetTool;
					if (nt != null && nt.m_prefab != null) {
						Debug.Log ("[Crossings] NetTool found: " + nt.name);
						Loader.netTool = nt;
						break;
					}
				}

				if (Loader.netTool == null)
					return;

				Debug.Log ("[Crossings] UI visible: " + Loader.ui.isVisible);
			}

			if (!Loader.ui.isVisible) {
				Loader.ui.Show ();
			}

			if (Loader.ui.toolEnabled) {
				if (ToolsModifierControl.toolController.CurrentTool != Loader.buildTool) {
					Debug.Log ("[Crossings] Another tool selected");
					Loader.SetToolEnabled (false);
				}
			} else {
				if (ToolsModifierControl.toolController.CurrentTool == Loader.buildTool) {
					ToolsModifierControl.toolController.CurrentTool = Loader.netTool;
				}
			}
		}

	}
}
