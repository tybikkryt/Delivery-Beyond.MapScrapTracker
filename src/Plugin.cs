using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HyenaQuest;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapScrapTracker;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static GameObject _trackerObject;
	private static TextMeshProUGUI _trackerText;
	private static ConfigEntry<bool> ShowProps;
	private static ConfigEntry<bool> ShowScrap;

	private void Awake()
	{
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

		ShowProps = Config.Bind(
			"General",
			"ShowProps",
			true,
			"Show the amount of props left on the map."
		);

		ShowScrap = Config.Bind(
			"General",
			"ShowScrap",
			true,
			"Show the amount of scrap left on the map."
		);

		Harmony harmony = new(PluginInfo.PLUGIN_GUID);
		harmony.PatchAll();

		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	
	/*
	private void Update()
	{
		if (UnityInput.Current.GetKeyDown(KeyCode.F9))
		{
			var props = FindObjectsByType<entity_phys_prop_scrap>(FindObjectsSortMode.InstanceID);

			Logger.LogInfo($"Total props: {props.Length}");

			int scrap_left = 0;
			foreach (var prop in props)
			{
				if (prop == null) continue;
				Logger.LogInfo($"Object: {prop.name}, scrap: {prop.scrap}");
				scrap_left += prop.scrap;
			}

			_trackerText.text = $"Scrap Left: {scrap_left}";

			Logger.LogInfo($"Scrap Left: {scrap_left}");

		}
	}
	*/

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != "INGAME") return;

		GameObject canvasGo = GameObject.Find("[CONTROLLERS]/UIController/[CANVAS]");
		Canvas canvas = canvasGo.GetComponent<Canvas>();

		if (canvas == null)
		{
			Logger.LogWarning("Canvas not found!");
			return;
		}

		if (_trackerObject == null)
		{
			_trackerObject = new GameObject("MapScrapTracker");
			_trackerObject.transform.SetParent(canvas.transform, false);
			_trackerObject.transform.localScale = new Vector3(25f, 25f, 25f);

			_trackerText = _trackerObject.AddComponent<TextMeshProUGUI>();

			RectTransform rect = _trackerObject.GetComponent<RectTransform>();

			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.pivot = new Vector2(0, 0);

			rect.sizeDelta = new Vector2(10, 1);

			if (ShowProps.Value && ShowScrap.Value)
			{
				rect.anchoredPosition = new Vector2(17, 147);
			}
			else
			{
				rect.anchoredPosition = new Vector2(17, 130);
			}
		}

		_trackerText.text = "";
		_trackerText.fontSize = 1;
		_trackerText.font = Resources.Load<TMP_FontAsset>("Pixellari SDF");
		_trackerText.alignment = TextAlignmentOptions.Left;

		Logger.LogInfo("Added tracker to Canvas");
	}

	public static void UpdateTracker(entity_phys_prop_scrap exclude = null)
	{
		var props = FindObjectsByType<entity_phys_prop_scrap>(FindObjectsSortMode.None);

		int scrap_left = 0;
		int props_left = 0;
		
		foreach (var prop in props)
		{
			if (prop == null || prop == exclude) continue;
			scrap_left += prop.scrap;
			props_left++;
		}

		string text = "";

		if (ShowProps.Value)
		{
			text += $"Props Left: {props_left}\n";
		}

		if (ShowScrap.Value)
		{
			text += $"Scrap Left: {scrap_left}";
		}

		_trackerText.text = text;

		//Logger.LogInfo($"Props Left: {props.Length} Scrap Left: {map_scrap}");
	}

	[HarmonyPatch(typeof(entity_phys_prop_scrap))]
	public class PropPatch
	{
		[HarmonyPatch("OnNetworkDespawn"), HarmonyPostfix]
		public static void Postfix(entity_phys_prop_scrap __instance)
		{
			UpdateTracker(__instance);
		}

		[HarmonyPatch("Awake"), HarmonyPostfix]
		public static void PostfixAwake()
		{
			UpdateTracker();
		}
	}

	[HarmonyPatch(typeof(MapController))]
	public class MapControllerPatch
	{
		[HarmonyPatch("MapClearedBroadcastRPC"), HarmonyPostfix]
		public static void Postfix()
		{
			_trackerText.text = ""; // Hide text when exit the level
		}
	}
}
