using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StressLevel { Easy, Medium, Hard }

public enum AudienceState
{
    Idle,
    Attentive,
    Neutral,
    Distracted,
    Bored,
    Applauding
}

[System.Serializable]
public class PerformanceThresholds
{
    [Header("Eye Contact")]
    public float eyeContactGoodThreshold = 0.70f;
    public float eyeContactPoorThreshold = 0.30f;

    [Header("Speech Pace (WPM)")]
    public float wpmTooFast = 180f;
    public float wpmTooSlow = 100f;

    [Header("Filler Words")]
    public float fillerWordsPerMinute = 5f;

    [Header("Monotone Detection")]
    public float monotoneDurationSeconds = 20f;
}

[System.Serializable]
public class StressScenario
{
    public StressLevel level;
    public int audienceSize;
    public float negativeReactionMultiplier;
    public float positiveReactionMultiplier;
    [TextArea] public string description;
}

public class AudienceBehaviorController : MonoBehaviour
{
    [Header("Scenario Settings")]
    public StressLevel currentStressLevel = StressLevel.Medium;
    public List<StressScenario> scenarios = new List<StressScenario>();

    [Header("Thresholds")]
    public PerformanceThresholds thresholds = new PerformanceThresholds();

    [Header("Audience Members")]
    public List<AudienceMember> audienceMembers = new List<AudienceMember>();

    private StressScenario _activeScenario;
    private float _monotoneTimer = 0f;
    private bool _isMonotone = false;

    [Header("Live Metrics (Test)")]
    public float eyeContactRatio = 1.0f;
    public float currentWPM = 130f;
    public float fillerWordsPerMin = 0f;
    public bool isVoiceMonotone = false;
    public bool sessionEnded = false;

    void Start()
    {
        ApplyScenario(currentStressLevel);
        foreach (var member in audienceMembers)
            if (member != null) member.SetState(AudienceState.Neutral);
    }

    void Update()
    {
        if (_activeScenario == null || _activeScenario.level != currentStressLevel)
            ApplyScenario(currentStressLevel);

        if (sessionEnded)
        {
            foreach (var member in audienceMembers)
                if (member != null) member.SetState(AudienceState.Applauding);
            return;
        }

        UpdateMonotoneTimer();
        EvaluateAndSetStatePerMember();
    }

    private void EvaluateAndSetStatePerMember()
    {
        float negMult = _activeScenario != null ? _activeScenario.negativeReactionMultiplier : 1f;

        foreach (var member in audienceMembers)
        {
            if (member == null) continue;

            float eyeTolerance = member.personalEyeContactTolerance;
            float wpmTolerance = member.personalWpmTolerance;

            AudienceState state;

            if ((_isMonotone && negMult >= 1.0f) || currentWPM < thresholds.wpmTooSlow + wpmTolerance)
            {
                state = AudienceState.Bored;
            }
            else if (eyeContactRatio < (thresholds.eyeContactPoorThreshold + eyeTolerance) * negMult
                || fillerWordsPerMin > thresholds.fillerWordsPerMinute * negMult)
            {
                state = AudienceState.Distracted;
            }
            else if (eyeContactRatio > thresholds.eyeContactGoodThreshold
                && currentWPM >= thresholds.wpmTooSlow
                && currentWPM <= thresholds.wpmTooFast
                && fillerWordsPerMin < thresholds.fillerWordsPerMinute)
            {
                state = AudienceState.Attentive;
            }
            else
            {
                state = AudienceState.Neutral;
            }

            Debug.Log($"[Member] {member.gameObject.name} → {state} | eyeTol: {eyeTolerance:F2} | wpmTol: {wpmTolerance:F1}");
            member.SetState(state);
        }
    }

    private void UpdateMonotoneTimer()
    {
        if (isVoiceMonotone)
        {
            _monotoneTimer += Time.deltaTime;
            if (_monotoneTimer >= thresholds.monotoneDurationSeconds)
                _isMonotone = true;
        }
        else
        {
            _monotoneTimer = 0f;
            _isMonotone = false;
        }
    }

    public void ApplyScenario(StressLevel level)
    {
        _activeScenario = scenarios.Find(s => s.level == level);
        if (_activeScenario == null)
        {
            Debug.LogWarning($"[Audience] Scenario not found for level: {level}");
            return;
        }
        currentStressLevel = level;
    }

    public void UpdateMetrics(float eyeContact, float wpm, float fillerWPM, bool monotone)
    {
        eyeContactRatio = Mathf.Clamp01(eyeContact);
        currentWPM = Mathf.Max(0, wpm);
        fillerWordsPerMin = Mathf.Max(0, fillerWPM);
        isVoiceMonotone = monotone;
    }

    public void TriggerSessionEnd() => sessionEnded = true;
    public void ChangeStressLevel(StressLevel level) => ApplyScenario(level);
}