using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatBillboard : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        private Transform _cameraTransform;

        public void Configure(Transform cameraTransform, Sprite sprite, float height = 1.6f)
        {
            _cameraTransform = cameraTransform;
            if (visualRoot == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "BillboardQuad";
                Destroy(quad.GetComponent<Collider>());
                visualRoot = quad.transform;
                visualRoot.SetParent(transform, false);
            }

            visualRoot.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visualRoot.localScale = new Vector3(height * 0.75f, height, 1f);

            var renderer = visualRoot.GetComponent<MeshRenderer>();
            if (renderer != null && sprite != null)
            {
                var mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = sprite.texture;
                renderer.material = mat;
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || visualRoot == null)
                return;

            visualRoot.rotation = _cameraTransform.rotation;
        }
    }
}
