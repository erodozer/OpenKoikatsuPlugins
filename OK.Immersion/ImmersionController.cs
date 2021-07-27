using KKAPI.MainGame;
using UnityEngine;
using Manager;
using ActionGame;
using HarmonyLib;

namespace OKPlug
{
    internal class ImmersionController : GameCustomFunctionController
    {
        private static ImmersionController Instance;
        private SaveData.Player player;
        
        /// <summary>
        /// Number of meals the player has eaten in a day
        /// </summary>
        private int MealsEaten;

        /// <summary>
        /// Maximum number of meals the player can eat
        /// Limit is relative to the player's physical strength
        /// If the player has reached their limit, they will be unable to accept a girl's invite to lunch,
        /// thus requiring the player to balance who they accept eating lunch with.
        /// </summary>
        private int MealLimit
        {
            get
            {
                return (int)Mathf.Lerp(1, 5, Mathf.InverseLerp(0, 100, player.physical));
            }
        }

        /// <summary>
        /// Flag indicating that the player is able to accept lunch invites
        /// </summary>
        public bool CanEat
        {
            get
            {
                return MealsEaten < MealLimit;
            }
        }

        /// <summary>
        /// Amount of experience player has with H.
        /// Can be increased by having H more often.
        /// The lower the experience, the more sensitive the player will be, causing them
        /// to orgasm faster.
        /// </summary>
        private int MaleExperience;

        /// <summary>
        /// Number of times the player has orgasmed.  This counter affects their
        /// sensisitivity and ability to perform H
        /// </summary>
        private int TimesOrgasmed;
        /// <summary>
        /// The number of times the player is allowed to orgasm
        /// Based on their H level and hunger state
        /// </summary>
        private int OrgasmLimit
        {
            get
            {
                // severely limit orgasm limit when on an empty stomach
                if (MealsEaten == 0)
                {
                    return 2;
                }
                return (int)Mathf.Lerp(1, 10, Mathf.InverseLerp(0, 100, player.hentai));
            }
        }

        /// <summary>
        /// Rate at which orgasm endurance is recovered each period
        /// </summary>
        private int RecoverRate
        {
            get
            {
                return (int)Mathf.Lerp(1, 3, Mathf.InverseLerp(0, 100, player.hentai));
            }
        }

        /// <summary>
        /// Flag indicating if the player is able to perform H
        /// H will end when the player is unable to orgasm.  H will also be disabled
        /// in events.
        /// </summary>
        public bool CanDoH
        {
            get
            {
                return TimesOrgasmed < OrgasmLimit;
            }
        }

        /// <summary>
        /// Player's sensitivity.  Impacted by several factors
        /// - male h experience
        /// - times orgasmed
        /// </summary>
        public float Sensitivity
        {
            get
            {
                var cherryBoy = Mathf.Lerp(
                    .5f, 5f,
                    Mathf.InverseLerp(100, 0, MaleExperience)
                );
                var soreMember = Mathf.Lerp(
                    .2f, 1.0f,
                    Mathf.InverseLerp(OrgasmLimit, 0, TimesOrgasmed)
                );

                return cherryBoy * soreMember;
            }
        }

        protected void Awake()
        {
            Instance = this;
        }

        override protected void OnNewGame()
        {
            player = Singleton<Game>.Instance.Player;
        }

        override protected void OnDayChange(Cycle.Week day)
        {
            if (TimesOrgasmed == 0)
            {
                MaleExperience -= 15;
            }
            else if (TimesOrgasmed < 3)
            {
                MaleExperience = 10;
            }

            MealsEaten = 0;
            TimesOrgasmed = 0;

            // assume lunch is eaten on holiday/dates
            if (day == Cycle.Week.Holiday)
            {
                MealsEaten = 1;
            }
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            TimesOrgasmed = (int)Mathf.Max(0, TimesOrgasmed - RecoverRate);
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            var flags = proc.flags;

            var experience =
                // no condom inside
                3 * (flags.count.sonyuInside + flags.count.sonyuOutside) +
                // condom inside, outside, and houshi
                2 * (
                    flags.count.sonyuAnalInside + flags.count.sonyuAnalOutside +
                    flags.count.sonyuCondomInside + flags.count.sonyuAnalCondomInside +
                    flags.count.houshiInside + flags.count.houshiOutside
                );
            MaleExperience = Mathf.Clamp(0, 100, MaleExperience + experience);
        }


        [HarmonyPrefix, HarmonyPatch(typeof(HFlag), nameof(HFlag.MaleGaugeUp))]
        private static void SoreMemberMultiplier(ref float _addPoint)
        {
             _addPoint *= Instance.Sensitivity;
        }


        [HarmonyPostfix, 
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOutside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOutside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuCondomInside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalCondomInside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiInside)),
            HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiOutside))
        ]
        private static void OnOrgasm()
        {
            Instance.TimesOrgasmed += 1;
        }
    }
}
