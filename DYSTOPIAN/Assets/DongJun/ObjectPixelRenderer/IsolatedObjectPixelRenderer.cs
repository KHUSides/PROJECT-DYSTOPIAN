using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dystopian.ObjectPixel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class IsolatedObjectPixelRenderer : MonoBehaviour
    {
        private const string EffectShaderName =
            "Hidden/DYSTOPIAN/IsolatedObjectPixelComposite";
        private const string DisplayShaderName =
            "Hidden/DYSTOPIAN/IsolatedObjectPixelDisplay";

        [Header("Target isolation")]
        [Tooltip("Only objects on this layer enter the pixel buffer.")]
        [Range(0, 31)]
        public int pixelObjectLayer = 30;

        [Tooltip("Internal layer used by the fullscreen composite quad.")]
        [Range(0, 31)]
        public int compositeLayer = 29;

        [Header("Pixel resolution")]
        [Tooltip("Vertical resolution of the isolated object buffer.")]
        [Range(90, 360)]
        public int pixelHeight = 180;

        [Header("Color grouping")]
        [Tooltip("Number of available steps per RGB channel.")]
        [Range(2, 16)]
        public int colorSteps = 6;

        [Tooltip("0 uses clean bands. 1 mixes adjacent colors with a 4x4 Bayer pattern.")]
        [Range(0f, 1f)]
        public float ditherStrength = 0.85f;

        [Header("Pixel outline")]
        [Tooltip("Outline width in low-resolution pixels.")]
        [Range(0, 6)]
        public int outlinePixels = 1;

        public Color outlineColor = new Color(0.025f, 0.02f, 0.025f, 1f);

        [Tooltip("Alpha threshold used to define the visible object silhouette.")]
        [Range(0.01f, 0.99f)]
        public float silhouetteThreshold = 0.45f;

        private Camera mainCamera;
        private Camera sourceCamera;
        private Camera compositeCamera;
        private GameObject runtimeRoot;
        private GameObject compositeQuad;
        private RenderTexture pixelBuffer;
        private RenderTexture processedBuffer;
        private Material effectMaterial;
        private Material displayMaterial;
        private UniversalAdditionalCameraData mainCameraData;
        private int originalCullingMask;
        private int activeWidth;
        private int activeHeight;

        private static readonly int SourceTexId =
            Shader.PropertyToID("_SourceTex");
        private static readonly int ColorStepsId =
            Shader.PropertyToID("_ColorSteps");
        private static readonly int DitherStrengthId =
            Shader.PropertyToID("_DitherStrength");
        private static readonly int OutlinePixelsId =
            Shader.PropertyToID("_OutlinePixels");
        private static readonly int OutlineColorId =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int SilhouetteThresholdId =
            Shader.PropertyToID("_SilhouetteThreshold");

        private void OnEnable()
        {
            mainCamera = GetComponent<Camera>();
            originalCullingMask = mainCamera.cullingMask;

            if (pixelObjectLayer == compositeLayer)
            {
                Debug.LogError(
                    "[IsolatedObjectPixelRenderer] Pixel and composite layers must be different.",
                    this);
                enabled = false;
                return;
            }

            Shader effectShader = Shader.Find(EffectShaderName);
            Shader displayShader = Shader.Find(DisplayShaderName);

            if (effectShader == null || displayShader == null)
            {
                Debug.LogError(
                    "[IsolatedObjectPixelRenderer] Required shader not found. " +
                    $"Effect: {EffectShaderName}, Display: {DisplayShaderName}",
                    this);
                enabled = false;
                return;
            }

            effectMaterial = new Material(effectShader)
            {
                name = "Isolated Object Pixel Effect (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            displayMaterial = new Material(displayShader)
            {
                name = "Isolated Object Pixel Display (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };

            BuildRuntimeCameras();
            RebuildPixelBuffers();
            ApplySettings();

            RenderPipelineManager.endCameraRendering -=
                HandleEndCameraRendering;
            RenderPipelineManager.endCameraRendering +=
                HandleEndCameraRendering;
        }

        private void LateUpdate()
        {
            if (mainCamera == null || sourceCamera == null ||
                compositeCamera == null)
            {
                return;
            }

            int desiredHeight = Mathf.Clamp(pixelHeight, 90, 360);
            int desiredWidth = Mathf.Max(
                1,
                Mathf.RoundToInt(desiredHeight * GetCameraAspect()));

            if (desiredWidth != activeWidth || desiredHeight != activeHeight)
                RebuildPixelBuffers();

            SynchronizeCameras();
            UpdateCompositeQuad();
            ApplySettings();
        }

        private void BuildRuntimeCameras()
        {
            runtimeRoot = new GameObject("__ISOLATED_OBJECT_PIXEL_RUNTIME");
            runtimeRoot.hideFlags = HideFlags.HideAndDontSave;
            runtimeRoot.transform.SetParent(transform, false);

            GameObject sourceObject = new GameObject("Pixel Object Camera");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            sourceObject.transform.SetParent(runtimeRoot.transform, false);
            sourceCamera = sourceObject.AddComponent<Camera>();

            GameObject compositeObject = new GameObject("Pixel Composite Camera");
            compositeObject.hideFlags = HideFlags.HideAndDontSave;
            compositeObject.transform.SetParent(runtimeRoot.transform, false);
            compositeCamera = compositeObject.AddComponent<Camera>();

            UniversalAdditionalCameraData sourceCameraData =
                sourceCamera.GetUniversalAdditionalCameraData();
            sourceCameraData.renderType = CameraRenderType.Base;
            sourceCameraData.renderPostProcessing = false;

            UniversalAdditionalCameraData compositeCameraData =
                compositeCamera.GetUniversalAdditionalCameraData();
            compositeCameraData.renderType = CameraRenderType.Overlay;
            compositeCameraData.renderPostProcessing = false;

            mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (!mainCameraData.cameraStack.Contains(compositeCamera))
                mainCameraData.cameraStack.Add(compositeCamera);

            compositeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            compositeQuad.name = "Pixel Composite Quad";
            compositeQuad.hideFlags = HideFlags.HideAndDontSave;
            compositeQuad.layer = compositeLayer;
            compositeQuad.transform.SetParent(compositeObject.transform, false);

            Collider quadCollider = compositeQuad.GetComponent<Collider>();
            if (quadCollider != null)
                Destroy(quadCollider);

            MeshRenderer quadRenderer =
                compositeQuad.GetComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = displayMaterial;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            SynchronizeCameras();
        }

        private void SynchronizeCameras()
        {
            int pixelMask = 1 << pixelObjectLayer;
            int compositeMask = 1 << compositeLayer;
            mainCamera.cullingMask =
                originalCullingMask & ~pixelMask & ~compositeMask;

            CopyCameraSettings(mainCamera, sourceCamera);
            sourceCamera.cullingMask = pixelMask;
            sourceCamera.clearFlags = CameraClearFlags.SolidColor;
            sourceCamera.backgroundColor = Color.clear;
            sourceCamera.depth = mainCamera.depth - 100f;
            sourceCamera.targetTexture = pixelBuffer;
            sourceCamera.allowHDR = false;
            sourceCamera.allowMSAA = false;

            CopyCameraSettings(mainCamera, compositeCamera);
            compositeCamera.cullingMask = compositeMask;
            compositeCamera.clearFlags = CameraClearFlags.Depth;
            compositeCamera.backgroundColor = Color.clear;
            compositeCamera.depth = mainCamera.depth + 100f;
            compositeCamera.targetTexture = null;
            compositeCamera.allowHDR = false;
            compositeCamera.allowMSAA = false;
        }

        private static void CopyCameraSettings(Camera source, Camera target)
        {
            target.orthographic = source.orthographic;
            target.orthographicSize = source.orthographicSize;
            target.fieldOfView = source.fieldOfView;
            target.nearClipPlane = source.nearClipPlane;
            target.farClipPlane = source.farClipPlane;
            target.rect = source.rect;
            target.aspect = source.aspect;
            target.transform.position = source.transform.position;
            target.transform.rotation = source.transform.rotation;
        }

        private void RebuildPixelBuffers()
        {
            ReleasePixelBuffers();

            activeHeight = Mathf.Clamp(pixelHeight, 90, 360);
            activeWidth = Mathf.Max(
                1,
                Mathf.RoundToInt(activeHeight * GetCameraAspect()));

            pixelBuffer = CreatePixelBuffer(
                $"IsolatedObjects_Raw_{activeWidth}x{activeHeight}",
                24);
            processedBuffer = CreatePixelBuffer(
                $"IsolatedObjects_Processed_{activeWidth}x{activeHeight}",
                0);

            ClearRenderTexture(processedBuffer);

            if (sourceCamera != null)
                sourceCamera.targetTexture = pixelBuffer;
            if (effectMaterial != null)
                effectMaterial.SetTexture(SourceTexId, pixelBuffer);
            if (displayMaterial != null)
                displayMaterial.SetTexture(SourceTexId, processedBuffer);
        }

        private RenderTexture CreatePixelBuffer(string bufferName, int depth)
        {
            RenderTexture buffer = new RenderTexture(
                activeWidth,
                activeHeight,
                depth,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default)
            {
                name = bufferName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            buffer.Create();
            return buffer;
        }

        private static void ClearRenderTexture(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }

        private void HandleEndCameraRendering(
            ScriptableRenderContext context,
            Camera renderedCamera)
        {
            if (renderedCamera != sourceCamera ||
                pixelBuffer == null || processedBuffer == null ||
                effectMaterial == null)
            {
                return;
            }

            // Execute immediately after the isolated camera finishes. A queued
            // SRP blit can run after the overlay camera has already sampled the
            // destination on some URP versions, leaving one or every frame
            // transparent.
            Graphics.Blit(
                pixelBuffer,
                processedBuffer,
                effectMaterial,
                0);
        }

        private float GetCameraAspect()
        {
            if (mainCamera != null && mainCamera.targetTexture != null)
            {
                return (float)mainCamera.targetTexture.width /
                       Mathf.Max(1, mainCamera.targetTexture.height);
            }

            if (mainCamera != null && mainCamera.aspect > 0f)
                return mainCamera.aspect;

            return 16f / 9f;
        }

        private void UpdateCompositeQuad()
        {
            if (compositeQuad == null || compositeCamera == null)
                return;

            float distance = Mathf.Max(
                compositeCamera.nearClipPlane + 0.05f,
                0.1f);
            float height;

            if (compositeCamera.orthographic)
            {
                height = compositeCamera.orthographicSize * 2f;
            }
            else
            {
                height = 2f * distance * Mathf.Tan(
                    compositeCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            float width = height * GetCameraAspect();
            compositeQuad.transform.localPosition =
                new Vector3(0f, 0f, distance);
            compositeQuad.transform.localRotation = Quaternion.identity;
            compositeQuad.transform.localScale =
                new Vector3(width, height, 1f);
        }

        private void ApplySettings()
        {
            if (effectMaterial == null)
                return;

            effectMaterial.SetFloat(
                ColorStepsId,
                Mathf.Clamp(colorSteps, 2, 16));
            effectMaterial.SetFloat(
                DitherStrengthId,
                Mathf.Clamp01(ditherStrength));
            effectMaterial.SetFloat(
                OutlinePixelsId,
                Mathf.Clamp(outlinePixels, 0, 6));
            effectMaterial.SetColor(OutlineColorId, outlineColor);
            effectMaterial.SetFloat(
                SilhouetteThresholdId,
                Mathf.Clamp01(silhouetteThreshold));
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
            RenderPipelineManager.endCameraRendering -=
                HandleEndCameraRendering;

            if (mainCamera != null)
                mainCamera.cullingMask = originalCullingMask;

            if (mainCameraData != null && compositeCamera != null)
                mainCameraData.cameraStack.Remove(compositeCamera);

            ReleasePixelBuffers();

            if (effectMaterial != null)
            {
                Destroy(effectMaterial);
                effectMaterial = null;
            }

            if (displayMaterial != null)
            {
                Destroy(displayMaterial);
                displayMaterial = null;
            }

            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot);
                runtimeRoot = null;
            }
        }

        private void ReleasePixelBuffers()
        {
            if (sourceCamera != null &&
                sourceCamera.targetTexture == pixelBuffer)
            {
                sourceCamera.targetTexture = null;
            }

            ReleaseBuffer(ref pixelBuffer);
            ReleaseBuffer(ref processedBuffer);
        }

        private static void ReleaseBuffer(ref RenderTexture buffer)
        {
            if (buffer == null)
                return;

            if (buffer.IsCreated())
                buffer.Release();
            Destroy(buffer);
            buffer = null;
        }

        private void OnValidate()
        {
            pixelHeight = Mathf.Clamp(pixelHeight, 90, 360);
            colorSteps = Mathf.Clamp(colorSteps, 2, 16);
            ditherStrength = Mathf.Clamp01(ditherStrength);
            outlinePixels = Mathf.Clamp(outlinePixels, 0, 6);
            silhouetteThreshold = Mathf.Clamp01(silhouetteThreshold);
        }
    }
}
