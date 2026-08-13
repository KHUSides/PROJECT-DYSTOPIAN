using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class MerlinHeldSpearPoseController : MonoBehaviour
    {
        private const string IntroStateName = "Intro";
        private const string IdleStateName = "Idle";
        private const string ThrustStateName = "SpearThrust";
        private const string SwingStateName = "SpearSwing";
        private const string ChargeStateName = "SpearCharge";
        private const string ThrowStateName = "SpearThrow";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private BossController bossController;
        [SerializeField] private Transform heldSpear;
        [SerializeField] private Transform oneHandSocket;

        [Header("Grip Points")]
        [SerializeField] private bool autoEstimatePalmOffsets = true;
        [SerializeField] private Vector3 rightHandGripOffset = new Vector3(0.08f, 0.003f, -0.013f);
        [SerializeField] private Vector3 leftHandGripOffset = new Vector3(0.08f, -0.003f, 0.013f);
        [SerializeField] private Vector3 spearRightGripLocal = Vector3.zero;
        [SerializeField] private Vector3 oneHandSpearLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 oneHandSpearLocalEuler = new Vector3(0f, 0f, 180f);

        [Header("Pose")]
        [SerializeField, Min(0.01f)] private float minimumTwoHandDistance = 0.18f;
        [SerializeField] private Vector3 sideViewForward = Vector3.forward;
        [SerializeField] private Vector3 oneHandThrowAxis = new Vector3(1f, 0.08f, 0f);
        [SerializeField] private bool forceHorizontalChargePose = true;
        [SerializeField] private Vector3 horizontalChargeAxis = new Vector3(1f, 0.03f, 0f);
        [SerializeField] private bool forceTipTowardFacingDirection = true;
        [SerializeField] private bool drawDebugGizmos;

        [Header("Thrust Emphasis")]
        [SerializeField] private bool forceForwardThrustPose = true;
        [SerializeField] private Vector3 thrustAxis = new Vector3(1f, 0.015f, 0f);
        [SerializeField, Min(0f)] private float thrustForwardWorldOffset = 0.675f;
        [SerializeField, Range(0f, 1f)] private float thrustForwardStartNormalizedTime = 0.16f;
        [SerializeField, Range(0f, 1f)] private float thrustForwardPeakNormalizedTime = 0.48f;
        [SerializeField, Range(0f, 1f)] private float thrustForwardEndNormalizedTime = 0.82f;

        private Transform rightHand;
        private Transform leftHand;
        private bool palmOffsetsEstimated;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            EstimatePalmOffsetsIfNeeded();
        }

        private void LateUpdate()
        {
            ForceUpdatePose();
        }

        public void ForceUpdatePose()
        {
            CacheReferences();
            EstimatePalmOffsetsIfNeeded();

            if (heldSpear == null || !heldSpear.gameObject.activeInHierarchy || rightHand == null)
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            bool isChargeState = StateMatches(stateInfo, ChargeStateName);
            bool isThrustState = StateMatches(stateInfo, ThrustStateName);
            bool useTwoHandPose = ShouldUseTwoHandPose(stateInfo);
            if (!useTwoHandPose && TryApplyOneHandSocketPose())
            {
                return;
            }

            Vector3 rightGrip = rightHand.TransformPoint(rightHandGripOffset);
            Vector3 spearAxis = GetSpearAxis(rightGrip, useTwoHandPose, isChargeState, isThrustState);
            if (spearAxis.sqrMagnitude <= 0.0001f)
            {
                spearAxis = GetFacingAxis();
            }

            spearAxis.Normalize();

            if (forceTipTowardFacingDirection)
            {
                Vector3 facingAxis = GetFacingAxis();
                if (Vector3.Dot(spearAxis, facingAxis) < 0f)
                {
                    spearAxis = -spearAxis;
                }
            }

            Quaternion spearRotation = BuildSpearRotation(spearAxis);
            Vector3 spearPosition = rightGrip - spearRotation * spearRightGripLocal;
            if (isThrustState)
            {
                spearPosition += GetFacingAxis() * EvaluateThrustForwardOffset(stateInfo);
            }

            heldSpear.SetPositionAndRotation(spearPosition, spearRotation);
        }

        private bool TryApplyOneHandSocketPose()
        {
            if (oneHandSocket == null || heldSpear == null)
            {
                return false;
            }

            Quaternion localRotation = Quaternion.Euler(oneHandSpearLocalEuler);
            if (heldSpear.parent == oneHandSocket)
            {
                heldSpear.localPosition = oneHandSpearLocalPosition;
                heldSpear.localRotation = localRotation;
                return true;
            }

            Vector3 worldPosition = oneHandSocket.TransformPoint(oneHandSpearLocalPosition);
            Quaternion worldRotation = oneHandSocket.rotation * localRotation;
            heldSpear.SetPositionAndRotation(worldPosition, worldRotation);
            return true;
        }

        private bool ShouldUseTwoHandPose(AnimatorStateInfo stateInfo)
        {
            if (animator == null || leftHand == null)
            {
                return false;
            }

            return StateMatches(stateInfo, IntroStateName) ||
                   StateMatches(stateInfo, IdleStateName) ||
                   StateMatches(stateInfo, ThrustStateName) ||
                   StateMatches(stateInfo, SwingStateName) ||
                   StateMatches(stateInfo, ChargeStateName);
        }

        private Vector3 GetSpearAxis(Vector3 rightGrip, bool useTwoHandPose, bool isChargeState, bool isThrustState)
        {
            if (isThrustState && forceForwardThrustPose)
            {
                return GetThrustAxis();
            }

            if (isChargeState && forceHorizontalChargePose)
            {
                return GetHorizontalChargeAxis();
            }

            return useTwoHandPose ? GetTwoHandAxis(rightGrip) : GetOneHandThrowAxis();
        }

        private Vector3 GetThrustAxis()
        {
            Vector3 axis = thrustAxis;
            axis.x = Mathf.Abs(axis.x) * (bossController != null ? bossController.FacingSign : 1);
            return axis;
        }

        private float EvaluateThrustForwardOffset(AnimatorStateInfo stateInfo)
        {
            if (thrustForwardWorldOffset <= 0f)
            {
                return 0f;
            }

            float start = Mathf.Clamp01(thrustForwardStartNormalizedTime);
            float peak = Mathf.Clamp01(thrustForwardPeakNormalizedTime);
            float end = Mathf.Clamp01(thrustForwardEndNormalizedTime);

            if (peak <= start)
            {
                peak = Mathf.Min(0.99f, start + 0.01f);
            }

            if (end <= peak)
            {
                end = Mathf.Min(1f, peak + 0.01f);
            }

            float normalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);
            float weight;
            if (normalizedTime < start || normalizedTime > end)
            {
                weight = 0f;
            }
            else if (normalizedTime <= peak)
            {
                weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, peak, normalizedTime));
            }
            else
            {
                weight = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(peak, end, normalizedTime));
            }

            return thrustForwardWorldOffset * weight;
        }

        private Vector3 GetHorizontalChargeAxis()
        {
            Vector3 axis = horizontalChargeAxis;
            axis.x = Mathf.Abs(axis.x) * (bossController != null ? bossController.FacingSign : 1);
            return axis;
        }

        private Vector3 GetTwoHandAxis(Vector3 rightGrip)
        {
            if (leftHand == null)
            {
                return GetFacingAxis();
            }

            Vector3 leftGrip = leftHand.TransformPoint(leftHandGripOffset);
            Vector3 axis = leftGrip - rightGrip;
            if (axis.sqrMagnitude < minimumTwoHandDistance * minimumTwoHandDistance)
            {
                return GetFacingAxis();
            }

            return axis;
        }

        private Vector3 GetOneHandThrowAxis()
        {
            Vector3 axis = oneHandThrowAxis;
            axis.x = Mathf.Abs(axis.x) * (bossController != null ? bossController.FacingSign : 1);
            return axis;
        }

        private Quaternion BuildSpearRotation(Vector3 spearAxis)
        {
            Vector3 forward = sideViewForward.sqrMagnitude > 0.0001f ? sideViewForward.normalized : Vector3.forward;
            forward = Vector3.ProjectOnPlane(forward, spearAxis);

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.Cross(spearAxis, Vector3.up);
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return Quaternion.LookRotation(forward, spearAxis);
        }

        private Vector3 GetFacingAxis()
        {
            int facingSign = bossController != null ? bossController.FacingSign : 1;
            return Vector3.right * facingSign;
        }

        private static bool StateMatches(AnimatorStateInfo stateInfo, string stateName)
        {
            return stateInfo.IsName(stateName) || stateInfo.IsName("Base Layer." + stateName);
        }

        private void CacheReferences()
        {
            if (bossController == null)
            {
                bossController = GetComponent<BossController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (heldSpear == null)
            {
                Transform[] transforms = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "Merlin_Spear")
                    {
                        heldSpear = transforms[i];
                        break;
                    }
                }
            }

            if (oneHandSocket == null)
            {
                Transform[] transforms = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "WeaponSocket_R")
                    {
                        oneHandSocket = transforms[i];
                        break;
                    }
                }
            }

            if (animator != null)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
        }

        private void EstimatePalmOffsetsIfNeeded()
        {
            if (!autoEstimatePalmOffsets || palmOffsetsEstimated)
            {
                return;
            }

            if (rightHand != null)
            {
                rightHandGripOffset = EstimatePalmOffset(rightHand, rightHandGripOffset);
            }

            if (leftHand != null)
            {
                leftHandGripOffset = EstimatePalmOffset(leftHand, leftHandGripOffset);
            }

            palmOffsetsEstimated = true;
        }

        private static Vector3 EstimatePalmOffset(Transform hand, Vector3 fallback)
        {
            if (hand == null)
            {
                return fallback;
            }

            Vector3 sum = Vector3.zero;
            int count = 0;
            Transform[] children = hand.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == hand)
                {
                    continue;
                }

                if (child.name.StartsWith("Finger_01") ||
                    child.name.StartsWith("IndexFinger_01") ||
                    child.name.StartsWith("Thumb_01"))
                {
                    sum += hand.InverseTransformPoint(child.position);
                    count++;
                }
            }

            return count > 0 ? sum / count : fallback;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            CacheReferences();
            if (rightHand == null)
            {
                return;
            }

            Vector3 rightGrip = rightHand.TransformPoint(rightHandGripOffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightGrip, 0.05f);

            if (leftHand != null)
            {
                Vector3 leftGrip = leftHand.TransformPoint(leftHandGripOffset);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(leftGrip, 0.05f);
                Gizmos.DrawLine(rightGrip, leftGrip);
            }
        }
    }
}
