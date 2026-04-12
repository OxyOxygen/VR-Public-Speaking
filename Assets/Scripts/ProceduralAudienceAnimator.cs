using UnityEngine;

public class ProceduralAudienceAnimator : MonoBehaviour
{
    public enum StudentState { Idle, Nodding, Distracted, Stretching, Sleeping, Applauding }

    [Header("State Control")]
    [SerializeField] private StudentState currentState = StudentState.Idle;

    [Header("Bone References")]
    [SerializeField] private Transform spine;
    [SerializeField] private Transform neck; // Organik hareket için eklendi
    [SerializeField] private Transform head;

    [Header("Settings")]
    [SerializeField] private float transitionSpeed = 4f;
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private float breathingAmount = 6f; 
    [SerializeField] private float noddingSpeed = 5f;
    [SerializeField] private float noddingAmount = 25f; // Gözle görülür sallama
    [SerializeField] private float distractionSpeed = 1.5f;
    [SerializeField] private float distractionAmount = 35f;

    [Header("External Integration")]
    [HideInInspector] public float externalBoredomLevel = 0f;
    [HideInInspector] public float externalPerformanceScore = 0f;
    [HideInInspector] public Transform[] distractionTargets;

    private Quaternion initialSpineRot;
    private Quaternion initialNeckRot;
    private Quaternion initialHeadRot;
    private Quaternion targetSpineRot;
    private Quaternion targetNeckRot;
    private Quaternion targetHeadRot;
    
    // SÜREKLİ EZİLEN ANIMATOR'A KARŞI GERÇEK DEĞERLERİ TUTAN FİZİKSEL DEĞİŞKENLER
    private Quaternion currentSpineRot;
    private Quaternion currentNeckRot;
    private Quaternion currentHeadRot;

