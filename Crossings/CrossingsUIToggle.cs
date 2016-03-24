using System.Collections.Generic;

using ColossalFramework.UI;
using UnityEngine;

namespace Crossings {

	public enum ToolMode {
		Off,
		On
	};

	public class CrossingsUI {
		public bool isVisible { get; private set; }
			
		UIComponent roadsOptionPanel = null;
		UITabstrip builtinTabstrip = null;
		UIButton button = null;

		bool _toolEnabled = false;

		public bool toolEnabled {
			get { return _toolEnabled; }
			set {
				if (value == _toolEnabled) return;

				_toolEnabled = value;
				if (builtinTabstrip != null) {
					Debug.Log ("[Crossings] builtinTabstrip.selectedIndex: " + builtinTabstrip.selectedIndex);
					if (_toolEnabled) {

						Debug.Log("[Crossings] Setting builtin tabstrip mode: " + (-1));
					//	builtinTabstrip.selectedIndex = -1;
					}
				}
			}
		}

		public event System.Action<bool> selectedToolModeChanged;

		bool initialized {
			get { return button != null; }
		}


		public void Show() {
			if (!initialized) {
				if (!Initialize()) return;
			}

			Debug.Log("[Crossings] Showing UI");
			isVisible = true;
		}

		PropertyChangedEventHandler<int> builtinModeChangedHandler = null;

		public void DestroyView() {
			Debug.Log ("[Crossings] Destroying view");
			if (button != null) {
				if (builtinTabstrip != null) {
					builtinTabstrip.eventSelectedIndexChanged -= builtinModeChangedHandler;
				}

				UIView.Destroy(button);
				button = null;
			}
			isVisible = false;
			_toolEnabled = false;
		}

		bool Initialize() {
			Debug.Log("[Crossings] Initializing UI");

			if (UIUtils.Instance == null) return false;

			roadsOptionPanel = UIUtils.Instance.FindComponent<UIComponent>("RoadsOptionPanel", null, UIUtils.FindOptions.NameContains);
			if (roadsOptionPanel == null || !roadsOptionPanel.gameObject.activeInHierarchy) return false;

			builtinTabstrip = UIUtils.Instance.FindComponent<UITabstrip>("ToolMode", roadsOptionPanel);
			if (builtinTabstrip == null || !builtinTabstrip.gameObject.activeInHierarchy) return false;

			Debug.Log ("[Crossings] Initial builtinTabstrip.selectedIndex: " + builtinTabstrip.selectedIndex);

			button = UIUtils.Instance.FindComponent<UIButton>("CrossingsButton");
			if (button != null) {
				DestroyView();
			}

			CreateButton();

			if (button == null) return false; 

			return true;
		}

		void CreateButton() {
			Debug.Log("[Crossings] Creating button");

			UIButton tabTemplate = (UIButton)builtinTabstrip.tabs[0];

			int spriteWidth = 31;
			int spriteHeight = 31;
			string[] spriteNames = {
				"CrossingsButtonBg", 
				"CrossingsButtonBgPressed", 
				"CrossingsButtonBgHovered", 
				"CrossingsIcon", 
				"CrossingsIconPressed", 
			};

			UITextureAtlas atlas = CreateTextureAtlas("sprites.png", "CrossingsUI", tabTemplate.atlas.material, spriteWidth, spriteHeight, spriteNames);
			button = roadsOptionPanel.AddUIComponent<UIButton> ();

			button.name = "CrossingsButton";
			button.atlas = atlas;
			button.size = new Vector2(spriteWidth, spriteHeight);
			button.normalBgSprite = "CrossingsButtonBg";
			button.disabledBgSprite = "CrossingsButtonBg";
			button.hoveredBgSprite = "CrossingsButtonBgHovered";
			button.pressedBgSprite = "CrossingsButtonBgPressed";
			button.focusedBgSprite = "CrossingsButtonBgPressed";
			button.playAudioEvents = true;
			button.tooltip = "Build Crossing";
			button.normalFgSprite = button.disabledFgSprite = button.hoveredFgSprite = "CrossingsIcon";
			button.pressedFgSprite = button.focusedFgSprite = "CrossingsIconPressed";

			button.relativePosition = new Vector3(94, 38);
			button.size = new Vector2 (spriteWidth, spriteHeight);
	//		button.selectedIndex = -1;
	//		button.padding = new RectOffset(0, 1, 0, 0);

			if (builtinModeChangedHandler == null) {
				builtinModeChangedHandler = (UIComponent component, int index) => {
					button.state = UIButton.ButtonState.Normal;
					if (selectedToolModeChanged != null) selectedToolModeChanged(false);
				};
			}

			builtinTabstrip.eventSelectedIndexChanged += builtinModeChangedHandler;

			button.eventClick += (UIComponent component, UIMouseEventParameter param) => {
				Debug.Log("button.eventClick");
				if (selectedToolModeChanged != null) selectedToolModeChanged(true);
			};
		}


		UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight, string[] spriteNames) {

			Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
			tex.filterMode = FilterMode.Bilinear;

			{ // LoadTexture
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
				System.IO.Stream textureStream = assembly.GetManifestResourceStream("Crossings." + textureFile);

				byte[] buf = new byte[textureStream.Length];  //declare arraysize
				textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

				tex.LoadImage(buf);

				tex.Apply(true, true);
			}

			UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

			{ // Setup atlas
				Material material = (Material)Material.Instantiate(baseMaterial);
				material.mainTexture = tex;

				atlas.material = material;
				atlas.name = atlasName;
			}

			// Add sprites
			for (int i = 0; i < spriteNames.Length; ++i) {
				float uw = 1.0f / spriteNames.Length;

				var spriteInfo = new UITextureAtlas.SpriteInfo() {
					name = spriteNames[i],
					texture = tex,
					region = new Rect(i * uw, 0, uw, 1),
				};

				atlas.AddSprite(spriteInfo);
			}

			return atlas;
		}
	}
}