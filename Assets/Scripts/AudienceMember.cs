using System.Collections;
using UnityEngine;

public class AudienceMember : MonoBehaviour
{
    public Animator animator;
    public float reactionDelay = 0f;
    public float personalWpmTolerance;
    public float personalEyeContactTolerance;
    private AudienceState _currentState = AudienceState.Idle;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        reactionDelay = Random.Range(0f, 3.0f);
        personalWpmTolerance = 0f;
        personalEyeContactTolerance = 0f;
    }

    public void SetState(AudienceState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        float delay = newState == AudienceState.Applauding ? 0f : reactionDelay;
        StartCoroutine(ApplyStateWithDelay(newState, delay));
    }

    private IEnumerator ApplyStateWithDelay(AudienceState state, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerAnimation(state);
    }

    private void TriggerAnimation(AudienceState state)
    {
        if (animator == null) return;
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Attentive");
        animator.ResetTrigger("Neural");
        animator.ResetTrigger("Distracted");
        animator.ResetTrigger("Bored");
        animator.ResetTrigger("Applaud");
        switch (state)
        {
            case AudienceState.Idle: animator.SetTrigger("Idle"); break;
            case AudienceState.Attentive: animator.SetTrigger("Attentive"); break;
            case AudienceState.Neutral: animator.SetTrigger("Neural"); break;
            case AudienceState.Distracted: animator.SetTrigger("Distracted"); break;
            case AudienceState.Bored: animator.SetTrigger("Bored"); break;
            case AudienceState.Applauding: animator.SetTrigger("Applaud"); break;
        }
    }
}