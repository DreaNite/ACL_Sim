using System.Collections;
using TMPro;
using UnityEngine;
using THSDK;

namespace ACLSimulator.Core
{
    /// <summary>
    /// Drives the Tekle Holographic Device rig so stereo/multi-user rendering
    /// tracks correctly on the wall. Moves the whole rig instead of any single
    /// Unity Camera so per-user calibrated offsets are preserved.
    ///
    /// Lifecycle:
    ///   Awake     - cache current rig pose as Default; teleport rig to StartViewpoint.
    ///   PlayIntro - lerp StartViewpoint -> Default; show fullscreen welcome.
    ///   On step   - show info panel; lerp Default -> step viewpoint; dwell; lerp back.
    ///               After the last step, show the completion panel.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Holographic Rig")]
        [SerializeField] private HolographicDevice _holographicDevice;

        [Tooltip("Rendering cameras inside the rig (one per User's Glasses). " +
                 "Their averaged pose is used so the viewpoint Transform represents " +
                 "the midpoint between users, not the rig pivot.")]
        [SerializeField] private Transform[] _referenceCameras;

        [Header("Viewpoints")]
        [Tooltip("Where the rig starts before the intro pan. The pan ends at the " +
                 "Default pose cached at Awake.")]
        [SerializeField] private Transform _startViewpoint;

        [Tooltip("One viewpoint per step. Index 0 = step 1.")]
        [SerializeField] private Transform[] _stepViewpoints = new Transform[4];

