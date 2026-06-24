using System;
using UnityEngine;

/// <summary>
/// Watches <see cref="RAGInput.step"/> (set by UdpSocket from the Python
/// trigger) and activates the corresponding Step GameObject, deactivating the
/// others. Raises <see cref="OnStepChanged"/> for listeners such as the
/// CameraController.
/// </summary>
public class StepCounter : MonoBehaviour
{
    [SerializeField] private GameObject stepObject1;
    [SerializeField] private GameObject stepObject2;
    [SerializeField] private GameObject stepObject3;
    [SerializeField] private GameObject stepObject4;

    /// <summary>Fired with the new 1-based step index whenever the step changes.</summary>
    public static event Action<int> OnStepChanged;

    private int previousStep = 0;

    void Update()
    {
        if (RAGInput.step == previousStep) return;

        previousStep = RAGInput.step;

        SetStepActive(stepObject1, false);
        SetStepActive(stepObject2, false);
        SetStepActive(stepObject3, false);
        SetStepActive(stepObject4, false);

        Debug.Log("Step " + RAGInput.step);

        switch (RAGInput.step)
        {
            case 1: SetStepActive(stepObject1, true); break;
            case 2: SetStepActive(stepObject2, true); break;
            case 3: SetStepActive(stepObject3, true); break;
            case 4: SetStepActive(stepObject4, true); break;
        }

        OnStepChanged?.Invoke(RAGInput.step);
    }

    private static void SetStepActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
