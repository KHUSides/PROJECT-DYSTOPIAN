using UnityEngine;

namespace Dystopian.EnemyTest
{
    [ExecuteAlways]
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyDebugOverlay : MonoBehaviour
    {
        [Header("Chase Range")]
        [SerializeField] private bool showChaseRange = true;
        [SerializeField, Min(12)] private int circleSegments = 96;
        [SerializeField, Min(0.005f)] private float circleLineWidth = 0.05f;
        [SerializeField] private float visualZOffset = -0.35f;
        [SerializeField] private Color chaseRangeColor = new Color(1f, 0.65f, 0.05f, 0.9f);

        [Header("State Label")]
        [SerializeField] private bool showStateLabel = true;
        [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, 3.9f, -0.1f);
        [SerializeField, Min(0.01f)] private float labelCharacterSize = 0.14f;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color wanderColor = new Color(0.3f, 0.85f, 1f, 1f);
        [SerializeField] private Color chaseColor = new Color(1f, 0.75f, 0.05f, 1f);
        [SerializeField] private Color attackReadyColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("Health Bar")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector3 healthBarLocalOffset = new Vector3(0f, 3.15f, -0.12f);
        [SerializeField, Min(0.1f)] private float healthBarWidth = 3f;
        [SerializeField, Min(0.01f)] private float healthBarLineWidth = 0.28f;
        [SerializeField] private Vector3 healthTextLocalOffset = new Vector3(0f, 2.45f, -0.12f);
        [SerializeField, Min(0.01f)] private float healthTextCharacterSize = 0.12f;
        [SerializeField] private Color healthBarBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        [SerializeField] private Color highHealthColor = new Color(0.15f, 1f, 0.25f, 1f);
        [SerializeField] private Color midHealthColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.15f, 0.1f, 1f);

        private EnemyController enemyController;
        private EnemyHealth enemyHealth;
        private LineRenderer chaseRangeLine;
        private LineRenderer healthBarBackgroundLine;
        private LineRenderer healthBarFillLine;
        private TextMesh stateText;
        private TextMesh healthText;
        private Material lineMaterial;

        private void Awake()
        {
            RefreshOverlay();
        }

