using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeMonkey.Toolkit.TShaderBlur.Demo {

    public class Demo : MonoBehaviour {


        private const string BLUR_AMOUNT = "_BlurAmount";


        [SerializeField] private Material material;


        private float blurAmount;
        private bool blurActive;


        private void Start() {
            blurAmount = 0;
        }

        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                blurActive = !blurActive;
            }

            float blurSpeed = 15f;
            if (blurActive) {
                blurAmount += blurSpeed * Time.deltaTime;
            } else {
                blurAmount -= blurSpeed * Time.deltaTime;
            }

            blurAmount = Mathf.Clamp(blurAmount, 0f, 3f);
            material.SetFloat(BLUR_AMOUNT, blurAmount);
        }

    }

}