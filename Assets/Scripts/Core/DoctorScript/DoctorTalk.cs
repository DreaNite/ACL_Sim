using UnityEngine;

/// <summary>
/// Drives the surgeon's talking animation based on <see cref="RAGInput.action"/>.
/// The animator's "control" int is 1 while talking and 0 otherwise.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DoctorTalk : MonoBehaviour
{
    private Animator _animator;

    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        _animator.SetInteger("control", RAGInput.action.Equals("talk") ? 1 : 0);
    }
}
