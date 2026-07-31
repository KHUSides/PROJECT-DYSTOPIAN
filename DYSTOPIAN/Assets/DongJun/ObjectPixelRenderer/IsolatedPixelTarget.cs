using UnityEngine;

namespace Dystopian.ObjectPixel
{
    [DisallowMultipleComponent]
    public sealed class IsolatedPixelTarget : MonoBehaviour
    {
        [Tooltip("Must match the Pixel Object Layer on the camera renderer.")]
        [Range(0, 31)]
        public int pixelLayer = 30;

        private void Awake()
        {
            ApplyLayer(transform);
        }

        private void OnEnable()
        {
            ApplyLayer(transform);
        }

        private void ApplyLayer(Transform root)
        {
            root.gameObject.layer = pixelLayer;

            for (int i = 0; i < root.childCount; i++)
                ApplyLayer(root.GetChild(i));
        }

        private void OnValidate()
        {
            pixelLayer = Mathf.Clamp(pixelLayer, 0, 31);
        }
    }
}
