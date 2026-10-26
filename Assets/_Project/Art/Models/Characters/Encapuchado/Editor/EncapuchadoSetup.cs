// Configura el personaje Encapuchado exportado desde Blender:
// materiales URP con emisión HDR, clips de animación, Animator Controller, prefab y perfil de Bloom.
// Menú: Tools > Encapuchado
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class EncapuchadoSetup
{
    const string Dir = "Assets/_Project/Art/Models/Characters/Encapuchado";
    const string MatDir = Dir + "/Materials";
    const string MainFbx = Dir + "/Encapuchado.fbx";
    const string ControllerPath = Dir + "/Encapuchado.controller";
    const string PrefabPath = Dir + "/Encapuchado.prefab";
    const string BloomPath = Dir + "/Bloom_Encapuchado.asset";
    static readonly string[] Clips = { "Idle", "Caminar", "Saltar", "Festejar" };
    static readonly string[] Looping = { "Idle", "Caminar" };

    struct MatDef
    {
        public string Name; public Color BaseLinear; public float Smoothness; public Color EmissionLinear; public float EmissionMult;
        public MatDef(string n, Color b, float s, Color e, float m) { Name = n; BaseLinear = b; Smoothness = s; EmissionLinear = e; EmissionMult = m; }
    }

    // Valores lineales tomados de los materiales de Blender
    static readonly MatDef[] Mats =
    {
        new MatDef("M_NegroAzabache",  new Color(0.018f, 0.018f, 0.020f), 0.10f, Color.black, 0f),
        new MatDef("M_BlancoRostro",   new Color(0.850f, 0.830f, 0.800f), 0.55f, Color.black, 0f),
        new MatDef("M_OjosNegros",     new Color(0.003f, 0.003f, 0.003f), 0.75f, Color.black, 0f),
        new MatDef("M_Pies",           new Color(0.020f, 0.020f, 0.025f), 0.40f, Color.black, 0f),
        new MatDef("M_VerdeLuminoso",  new Color(0.120f, 0.550f, 0.020f), 0.40f, new Color(0.22f, 1.0f, 0.02f), 2.0f),
        new MatDef("M_EspiralBufanda", new Color(0.350f, 0.900f, 0.080f), 0.40f, new Color(0.42f, 1.0f, 0.08f), 3.0f),
        new MatDef("M_AzulRunas",      new Color(0.050f, 0.400f, 1.000f), 0.50f, new Color(0.05f, 0.5f, 1.0f), 4.0f),
    };

    [MenuItem("Tools/Encapuchado/1. Configurar personaje (materiales, clips, Animator, prefab)")]
    public static void Configure()
    {
        if (AssetImporter.GetAtPath(MainFbx) == null) { Debug.LogError("No se encontró " + MainFbx); return; }

        var mats = CreateMaterials();

        // Modelo base: sin animación, crea el Avatar genérico
        var main = (ModelImporter)AssetImporter.GetAtPath(MainFbx);
        ApplyCommon(main, mats);
        main.importAnimation = false;
        main.animationType = ModelImporterAnimationType.Generic;
        main.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        main.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(MainFbx).OfType<Avatar>().FirstOrDefault();

        // Clips: Encapuchado@<Clip>.fbx
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var name in Clips)
        {
            string path = $"{Dir}/Encapuchado@{name}.fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogWarning("Falta " + path); continue; }
            ApplyCommon(imp, mats);
            imp.importAnimation = true;
            imp.animationType = ModelImporterAnimationType.Generic;
            if (avatar != null) { imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; imp.sourceAvatar = avatar; }
            var defs = imp.defaultClipAnimations;
            if (defs == null || defs.Length == 0) { imp.SaveAndReimport(); defs = imp.defaultClipAnimations; }
            if (defs == null || defs.Length == 0) { Debug.LogWarning("Sin animación en " + path); continue; }
            var clip = defs[0];
            clip.name = name;
            clip.loopTime = Looping.Contains(name);
            clip.loopPose = false;
            imp.clipAnimations = new[] { clip };
            imp.SaveAndReimport();
            clips[name] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        var controller = CreateController(clips);

        // Prefab listo para usar
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MainFbx);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        var animator = inst.GetComponent<Animator>();
        if (animator == null) animator = inst.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath);
        Object.DestroyImmediate(inst);

        CreateBloomProfile();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"Encapuchado configurado: {mats.Count} materiales, {clips.Count} clips, Animator y prefab en {Dir}");
    }

    [MenuItem("Tools/Encapuchado/2. Agregar Bloom a la escena abierta")]
    public static void AddBloomToScene()
    {
        var profile = CreateBloomProfile();
        AssetDatabase.SaveAssets();
        const string goName = "Global Volume (Bloom Encapuchado)";
        var go = GameObject.Find(goName);
        if (go == null) { go = new GameObject(goName); Undo.RegisterCreatedObjectUndo(go, "Agregar Bloom"); }
        var vol = go.GetComponent<Volume>();
        if (vol == null) vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeObject = go;
        Debug.Log("Bloom agregado a la escena y post-proceso activado en las cámaras.");
    }

    static void ApplyCommon(ModelImporter imp, Dictionary<string, Material> mats)
    {
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.bakeAxisConversion = true;
        imp.importCameras = false;
        imp.importLights = false;
        imp.importBlendShapes = false;
        imp.importNormals = ModelImporterNormals.Import;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        foreach (var kv in mats)
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
    }

    static Dictionary<string, Material> CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(Dir, "Materials");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var result = new Dictionary<string, Material>();
        foreach (var d in Mats)
        {
            string p = $"{MatDir}/{d.Name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, p); }
            m.shader = shader;
            m.SetColor("_BaseColor", d.BaseLinear.gamma);
            m.SetFloat("_Smoothness", d.Smoothness);
            m.SetFloat("_Metallic", 0f);
            if (d.EmissionMult > 0f)
            {
                // color HDR: la conversión gamma->lineal de Unity reproduce ~EmissionMult en lineal
                var e = d.EmissionLinear.gamma * Mathf.Pow(d.EmissionMult, 1f / 2.2f);
                e.a = 1f;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", e);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(m);
            result[d.Name] = m;
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    static AnimatorController CreateController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Festejar", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;

        AnimatorState Add(string n, Vector3 pos)
        {
            var s = sm.AddState(n, pos);
            if (clips.TryGetValue(n, out var c)) s.motion = c;
            return s;
        }
        var idle = Add("Idle", new Vector3(300, 0));
        var walk = Add("Caminar", new Vector3(560, 0));
        var jump = Add("Saltar", new Vector3(300, 140));
        var party = Add("Festejar", new Vector3(560, 140));
        sm.defaultState = idle;

        var t = idle.AddTransition(walk); t.hasExitTime = false; t.duration = 0.15f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t = walk.AddTransition(idle); t.hasExitTime = false; t.duration = 0.15f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var a = sm.AddAnyStateTransition(jump); a.hasExitTime = false; a.duration = 0.05f; a.canTransitionToSelf = false;
        a.AddCondition(AnimatorConditionMode.If, 0, "Saltar");
        t = jump.AddTransition(idle); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f;

        a = sm.AddAnyStateTransition(party); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false;
        a.AddCondition(AnimatorConditionMode.If, 0, "Festejar");
        t = party.AddTransition(idle); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.15f;

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static VolumeProfile CreateBloomProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BloomPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, BloomPath);
        }
        if (!profile.TryGet<Bloom>(out var bloom))
        {
            bloom = profile.Add<Bloom>(true);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.65f);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);
        return profile;
    }
}
