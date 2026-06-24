using UnityEngine;
using THSDK;

namespace ACLSimulator.Core
{
    /// <summary>
    /// Blocks the procedure until the user presses any controller button (any
    /// keyboard key in the Editor). On press, triggers the intro sequence on
    /// the CameraController and disables itself.
    /// </summary>
    public class StartGate : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private CameraController _cameraController;

        [Header("Input")]
        [Tooltip("Also accept any keyboard key (Editor testing without the device).")]
        [SerializeField] private bool _acceptKeyboardAsFallback = true;

        private bool _started;

        private void Update()
        {
            if (_started) return;
            if (!AnyButtonPressed()) return;

            _started = true;
            if (_cameraController != null) _cameraController.PlayIntroSequence();
            enabled = false;
        }

        private bool AnyButtonPressed()
        {
            var device = HolographicDevice.current;
            if (device != null)
            {
                int users = device.userCount;
                int buttons = device.ControllerButtonCount;
                for (int u = 0; u < users; u++)
                {
                    // Standard setups have 2 controllers per user; query both.
                    for (int c = 0; c < 2; c++)
                    {
                        Controller controller = null;
                        try { controller = device.GetUserController(u, c); }
                        catch { controller = null; }
                        if (controller == null) continue;

                        for (int b = 0; b < buttons; b++)
                        {
                            if (controller.GetButtonDown(b)) return true;
                        }
                    }
                }
            }

            if (_acceptKeyboardAsFallback && Input.anyKeyDown) return true;

            return false;
        }
    }
}
