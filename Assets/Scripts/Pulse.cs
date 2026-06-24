using UnityEngine;

/// <summary>
/// Smoothly oscillates the object's local scale between minScale and maxScale
/// using a sine wave. Used on the Step pointers to draw attention.
/// </summary>
public class Pulse : MonoBehaviour
{
    public float speed = 2f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        transform.localScale = originalScale * Mathf.Lerp(minScale, maxScale, t);
    }
}
