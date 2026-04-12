using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MocapMaskSetup
{
    [MenuItem("Tools/1-Tıkla Animasyonları Maskele & Katman Temizle")]
    public static void SetupMocapMaskLayer()
    {
        string controllerPath = "Assets/AudienceAnimator.controller";
        string maskPath = "Assets/UpperBodyMask.mask";
        
        Dictionary<string, string> animations = new Dictionary<string, string>()
        {
            { "Yawn", "Assets/Remy@Yawn.fbx" },
            { "Distracted", "Assets/Remy@Look Around.fbx" },
            { "Attentive", "Assets/Remy@Head Nod Yes.fbx" },
            { "Writing", "Assets/Ch07_nonPBR@Writing.fbx" },
            { "Texting", "Assets/Ch07_nonPBR@Texting.fbx" },
            { "Tablet", "Assets/Ch07_nonPBR@Standing Using Touchscreen Tablet.fbx" }
        };

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) { Debug.LogError("Controller bulunamadı!"); return; }

        // --- 0. OTO-RIG & OTO-LOOP (Humanoid ve Döngü Yapma) ---
        foreach (var entry in animations)
        {
            ModelImporter importer = AssetImporter.GetAtPath(entry.Value) as ModelImporter;
            if (importer != null)
            {
                bool changed = false;

                // Humanoid yap
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    changed = true;
                }

                // Döngü yap (Loop Time)
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (var clip in clips)
                    {
                        if (!clip.loopTime) { clip.loopTime = true; changed = true; }
                    }
                    importer.clipAnimations = clips; 
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    Debug.Log($"[MocapMaskSetup] {entry.Key} dosyası HUMAN ve LOOP olarak güncellendi.");
                }
            }
        }

        // 1. BASE LAYER TEMİZLİĞİ
        CleanBaseLayerTransitions(controller, animations.Keys);

        // 2. MASK HAZIRLIĞI
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null) mask = CreateUpperBodyMask(maskPath);

        // 3. LAYER BUL VEYA OLUŞTUR
        int layerIndex = -1;
        for (int i = 0; i < controller.layers.Length; i++)
            if (controller.layers[i].name == "UpperBodyMocap") layerIndex = i;

        if (layerIndex == -1)
        {
            controller.AddLayer("UpperBodyMocap");
            layerIndex = controller.layers.Length - 1;
        }

        // Katman ayarlarını tazele
        var layers = controller.layers;
        layers[layerIndex].avatarMask = mask;
        layers[layerIndex].defaultWeight = 1f;
        layers[layerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[layerIndex].stateMachine;

        // --- TAM TEMİZLİK ---
        // Mevcut tüm stateleri ve anyState transitionları sil
        var states = sm.states;
        foreach (var s in states) sm.RemoveState(s.state);
        var anyTrans = sm.anyStateTransitions;
        foreach (var t in anyTrans) sm.RemoveAnyStateTransition(t);

        // 4. PARAMETRELERİ EKLE
        foreach (var pName in animations.Keys)
        {
            bool exists = false;
            foreach (var p in controller.parameters) if (p.name == pName) exists = true;
            if (!exists) controller.AddParameter(pName, AnimatorControllerParameterType.Trigger);
        }

        // 5. STATES VE TRANSITIONS YENİDEN KUR
        AnimatorState emptyState = sm.AddState("Empty", new Vector3(300, 0, 0));
        sm.defaultState = emptyState;

        int yPos = 50;
        foreach (var entry in animations)
        {
            AnimatorState state = sm.AddState(entry.Key, new Vector3(300, yPos, 0));
            yPos += 50;
            state.motion = LoadFirstClipFromFBX(entry.Value);

            // Any State -> Animasyon
            var t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, entry.Key);
            t.duration = 0.25f;
            t.canTransitionToSelf = false;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[MocapMaskSetup] Animator Controller '%100 TEMİZLENDİ' ve Mocap sistemi yeniden kuruldu.");
    }

    private static void CleanBaseLayerTransitions(AnimatorController controller, IEnumerable<string> badTriggers)
    {
        var sm = controller.layers[0].stateMachine;
        var transitions = sm.anyStateTransitions;
        var newList = new List<AnimatorStateTransition>();
        foreach (var t in transitions)
        {
            bool isBad = false;
            foreach (var c in t.conditions) if (badTriggers.Contains(c.parameter)) isBad = true;
            if (!isBad) newList.Add(t);
        }
        sm.anyStateTransitions = newList.ToArray();
    }

    private static AvatarMask CreateUpperBodyMask(string path)
    {
        AvatarMask mask = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
        AssetDatabase.CreateAsset(mask, path);
        return mask;
    }

    private static AnimationClip LoadFirstClipFromFBX(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
            if (asset is AnimationClip && !asset.name.StartsWith("__preview__")) return asset as AnimationClip;
        return null;
    }
}
