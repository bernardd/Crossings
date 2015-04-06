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
			Debug.Log("Start()\n");
			base.Start();
			this.text = "Xings";
			this.transformPosition = new Vector3(-0.55f, 0.97f);
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
		}

		public void BeginCrossingSelect()
		{
			Debug.Log("BeginCrossingSelect()\n");
			crossingSelector = new GameObject("NetTool");
			NetTool nodeSelector = crossingSelector.AddComponent<NetTool>();
			//segmentSelector.button = this;
			this.textColor = new Color32(0, 255, 0, 255);
			crossingSelectEnabled = true;
		}

		public void CancelCrossingSelect()
		{
			Debug.Log("CancelCrossingSelect()\n");
			ToolController toolController = GameObject.FindObjectOfType<ToolController>();
			toolController.CurrentTool = toolController.GetComponent<DefaultTool>();
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

