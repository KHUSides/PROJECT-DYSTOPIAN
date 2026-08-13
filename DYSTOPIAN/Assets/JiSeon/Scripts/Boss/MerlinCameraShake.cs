using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class MerlinCameraShake : MonoBehaviour
    {
        private const float MinimumDuration = 0.01f;
        private const float MinimumFrequency = 0.1f;

        private float duration;
        private float remainingSeconds;
        private float amplitude;
        private float frequency;
        private float seedX;
        private float seedY;
        private Vector3 appliedOffset;

        public bool IsShaking => remainingSeconds > 0f;
        public Vector3 CurrentOffset => appliedOffset;

        public static MerlinCameraShake ShakeMainCamera(float shakeDuration, float shakeAmplitude, float shakeFrequency)
        {
            if (shakeDuration <= 0f || shakeAmplitude <= 0f)
            {
                return null;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            MerlinCameraShake cameraShake = mainCamera.GetComponent<MerlinCameraShake>();
            if (cameraShake == null)
            {
                cameraShake = mainCamera.gameObject.AddComponent<MerlinCameraShake>();
            }

            cameraShake.Play(shakeDuration, shakeAmplitude, shakeFrequency);
            return cameraShake;
        }

        public void Play(float shakeDuration, float shakeAmplitude, float shakeFrequency)
        {
            RemoveAppliedOffset();

            duration = Mathf.Max(MinimumDuration, shakeDuration);
            remainingSeconds = duration;
            amplitude = shakeAmplitude;
            frequency = Mathf.Max(MinimumFrequency, shakeFrequency);
            seedX = Random.value * 100f;
            seedY = Random.value * 100f;
        }

        private void LateUpdate()
        {
            RemoveAppliedOffset();

            if (remainingSeconds <= 0f)
            {
                amplitude = 0f;
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            float normalizedRemaining = remainingSeconds / Mathf.Max(MinimumDuration, duration);
            float fade = normalizedRemaining * normalizedRemaining;
            float sampleTime = Time.time * frequency;

            float x = (Mathf.PerlinNoise(seedX, sampleTime) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(seedY, sampleTime) - 0.5f) * 2f;
            appliedOffset = new Vector3(x, y, 0f) * amplitude * fade;
            transform.localPosition += appliedOffset;
        }

        private void OnDisable()
        {
            RemoveAppliedOffset();
        }

        private void OnDestroy()
        {
            RemoveAppliedOffset();
        }

        private void RemoveAppliedOffset()
        {
            if (appliedOffset.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            transform.localPosition -= appliedOffset;
            appliedOffset = Vector3.zero;
        }
    }
}
