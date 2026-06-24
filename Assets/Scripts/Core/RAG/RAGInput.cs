using UnityEngine;

/// <summary>
/// Shared bus between the UDP listener (UdpSocket) and the rest of the scene.
/// UdpSocket writes <see cref="action"/> and <see cref="step"/> when packets
/// arrive from the Python side; consumers (StepCounter, DoctorTalk, ...) read
/// them in Update.
/// </summary>
public class RAGInput : MonoBehaviour
{
    public static string action = "idle";
    public static int step = 0;
}