    private float timer;
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        if (spine == null || head == null)
        {
            if (_animator != null && _animator.isHuman)
            {
                if (spine == null) spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                if (neck == null)  neck  = _animator.GetBoneTransform(HumanBodyBones.Neck);
                if (head == null)  head  = _animator.GetBoneTransform(HumanBodyBones.Head);
            }

            // EĞER HUMAN DEĞİLSE VEYA AVATAR YOKSA İSİMDEN (RECURSIVE) BULMAYA ÇALIŞ:
            if (spine == null) spine = FindBoneRecursive(transform, "Spine");
            if (neck == null)  neck  = FindBoneRecursive(transform, "Neck");
            if (head == null)  head  = FindBoneRecursive(transform, "Head");
            
            if (spine == null || head == null)
            {
                Debug.LogError($"[ProceduralAudienceAnimator] {gameObject.name} karakterinde Spine veya Head kemiği bulunamadı!", this);
            }
        }
    }

    private Transform FindBoneRecursive(Transform current, string boneName)
    {
        if (current.name.ToLower().Contains(boneName.ToLower()))
            return current;

        foreach (Transform child in current)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    void Start()
    {
        if (spine != null) 
        {
            initialSpineRot = spine.localRotation;
            currentSpineRot = initialSpineRot;
        }
        if (neck != null)
        {
            initialNeckRot = neck.localRotation;
            currentNeckRot = initialNeckRot;
        }
        if (head != null) 
        {
            initialHeadRot = head.localRotation;
            currentHeadRot = initialHeadRot;
        }

        // Kargaşa ve Offset:
        timer = Random.Range(0f, 1000f);
        transitionSpeed *= Random.Range(0.6f, 1.4f);
        breathingSpeed *= Random.Range(0.8f, 1.2f);
        noddingSpeed *= Random.Range(0.8f, 1.2f);
    }

    void LateUpdate()
    {
        if (currentState == StudentState.Applauding) return;
        if (spine == null || head == null) return;

        timer += Time.deltaTime;
        CalculateTargets();

        // Daha insansı ve yumuşak kavisler için Lerp yerine Slerp (Spherical Lerp) kullanıyoruz
        currentSpineRot = Quaternion.Slerp(currentSpineRot, targetSpineRot, Time.deltaTime * transitionSpeed);
        if (neck != null) currentNeckRot = Quaternion.Slerp(currentNeckRot, targetNeckRot, Time.deltaTime * transitionSpeed);
        currentHeadRot = Quaternion.Slerp(currentHeadRot, targetHeadRot, Time.deltaTime * transitionSpeed);

        spine.localRotation = currentSpineRot;
        if (neck != null) neck.localRotation = currentNeckRot;
        head.localRotation = currentHeadRot;
    }

    // GİZLİ RIG HATALARINI ÇÖZEN SABİT EKSEN HESAPLAYICISI
    // Rig (çizim) X ekseni yukarı mı bakıyor aşağı mı fark etmeksizin, objenin kendisine göre öne eğmesini sağlar.
    private Quaternion SafeEuler(float pitchForward, float yawRight, float rollSideways, Transform bone)
    {
        if (bone == null || bone.parent == null) return Quaternion.identity;

        Vector3 localRight = bone.parent.InverseTransformDirection(transform.right);
        Vector3 localUp = bone.parent.InverseTransformDirection(transform.up);
        Vector3 localForward = bone.parent.InverseTransformDirection(transform.forward);

        Quaternion p = Quaternion.AngleAxis(pitchForward, localRight);
        Quaternion y = Quaternion.AngleAxis(yawRight, localUp);
        Quaternion r = Quaternion.AngleAxis(rollSideways, localForward);

        return p * y * r;
    }

    private void CalculateTargets()
    {
        // 1. ORGANİK NEFES: İnsan nefesi kusursuz robotik bir ritimde olmaz. Sinüs dalgasına Perlin gürültüsü katıyoruz.
        float organicBreathModifier = 1f + (Mathf.PerlinNoise(timer * 0.2f, 0f) - 0.5f) * 0.4f; 
        float breath = Mathf.Sin(timer * breathingSpeed * organicBreathModifier) * breathingAmount;
        
        // 2. MİKRO VÜCUT SARKMASI: İnsanlar otururken omurgaları milimetrik olarak yer değiştirir.
        float microSwayX = (Mathf.PerlinNoise(timer * 0.15f, 10f) - 0.5f) * 3f; 
        float microSwayZ = (Mathf.PerlinNoise(20f, timer * 0.15f) - 0.5f) * 2f;

        Quaternion breathSpineRot = SafeEuler(breath + microSwayX, 0f, microSwayZ, spine);
        Quaternion breathNeckRot = SafeEuler(breath * -0.15f, 0f, 0f, neck);
        Quaternion breathHeadRot = SafeEuler(breath * -0.15f, 0f, 0f, head);

        if (neck == null) targetNeckRot = Quaternion.identity; // Güvenlik

        switch (currentState)
        {
            case StudentState.Idle:
                // 3. MİKRO BAKIŞLAR: Dinleyen biri robot gibi kilitlenmez, kafası 2-3 derece arayla istemsizce sağa sola kayar
                float microGlanceY = (Mathf.PerlinNoise(timer * 0.3f, 30f) - 0.5f) * 6f; // Sağa sola hafif süzme
                float microGlanceX = (Mathf.PerlinNoise(40f, timer * 0.3f) - 0.5f) * 3f; // Yukarı aşağı hafif oynama

                targetSpineRot = breathSpineRot * initialSpineRot;
                if (neck != null) targetNeckRot = SafeEuler(microGlanceX * 0.5f, microGlanceY * 0.5f, 0f, neck) * breathNeckRot * initialNeckRot;
                targetHeadRot = SafeEuler(microGlanceX, microGlanceY, 0f, head) * breathHeadRot * initialHeadRot;
                break;

            case StudentState.Nodding:
                targetSpineRot = SafeEuler(4f, 0f, 0f, spine) * breathSpineRot * initialSpineRot;
                
                // Kafa sallarken daha insansı bir kavis (yukarı yavaş, aşağı hızlı vurma efekti için Abs ve ağırlık)
                float rawNod = Mathf.Sin(timer * noddingSpeed);
                float organicNod = rawNod * noddingAmount + (Mathf.PerlinNoise(timer, 0f)*2f);
                
                if (neck != null) targetNeckRot = SafeEuler(organicNod * 0.4f, 0f, 0f, neck) * breathNeckRot * initialNeckRot;
                targetHeadRot = SafeEuler(organicNod * 0.6f, 0f, 0f, head) * breathHeadRot * initialHeadRot;
                break;

            case StudentState.Distracted:
                float bodySwayX = (Mathf.PerlinNoise(timer * distractionSpeed * 0.4f, 0f) - 0.5f) * 18f; 
                float bodySwayZ = (Mathf.PerlinNoise(0f, timer * distractionSpeed * 0.4f) - 0.5f) * 18f;
                targetSpineRot = SafeEuler(bodySwayX, 0f, bodySwayZ, spine) * breathSpineRot * initialSpineRot;
                
                // Etrafa daha detaylı bakınma (Hem boyun hem kafa kullanarak)
                float lookX = (Mathf.PerlinNoise(timer * distractionSpeed, 0f) - 0.5f) * distractionAmount;
                float lookY = (Mathf.PerlinNoise(0f, timer * distractionSpeed) - 0.5f) * distractionAmount;
                
                if (neck != null) targetNeckRot = SafeEuler(lookX * 0.4f, lookY * 0.4f, 0f, neck) * breathNeckRot * initialNeckRot;
                targetHeadRot = SafeEuler(lookX * 0.6f, lookY * 0.6f, 0f, head) * breathHeadRot * initialHeadRot;
                break;

            case StudentState.Stretching:
                float stretchBreath = Mathf.Sin(timer * breathingSpeed) * (breathingAmount * 1.5f);
                targetSpineRot = SafeEuler(-20f, 0f, 0f, spine) * SafeEuler(stretchBreath, 0f, 0f, spine) * initialSpineRot;
                if (neck != null) targetNeckRot = SafeEuler(-10f, 0f, 0f, neck) * initialNeckRot;
                targetHeadRot = SafeEuler(-5f, 0f, 0f, head) * initialHeadRot;
                break;

            case StudentState.Sleeping:
                // Uyuyan insanın nefesi derin ve yavaştır
                float sleepBreath = Mathf.Sin(timer * (breathingSpeed * 0.4f)) * (breathingAmount * 0.7f);
                targetSpineRot = SafeEuler(65f, 0f, 0f, spine) * SafeEuler(sleepBreath, 0f, 0f, spine) * initialSpineRot;
                if (neck != null) targetNeckRot = SafeEuler(20f, 0f, 0f, neck) * initialNeckRot;
                targetHeadRot = SafeEuler(20f, 0f, 0f, head) * initialHeadRot;
                break;
        }
    }

    public void SetState(AudienceState extState)
    {
        Debug.Log($"[{gameObject.name}] received SetState command: {extState}");
        switch (extState)
        {
            case AudienceState.Applauding:
                currentState = StudentState.Applauding;
                if (_animator != null) _animator.SetTrigger("Applaud");
                break;
            case AudienceState.Nodding:
                currentState = StudentState.Nodding;
                break;
            case AudienceState.Bored:
                // Sıkıntı arttığında geriye yaslanma yerine Masaya Yığılmaya daha hızlı geçsin
                currentState = StudentState.Sleeping; 
                break;
            case AudienceState.Sleeping:
                currentState = StudentState.Sleeping;
                break;
            case AudienceState.Distracted:
                currentState = StudentState.Distracted;
                break;
            case AudienceState.Stretching:
                currentState = StudentState.Stretching;
                break;
            default:
                currentState = StudentState.Idle; // Idle, Attentive, Neutral
                break;
        }
    }
}
