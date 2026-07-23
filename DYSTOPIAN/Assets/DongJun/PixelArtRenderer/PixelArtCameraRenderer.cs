using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(32000)]
public sealed class PixelArtCameraRenderer : MonoBehaviour
{
    [Header("Internal Resolution")]
    [SerializeField, Range(90, 360)] private int internalHeight = 180;
    [SerializeField] private bool snapCameraToPixelGrid = true;

    [Header("Display")]
    [SerializeField] private Material displayMaterial;

    private Camera sourceCamera;
    private RenderTexture lowResolutionTarget;
    private GameObject outputCanvasRoot;

    private Camera displayCamera;
    private RawImage outputImage;
    private int activeWidth;
    private int activeHeight;

    public int ActiveWidth => activeWidth;
    public int ActiveHeight => activeHeight;
    public RenderTexture LowResolutionTarget => lowResolutionTarget;

    private void Awake()
    {
        sourceCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        sourceCamera = GetComponent<Camera>();

        if (Application.isPlaying)
        {
            ConfigureCameraForHardPixels();
            RebuildOutput();
        }
    }

    private void Start()
    {
        if (Application.isPlaying && lowResolutionTarget == null)
        {
            ConfigureCameraForHardPixels();
            RebuildOutput();
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureResolutionMatchesScreen();

        if (snapCameraToPixelGrid && sourceCamera.orthographic)
        {
            SnapCameraPosition();
        }
    }

    private void ConfigureCameraForHardPixels()
    {
        sourceCamera.allowMSAA = false;
        sourceCamera.allowDynamicResolution = false;

        UniversalAdditionalCameraData cameraData =
            GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.renderPostProcessing = false;
            cameraData.dithering = false;
        }
    }

    private void EnsureResolutionMatchesScreen()
    {
        int screenHeight = Mathf.Max(1, Screen.height);
        int desiredHeight = Mathf.Max(1, internalHeight);
        int desiredWidth = Mathf.Max(
            1,
            Mathf.RoundToInt(desiredHeight * (Screen.width / (float)screenHeight)));

        if (desiredWidth != activeWidth
            || desiredHeight != activeHeight
            || lowResolutionTarget == null)
        {
            RebuildOutput();
        }
    }

    private void RebuildOutput()
    {
        ReleaseRenderTexture();

        int screenHeight = Mathf.Max(1, Screen.height);
        activeHeight = Mathf.Max(1, internalHeight);
        activeWidth = Mathf.Max(
            1,
            Mathf.RoundToInt(activeHeight * (Screen.width / (float)screenHeight)));

        lowResolutionTarget = new RenderTexture(
            activeWidth,
            activeHeight,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default)
        {
            name = $"PixelArt_{activeWidth}x{activeHeight}",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            anisoLevel = 0,
            useDynamicScale = false
        };

        lowResolutionTarget.Create();
        sourceCamera.targetTexture = lowResolutionTarget;
        EnsureOutputCanvas();

        outputImage.texture = lowResolutionTarget;
        outputImage.material = displayMaterial;
    }

    private void EnsureOutputCanvas()
    {
        if (outputCanvasRoot != null
            && outputImage != null
            && displayCamera != null)
        {
            displayCamera.targetDisplay = sourceCamera.targetDisplay;
            return;
        }

        outputCanvasRoot = new GameObject(
            "__PIXEL_ART_OUTPUT",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        outputCanvasRoot.hideFlags = HideFlags.DontSave;

        GameObject displayCameraObject = new GameObject(
            "DisplayCamera",
            typeof(Camera));

        displayCameraObject.hideFlags = HideFlags.DontSave;
        displayCameraObject.transform.SetParent(outputCanvasRoot.transform, false);

        displayCamera = displayCameraObject.GetComponent<Camera>();
        displayCamera.clearFlags = CameraClearFlags.SolidColor;
        displayCamera.backgroundColor = Color.black;
        displayCamera.cullingMask = 0;
        displayCamera.depth = -1000f;
        displayCamera.targetDisplay = sourceCamera.targetDisplay;
        displayCamera.targetTexture = null;
        displayCamera.allowHDR = false;
        displayCamera.allowMSAA = false;
        displayCamera.allowDynamicResolution = false;
        displayCamera.useOcclusionCulling = false;
        displayCamera.orthographic = true;

        UniversalAdditionalCameraData displayCameraData =
            displayCamera.GetUniversalAdditionalCameraData();
        displayCameraData.antialiasing = AntialiasingMode.None;
        displayCameraData.renderPostProcessing = false;
        displayCameraData.dithering = false;

        Canvas canvas = outputCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MinValue;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = outputCanvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GameObject imageObject = new GameObject(
            "NearestNeighbourDisplay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        imageObject.transform.SetParent(outputCanvasRoot.transform, false);

        RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
        imageTransform.anchorMin = Vector2.zero;
        imageTransform.anchorMax = Vector2.one;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;

        outputImage = imageObject.GetComponent<RawImage>();
        outputImage.raycastTarget = false;
        outputImage.color = Color.white;
    }

    private void SnapCameraPosition()
    {
        float unitsPerPixel =
            (sourceCamera.orthographicSize * 2f) / Mathf.Max(1, activeHeight);

        Vector3 position = transform.position;
        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        transform.position = position;
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        ReleaseRenderTexture();

        if (outputCanvasRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(outputCanvasRoot);
            }
            else
            {
                DestroyImmediate(outputCanvasRoot);
            }
        }

        outputCanvasRoot = null;
        outputImage = null;
        displayCamera = null;
    }

    private void ReleaseRenderTexture()
    {
        if (sourceCamera != null
            && sourceCamera.targetTexture == lowResolutionTarget)
        {
            sourceCamera.targetTexture = null;
        }

        if (lowResolutionTarget == null)
        {
            return;
        }

        lowResolutionTarget.Release();

        if (Application.isPlaying)
        {
            Destroy(lowResolutionTarget);
        }
        else
        {
            DestroyImmediate(lowResolutionTarget);
        }

        lowResolutionTarget = null;
    }
}
