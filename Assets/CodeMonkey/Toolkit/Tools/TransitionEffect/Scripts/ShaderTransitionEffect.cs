using System;
using UnityEngine;
using UnityEngine.UI;

namespace CodeMonkey.Toolkit.TShaderTransitionEFfect {

    /// <summary>
    /// ** Shader Transition Effect **
    /// 
    /// Smooth shader effect, perfect for scene transitions.
    /// You can draw the Texture yourself, just make it greyscale
    /// The white parts transition after the grey and dark grey parts.
    /// 
    /// To setup: Just call ShaderTransitionEffect.Show(); or manually drag the Prefab onto your Canvas 
    ///           Use the *onComplete* callback for example to load the next scene
    /// </summary>
    public class ShaderTransitionEffect : MonoBehaviour {



        private static ShaderTransitionEffect instance;


        private static void Init() {
            if (instance == null) {
                Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
                if (canvas == null) {
                    Debug.LogError("No Canvas was found in Scene! " + nameof(ShaderTransitionEffect) + " needs a Canvas to work.");
                    return;
                }
                ShaderTransitionEffect shaderTransitionEffect = Resources.Load<ShaderTransitionEffect>(nameof(ShaderTransitionEffect));
                if (shaderTransitionEffect == null) {
                    Debug.LogError("Could not find " + nameof(ShaderTransitionEffect) + " in Resources! Is the prefab inside a folder named exactly 'Resources'? And is the prefab named exactly '" + nameof(ShaderTransitionEffect) + "'?");
                    return;
                }
                instance = Instantiate(shaderTransitionEffect, canvas.transform);
            }
        }



        private const string MASK_AMOUNT = "_MaskAmount";


        [SerializeField] private Material material;
        [SerializeField] private float changeSpeed = 6f;


        private Action onComplete;
        private Image image;
        private float maskAmount = 0f;
        private float maskAmountTarget = 0f;


        private void Awake() {
            instance = this;

            image = GetComponent<Image>();
        }

        private void Update() {
            float maskAmountChange = maskAmountTarget > maskAmount ? +.1f : -.1f;
            maskAmount += maskAmountChange * Time.deltaTime * changeSpeed;
            maskAmount = Mathf.Clamp01(maskAmount);

            if (onComplete != null) {
                if (maskAmount == 0 || maskAmount == 1) {
                    Action backupOnComplete = onComplete;
                    onComplete = null;
                    backupOnComplete();
                }
            }

            material.SetFloat(MASK_AMOUNT, maskAmount);

            image.enabled = maskAmount > 0;
        }

        public void Show_Instance(Action onComplete = null) {
            this.onComplete = onComplete;

            maskAmountTarget = 1f;

            transform.SetAsLastSibling();
        }

        public void Hide_Instance(Action onComplete = null) {
            this.onComplete = onComplete;

            maskAmountTarget = 0f;

            transform.SetAsLastSibling();
        }

        public static void Show(Action onComplete = null) {
            Init();
            instance.Show_Instance(onComplete);
        }

        public static void Hide(Action onComplete = null) {
            Init();
            instance.Hide_Instance(onComplete);
        }

    }

}