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

            if (sprite == null)
            {
                HideVisual();
                return;
            }

            if (visualRoot == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "BillboardQuad";
                Destroy(quad.GetComponent<Collider>());
                visualRoot = quad.transform;
                visualRoot.SetParent(transform, false);
            }

            visualRoot.gameObject.SetActive(true);
            visualRoot.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visualRoot.localScale = new Vector3(height * 0.75f, height, 1f);

            var renderer = visualRoot.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Texture");
            var mat = new Material(shader);
            mat.mainTexture = sprite.texture;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", sprite.texture);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            renderer.material = mat;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || visualRoot == null || !visualRoot.gameObject.activeSelf)
                return;

            visualRoot.rotation = _cameraTransform.rotation;
        }

        private void HideVisual()
        {
            if (visualRoot != null)
                visualRoot.gameObject.SetActive(false);
        }
    }
}
