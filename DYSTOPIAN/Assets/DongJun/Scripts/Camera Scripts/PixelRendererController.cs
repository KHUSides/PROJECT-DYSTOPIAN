using UnityEngine;
using UnityEngine.UI;

public class PixelRenderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RawImage displayImage;

    [Header("Pixel Resolution")]
    [SerializeField] private int verticalResolution = 270;
    [SerializeField] private bool matchScreenAspect = true;
    [SerializeField] private Vector2Int fixedResolution = new Vector2Int(480, 270);

    [Header("Texture")]
    [SerializeField] private FilterMode filterMode = FilterMode.Point;

    private RenderTexture pixelTexture;
    private int currentWidth;
    private int currentHeight;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        CreateOrResizeTexture();
    }

    private void Update()
    {
        int desiredWidth;
        int desiredHeight;

        GetDesiredResolution(out desiredWidth, out desiredHeight);

        if (desiredWidth != currentWidth || desiredHeight != currentHeight)
        {
            CreateOrResizeTexture();
        }
    }

    private void OnDisable()
    {
        if (worldCamera != null)
        {
            worldCamera.targetTexture = null;
        }

        if (pixelTexture != null)
        {
            pixelTexture.Release();
            Destroy(pixelTexture);
            pixelTexture = null;
        }
    }

    private void CreateOrResizeTexture()
    {
        int desiredWidth;
        int desiredHeight;

        GetDesiredResolution(out desiredWidth, out desiredHeight);

        currentWidth = Mathf.Max(1, desiredWidth);
        currentHeight = Mathf.Max(1, desiredHeight);

        if (pixelTexture != null)
        {
            pixelTexture.Release();
            Destroy(pixelTexture);
        }

        pixelTexture = new RenderTexture(currentWidth, currentHeight, 24);
        pixelTexture.name = $"Pixel Render Texture {currentWidth}x{currentHeight}";
        pixelTexture.filterMode = filterMode;
        pixelTexture.wrapMode = TextureWrapMode.Clamp;
        pixelTexture.antiAliasing = 1;
        pixelTexture.useMipMap = false;
        pixelTexture.autoGenerateMips = false;
        pixelTexture.Create();

        if (worldCamera != null)
        {
            worldCamera.targetTexture = pixelTexture;
        }

        if (displayImage != null)
        {
            displayImage.texture = pixelTexture;
        }

        Debug.Log($"[CameraLog] Pixel Render Texture Created: {currentWidth}x{currentHeight}");
    }

    private void GetDesiredResolution(out int width, out int height)
    {
        if (!matchScreenAspect)
        {
            width = fixedResolution.x;
            height = fixedResolution.y;
            return;
        }

        height = verticalResolution;

        float aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
        width = Mathf.RoundToInt(height * aspect);
    }
}