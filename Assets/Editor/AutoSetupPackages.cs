#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class AutoSetupPackages : EditorWindow
{
    private static int packagesImported = 0;
    private static string[] packageNames = {
        "classroom.unitypackage",
        "conference_hall.unitypackage",
        "meeting_room.unitypackage"
    };

    [MenuItem("AR Trainer/🚀 1- Sahneleri ve Animasyonlari Otomatik Kur")]
    public static void RunFullSetup()
    {
        packagesImported = 0;
        AssetDatabase.importPackageCompleted += OnPackageImported;

        string downloadsFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
        string targetFolder1 = Path.Combine(downloadsFolder, "scenes_packeges,", "scenes_packeges,");
        string targetFolder2 = Path.Combine(downloadsFolder, "scenes_packeges,");
        
        bool anyFound = false;

        foreach (var pkg in packageNames)
        {
            string pkgPath = Path.Combine(targetFolder1, pkg);
            if (!File.Exists(pkgPath))
            {
                pkgPath = Path.Combine(targetFolder2, pkg);
            }

            if (File.Exists(pkgPath))
            {
                anyFound = true;
                Debug.Log("[Oto-Kurulum] Paket ice aktariliyor: " + pkgPath);
                AssetDatabase.ImportPackage(pkgPath, false);
            }
            else
            {
                Debug.LogError("[Oto-Kurulum] Paket bulunamadi: " + pkgPath);
                packagesImported++;
            }
        }

        if (!anyFound)
        {
            Debug.LogWarning("[Oto-Kurulum] İndirilenlerde paketler bulunamadı. Dilerseniz sahne konfigürasyonuna direkt geçiliyor.");
            AssetDatabase.importPackageCompleted -= OnPackageImported;
            ConfigureAllScenes();
        }
    }

    private static void OnPackageImported(string packageName)
    {
        packagesImported++;
        Debug.Log("[Oto-Kurulum] Paket tamamlandi: " + packageName);

        if (packagesImported >= packageNames.Length)
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
            ConfigureAllScenes();
        }
    }

    [MenuItem("AR Trainer/⚙️ 2- Tum Sahneleri Konfigure Et (Manuel)")]
    public static void ConfigureAllScenes()
    {
        string[] scenesToProcess = {
            "Assets/Scenes/Scene_Classroom.unity",
            "Assets/Scenes/Scene_MeetingRoom.unity",
            "Assets/Scenes/Scene_ConferenceHall.unity"
        };

        // Find all student / character prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        List<GameObject> diverseStudents = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null && prefab.GetComponentInChildren<Animator>() != null)
            {
                if (!prefab.name.ToLower().Contains("gray") && !prefab.name.ToLower().Contains("default"))
                {
                    if (prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    {
                        diverseStudents.Add(prefab);
                    }
                }
            }
        }

        if (diverseStudents.Count == 0)
        {
            Debug.LogWarning("[Oto-Kurulum] İnsan modeli bulunamadı! Eğer Mixamo karakter atarsanız otomatik tanınacaktır.");
        }

        foreach (string scenePath in scenesToProcess)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning("Sahne bulunamadi: " + scenePath);
                continue;
            }

            // Acmak uzere oldugumuz sahneyi kaydedelim
            if (EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("[Oto-Kurulum] " + scene.name + " konfigure ediliyor...");

            // Determine environment type based on scene name
            EnvironmentType envType = EnvironmentType.classroom;
            if (scene.name.Contains("Meeting")) envType = EnvironmentType.meeting_room;
            else if (scene.name.Contains("Conference")) envType = EnvironmentType.conference_hall;

            // Find or Create AudienceBehaviorController
            AudienceBehaviorController behaviorController = Object.FindFirstObjectByType<AudienceBehaviorController>();
            if (behaviorController == null)
            {
                GameObject behaviorObj = new GameObject("AudienceSystem");
                behaviorController = behaviorObj.AddComponent<AudienceBehaviorController>();
            }

            // Setup Engines
            AudienceReactionEngine engine = Object.FindFirstObjectByType<AudienceReactionEngine>();
            if (engine == null)
            {
                engine = behaviorController.gameObject.AddComponent<AudienceReactionEngine>();
                PerformanceScoringEngine perf = behaviorController.gameObject.AddComponent<PerformanceScoringEngine>();
                engine.scoringEngine = perf;
            }
            engine.environmentType = envType;
            behaviorController.reactionEngine = engine;

            // Setup Spawner
            AudienceSpawner spawner = Object.FindFirstObjectByType<AudienceSpawner>();
            if (spawner == null)
            {
                spawner = behaviorController.gameObject.AddComponent<AudienceSpawner>();
            }
            spawner.controller = behaviorController;
            spawner.audiencePrefabs = diverseStudents;

            // Editor Save Make Dirty
            EditorUtility.SetDirty(behaviorController);
            EditorUtility.SetDirty(engine);
            EditorUtility.SetDirty(spawner);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Oto-Kurulum] " + scene.name + " basariyla kaydedildi.");
        }

        EditorUtility.DisplayDialog("Basarili", "Sahneler (Classroom, Meeting, Conference) ayri ayri konfigure edildi. Ten renkli ogrenciler atandi ve gercekci reaksiyon motorlari baglandi!", "Tamam");
    }
}
#endif
