using BepInEx.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using KKAPI.MainGame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OKPlug
{
    internal class TopMeController : GameCustomFunctionController
    {
        // list of states where the controller is allowed to think and pick the next action
        private static readonly List<string> ThinkStates = new List<string>()
        {
            "OUT_A", "A_OUT_A", "Idle", "A_Idle", "Vomit_A", "Drink_A"
        };

        // list of states with actions that we will automatically choose from
        private static readonly List<string> ActionableStates = new List<string>()
        {
            "IN_A", "A_IN_A", "Oral_Idle", "Idle"
        };

        private static readonly List<string> ReplayStates = new List<string>()
        {
            "IN_A", "A_IN_A"
        };

        private static readonly List<string> HLoops = new List<string>()
        {
            "WLoop", "SLoop", "A_WLoop", "A_SLoop", "InsertIdle", "A_InsertIdle", "OLoop", "A_OLoop"
        };

        private System.Random rand = new System.Random();
        private GameObject fakeAnimButton;

        private HSceneProc HScene;
        private HFlag Flags { get { return HScene?.flags; } }
        private HSprite Sprite { get { return HScene?.sprite; } }
        private int category = 0;

        private HSceneProc.AnimationListInfo CurrentAnimation { get { return Flags?.nowAnimationInfo; } }
        private string CurrentAnimationState { get { return Flags?.nowAnimStateName; } }

        private Button nextAction;
        private HSceneProc.AnimationListInfo nextAnimation;

        public bool HasOrgasmed { get; private set; }

        public List<Button> AvailableActions
        {
            get
            {
                if (Flags == null)
                {
                    return new List<Button>();
                }

                List<Button> menu;
                switch (Flags.mode)
                {
                    case HFlag.EMode.houshi:
                        menu = Sprite.houshi.categoryActionButton.lstButton;
                        break;
                    case HFlag.EMode.sonyu:
                        menu = Sprite.sonyu.categoryActionButton.lstButton;
                        break;
                    default:
                        return new List<Button>();
                }

                var choices = menu.Where(
                    button => button.isActiveAndEnabled && button.interactable
                ).ToList();

                return choices;
            }
        }

        public List<HSceneProc.AnimationListInfo> AvailableAnimations
        {
            get
            {
                if (HScene == null)
                {
                    return new List<HSceneProc.AnimationListInfo>();
                }

                return HScene.lstUseAnimInfo.SelectMany(
                        e => e,
                        (e, anim) => anim
                    ).Where(anim => anim.mode != HFlag.EMode.aibu).ToList();
            }
        }

        public bool IsAnimationOver
        {
            get
            {
                return ThinkStates.Contains(CurrentAnimationState) && HasOrgasmed && !Flags.voiceWait;
            }
        }

        public bool IsActionable
        {
            get
            {
                return AvailableActions.Count > 0 && !Flags.voiceWait;
            }
        }

        public bool IsInHLoop
        {
            get
            {
                return HLoops.Contains(CurrentAnimationState);
            }
        }

        public HActionBase CurrentHProc
        {
            get
            {
                var mode = ((int)Flags?.mode);
                if (mode == (int)HFlag.EMode.none)
                {
                    return null;
                }
                return HScene?.lstProc[mode];
            }
        }

        public void DoOrgasm()
        {
            HasOrgasmed = true;
        }

        public void ChangeAnimation(HSceneProc.AnimationListInfo nextAnimInfo)
        {
            HasOrgasmed = false;
            switch (nextAnimInfo.mode)
            {
                case HFlag.EMode.houshi:
                    Flags.rely = true;
                    HScene.rely.InitTimer();
                    break;
                case HFlag.EMode.sonyu:
                    // set to auto by default
                    Flags.click = HFlag.ClickKind.modeChange;
                    break;
            }
        }

        public void ChangeCategory(int category)
        {
            this.category = category;
        }

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            this.HScene = proc;

            fakeAnimButton = Instantiate(proc.sprite.objMotionListNode, gameObject.transform, false);
            fakeAnimButton.AddComponent<HSprite.AnimationInfoComponent>();
            fakeAnimButton.SetActive(true);

            StartCoroutine(PickAction());
            StartCoroutine(PickNextAnimation());
            StartCoroutine(AdjustSpeed());
            StartCoroutine(AdjustMotion());
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            Destroy(fakeAnimButton);
            StopAllCoroutines();
        }

        IEnumerator PickAction()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                nextAction = null;
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value && IsActionable);

                // ignore early orgasm buttons if auto orgasm is enabled
                if (TopMePlugin.AutoOrgasm.Value && Flags.gaugeMale < 100f && IsInHLoop)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // pause for a bit to let the player potentially choose an interaction
                yield return new WaitForSeconds(3f);

                // leverage simulating clicking in the menu
                var choices = AvailableActions;

                // choices might have disappeared during the wait time
                if (choices.Count <= 0)
                {
                    continue;
                }

                var index = rand.Next(0, choices.Count);
                nextAction = choices[index];

                InputSimulator.MouseButtonUp(0); // koikatsu actions check for left click mouse up
                nextAction?.onClick?.Invoke();
                InputSimulator.UnsetMouseButton(0);
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator PickNextAnimation()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.PickPosition.Value);
                yield return new WaitUntil(() => IsAnimationOver);
                yield return new WaitForSeconds((float)(rand.NextDouble() * 2.0) + 1f);
                yield return new WaitUntil(() => IsAnimationOver);

                var choices = AvailableAnimations;

                // THIS SHOULDN'T HAPPEN but catch it just in case
                if (choices.Count <= 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                nextAnimation = choices[rand.Next(0, choices.Count)];
                fakeAnimButton.GetComponent<HSprite.AnimationInfoComponent>().info = nextAnimation;
                Sprite.OnChangePlaySelect(fakeAnimButton);
            }
        }

        IEnumerator AdjustSpeed()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value);
                // don't need to do any behavior since koikatsu has this
                // out of the box for houshi
                if (Flags.mode == HFlag.EMode.houshi)
                {
                    if (!Flags.rely)
                    {
                        Flags.rely = true;
                        HScene.rely.InitTimer();
                    }
                    continue;
                }
                if (Flags.mode != HFlag.EMode.sonyu)
                {
                    continue;
                }

                var proc = (HSonyu)CurrentHProc;
                if (!HLoops.Contains(CurrentAnimationState))
                {
                    continue;
                }
                
                if (!proc.isAuto)
                {
                    Flags.click = HFlag.ClickKind.modeChange;
                    continue;
                }
                // TODO smooth this
                Flags.speedCalc = Mathf.Clamp01(Flags.speedCalc + ((float)rand.NextDouble() * .4f) - 0.2f);
                yield return new WaitForSeconds((float)rand.NextDouble() * 3f + 0.1f);
            }
        }

        IEnumerator AdjustMotion()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value &&
                    (
                        Flags.mode == HFlag.EMode.houshi ||
                        Flags.mode == HFlag.EMode.sonyu
                    )
                );
                yield return new WaitForSeconds((float)rand.NextDouble() * 5f + 5f);
                // occasionally change mode
                if (rand.NextDouble() < 0.1)
                {
                    Flags.click = HFlag.ClickKind.motionchange;
                }
            }
        }
    }
}
