using System.Collections.Generic;
using UnityEngine;

public class AudienceSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public List<GameObject> audiencePrefabs;

    [Header("Seating Layout")]
    public int rowCount = 5;
    public int colCount = 7;
    public float rowSpacing = 2.0f;
    public float colSpacing = 1.5f;

    [Header("Start Position")]
    public Vector3 startPosition = new Vector3(-4.5f, 0, 2f);

    [Header("Audience Controller")]
    public AudienceBehaviorController controller;

    void Start()
    {
        SpawnAudience();
    }

    public void SpawnAudience()
    {
        foreach (var m in controller.audienceMembers)
            if (m != null) Destroy(m.gameObject);
        controller.audienceMembers.Clear();

        int count = GetAudienceCount();
        int spawned = 0;

        float[] blockOffsets = { -5f, 0f, 5f };

        for (int row = 0; row < rowCount && spawned < count; row++)
        {
            foreach (float blockX in blockOffsets)
            {
                for (int col = 0; col < 2 && spawned < count; col++)
                {
                    float x = blockX + col * colSpacing;
                    float z = startPosition.z + row * rowSpacing;
                    Vector3 pos = new Vector3(x, startPosition.y, z);

                    int randomIndex = Random.Range(0, audiencePrefabs.Count);
                    GameObject member = Instantiate(audiencePrefabs[randomIndex], pos, Quaternion.Euler(0, 180, 0));

                    AudienceMember am = member.GetComponent<AudienceMember>();
                    if (am == null) am = member.AddComponent<AudienceMember>();

                    switch (controller.currentStressLevel)
                    {
                        case StressLevel.Easy:
                            am.personalWpmTolerance = Random.Range(-30f, -5f);
                            am.personalEyeContactTolerance = Random.Range(-0.2f, -0.05f);
                            break;
                        case StressLevel.Medium:
                            am.personalWpmTolerance = Random.Range(-10f, 10f);
                            am.personalEyeContactTolerance = Random.Range(-0.08f, 0.08f);
                            break;
                        case StressLevel.Hard:
                            am.personalWpmTolerance = Random.Range(10f, 40f);
                            am.personalEyeContactTolerance = Random.Range(0.1f, 0.3f);
                            break;
                    }

                    controller.audienceMembers.Add(am);
                    spawned++;
                }
            }
        }
    }

    int GetAudienceCount()
    {
        switch (controller.currentStressLevel)
        {
            case StressLevel.Easy: return 8;
            case StressLevel.Medium: return 18;
            case StressLevel.Hard: return 35;
            default: return 18;
        }
    }
}