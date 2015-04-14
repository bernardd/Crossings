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
		UIComponent uiComponent;

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

			uiComponent = UIView.GetAView().AddUIComponent (typeof(CrossingsUIToggle));
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

}
