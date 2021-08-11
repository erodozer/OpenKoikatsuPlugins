using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OKPlug
{
    class VRHSceneAdapter : HSceneProc
    {
        private MonoBehaviour vrHScene;

        public void InitAdapter(MonoBehaviour vrHScene)
        {
            this.vrHScene = vrHScene;
            StartCoroutine(UpdateFields());
        }

        private IEnumerator UpdateFields()
        {
            while (true) {
                flags = (HFlag)vrHScene.GetType()
                    .GetField("flags")
                    .GetValue(vrHScene);
                sprite = ((HSprite[])vrHScene.GetType()
                    .GetField("sprites")
                    .GetValue(vrHScene))[0];
                rely = (AutoRely)vrHScene.GetType()
                    .GetField("rely")
                    .GetValue(vrHScene);
                lstUseAnimInfo = (List<AnimationListInfo>[])vrHScene.GetType()
                    .GetField("lstUseAnimInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(vrHScene);
                lstProc = (List<HActionBase>)vrHScene.GetType()
                    .GetField("lstProc", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(vrHScene);
                yield return null;
            }
        }

        private void Update() { }
        private void LateUpdate() { }
        private void OnDestroy() { }
    }
}
