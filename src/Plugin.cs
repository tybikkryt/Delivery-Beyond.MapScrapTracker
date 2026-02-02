using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HyenaQuest;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapScrapTracker;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static GameObject trackerObject;
	private static TextMeshProUGUI trackerText;
	private static ConfigEntry<bool> showProps;
	private static ConfigEntry<bool> showScrap;
	private static ConfigEntry<float> positionX;
	private static ConfigEntry<float> positionY;

	private void Awake()
	{
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

		showProps = Config.Bind(
			"General",
			"ShowProps",
			true,
			"Show the amount of props left on the map."
		);

		showScrap = Config.Bind(
			"General",
			"ShowScrap",
			true,
			"Show the amount of scrap left on the map."
		);

		positionX = Config.Bind(
			"General",
			"PositionX",
			1490f,
			"Specifies the position on the X-axis."
		);

		positionY = Config.Bind(
			"General",
			"PositionY",
			25f,
			"Specifies the position on the Y-axis."
		);

		Harmony harmony = new(PluginInfo.PLUGIN_GUID);
		harmony.PatchAll();

		SceneManager.sceneLoaded += OnSceneLoaded;
	}

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

		if (trackerObject == null)
		{
			trackerObject = new GameObject("MapScrapTracker");
			trackerObject.transform.SetParent(canvas.transform, false);
			trackerObject.transform.localScale = new Vector3(25f, 25f, 25f);

			trackerText = trackerObject.AddComponent<TextMeshProUGUI>();

			RectTransform rect = trackerObject.GetComponent<RectTransform>();

			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.pivot = new Vector2(0, 0);

			rect.sizeDelta = new Vector2(15, 1);

			rect.anchoredPosition = new Vector2(positionX.Value, positionY.Value);
		}

		trackerText.text = "";
		trackerText.fontSize = 1;
		trackerText.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Pixellari SDF");
		trackerText.alignment = TextAlignmentOptions.Right;

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

		if (showProps.Value)
		{
			text += $"Props: {props_left}\n";
		}

		if (showScrap.Value)
		{
			text += $"Scrap: {scrap_left}";
		}

		trackerText.text = text;
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
			trackerText.text = ""; // Hide text when exit the level
		}
	}
}
