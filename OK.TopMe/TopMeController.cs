using BepInEx.Unity;
using KKAPI.MainGame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OKPlug
{
    internal class TopMeController : GameCustomFunctionController
    {
        // list of states where the controller is allowed to think and pick the next action
        private static readonly List<string> ThinkStates = new List<string>()
        {
            "OUT_A", "A_OUT_A", "Idle", "A_Idle", "Vomit_A", "Drink_A"
        };

        private static readonly List<string> HLoops = new List<string>()
        {
            "WLoop", "SLoop", "A_WLoop", "A_SLoop", "InsertIdle", "A_InsertIdle", "OLoop", "A_OLoop"
        };

        private static readonly List<string> ModeLoops = new List<string>()
        {
            "WLoop", "SLoop", "A_WLoop", "A_SLoop"
        };

        private System.Random rand = new System.Random();
        private GameObject fakeAnimButton;

        private HSceneProc HScene;
        private HFlag Flags { get { return HScene?.flags; } }
        private HSprite Sprite { get { return HScene?.sprite; } }

        private HSceneProc.AnimationListInfo CurrentAnimation { get { return Flags?.nowAnimationInfo; } }
        private string CurrentAnimationState { get { return Flags?.nowAnimStateName; } }

        private Button nextAction;
        private HSceneProc.AnimationListInfo nextAnimation;

        private bool IsEdging = false;

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

            if (Flags == null || HScene == null)
            {
                return;
            }

            switch (nextAnimInfo?.mode)
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

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            this.HScene = proc;

            IsEdging = false;

            fakeAnimButton = Instantiate(proc.sprite.objMotionListNode, gameObject.transform, false);
            fakeAnimButton.AddComponent<HSprite.AnimationInfoComponent>();
            fakeAnimButton.SetActive(true);

            StartCoroutine(PickAction());
            StartCoroutine(PickNextAnimation());
            StartCoroutine(AdjustSpeed());
            StartCoroutine(AdjustMotion());
            StartCoroutine(EdgeMe());
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            StopAllCoroutines();
            Destroy(fakeAnimButton);
            nextAction = null;
            nextAnimation = null;
        }

        internal void ForceStartH(HSceneProc proc, bool freeH)
        {
            OnStartH(proc, freeH);
        }

        internal void ForceEndH(HSceneProc proc, bool freeH)
        {
            OnEndH(proc, freeH);
        }

        IEnumerator PickAction()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
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
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                var index = rand.Next(0, choices.Count);
                nextAction = choices[index];

                InputSimulator.MouseButtonUp(0); // koikatsu actions check for left click mouse up
                nextAction?.onClick?.Invoke();
                InputSimulator.UnsetMouseButton(0);
                yield return new WaitForSeconds(3f);
                nextAction = null;
            }
        }

        IEnumerator PickNextAnimation()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.PickPosition.Value && IsAnimationOver);
                
                var choices = AvailableAnimations;

                // THIS SHOULDN'T HAPPEN but catch it just in case
                if (choices.Count <= 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                nextAnimation = choices[rand.Next(0, choices.Count)];
                fakeAnimButton.GetComponent<HSprite.AnimationInfoComponent>().info = nextAnimation;
                fakeAnimButton.GetComponent<Toggle>().isOn = false;
                Sprite.OnChangePlaySelect(fakeAnimButton);
                fakeAnimButton.GetComponent<HSprite.AnimationInfoComponent>().info = null;

                yield return new WaitForSeconds(3f);
            }
        }

        IEnumerator AdjustSpeed()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value);
                
                if (Flags.mode == HFlag.EMode.sonyu)
                {
                    var proc = (HSonyu)CurrentHProc;
                    if (IsEdging)
                    {
                        
                        continue;
                    }

                    if (!IsInHLoop)
                    {
                        continue;
                    }

                    if (!proc.isAuto)
                    {
                        Flags.click = HFlag.ClickKind.modeChange;
                        continue;
                    }

                    var target = (float)rand.NextDouble();
                    var start = Flags.speedCalc;
                    for (var d = 0.0f; d < 1.0f; d += Time.deltaTime)
                    {
                        Flags.speedCalc = Mathf.Lerp(start, target, d);
                        yield return new WaitForEndOfFrame();
                    }
                    Flags.speedCalc = Mathf.Lerp(start, target, 1.0f);
                    yield return new WaitForSeconds((float)rand.NextDouble() * 3f + 0.1f);
                }

                if (Flags.mode == HFlag.EMode.houshi)
                {
                    var proc = (HHoushi)CurrentHProc;
                    if (IsEdging)
                    {
                        Flags.rely = false;
                    }
                    else if (!Flags.rely)
                    {
                        Flags.rely = true;
                        HScene.rely.InitTimer();
                    }
                }
            }
        }

        IEnumerator AdjustMotion()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value &&
                    (
                        Flags.mode == HFlag.EMode.sonyu
                    )
                );

                if (IsEdging)
                {
                    continue;
                }

                // occasionally change mode
                if (ModeLoops.Contains(Flags.nowAnimStateName) && rand.NextDouble() < 0.1)
                {
                    yield return new WaitForSeconds((float)rand.NextDouble() * 5f + 2f);
                    Flags.click = HFlag.ClickKind.motionchange;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator EdgeMe()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            while (true)
            {
                yield return new WaitUntil(() => TopMePlugin.RelySonyu.Value &&
                    (
                        Flags.mode == HFlag.EMode.houshi
                        || (Flags.mode == HFlag.EMode.sonyu && Flags.nowAnimationInfo.isFemaleInitiative)
                    )
                );

                if (Flags.gaugeMale > 50 && rand.NextDouble() < .05f)
                {
                    if (Flags.mode == HFlag.EMode.houshi)
                    {
                        CurrentHProc.MotionChange(0);
                    }
                    else if (Flags.mode == HFlag.EMode.sonyu)
                    {
                        var proc = (HSonyu)CurrentHProc;
                        if (proc.isAuto)
                        {
                            Flags.click = HFlag.ClickKind.modeChange;
                        }
                        Flags.speedCalc = 0;
                    }
                    IsEdging = true;
                    var edgeFor = (float)rand.NextDouble() * 5f + 5f;
                    while (edgeFor > 0)
                    {
                        Flags.MaleGaugeUp(-8 * Time.deltaTime);
                        edgeFor -= Time.deltaTime;
                        yield return new WaitForEndOfFrame();
                    }
                    IsEdging = false;
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }
    }
}
