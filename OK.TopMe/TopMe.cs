using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.MainGame;
using System;
using UnityEngine;

namespace OKPlug
{
    [BepInPlugin(GUID, "OKPlug.TopMe", Version)]
    [BepInDependency("github.lunared.bepinex.unityinput", BepInDependency.DependencyFlags.HardDependency)]
    internal class TopMePlugin : BaseUnityPlugin
    {
        public const string GUID = "github.lunared.okplug.topme";
        public const string Version = "1.0.0";

        private const string VRHSceneTypeName = "VRHScene, Assembly-CSharp";

        public static new PluginInfo Info { get; private set; }

        public new static ManualLogSource Logger;

        public static ConfigEntry<bool> PickPosition { get; private set; }
        public static ConfigEntry<bool> RelySonyu { get; private set; }
        public static ConfigEntry<bool> AutoOrgasm { get; private set; }

        public static ConfigEntry<bool> Edge { get; private set; }

        private Harmony _pluginTriggers;

        public void Awake()
        {
            Logger = base.Logger;

            PickPosition = Config.Bind(
                section: "TopMe",
                key: "Pick Position",
                defaultValue: true,
                "Allow the Female AI to automatically pick the next position after orgasming"
            );
            RelySonyu = Config.Bind(
                section: "TopMe",
                key: "Rely",
                defaultValue: true,
                "Female character will guide things during all service and insert actions"
            );
            AutoOrgasm = Config.Bind(
                section: "TopMe",
                key: "Auto Orgasm",
                defaultValue: true,
                "Male character will automatically orgasm regardless of position when excitement guage is full"
            );

            Edge = Config.Bind(
                section: "TopMe",
                key: "Allow Edging",
                defaultValue: true,
                "Female character will occasionally edge the player, reducing stopping the animation and dropping their excitement"
            );

            GameAPI.RegisterExtraBehaviour<TopMeController>(GUID);

            _pluginTriggers = Harmony.CreateAndPatchAll(
                typeof(Triggers)
            );
            
            if (Type.GetType(VRHSceneTypeName) != null)
            {
                Logger.LogDebug("Adding triggers for VR");
                _pluginTriggers.PatchAll(typeof(VRTriggers));
            }
            else
            {
                Logger.LogDebug("VR assembly not present; omitting VR triggers");
            }
        }

        public void OnDestroy()
        {
            _pluginTriggers?.UnpatchSelf();
        }

        private static class Triggers
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiInside))]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiOutside))]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
            public static void HandleOrgasm()
            {
                ((TopMeController)GameAPI.GetRegisteredBehaviour(GUID)).DoOrgasm();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
            public static void OnAnimChange(ref HSceneProc.AnimationListInfo _nextAinmInfo)
            {
                ((TopMeController)GameAPI.GetRegisteredBehaviour(GUID)).ChangeAnimation(_nextAinmInfo);
            }
        }

        private static class VRTriggers
        {
            [HarmonyPostfix]
            [HarmonyPatch(VRHSceneTypeName, "Start")]
            public static void OnStart(MonoBehaviour __instance)
            {
                var adapter = Chainloader.ManagerObject.AddComponent<VRHSceneAdapter>();
                adapter.InitAdapter(__instance);
                ((TopMeController)GameAPI.GetRegisteredBehaviour(GUID)).ForceStartH(adapter, true);
            }

            [HarmonyPostfix]
            [HarmonyPatch(VRHSceneTypeName, "EndProc")]
            public static void OnEndProc(MonoBehaviour __instance)
            {
                var adapter = Chainloader.ManagerObject.GetComponent<VRHSceneAdapter>();
                ((TopMeController)GameAPI.GetRegisteredBehaviour(GUID)).ForceEndH(adapter, true);
                Destroy(adapter);
            }
        }
    }
}
