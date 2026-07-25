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
    private UniversalAdditionalCameraData sourceCameraData;
    private RenderTexture lowResolutionTarget;
    private RenderTexture originalTargetTexture;
    private GameObject outputCanvasRoot;
    private Canvas outputCanvas;
    private RawImage outputImage;
    private GameObject displayCameraRoot;
    private Camera displayCamera;
    private int activeWidth;
    private int activeHeight;
    private bool originalAllowMsaa;
    private bool originalAllowDynamicResolution;
    private AntialiasingMode originalAntialiasing;
    private bool originalPostProcessing;
    private bool originalDithering;
    private bool sourceSettingsCached;

    private void Awake()
    {
        sourceCamera = GetComponent<Camera>();
        sourceCameraData = GetComponent<UniversalAdditionalCameraData>();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        CacheSourceCameraSettings();
        ConfigureSourceCamera();
        EnsureDisplayCamera();
        EnsureOutputCanvas();
        RebuildOutput();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        EnsureDisplayCamera();
        EnsureResolutionMatchesScreen();

        if (snapCameraToPixelGrid && sourceCamera.orthographic)
            SnapCameraPosition();
    }

    private void CacheSourceCameraSettings()
    {
        if (sourceSettingsCached)
            return;

        originalTargetTexture = sourceCamera.targetTexture;
        originalAllowMsaa = sourceCamera.allowMSAA;
        originalAllowDynamicResolution = sourceCamera.allowDynamicResolution;

        if (sourceCameraData != null)
        {
            originalAntialiasing = sourceCameraData.antialiasing;
            originalPostProcessing = sourceCameraData.renderPostProcessing;
            originalDithering = sourceCameraData.dithering;
        }

        sourceSettingsCached = true;
    }

    private void ConfigureSourceCamera()
    {
        sourceCamera.allowMSAA = false;
        sourceCamera.allowDynamicResolution = false;

        if (sourceCameraData == null)
            return;

        sourceCameraData.antialiasing = AntialiasingMode.None;
        sourceCameraData.renderPostProcessing = false;
        sourceCameraData.dithering = false;
    }

    private void EnsureResolutionMatchesScreen()
    {
        CalculateTargetResolution(out int desiredWidth, out int desiredHeight);

        if (desiredWidth != activeWidth ||
            desiredHeight != activeHeight ||
            lowResolutionTarget == null)
        {
            RebuildOutput();
        }
    }

    private void CalculateTargetResolution(out int width, out int height)
    {
        int screenHeight = Mathf.Max(1, Screen.height);
        height = Mathf.Max(1, internalHeight);
        width = Mathf.Max(
            1,
            Mathf.RoundToInt(height * (Screen.width / (float)screenHeight)));
    }

    private void RebuildOutput()
    {
        ReleaseRenderTexture();
        CalculateTargetResolution(out activeWidth, out activeHeight);

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
        outputCanvasRoot.SetActive(true);
        outputImage.texture = lowResolutionTarget;
        outputImage.material = displayMaterial;
    }

    private void EnsureDisplayCamera()
    {
        if (displayCameraRoot == null || displayCamera == null)
        {
            displayCameraRoot = new GameObject(
                "__PIXEL_ART_DISPLAY_CAMERA",
                typeof(Camera),
                typeof(UniversalAdditionalCameraData));
            displayCameraRoot.hideFlags = HideFlags.DontSave;

            displayCamera = displayCameraRoot.GetComponent<Camera>();
            displayCamera.clearFlags = CameraClearFlags.Nothing;
            displayCamera.backgroundColor = Color.clear;
            displayCamera.cullingMask = 0;
            displayCamera.depth = sourceCamera.depth + 100f;
            displayCamera.orthographic = true;
            displayCamera.allowHDR = false;
            displayCamera.allowMSAA = false;
            displayCamera.allowDynamicResolution = false;
            displayCamera.useOcclusionCulling = false;

            UniversalAdditionalCameraData displayCameraData =
                displayCameraRoot.GetComponent<UniversalAdditionalCameraData>();
            displayCameraData.renderShadows = false;
            displayCameraData.renderPostProcessing = false;
            displayCameraData.antialiasing = AntialiasingMode.None;
            displayCameraData.dithering = false;
        }

        displayCamera.targetTexture = null;
        displayCamera.targetDisplay = sourceCamera.targetDisplay;
        displayCamera.depth = sourceCamera.depth + 100f;
        displayCamera.enabled = true;

        if (!displayCameraRoot.activeSelf)
            displayCameraRoot.SetActive(true);
    }

    private void EnsureOutputCanvas()
    {
        if (outputCanvasRoot != null && outputImage != null)
        {
            outputCanvas.targetDisplay = sourceCamera.targetDisplay;
            return;
        }

        outputCanvasRoot = new GameObject(
            "__PIXEL_ART_OUTPUT",
            typeof(RectTransform),
            typeof(Canvas));
        outputCanvasRoot.hideFlags = HideFlags.DontSave;

        outputCanvas = outputCanvasRoot.GetComponent<Canvas>();
        outputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        outputCanvas.overrideSorting = true;
        outputCanvas.sortingOrder = short.MinValue;
        outputCanvas.pixelPerfect = true;
        outputCanvas.targetDisplay = sourceCamera.targetDisplay;

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
            sourceCamera.orthographicSize * 2f / Mathf.Max(1, activeHeight);

        Vector3 position = transform.position;
        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        transform.position = position;
    }

    private void OnDisable()
    {
        ReleaseRenderTexture();
        RestoreSourceCameraSettings();

        if (outputCanvasRoot != null)
            outputCanvasRoot.SetActive(false);

        if (displayCameraRoot != null)
            displayCameraRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
        RestoreSourceCameraSettings();

        DestroyRuntimeObject(outputCanvasRoot);
        DestroyRuntimeObject(displayCameraRoot);
    }

    private static void DestroyRuntimeObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void RestoreSourceCameraSettings()
    {
        if (!sourceSettingsCached)
            return;

        sourceCamera.targetTexture = originalTargetTexture;
        sourceCamera.allowMSAA = originalAllowMsaa;
        sourceCamera.allowDynamicResolution = originalAllowDynamicResolution;

        if (sourceCameraData != null)
        {
            sourceCameraData.antialiasing = originalAntialiasing;
            sourceCameraData.renderPostProcessing = originalPostProcessing;
            sourceCameraData.dithering = originalDithering;
        }

        sourceSettingsCached = false;
    }

    private void ReleaseRenderTexture()
    {
        if (sourceCamera != null &&
            sourceCamera.targetTexture == lowResolutionTarget)
        {
            sourceCamera.targetTexture = originalTargetTexture;
        }

        if (lowResolutionTarget == null)
            return;

        lowResolutionTarget.Release();

        if (Application.isPlaying)
            Destroy(lowResolutionTarget);
        else
            DestroyImmediate(lowResolutionTarget);

        lowResolutionTarget = null;

        if (outputImage != null)
            outputImage.texture = null;
    }
}
