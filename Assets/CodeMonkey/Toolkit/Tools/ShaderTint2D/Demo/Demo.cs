using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeMonkey.Toolkit.TShaderTint.Demo {

    public class Demo : MonoBehaviour {


        private const string TINT = "_Tint";


        [SerializeField] private Material material;
        [SerializeField] private Material material2;
        [ColorUsage(true, true)]
        [SerializeField] private Color[] tintColorArray;


        private float flashYAmount;
        private int tintColorArrayIndex;


        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                flashYAmount = 1f;
                tintColorArrayIndex = (tintColorArrayIndex + 1) % tintColorArray.Length;
                material.SetColor(TINT, tintColorArray[tintColorArrayIndex]);
                material2.SetColor(TINT, tintColorArray[tintColorArrayIndex]);
            }

            float flashDropYAmount = 1.6f;
            flashYAmount -= Time.deltaTime * flashDropYAmount;
            flashYAmount = Mathf.Clamp01(flashYAmount);

            Color tintColor = material.GetColor(TINT);
            tintColor.a = flashYAmount;

            material.SetColor(TINT, tintColor);

            Color tintColor2 = material2.GetColor(TINT);
            tintColor2.a = flashYAmount;

            material2.SetColor(TINT, tintColor2);
        }


    }
}