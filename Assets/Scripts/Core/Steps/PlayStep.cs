using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Plays the attached <see cref="PlayableDirector"/> whenever the GameObject
/// is enabled. Used on each Step root so its timeline kicks in when the step
/// becomes active.
/// </summary>
public class PlayStep : MonoBehaviour
{
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool playOnlyOnce = false;

    private bool hasPlayed;

    void Start()
    {
        if (timelineDirector == null)
            timelineDirector = GetComponent<PlayableDirector>();
    }

    void OnEnable()
    {
        if (playOnEnable && !(playOnlyOnce && hasPlayed))
            PlayTimeline();
    }

    void PlayTimeline()
    {
        if (timelineDirector == null)
        {
            Debug.LogWarning($"No PlayableDirector assigned on {gameObject.name}");
            return;
        }

        timelineDirector.Stop();
        timelineDirector.Play();
        hasPlayed = true;
    }

    /// <summary>Plays the timeline regardless of the playOnEnable / playOnlyOnce flags.</summary>
    public void PlayTimelineManually() => PlayTimeline();

    /// <summary>Allows the timeline to play again even when playOnlyOnce is true.</summary>
    public void ResetPlayedFlag() => hasPlayed = false;
}
