#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Música ambiente de BaseScene con "25 Rpg Game Tracks": día = Ambient 9, 2 y 1; noche = Night Ambient 5, 3 y 2.
    /// Las pistas se importan en streaming y comprimidas (Vorbis) para no cargar ~150 MB de WAV en memoria en tablets.
    /// </summary>
    public static class AmbientMusicSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string DIR = "Assets/_Project/Audio/SFX/25 Rpg Game Tracks/";
        private static readonly string[] Day = { "Ambient 9.wav", "Ambient 2.wav", "Ambient 1.wav" };
        private static readonly string[] Night = { "Night Ambient 5 (Loop).wav", "Night Ambient 3 (Loop).wav", "Night Ambient 2 (Loop).wav" };

        [MenuItem("Tools/Scenes/Setup Ambient Music (BaseScene)", false, 38)]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            var dayClips = Import(Day);
            var nightClips = Import(Night);

            var existing = Object.FindFirstObjectByType<AmbientMusicPlayer>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("AmbientMusic");
            Undo.RegisterCreatedObjectUndo(go, "Ambient Music");
            var systems = GameObject.Find("--- [CAMERAS & SYSTEMS] ---");
            if (systems != null) go.transform.SetParent(systems.transform, false);

            var player = go.AddComponent<AmbientMusicPlayer>();
            var so = new SerializedObject(player);
            Fill(so.FindProperty("dayTracks"), dayClips);
            Fill(so.FindProperty("nightTracks"), nightClips);
            so.ApplyModifiedProperties();

            // La noche del ciclo debe estar marcada para que la música cambie de lista.
            var sky = Object.FindFirstObjectByType<SkyboxAnimator>();
            if (sky != null)
            {
                var sso = new SerializedObject(sky);
                var phases = sso.FindProperty("phases");
                for (int i = 0; i < phases.arraySize; i++)
                {
                    var p = phases.GetArrayElementAtIndex(i);
                    p.FindPropertyRelative("isNight").boolValue = p.FindPropertyRelative("name").stringValue == "Noche";
                }
                sso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AmbientMusic] Día: Ambient 9 > 2 > 1. Noche: Night Ambient 5 > 3 > 2.");
        }

        private static AudioClip[] Import(string[] files)
        {
            var clips = new AudioClip[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string path = DIR + files[i];
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp != null)
                {
                    var s = imp.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.Streaming;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.5f;
                    s.preloadAudioData = false;
                    imp.defaultSampleSettings = s;
                    imp.forceToMono = false;
                    imp.SaveAndReimport();
                }
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null) Debug.LogError("[AmbientMusic] No se encontró " + path);
            }
            return clips;
        }

        private static void Fill(SerializedProperty array, AudioClip[] clips)
        {
            array.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
    }
}
#endif
