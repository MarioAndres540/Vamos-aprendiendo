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

    const string TexDir = Dir + "/Textures";

    class MatDef
    {
        public string Name; public Color BaseLinear = Color.white; public float Smoothness = 0.1f;
        public Color EmissionLinear = Color.black; public float EmissionMult;
        public string BaseMap, EmissionMap, NormalMap; public float NormalScale = 1f; public Vector2 Tiling = Vector2.one;
        public string DetailAlbedo, DetailNormal; public float DetailNormalScale = 1f; public Vector2 DetailTiling = Vector2.one;
    }

    // Valores lineales y texturas tomados de los materiales de Blender
    static readonly MatDef[] Mats =
    {
        new MatDef { Name = "M_CapaTela", BaseMap = "Capa_BaseColor", EmissionMap = "Capa_Emision", NormalMap = "Capa_Normal",
                     NormalScale = 0.3f, Smoothness = 0.05f, EmissionLinear = Color.white, EmissionMult = 1.6f,
                     DetailAlbedo = "Lana_Detalle", DetailNormal = "Lana_Normal", DetailNormalScale = 0.9f, DetailTiling = new Vector2(9f, 3.5f) },
        new MatDef { Name = "M_CapuchaTela", BaseMap = "Lana_Oscura", NormalMap = "Lana_Normal", NormalScale = 0.6f,
                     Smoothness = 0.15f, Tiling = new Vector2(8, 8) },
        new MatDef { Name = "M_Bufanda", BaseMap = "Bufanda_BaseColor", EmissionMap = "Bufanda_Emision", NormalMap = "Bufanda_Normal",
                     NormalScale = 0.5f, Smoothness = 0.2f, EmissionLinear = Color.white, EmissionMult = 1.6f },
        new MatDef { Name = "M_BufandaCuello", BaseMap = "Verde_Tela", NormalMap = "Lana_Normal", NormalScale = 0.6f,
                     Smoothness = 0.2f, Tiling = new Vector2(10, 10), EmissionLinear = new Color(0.22f, 1f, 0.02f), EmissionMult = 0.45f },
        new MatDef { Name = "M_BlancoRostro", BaseLinear = new Color(0.80f, 0.76f, 0.67f), Smoothness = 0.58f,
                     EmissionLinear = new Color(0.80f, 0.76f, 0.67f), EmissionMult = 0.18f },
        new MatDef { Name = "M_OjosNegros", BaseLinear = new Color(0.003f, 0.003f, 0.003f), Smoothness = 0.75f },
        new MatDef { Name = "M_Pies", BaseLinear = new Color(0.02f, 0.02f, 0.025f), Smoothness = 0.4f },
        new MatDef { Name = "M_AzulRunas", BaseLinear = new Color(0.05f, 0.55f, 1f), Smoothness = 0.5f,
                     EmissionLinear = new Color(0.05f, 0.6f, 1f), EmissionMult = 2.2f },
        // materiales de versiones anteriores (por si algún FBX viejo los referencia)
        new MatDef { Name = "M_NegroAzabache", BaseLinear = new Color(0.018f, 0.018f, 0.02f), Smoothness = 0.1f },
        new MatDef { Name = "M_VerdeLuminoso", BaseLinear = new Color(0.12f, 0.55f, 0.02f), Smoothness = 0.4f,
                     EmissionLinear = new Color(0.22f, 1f, 0.02f), EmissionMult = 2f },
        new MatDef { Name = "M_EspiralBufanda", BaseLinear = new Color(0.35f, 0.9f, 0.08f), Smoothness = 0.4f,
                     EmissionLinear = new Color(0.42f, 1f, 0.08f), EmissionMult = 3f },
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

    static Texture2D Tex(string name, bool normal)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string p = $"{TexDir}/{name}.png";
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) { Debug.LogWarning("Falta textura " + p); return null; }
        bool dirty = false;
        if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
        if (!normal && !ti.sRGBTexture) { ti.sRGBTexture = true; dirty = true; }
        if (ti.maxTextureSize < 2048) { ti.maxTextureSize = 2048; dirty = true; }
        if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
        if (dirty) ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
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
            var baseTex = Tex(d.BaseMap, false);
            m.SetTexture("_BaseMap", baseTex);
            m.SetColor("_BaseColor", baseTex != null ? Color.white : d.BaseLinear.gamma);
            m.SetTextureScale("_BaseMap", d.Tiling);
            m.SetFloat("_Smoothness", d.Smoothness);
            m.SetFloat("_Metallic", 0f);

            var nrm = Tex(d.NormalMap, true);
            m.SetTexture("_BumpMap", nrm);
            m.SetFloat("_BumpScale", d.NormalScale);
            if (nrm != null) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");

            var detA = Tex(d.DetailAlbedo, false); var detN = Tex(d.DetailNormal, true);
            m.SetTexture("_DetailAlbedoMap", detA); m.SetTexture("_DetailNormalMap", detN);
            m.SetFloat("_DetailAlbedoMapScale", 1f); m.SetFloat("_DetailNormalMapScale", d.DetailNormalScale);
            m.SetTextureScale("_DetailAlbedoMap", d.DetailTiling);
            if (detA != null || detN != null) m.EnableKeyword("_DETAIL_MULX2"); else m.DisableKeyword("_DETAIL_MULX2");

            if (d.EmissionMult > 0f)
            {
                var emiTex = Tex(d.EmissionMap, false);
                m.SetTexture("_EmissionMap", emiTex);
                // color HDR: la conversión gamma->lineal de Unity reproduce ~EmissionMult en lineal
                var e = d.EmissionLinear.gamma * Mathf.Pow(d.EmissionMult, 1f / 2.2f);
                e.a = 1f;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", e);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.SetTexture("_EmissionMap", null);
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
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
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
        ConfigureJumpExit(sm, idle, walk, jump);

        a = sm.AddAnyStateTransition(party); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false;
        a.AddCondition(AnimatorConditionMode.If, 0, "Festejar");
        t = party.AddTransition(idle); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.15f;

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // El salto termina al tocar el suelo (parámetro Grounded) y sigue directo a Caminar si el jugador se mueve, o a Idle si está quieto.
    // El clip dura 1,63 s pero el salto en el aire ~0,8 s, así que se acelera para que el aterrizaje coincida con el suelo.
    static void ConfigureJumpExit(AnimatorStateMachine sm, AnimatorState idle, AnimatorState walk, AnimatorState jump)
    {
        foreach (var old in jump.transitions) jump.RemoveTransition(old);
        var toWalk = jump.AddTransition(walk); toWalk.hasExitTime = false; toWalk.duration = 0.12f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = jump.AddTransition(idle); toIdle.hasExitTime = false; toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        jump.speed = 1.8f;
    }

    [MenuItem("Tools/Encapuchado/3. Ajustar fin del salto (Animator)")]
    public static void FixJumpExit()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) { Debug.LogError("[Encapuchado] No existe " + ControllerPath); return; }
        bool has = false;
        foreach (var p in ctrl.parameters) if (p.name == "Grounded") has = true;
        if (!has) ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null, walk = null, jump = null;
        foreach (var cs in sm.states)
        {
            if (cs.state.name == "Idle") idle = cs.state;
            else if (cs.state.name == "Caminar") walk = cs.state;
            else if (cs.state.name == "Saltar") jump = cs.state;
        }
        if (idle == null || walk == null || jump == null) { Debug.LogError("[Encapuchado] Faltan estados Idle, Caminar o Saltar."); return; }
        ConfigureJumpExit(sm, idle, walk, jump);
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[Encapuchado] Saltar ahora termina al tocar el suelo y sigue a Caminar o Idle.");
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
