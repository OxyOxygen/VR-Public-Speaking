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
    Applauding,
    Nodding,
    Stretching,
    Sleeping
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

    [Header("Core Engines")]
    public AudienceReactionEngine reactionEngine;

    [Header("Audience Members")]
    public List<AudienceMember> audienceMembers = new List<AudienceMember>();

    private StressScenario _activeScenario;

    [Header("Live State")]
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

        EvaluateAndSetStatePerMember();
    }

    private void EvaluateAndSetStatePerMember()
    {
        if (reactionEngine == null || reactionEngine.scoringEngine == null)
            return; // Wait for engines

        // Get the frame from Engine
        ReactionFrame frame = reactionEngine.currentReaction;
        float negMult = _activeScenario != null ? _activeScenario.negativeReactionMultiplier : 1f;
        float finalScore = reactionEngine.scoringEngine.GetFinalScore();

        foreach (var member in audienceMembers)
        {
            if (member == null) continue;

            // ---- BİREYSEL SKOR HESABI ----
            // Her öğrencinin kişilik toleransı, genel skoru kendi perspektifinden kaydırır.
            float personalScore = finalScore 
                + (member.personalEyeContactTolerance * 40f)
                + (member.personalWpmTolerance * 0.3f);
            personalScore = Mathf.Clamp(personalScore, 0f, 100f);

            // Stress seviyesine göre negatif tepkileri güçlendir
            personalScore = Mathf.Lerp(personalScore, personalScore * (2f - negMult), 0.5f);

            AudienceState targetState;

            // ---- KESİN VE MUTLAK UYKU TETİKLEYİCİSİ ----
            if (finalScore < 20f)
            {
                // Tolerans falan dinlemeden, sınıf çok kötü performans sebebiyle TOPLUCA masaya yığılır
                targetState = AudienceState.Sleeping;
                
                if (member.proceduralAnimator != null)
                    member.proceduralAnimator.externalBoredomLevel = 1f;
            }
            // ---- BİREYSEL STATE SEÇİMİ YENİ (3 BAND - SCORE SCALING) ----
            else if (personalScore < 40f)
            {
                // LOW SCORE (20 - 40 Arası): 3 Farklı Olumsuz Animasyonun Dengeli Dağılımı
                // Öğrencinin bireysel tahammül seviyesine göre hala bazıları uyanık (Distracted) kalmaya çalışır
                if (member.personalWpmTolerance < -5f)
                {
                    targetState = AudienceState.Sleeping; 
                }
                else if (frame.dominant_factors.Contains("eye_contact_low") && personalScore > 30f)
                {
                    targetState = AudienceState.Distracted; // Sadece sıkılıp etrafa bakınsın
                }
                else
                {
                    targetState = AudienceState.Stretching; // Geriye kaykılma (Boredom)
                }
                
                if (member.proceduralAnimator != null)
                    member.proceduralAnimator.externalBoredomLevel = Mathf.InverseLerp(40f, 20f, personalScore);
            }
            else if (personalScore <= 60f)
            {
                // NORMAL SCORE (Normal dinleme, aşırı tepki yok)
                targetState = AudienceState.Neutral;
                
                if (member.proceduralAnimator != null)
                    member.proceduralAnimator.externalBoredomLevel = 0f;
            }
            else
            {
                // HIGH SCORE (Nodding, Attentive - Pür dikkat / Onaylama)
                // Göz teması iyiyse veya öğrencinin kişisel yatkınlığı varsa kafa sallayıp onaylasın
                if (frame.dominant_factors.Contains("good_eye_contact") || member.personalEyeContactTolerance > 0f)
                    targetState = AudienceState.Nodding;
                else
                    targetState = AudienceState.Attentive;
                
                if (member.proceduralAnimator != null)
                    member.proceduralAnimator.externalBoredomLevel = 0f;
            }

            member.SetState(targetState);
        }
    }

    public void ApplyScenario(StressLevel level)
    {
        _activeScenario = scenarios.Find(s => s.level == level);
        if (_activeScenario == null)
        {
            _activeScenario = CreateDefaultScenario(level);
        }
        currentStressLevel = level;
    }

    private StressScenario CreateDefaultScenario(StressLevel level)
    {
        StressScenario scenario = new StressScenario();
        scenario.level = level;

        switch (level)
        {
            case StressLevel.Easy:
                scenario.audienceSize = 20;
                scenario.negativeReactionMultiplier = 0.85f;
                scenario.positiveReactionMultiplier = 1.1f;
                scenario.description = "Default easy scenario";
                break;
            case StressLevel.Hard:
                scenario.audienceSize = 50;
                scenario.negativeReactionMultiplier = 1.2f;
                scenario.positiveReactionMultiplier = 0.9f;
                scenario.description = "Default hard scenario";
                break;
            case StressLevel.Medium:
            default:
                scenario.audienceSize = 35;
                scenario.negativeReactionMultiplier = 1f;
                scenario.positiveReactionMultiplier = 1f;
                scenario.description = "Default medium scenario";
                break;
        }

        return scenario;
    }

    public void TriggerSessionEnd() => sessionEnded = true;
    public void ChangeStressLevel(StressLevel level) => ApplyScenario(level);
}
