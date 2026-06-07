using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeMonkey.Toolkit.TShaderPixelate.Demo {

    public class Demo : MonoBehaviour {


        private const string PIXELATE_AMOUNT = "_PixelateAmount";


        [SerializeField] private Material material;


        private float pixelateAmount;
        private bool pixelateActive;


        private void Start() {
            pixelateAmount = 0;
        }

        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                pixelateActive = !pixelateActive;
            }

            float dissolvespeed = 2f;
            if (pixelateActive) {
                pixelateAmount += dissolvespeed * Time.deltaTime;
            } else {
                pixelateAmount -= dissolvespeed * Time.deltaTime;
            }

            pixelateAmount = Mathf.Clamp(pixelateAmount, 0f, .8f);
            material.SetFloat(PIXELATE_AMOUNT, pixelateAmount);
        }


    }

}