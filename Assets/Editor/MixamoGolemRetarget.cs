using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Aplica a los dos golems de BaseScene las animaciones de Mixamo (Art/Animations: Mutant Idle, Orc Idle, Clapping y
    /// Thoughtful Head Shake). Son FBX sin malla con el esqueleto de Mixamo:
    /// · Giant01 - Golem es humanoide: los FBX se importan como Humanoid y sus clips se usan tal cual (retarget de Mecanim).
    /// · Crowned Stone Golem tiene un esqueleto propio de 14 huesos y no puede ser humanoide: sus clips se "hornean" copiando,
    ///   cuadro a cuadro, el giro de cada hueso del esqueleto de Mixamo sobre el hueso equivalente del golem.
    /// Cada golem recibe un controlador con Idle (por defecto) y los triggers Festejar (Clapping) y Pensar (Thoughtful Head Shake).
    /// </summary>
    public static class MixamoGolemRetarget
    {
        private const string AnimDir = "Assets/_Project/Art/Animations/";
        private const string OutDir = "Assets/_Project/Art/Animations/Golems/";
        private const string CrownedFbx = "Assets/_Project/Art/Models/Environment/Crowned Stone Golem.fbx";
        private const string BaseScene = "Assets/_Project/Scenes/BaseScene.unity";
        private const float Fps = 30f;

        // (hueso de Mixamo, ruta del hueso del golem coronado). Cada hueso va después de su padre.
        private static readonly (string mixamo, string golem)[] BoneMap =
        {
            ("mixamorig:Hips", "Golem_Rig/Root/Hips"),
            ("mixamorig:LeftUpLeg", "Golem_Rig/Root/Hips/Leg.L"),
            ("mixamorig:LeftFoot", "Golem_Rig/Root/Hips/Leg.L/Foot.L"),
            ("mixamorig:RightUpLeg", "Golem_Rig/Root/Hips/Leg.R"),
            ("mixamorig:RightFoot", "Golem_Rig/Root/Hips/Leg.R/Foot.R"),
            ("mixamorig:Spine2", "Golem_Rig/Root/Hips/Spine"),
            ("mixamorig:Head", "Golem_Rig/Root/Hips/Spine/Head"),
            ("mixamorig:LeftArm", "Golem_Rig/Root/Hips/Spine/UpperArm.L"),
            ("mixamorig:LeftForeArm", "Golem_Rig/Root/Hips/Spine/UpperArm.L/Forearm.L"),
            ("mixamorig:LeftHand", "Golem_Rig/Root/Hips/Spine/UpperArm.L/Forearm.L/Hand.L"),
            ("mixamorig:RightArm", "Golem_Rig/Root/Hips/Spine/UpperArm.R"),
            ("mixamorig:RightForeArm", "Golem_Rig/Root/Hips/Spine/UpperArm.R/Forearm.R"),
            ("mixamorig:RightHand", "Golem_Rig/Root/Hips/Spine/UpperArm.R/Forearm.R/Hand.R"),
        };

        [MenuItem("Tools/Animation/Apply Mixamo Animations to Golems")]
        public static void Apply()
        {
            Directory.CreateDirectory(OutDir);

            // 1) Los FBX de Mixamo se importan como humanoides, en bucle los idles y sin que el personaje se desplace
            ConfigureImport("Mutant Idle", loop: true);
            ConfigureImport("Orc Idle", loop: true);
            ConfigureImport("Clapping", loop: false);
            ConfigureImport("Thoughtful Head Shake", loop: false);

            // 2) Giant01 (humanoide): los clips de Mixamo directamente
            var giant = MakeController("Giant01", ClipOf("Mutant Idle"), ClipOf("Clapping"), ClipOf("Thoughtful Head Shake"));

            // 3) Crowned Stone Golem (genérico): clips horneados sobre sus 14 huesos
            var crownedIdle = Bake("Orc Idle", "CrownedGolem_Idle", loop: true);
            var crownedParty = Bake("Clapping", "CrownedGolem_Festejar", loop: false);
            var crownedThink = Bake("Thoughtful Head Shake", "CrownedGolem_Pensar", loop: false);
            AssetDatabase.SaveAssets();
            var crowned = MakeController("CrownedGolem", crownedIdle, crownedParty, crownedThink);

            var scene = EditorSceneManager.OpenScene(BaseScene, OpenSceneMode.Single);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Crowned Stone Golem") Assign(t.gameObject, crowned);
                else if (t.name == "Giant01 - Golem") Assign(t.gameObject, giant);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Mixamo] Animaciones de Mixamo aplicadas a los dos golems en BaseScene.");
        }

        // ---------- importación ----------

        private static void ConfigureImport(string name, bool loop)
        {
            string path = AnimDir + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogError($"[Mixamo] Falta {path}"); return; }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = name;
                c.loopTime = loop;
                c.loopPose = loop;
                // Se quedan en su sitio: lo que Mixamo mueve de la cadera se hornea en la pose
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip ClipOf(string fbxName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AnimDir + fbxName + ".fbx"))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview")) return clip;
            Debug.LogError($"[Mixamo] {fbxName}.fbx no trae clips");
            return null;
        }

        // ---------- horneado sobre el golem coronado ----------

        private static AnimationClip Bake(string mixamoFbx, string outName, bool loop)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(AnimDir + mixamoFbx + ".fbx");
            var clip = ClipOf(mixamoFbx);
            var mixamo = (GameObject)Object.Instantiate(source, new Vector3(0f, 7000f, 0f), Quaternion.identity);
            var golem = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CrownedFbx), new Vector3(0f, 7100f, 0f), Quaternion.identity);
            try
            {
                // El clip ya es humanoide (músculos, sin curvas de huesos): para muestrearlo hace falta un Animator con el avatar del FBX
                var animator = mixamo.GetComponent<Animator>();
                if (animator == null) animator = mixamo.AddComponent<Animator>();
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AnimDir + mixamoFbx + ".fbx"))
                    if (asset is Avatar avatar) animator.avatar = avatar;
                animator.runtimeAnimatorController = null;
                animator.Rebind();

                // El FBX de Mixamo trae el esqueleto en la pose del primer cuadro, no en T. El "reposo" de referencia es la pose T
                // del avatar humanoide (todos los músculos en 0), que es la misma pose en la que viene el golem.
                var handler = new HumanPoseHandler(animator.avatar, mixamo.transform);
                var tPose = new HumanPose();
                handler.GetHumanPose(ref tPose);
                for (int i = 0; i < tPose.muscles.Length; i++) tPose.muscles[i] = 0f;
                handler.SetHumanPose(ref tPose);

                // Si el lado izquierdo del golem queda del lado contrario al de Mixamo, se cruzan izquierda y derecha
                var leftMixamo = FindDeep(mixamo.transform, "mixamorig:LeftArm");
                var leftGolem = golem.transform.Find("Golem_Rig/Root/Hips/Spine/UpperArm.L");
                bool crossed = leftMixamo != null && leftGolem != null &&
                               Mathf.Sign(leftMixamo.position.x - mixamo.transform.position.x) != Mathf.Sign(leftGolem.position.x - golem.transform.position.x);
                if (crossed) Debug.Log("[Mixamo] El lado izquierdo del golem es el derecho de Mixamo: se cruzan los lados.");

                var pairs = new List<(Transform m, Transform g, string path)>();
                foreach (var (mixamoName, golemPath) in BoneMap)
                {
                    string boneName = !crossed ? mixamoName
                        : mixamoName.Contains("Left") ? mixamoName.Replace("Left", "Right")
                        : mixamoName.Contains("Right") ? mixamoName.Replace("Right", "Left") : mixamoName;
                    var m = FindDeep(mixamo.transform, boneName);
                    var g = golem.transform.Find(golemPath);
                    if (m == null || g == null) { Debug.LogError($"[Mixamo] Falta el hueso {boneName} o {golemPath}"); return null; }
                    pairs.Add((m, g, golemPath));
                }

                var mixamoRest = new Quaternion[pairs.Count];
                var golemRest = new Quaternion[pairs.Count];
                for (int i = 0; i < pairs.Count; i++)
                {
                    mixamoRest[i] = pairs[i].m.rotation;
                    golemRest[i] = pairs[i].g.rotation;
                }
                // Para la cadera, la referencia es el primer cuadro de la animación (así no se hunde ni se eleva respecto a su reposo)
                clip.SampleAnimation(mixamo, 0f);
                Vector3 mixamoHipsRest = pairs[0].m.position - mixamo.transform.position;
                Vector3 golemHipsRest = pairs[0].g.position - golem.transform.position;
                float scale = golemHipsRest.y / Mathf.Max(0.01f, mixamoHipsRest.y);
                Vector3 golemHipsLocalRest = pairs[0].g.localPosition;

                int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * Fps) + 1);
                var times = new float[frames];
                var rotations = new Quaternion[pairs.Count][];
                for (int i = 0; i < pairs.Count; i++) rotations[i] = new Quaternion[frames];
                var hipsLocal = new Vector3[frames];

                for (int f = 0; f < frames; f++)
                {
                    float time = Mathf.Min(clip.length, f / Fps);
                    times[f] = time;
                    clip.SampleAnimation(mixamo, time);

                    for (int i = 0; i < pairs.Count; i++)
                    {
                        // Giro del hueso respecto a su reposo, medido en el mundo, aplicado al reposo del hueso del golem
                        Quaternion delta = pairs[i].m.rotation * Quaternion.Inverse(mixamoRest[i]);
                        // El golem es un cuerpo redondo y corto: el torso y las piernas copian solo una parte del giro (los idles de
                        // Mixamo van muy encorvados) y los brazos y la cabeza lo copian entero
                        delta = Quaternion.Slerp(Quaternion.identity, delta, Influence(pairs[i].g.name));
                        pairs[i].g.rotation = delta * golemRest[i];
                    }
                    Vector3 move = (pairs[0].m.position - mixamo.transform.position) - mixamoHipsRest;
                    pairs[0].g.position = golem.transform.position + golemHipsRest + move * scale;

                    for (int i = 0; i < pairs.Count; i++) rotations[i][f] = pairs[i].g.localRotation;
                    hipsLocal[f] = pairs[0].g.localPosition;
                }

                string path = $"{OutDir}{outName}.anim";
                AssetDatabase.DeleteAsset(path);
                var result = new AnimationClip { frameRate = Fps, name = outName };
                AssetDatabase.CreateAsset(result, path);

                for (int i = 0; i < pairs.Count; i++)
                {
                    string bone = pairs[i].path;
                    var x = new float[frames]; var y = new float[frames]; var z = new float[frames]; var w = new float[frames];
                    Quaternion previous = rotations[i][0];
                    for (int f = 0; f < frames; f++)
                    {
                        Quaternion q = rotations[i][f];
                        if (Quaternion.Dot(previous, q) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                        previous = q;
                        x[f] = q.x; y[f] = q.y; z[f] = q.z; w[f] = q.w;
                    }
                    result.SetCurve(bone, typeof(Transform), "localRotation.x", Curve(times, x));
                    result.SetCurve(bone, typeof(Transform), "localRotation.y", Curve(times, y));
                    result.SetCurve(bone, typeof(Transform), "localRotation.z", Curve(times, z));
                    result.SetCurve(bone, typeof(Transform), "localRotation.w", Curve(times, w));
                }
                var hx = new float[frames]; var hy = new float[frames]; var hz = new float[frames];
                for (int f = 0; f < frames; f++) { hx[f] = hipsLocal[f].x; hy[f] = hipsLocal[f].y; hz[f] = hipsLocal[f].z; }
                string hips = pairs[0].path;
                result.SetCurve(hips, typeof(Transform), "localPosition.x", Curve(times, hx));
                result.SetCurve(hips, typeof(Transform), "localPosition.y", Curve(times, hy));
                result.SetCurve(hips, typeof(Transform), "localPosition.z", Curve(times, hz));

                var settings = AnimationUtility.GetAnimationClipSettings(result);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(result, settings);
                result.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                EditorUtility.SetDirty(result);
                AssetDatabase.SaveAssetIfDirty(result);   // se guarda ya: el siguiente clip reimporta cosas y no debe perder este
                return result;
            }
            finally
            {
                Object.DestroyImmediate(mixamo);
                Object.DestroyImmediate(golem);
            }
        }

        // Cuánto del giro de Mixamo copia cada hueso del golem (0 = nada, 1 = todo)
        private static float Influence(string golemBone)
        {
            switch (golemBone)
            {
                case "Hips": return 0.3f;
                case "Spine": return 0.3f;
                case "Leg.L": case "Leg.R": case "Foot.L": case "Foot.R": return 0.25f;
                case "Head": return 0.8f;
                default: return 1f;   // brazos, antebrazos y manos
            }
        }

        private static AnimationCurve Curve(float[] times, float[] values)
        {
            var curve = new AnimationCurve();
            for (int i = 0; i < times.Length; i++) curve.AddKey(new Keyframe(times[i], values[i]));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ---------- controlador y escena ----------

        private static AnimatorController MakeController(string name, AnimationClip idle, AnimationClip celebrate, AnimationClip think)
        {
            string path = $"{OutDir}{name}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Festejar", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Pensar", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;

            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;

            AddOneShot(sm, idleState, "Festejar", celebrate);
            AddOneShot(sm, idleState, "Pensar", think);
            return controller;
        }

        // Estado que se dispara con un trigger desde cualquier estado y vuelve solo al Idle
        private static void AddOneShot(AnimatorStateMachine sm, AnimatorState idle, string trigger, AnimationClip clip)
        {
            var state = sm.AddState(trigger);
            state.motion = clip;
            var enter = sm.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.25f;
            enter.canTransitionToSelf = false;
            var back = state.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0.3f;
        }

        private static void Assign(GameObject go, RuntimeAnimatorController controller)
        {
            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
        }
    }
}