        [Header("Timings")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _introPanDuration = 5f;

        [Range(0.1f, 5f)]
        [SerializeField] private float _stepLerpDuration = 1.2f;

        [Range(0f, 30f)]
        [SerializeField] private float _stepDwellSeconds = 5f;

        [Tooltip("When a step trigger arrives mid-transition, quick-lerp the rig back to default " +
                 "in this many seconds before running the new step. Skipped if the rig is already at default.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stepRecoveryDuration = 0.4f;

        [Header("Welcome Panel")]
        [SerializeField] private GameObject _welcomePanel;
        [SerializeField] private TMP_Text _welcomeText;
        [TextArea(1, 3)]
        [SerializeField] private string _welcomeMessage = "Welcome to the ACL Reconstruction Simulator";
        [Range(0.5f, 20f)]
        [SerializeField] private float _welcomeSeconds = 4f;

        [Header("Step Info Panel")]
        [SerializeField] private GameObject _stepInfoPanel;
        [SerializeField] private TMP_Text _stepInfoText;

        [Tooltip("One description per step. Index 0 = step 1.")]
        [TextArea(2, 5)]
        [SerializeField] private string[] _stepDescriptions = new string[4];

        [Range(0.5f, 20f)]
        [SerializeField] private float _stepInfoSeconds = 4f;

        [Header("Completion Panel")]
        [Tooltip("Shown after the last step's camera returns to default. Reuses the WelcomePanel/Text refs above.")]
        [TextArea(1, 3)]
        [SerializeField] private string _completionMessage = "ACL reconstruction complete";

        [Range(0.5f, 20f)]
        [SerializeField] private float _completionSeconds = 5f;

        private Vector3 _defaultPos;
        private Quaternion _defaultRot;
        private Coroutine _transition;
        private bool _waitingAtViewpoint;

        private Transform Rig => _holographicDevice != null ? _holographicDevice.transform : null;

        private void Awake()
        {
            Transform rig = Rig;
            if (rig != null)
            {
                _defaultPos = rig.position;
                _defaultRot = rig.rotation;

                if (_startViewpoint != null)
                {
                    ComputeRigPose(rig, _startViewpoint, out Vector3 sPos, out Quaternion sRot);
                    rig.position = sPos;
                    rig.rotation = sRot;
                }
            }

            if (_welcomePanel != null) _welcomePanel.SetActive(false);
            if (_stepInfoPanel != null) _stepInfoPanel.SetActive(false);
        }

        private void OnEnable()  => StepCounter.OnStepChanged += HandleStepChanged;
        private void OnDisable() => StepCounter.OnStepChanged -= HandleStepChanged;

        public void PlayIntroSequence()
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            Transform rig = Rig;
            if (rig != null)
            {
                yield return LerpRigTo(rig, _defaultPos, _defaultRot, _introPanDuration);
            }

            if (_welcomePanel != null)
            {
                if (_welcomeText != null) _welcomeText.text = _welcomeMessage;
                _welcomePanel.SetActive(true);
                yield return new WaitForSeconds(_welcomeSeconds);
                _welcomePanel.SetActive(false);
            }

            _transition = null;
        }

        private void HandleStepChanged(int step)
        {
            int idx = step - 1;
            if (_stepViewpoints == null || idx < 0 || idx >= _stepViewpoints.Length) return;
            Transform target = _stepViewpoints[idx];
            Transform rig = Rig;
            if (target == null || rig == null) return;

            _waitingAtViewpoint = false;
            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(StepRoutine(rig, target, idx));
        }

        private IEnumerator StepRoutine(Transform rig, Transform target, int stepIndex)
        {
            // Recover from any interrupted previous step: hide lingering panels
            // and lerp back to default if the rig was left elsewhere. No-op when
            // arriving cleanly from default.
            if (_stepInfoPanel != null) _stepInfoPanel.SetActive(false);
            if (_welcomePanel  != null) _welcomePanel.SetActive(false);

            if (Vector3.Distance(rig.position, _defaultPos) > 0.001f ||
                Quaternion.Angle(rig.rotation, _defaultRot) > 0.1f)
            {
                yield return LerpRigTo(rig, _defaultPos, _defaultRot, _stepRecoveryDuration);
            }

            // Show the info panel before the camera move so the user reads about
            // the upcoming view; hide it before the lerp starts.
            if (_stepInfoPanel != null && _stepInfoSeconds > 0f)
            {
                string description = (_stepDescriptions != null &&
                                      stepIndex >= 0 &&
                                      stepIndex < _stepDescriptions.Length)
                    ? _stepDescriptions[stepIndex]
                    : string.Empty;

                if (_stepInfoText != null) _stepInfoText.text = description;
                _stepInfoPanel.SetActive(true);
                yield return new WaitForSeconds(_stepInfoSeconds);
                _stepInfoPanel.SetActive(false);
            }

            ComputeRigPose(rig, target, out Vector3 toPos, out Quaternion toRot);
            yield return LerpRigTo(rig, toPos, toRot, _stepLerpDuration);

            bool isLastStep = _stepViewpoints != null && stepIndex == _stepViewpoints.Length - 1;

            if (isLastStep)
                yield return new WaitForSeconds(_stepDwellSeconds);
            else
            {
                _waitingAtViewpoint = true;
                while (_waitingAtViewpoint)
                    yield return null;
            }

            yield return LerpRigTo(rig, _defaultPos, _defaultRot, _stepLerpDuration);

            if (isLastStep && _welcomePanel != null && _completionSeconds > 0f)
            {
                if (_welcomeText != null) _welcomeText.text = _completionMessage;
                _welcomePanel.SetActive(true);
                yield return new WaitForSeconds(_completionSeconds);
                _welcomePanel.SetActive(false);
            }

            _transition = null;
        }

        private IEnumerator LerpRigTo(Transform rig, Vector3 toPos, Quaternion toRot, float duration)
        {
            Vector3 fromPos = rig.position;
            Quaternion fromRot = rig.rotation;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                rig.position = Vector3.Lerp(fromPos, toPos, k);
                rig.rotation = Quaternion.Slerp(fromRot, toRot, k);
                yield return null;
            }

            rig.position = toPos;
            rig.rotation = toRot;
        }

        /// <summary>
        /// Compute the rig pose that puts the midpoint of all reference cameras
        /// at the viewpoint Transform. With no references, the rig pivot is
        /// placed at the viewpoint directly.
        /// </summary>
        private void ComputeRigPose(Transform rig, Transform target,
                                    out Vector3 rigPos, out Quaternion rigRot)
        {
            if (!TryGetReferencePose(out Vector3 refPos, out Quaternion refRot))
            {
                rigPos = target.position;
                rigRot = target.rotation;
                return;
            }

            Quaternion localChainRot = Quaternion.Inverse(rig.rotation) * refRot;
            rigRot = target.rotation * Quaternion.Inverse(localChainRot);

            Vector3 localChainPos = rig.InverseTransformPoint(refPos);
            Vector3 lossy = rig.lossyScale;
            Vector3 scaledLocal = new Vector3(
                localChainPos.x * lossy.x,
                localChainPos.y * lossy.y,
                localChainPos.z * lossy.z);
            Vector3 worldOffset = rigRot * scaledLocal;

            rigPos = target.position - worldOffset;
        }

        private bool TryGetReferencePose(out Vector3 avgPos, out Quaternion avgRot)
        {
            avgPos = Vector3.zero;
            avgRot = Quaternion.identity;

            if (_referenceCameras == null || _referenceCameras.Length == 0) return false;

            int count = 0;
            foreach (var t in _referenceCameras)
            {
                if (t == null) continue;
                if (count == 0)
                {
                    avgPos = t.position;
                    avgRot = t.rotation;
                }
                else
                {
                    avgPos += t.position;
                    // Incremental slerp with weight 1/(n+1): exact for equal rotations,
                    // stable approximation for similar ones.
                    avgRot = Quaternion.Slerp(avgRot, t.rotation, 1f / (count + 1));
                }
                count++;
            }

            if (count == 0) return false;
            avgPos /= count;
            return true;
        }
    }
}
