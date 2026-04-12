using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public class AutoFBXExtractor
{
    static AutoFBXExtractor()
    {
        EditorApplication.delayCall += RunExtraction;
    }

    [MenuItem("Tools/Sihirli Karakter Yükleyici (Extract & Auto-Link)")]
    public static void RunExtraction()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");
        bool extractedAny = false;
        List<GameObject> detectedCharacters = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Sadece .fbx dosyalarını hedefliyoruz, animasyon dosyalarını (@) atlıyoruz.
            // Ayrıca istemediğimiz "Sitting.fbx" isimli man/woman hazır modellerini dahil etmiyoruz.
            if (path.ToLower().EndsWith(".fbx") && !path.Contains("@") && !path.ToLower().EndsWith("sitting.fbx"))
            {
                GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbxObj != null && IsCharacterModel(fbxObj))
                {
                    detectedCharacters.Add(fbxObj);

                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null)
                    {
                        bool needsReimport = false;

                        // 1. ZORUNLU HUMANOID KONTROLÜ (Kıyafetten Bağımsız Çalışmalı!)
                        // Eğer karakter jenerik ise animasyon alamaz ve T-Pose'da kalır.
                        if (importer.animationType != ModelImporterAnimationType.Human)
                        {
                            importer.animationType = ModelImporterAnimationType.Human;
                            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                            Debug.Log("[AutoFBXExtractor] Hata Düzeltildi - T-Pose engellendi, Humanoid'e Çevrildi: " + path);
                            needsReimport = true;
                        }

                        // 2. MATERYAL KONTROLÜ (Sadece içinde saklıysa çıkar)
                        if (importer.materialLocation == ModelImporterMaterialLocation.InPrefab)
                        {
                            string folderName = "Materials_" + Path.GetFileNameWithoutExtension(path);
                            string parentDir = Path.GetDirectoryName(path);
                            string targetFolder = parentDir + "/" + folderName;

                            if (!AssetDatabase.IsValidFolder(targetFolder))
                            {
                                AssetDatabase.CreateFolder(parentDir, folderName);
                            }

                            // Kıyafet resimlerini FBX'in içinden çekip çıkarır
                            importer.ExtractTextures(targetFolder);
                            
                            importer.materialLocation = ModelImporterMaterialLocation.External;
                            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                            
                            Debug.Log("[AutoFBXExtractor] Renkler ve Kıyafetler Çıkarıldı: " + path);
                            needsReimport = true;
                            extractedAny = true;
                        }

                        if (needsReimport)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
        }

        // ==============================================================
        //  OTOMATİK LİSTE EKLEMESİ (SADECE SÜRÜKLE BIRAK YETSİN DİYE)
        // ==============================================================
        bool listsUpdated = false;

        // Bütün Prefab'lerdeki Spawnerleri bulalım
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach(var pGuid in prefabGuids)
        {
            string pPath = AssetDatabase.GUIDToAssetPath(pGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            if(prefab != null)
            {
                AudienceSpawner[] spawners = prefab.GetComponentsInChildren<AudienceSpawner>(true);
                foreach(var spawner in spawners)
                {
                    // Eski eklenip sonradan yasaklananları temizlemek için önce listeyi sıfırla
                    spawner.audiencePrefabs.Clear();
                    foreach(var character in detectedCharacters)
                    {
                        if(!spawner.audiencePrefabs.Contains(character))
                        {
                            spawner.audiencePrefabs.Add(character);
                            EditorUtility.SetDirty(prefab);
                            listsUpdated = true;
                        }
                    }
                }
            }
        }

        // Sahnede açık olan Spawnerleri bulalım
        AudienceSpawner[] sceneSpawners = Object.FindObjectsByType<AudienceSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(var spawner in sceneSpawners)
        {
            // Eski eklenip sonradan yasaklananları temizlemek için önce listeyi sıfırla
            spawner.audiencePrefabs.Clear();
            foreach(var character in detectedCharacters)
            {
                if(!spawner.audiencePrefabs.Contains(character))
                {
                    spawner.audiencePrefabs.Add(character);
                    EditorUtility.SetDirty(spawner);
                    listsUpdated = true;
                }
            }
        }

        if (extractedAny)
            Debug.Log("[AutoFBXExtractor] Yeni karakterlerin kıyafetleri dışarı çıkartılıp başarıyla giydirildi!");
        
        if (listsUpdated)
            Debug.Log($"[AutoFBXExtractor] {detectedCharacters.Count} adet farklı insan modeli bulundu ve Sınıf/Salon listelerine otomatik eklendi!");
    }

    private static bool IsCharacterModel(GameObject fbxObj)
    {
        // Eğer obje bir insansa (Mixamo karakteri vb.), %99 oranında içinde "Hips" isminde bir kemik barındırır.
        // Masa, sandalye gibi FBX'lerde bu kelime olmaz.
        Transform[] allTransforms = fbxObj.GetComponentsInChildren<Transform>(true);
        foreach(var t in allTransforms)
        {
            if (t.name.ToLower().Contains("hips"))
                return true;
        }
        return false;
    }
}