        private void OnEnable()
        {
            RefreshOverlay();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                RefreshOverlay();
            }
        }

        private void LateUpdate()
        {
            RefreshOverlay();
        }

        private void RefreshOverlay()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            EnsureChaseRangeLine();
            EnsureStateLabel();
            EnsureHealthBar();
            UpdateChaseRangeLine();
            UpdateStateLabel();
            UpdateHealthBar();
        }

        private void EnsureChaseRangeLine()
        {
            if (chaseRangeLine != null)
            {
                return;
            }

            Transform existingLine = transform.Find("Debug_ChaseRange");
            GameObject lineObject = existingLine != null
                ? existingLine.gameObject
                : new GameObject("Debug_ChaseRange");

            lineObject.transform.SetParent(transform, false);

            chaseRangeLine = lineObject.GetComponent<LineRenderer>();
            if (chaseRangeLine == null)
            {
                chaseRangeLine = lineObject.AddComponent<LineRenderer>();
            }

            chaseRangeLine.useWorldSpace = true;
            chaseRangeLine.loop = true;
            chaseRangeLine.positionCount = circleSegments;
            chaseRangeLine.startWidth = circleLineWidth;
            chaseRangeLine.endWidth = circleLineWidth;
            chaseRangeLine.numCornerVertices = 4;
            chaseRangeLine.numCapVertices = 4;
            chaseRangeLine.sortingOrder = 80;

            chaseRangeLine.material = GetLineMaterial();
        }

        private void EnsureStateLabel()
        {
            if (stateText != null)
            {
                return;
            }

            Transform existingLabel = transform.Find("Debug_StateLabel");
            GameObject labelObject = existingLabel != null
                ? existingLabel.gameObject
                : new GameObject("Debug_StateLabel");

            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = labelLocalOffset;
            labelObject.transform.localRotation = Quaternion.identity;

            stateText = labelObject.GetComponent<TextMesh>();
            if (stateText == null)
            {
                stateText = labelObject.AddComponent<TextMesh>();
            }

            stateText.anchor = TextAnchor.MiddleCenter;
            stateText.alignment = TextAlignment.Center;
            stateText.fontSize = 64;
            stateText.characterSize = labelCharacterSize;
            stateText.text = "STATE: ?";

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 100;
            }
        }

        private void EnsureHealthBar()
        {
            if (healthBarBackgroundLine == null)
            {
                healthBarBackgroundLine = EnsureLineRendererChild("Debug_HealthBar_Background", 110);
            }

            if (healthBarFillLine == null)
            {
                healthBarFillLine = EnsureLineRendererChild("Debug_HealthBar_Fill", 120);
            }

            if (healthText != null)
            {
                return;
            }

            Transform existingText = transform.Find("Debug_HealthText");
            GameObject textObject = existingText != null
                ? existingText.gameObject
                : new GameObject("Debug_HealthText");

            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = healthTextLocalOffset;
            textObject.transform.localRotation = Quaternion.identity;

            healthText = textObject.GetComponent<TextMesh>();
            if (healthText == null)
            {
                healthText = textObject.AddComponent<TextMesh>();
            }

            healthText.anchor = TextAnchor.MiddleCenter;
            healthText.alignment = TextAlignment.Center;
            healthText.fontSize = 64;
            healthText.characterSize = healthTextCharacterSize;
            healthText.color = Color.white;
            healthText.text = "?/?";

            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 130;
            }
        }

        private LineRenderer EnsureLineRendererChild(string childName, int sortingOrder)
        {
            Transform existing = transform.Find(childName);
            GameObject lineObject = existing != null
                ? existing.gameObject
                : new GameObject(childName);

            lineObject.transform.SetParent(transform, false);

            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = lineObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = healthBarLineWidth;
            lineRenderer.endWidth = healthBarLineWidth;
            lineRenderer.numCapVertices = 4;
            lineRenderer.sortingOrder = sortingOrder;
            lineRenderer.material = GetLineMaterial();
            return lineRenderer;
        }

        private Material GetLineMaterial()
        {
            if (lineMaterial != null)
            {
                return lineMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return lineMaterial;
        }

        private void UpdateChaseRangeLine()
        {
            if (chaseRangeLine == null)
            {
                return;
            }

            chaseRangeLine.enabled = showChaseRange && enemyController != null;
            if (!chaseRangeLine.enabled)
            {
                return;
            }

            if (chaseRangeLine.positionCount != circleSegments)
            {
                chaseRangeLine.positionCount = circleSegments;
            }

            chaseRangeLine.startWidth = circleLineWidth;
            chaseRangeLine.endWidth = circleLineWidth;
            chaseRangeLine.startColor = chaseRangeColor;
            chaseRangeLine.endColor = chaseRangeColor;

            Vector3 center = transform.position;
            center.z += visualZOffset;

            float radius = enemyController.DetectionRange;
            for (int i = 0; i < circleSegments; i++)
            {
                float ratio = i / (float)circleSegments;
                float angle = ratio * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
                chaseRangeLine.SetPosition(i, point);
            }
        }

        private void UpdateStateLabel()
        {
            if (stateText == null)
            {
                return;
            }

            stateText.gameObject.SetActive(showStateLabel && enemyController != null);
            if (!stateText.gameObject.activeSelf)
            {
                return;
            }

            string stateName = enemyController.CurrentStateName;
            stateText.text = $"STATE: {stateName.ToUpperInvariant()}";
            stateText.color = GetStateColor(stateName);
            stateText.characterSize = labelCharacterSize;
            stateText.transform.localPosition = labelLocalOffset;
        }

        private void UpdateHealthBar()
        {
            bool visible = showHealthBar && enemyHealth != null;

            if (healthBarBackgroundLine != null)
            {
                healthBarBackgroundLine.enabled = visible;
            }

            if (healthBarFillLine != null)
            {
                healthBarFillLine.enabled = visible;
            }

            if (healthText != null)
            {
                healthText.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            int currentHealth = Mathf.Max(0, enemyHealth.CurrentHealth);
            int maxHealth = Mathf.Max(1, enemyHealth.MaxHealth);
            float healthRatio = Mathf.Clamp01(currentHealth / (float)maxHealth);

            Vector3 center = transform.position + healthBarLocalOffset;
            float halfWidth = healthBarWidth * 0.5f;
            Vector3 left = center + Vector3.left * halfWidth;
            Vector3 right = center + Vector3.right * halfWidth;
            Vector3 fillRight = Vector3.Lerp(left, right, healthRatio);

            healthBarBackgroundLine.startWidth = healthBarLineWidth;
            healthBarBackgroundLine.endWidth = healthBarLineWidth;
            healthBarBackgroundLine.startColor = healthBarBackgroundColor;
            healthBarBackgroundLine.endColor = healthBarBackgroundColor;
            healthBarBackgroundLine.SetPosition(0, left);
            healthBarBackgroundLine.SetPosition(1, right);

            Color fillColor = GetHealthColor(healthRatio);
            healthBarFillLine.startWidth = healthBarLineWidth;
            healthBarFillLine.endWidth = healthBarLineWidth;
            healthBarFillLine.startColor = fillColor;
            healthBarFillLine.endColor = fillColor;
            healthBarFillLine.SetPosition(0, left);
            healthBarFillLine.SetPosition(1, fillRight);

            healthText.transform.localPosition = healthTextLocalOffset;
            healthText.characterSize = healthTextCharacterSize;
            healthText.text = $"{currentHealth}/{maxHealth}";
            healthText.color = Color.white;
        }

        private Color GetHealthColor(float healthRatio)
        {
            if (healthRatio > 0.5f)
            {
                return highHealthColor;
            }

            if (healthRatio > 0.25f)
            {
                return midHealthColor;
            }

            return lowHealthColor;
        }

        private Color GetStateColor(string stateName)
        {
            switch (stateName)
            {
                case "Idle":
                    return idleColor;
                case "Wander":
                    return wanderColor;
                case "Chase":
                    return chaseColor;
                case "AttackReady":
                    return attackReadyColor;
                case "Dead":
                    return deadColor;
                default:
                    return Color.white;
            }
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(lineMaterial);
                }
                else
                {
                    DestroyImmediate(lineMaterial);
                }
            }
        }

        private void OnValidate()
        {
            circleSegments = Mathf.Max(12, circleSegments);
            circleLineWidth = Mathf.Max(0.005f, circleLineWidth);
            labelCharacterSize = Mathf.Max(0.01f, labelCharacterSize);
            healthBarWidth = Mathf.Max(0.1f, healthBarWidth);
            healthBarLineWidth = Mathf.Max(0.01f, healthBarLineWidth);
            healthTextCharacterSize = Mathf.Max(0.01f, healthTextCharacterSize);
        }
    }
}
