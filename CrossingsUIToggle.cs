using System;
using UnityEngine;
using ColossalFramework.UI;

namespace Crossings
{
	public class CrossingsUIToggle : UIButton
	{
		GameObject crossingSelector;
		public bool crossingSelectEnabled = false;

		public override bool canFocus
		{
			get
			{
				return false;
			}
		}

		public override void Start()
		{
			Debug.Log("Start()");
			base.Start();
			this.text = "Crossings";
			this.transformPosition = new Vector3(-1.55f, 0.97f);
			this.normalBgSprite = "ButtonMenu";
			this.disabledBgSprite = "ButtonMenuDisabled";
			this.hoveredBgSprite = "ButtonMenuHovered";
			this.pressedBgSprite = "ButtonMenuPressed";
			this.textColor = new Color32(255, 255, 255, 255);
			this.disabledTextColor = new Color32(7, 7, 7, 255);
			this.hoveredTextColor = new Color32(7, 132, 255, 255);
			this.pressedTextColor = new Color32(30, 30, 44, 255);
			this.playAudioEvents = true;
			this.eventClick += ButtonClick;
			this.width = 60;
			this.height = 30;
			this.cachedName = "CrossingsUIToggle";
		}

		public void BeginCrossingSelect()
		{
			Debug.Log("BeginCrossingSelect()");
			crossingSelector = new GameObject("CrossingTool");
			CrossingTool crossingTool = crossingSelector.AddComponent<CrossingTool>();
			crossingTool.button = this;
			this.textColor = new Color32(0, 255, 0, 255);
			crossingSelectEnabled = true;
			Debug.Log("BeginCrossingSelect() end\n");
		}

		public void CancelCrossingSelect()
		{
			Debug.Log("CancelCrossingSelect()");
			ToolController toolController = GameObject.FindObjectOfType<ToolController>();
			toolController.CurrentTool = toolController.GetComponent<DefaultTool>();
			crossingSelectEnabled = false;
		}

		private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
		{
			Debug.Log("ButtonClick()\n");
			if (crossingSelectEnabled)
				CancelCrossingSelect();
			else
				BeginCrossingSelect();
		} 
	}
}

