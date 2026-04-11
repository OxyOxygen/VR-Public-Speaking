using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Sınıftaki tüm öğrencilerin Prosedürel Animasyon (IK) sistemini tek tıkla kuran editör aracı.
/// Hedefleri otomatik oluşturur, akıllı konumlandırır, scriptleri ekler ve referansları bağlar.
/// </summary>
public class ClassroomSetupManager : EditorWindow
{
    [MenuItem("Tools/AR Speaking Trainer/Setup Classroom Procedural Animation")]
    public static void SetupClassroom()
    {
        Debug.Log("[ClassroomSetupManager] Otomatik kurulum başlatılıyor...");

        // 1. ÖĞRENCİLERİ BUL
        // Kullanıcının isteği üzerine sahnede Animator bileşeni olan objeleri arıyoruz.
        // İsmi "remy" veya "ch" içerenleri öğrenci olarak kabul ediyoruz.
        List<GameObject> allStudents = new List<GameObject>();
        
        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator anim in allAnimators)
        {
            string objName = anim.gameObject.name.ToLower();
            if (objName.Contains("remy") || objName.Contains("ch"))
            {
                // Aynı objeyi iki kez eklememek için kontrol
                if (!allStudents.Contains(anim.gameObject))
                {
                    allStudents.Add(anim.gameObject);
                }
            }
        }

        // Güvenlik Kontrolü: Öğrenci bulunamadıysa kullanıcıyı uyar.
        if (allStudents.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Kurulum Hatası", 
                "Sahnede ismi 'remy' veya 'ch' içeren ve üzerinde Animator bulunan hiçbir öğrenci bulunamadı!\n\nLütfen karakterlerinizin isimlerini kontrol edin.", 
                "Tamam"
            );
            return;
        }

        // 2. AKILLI HEDEF YERLEŞİMİ İÇİN ÖĞRENCİLERİN ORTALAMA POZİSYONUNU HESAPLA
        Vector3 averagePosition = Vector3.zero;
        foreach (var student in allStudents)
        {
            averagePosition += student.transform.position;
        }
        averagePosition /= allStudents.Count;

        // 3. ANA HEDEFİ (MAIN TARGET) BUL VEYA OLUŞTUR
        GameObject mainTarget = GameObject.Find("Main_Board_Target");
        if (mainTarget == null)
        {
            mainTarget = new GameObject("Main_Board_Target");
            
            // Ortalama pozisyonun 10 birim ilerisine (Z ekseni) ve biraz yukarısına yerleştir
            mainTarget.transform.position = averagePosition + new Vector3(0, 1.5f, 10f); 
            Debug.Log("-> Main_Board_Target akıllı konuma oluşturuldu.");
        }

        // 4. DİKKAT DAĞITICI HEDEFLERİ (DISTRACTIONS) BUL VEYA OLUŞTUR
        GameObject distractionsRoot = GameObject.Find("Distractions_Root");
        if (distractionsRoot == null)
        {
            distractionsRoot = new GameObject("Distractions_Root");
            distractionsRoot.transform.position = averagePosition; // Root'u ortalamaya koyalım
            
            // Öğrencilerin ortalama pozisyonuna göre etraftaki hedefleri yerleştir
            Vector3[] distractionPositions = new Vector3[]
            {
                averagePosition + new Vector3(-8f, 2f, 5f),   // Sol Ön
                averagePosition + new Vector3(8f, 2f, 5f),    // Sağ Ön
                averagePosition + new Vector3(0f, 3f, -8f),   // Arka (Saat vs.)
                averagePosition + new Vector3(-6f, 0.5f, -2f),// Sol Arka
                averagePosition + new Vector3(6f, 0.5f, -2f)  // Sağ Arka
            };

            for (int i = 0; i < distractionPositions.Length; i++)
            {
                GameObject dist = new GameObject($"Distraction_Target_{i + 1}");
                dist.transform.parent = distractionsRoot.transform;
                dist.transform.position = distractionPositions[i];
            }
            Debug.Log("-> Distractions_Root ve 5 adet dikkat dağıtıcı hedef akıllı konumlara oluşturuldu.");
        }

        // Distraction hedeflerini bir diziye (array) topla
        Transform[] distractionTargets = new Transform[distractionsRoot.transform.childCount];
        for (int i = 0; i < distractionsRoot.transform.childCount; i++)
        {
            distractionTargets[i] = distractionsRoot.transform.GetChild(i);
        }

        // 5. SCRİPTLERİ BUL VE REFERANSLARI BAĞLA
        int modifiedCount = 0;
        foreach (GameObject student in allStudents)
        {
            // Sistemin asıl scripti olan ProceduralAudienceAnimator'ü buluyoruz
            ProceduralAudienceAnimator controller = student.GetComponent<ProceduralAudienceAnimator>();
            if (controller == null)
            {
                controller = student.AddComponent<ProceduralAudienceAnimator>();
            }

            // Distraction hedeflerini otomatik atıyoruz
            controller.distractionTargets = distractionTargets;

            // Yapılan değişikliğin Unity tarafından kaydedilmesi (Save edilmesi) için objeyi işaretliyoruz
            EditorUtility.SetDirty(controller);
            modifiedCount++;
        }

        // İşlem bittiğinde ekrana kesinlikle bir popup penceresi çıkar
        EditorUtility.DisplayDialog(
            "Kurulum Sonucu", 
            $"Kurulum Başarıyla Tamamlandı!\n\nBulunan Öğrenci: {allStudents.Count}\nEklenen Hedef: {distractionTargets.Length + 1} (1 Ana, {distractionTargets.Length} Yan)\nGüncellenen Öğrenci: {modifiedCount}", 
            "Tamam"
        );
        
        Debug.Log($"[ClassroomSetupManager] Kurulum Tamamlandı! Toplam {modifiedCount} öğrenci güncellendi.");
    }
}
