using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeMonkey.Toolkit.TShaderTransitionEFfect.Demo {

    public class Demo : MonoBehaviour {


        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                ShaderTransitionEffect.Show(() => {
                    Debug.Log("Fully Black");
                });
            }
            if (Keyboard.current.yKey.wasPressedThisFrame) {
                ShaderTransitionEffect.Hide(() => {
                    Debug.Log("Fully Transparent");
                });
            }
        }

    }

}