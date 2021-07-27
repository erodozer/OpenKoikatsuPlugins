using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.MainGame;

namespace OKPlug
{
    [BepInPlugin(GUID, "OKPlug.Immersion", Version)]
    [BepInDependency("github.lunared.bepinex.unityinput", BepInDependency.DependencyFlags.HardDependency)]
    internal class ImmersionPlugin : BaseUnityPlugin
    {
        public const string GUID = "github.lunared.okplug.immersion";
        public const string Version = "1.0.0";
        public static new PluginInfo Info { get; private set; }
        public static ConfigEntry<bool> FullStomach { get; private set; }
        public static ConfigEntry<bool> EmptyStomach { get; private set; }
        public static ConfigEntry<bool> SoreMember { get; private set; }
        public static ConfigEntry<bool> CherryBoy { get; private set; }

        private Harmony _pluginTriggers;

        public void Awake()
        {
            FullStomach = Config.Bind(
                section: "Immersion",
                key: "Full Stomach",
                defaultValue: true,
                "Limit the amount of times you can go eat lunch."
            );

            EmptyStomach = Config.Bind(
                section: "Immersion",
                key: "Empty Stomach",
                defaultValue: true,
                "Require eating a meal to operate at ideal performance."
            );

            SoreMember = Config.Bind(
                section: "Immersion",
                key: "Sore Member",
                defaultValue: true,
                "Decrease male sensitivity the more times they orgasm.  Orgasming too many times will lead to exhaustion."
            );

            CherryBoy = Config.Bind(
                section: "Immersion",
                key: "Cherry Boy",
                defaultValue: true,
                "Improve endurance (decrease sensitivity) by having H more often.  At the start, player has virgin level sensitivity."
            );

            GameAPI.RegisterExtraBehaviour<ImmersionController>(GUID);

            _pluginTriggers = Harmony.CreateAndPatchAll(typeof(ImmersionController));
        }

        public void OnDestroy()
        {
            _pluginTriggers?.UnpatchSelf();
        }
    }
}
