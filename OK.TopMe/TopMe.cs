using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.MainGame;

namespace OKPlug
{
    [BepInPlugin(GUID, "OKPlug.TopMe", Version)]
    [BepInDependency("github.lunared.bepinex.unityinput", BepInDependency.DependencyFlags.HardDependency)]
    internal class TopMePlugin : BaseUnityPlugin
    {
        public const string GUID = "github.lunared.okplug.topme";
        public const string Version = "1.0.0";

        public static new PluginInfo Info { get; private set; }

        public static ConfigEntry<bool> PickPosition { get; private set; }
        public static ConfigEntry<bool> RelySonyu { get; private set; }
        public static ConfigEntry<bool> AutoOrgasm { get; private set; }

        private Harmony _pluginTriggers;

        public void Awake()
        {
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

            GameAPI.RegisterExtraBehaviour<TopMeController>(GUID);
            
            _pluginTriggers = Harmony.CreateAndPatchAll(
                typeof(Triggers)
            );
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

            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeCategory))]
            private static void ChangeCategory(int _category)
            {
                ((TopMeController)GameAPI.GetRegisteredBehaviour(GUID)).ChangeCategory(_category);
            }
        }
    }
}
