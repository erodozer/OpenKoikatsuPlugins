using BepInEx.Unity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using KKAPI.MainGame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OKPlug
{
    internal enum EdgeMode
    {
        Disabled,
        Service,
        FemaleInitiated,
        AllPositions
    }

    internal class TopMeController : GameCustomFunctionController
    {
        // list of states where the controller is allowed to think and pick the next action
        private static readonly List<string> ThinkStates = new List<string>()
        {
            "OUT_A", "A_OUT_A", "Idle", "A_Idle", "Vomit_A", "Drink_A"
        };

        private static readonly List<string> HLoops = new List<string>()
        {
            "WLoop", "SLoop", "A_WLoop", "A_SLoop", "InsertIdle", "A_InsertIdle"
        };

        private static readonly List<string> ModeLoops = new List<string>()
        {
            "WLoop", "SLoop", "A_WLoop", "A_SLoop"
        };

        private static readonly Dictionary<HFlag.EMode, List<string>> EdgableLoops = new Dictionary<HFlag.EMode, List<string>>()
        {
            {  
                HFlag.EMode.houshi, 
                new List<string>(){
                    "WLoop", "SLoop", "OLoop"
                } 
            },
            {  
                HFlag.EMode.sonyu, 
                new List<string>() {
                    "WLoop", "SLoop", "A_WLoop", "A_SLoop"
                } 
            }
        };

        private System.Random rand = new System.Random();
        private GameObject fakeAnimButton;

        private List<HActionBase> lstProc;
        private Traverse<List<HSceneProc.AnimationListInfo>[]> lstUseAnimInfo;
        private HFlag Flags;
        private HSprite Sprite;

        private HSceneProc.AnimationListInfo CurrentAnimation { get { return Flags?.nowAnimationInfo; } }
        private string CurrentAnimationState { get { return Flags?.nowAnimStateName; } }

        private Button nextAction;
        private HSceneProc.AnimationListInfo nextAnimation;

        private bool IsEdging = false;
        private float EdgeTimer = 0;

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

                if (lstUseAnimInfo == null)
                {
                    return new List<HSceneProc.AnimationListInfo>();
                }

                return lstUseAnimInfo.Value.SelectMany(
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
                return lstProc[mode];
            }
        }

        public void DoOrgasm()
        {
            HasOrgasmed = true;
        }

        public void ChangeAnimation(HSceneProc.AnimationListInfo nextAnimInfo)
        {
            HasOrgasmed = false;

            if (Flags == null)
            {
                return;
            }

            switch (nextAnimInfo?.mode)
            {
                case HFlag.EMode.houshi:
                    Flags.rely = true;
                    Sprite.rely.InitTimer();
                    break;
                case HFlag.EMode.sonyu:
                    // set to auto by default
                    Flags.click = HFlag.ClickKind.modeChange;
                    break;
            }

            ResetEdgeTimer();
        }

        protected override void OnStartH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            lstUseAnimInfo = Traverse.Create(proc).Field<List<HSceneProc.AnimationListInfo>[]>("lstUseAnimInfo");
            lstProc = Traverse.Create(proc).Field<List<HActionBase>>("lstProc").Value;
            Flags = hFlag;
            Sprite = Traverse.Create(proc).Field<HSprite>("sprite").Value;

            IsEdging = false;

            fakeAnimButton = Instantiate(Sprite.objMotionListNode, gameObject.transform, false);
            fakeAnimButton.AddComponent<HSprite.AnimationInfoComponent>();
            fakeAnimButton.SetActive(true);

            StartCoroutine(PickAction());
            StartCoroutine(PickNextAnimation());
            StartCoroutine(AdjustSpeed());
            StartCoroutine(AdjustMotion());
            StartCoroutine(EdgeMe());
        }

        protected override void OnEndH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            StopAllCoroutines();
            Destroy(fakeAnimButton);
            nextAction = null;
            nextAnimation = null;

            lstUseAnimInfo = null;
            lstProc = null;
            Sprite = null;
            Flags = null;
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

        void ResetEdgeTimer()
        {
            EdgeTimer = (float)rand.NextDouble() * 40f + 20f;
            IsEdging = false;
        }

        IEnumerator EdgeMe()
        {
            yield return new WaitUntil(() => Flags != null && Sprite != null);
            ResetEdgeTimer();
            var edgeFor = 0f;
            while (true)
            {
                yield return new WaitUntil(() => 
                    (TopMePlugin.RelySonyu.Value && TopMePlugin.Edge.Value != EdgeMode.Disabled)
                    && (
                        TopMePlugin.Edge.Value != EdgeMode.AllPositions
                        || (CurrentAnimation.mode == HFlag.EMode.houshi && TopMePlugin.Edge.Value == EdgeMode.Service)
                        || (CurrentAnimation.mode == HFlag.EMode.houshi && TopMePlugin.Edge.Value == EdgeMode.FemaleInitiated)
                        || (CurrentAnimation.mode == HFlag.EMode.sonyu && CurrentAnimation.isFemaleInitiative && TopMePlugin.Edge.Value == EdgeMode.FemaleInitiated)
                    )
                );

                // decrease male gauge while edging
                if (IsEdging && edgeFor > 0)
                {
                    Flags.MaleGaugeUp(-5 * Time.deltaTime);
                    edgeFor -= Time.deltaTime;
                    continue;
                }

                // reset back to edge loop when edging is complete
                if (IsEdging && edgeFor <= 0)
                {
                    if (Flags.mode == HFlag.EMode.houshi)
                    {
                        Flags.rely = true;
                        CurrentHProc.MotionChange(1);
                    }
                    ResetEdgeTimer();
                    continue;
                }

                // wait until next edge moment
                if (EdgeTimer > 0)
                {
                    EdgeTimer -= Time.deltaTime;
                    continue;
                }

                // only edge when appropriate
                if (!(EdgableLoops.ContainsKey(Flags.mode) &&
                    EdgableLoops[Flags.mode].Contains(CurrentAnimationState)))
                {
                    continue;
                }
                
                // koikatsu's proc loop uses animation state name instead of gauge
                // to know what to go to.  It doesn't understand how to handle the
                // gauge going down except after orgasm, so we forceably will move
                // back to idle.
                if (Flags.mode == HFlag.EMode.houshi)
                {
                    CurrentHProc.MotionChange(0);
                    Flags.rely = false;
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
                else
                {
                    continue;
                }
                IsEdging = true;
                edgeFor = (float)rand.NextDouble() * 5f + 5f;
            }
        }
    }
}
