using System.IO;
using UnityEditor;
using UnityEngine;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Arma Resources/LetterVoices.asset con los audios de Audio/SFX/vocales (vocal-a.ogg...) y Audio/SFX/consonantes (letra-b.ogg...).
    /// Se puede ejecutar de nuevo cuando se agreguen o cambien audios. Los clips se importan en mono (son voces cortas).
    /// </summary>
    public static class LetterVoiceLibraryBuilder
    {
        private const string AssetPath = "Assets/Resources/LetterVoices.asset";
        private static readonly string[] Folders =
        {
            "Assets/_Project/Audio/SFX/vocales",
            "Assets/_Project/Audio/SFX/consonantes",
        };

        [MenuItem("Tools/Audio/Build Letter Voice Library")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Resources");
            var library = AssetDatabase.LoadAssetAtPath<LetterVoiceLibrary>(AssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<LetterVoiceLibrary>();
                AssetDatabase.CreateAsset(library, AssetPath);
            }
            library.Clear();

            int count = 0;
            foreach (string folder in Folders)
            {
                // Se recorre la carpeta del disco (no la búsqueda de Unity, que no ve los archivos recién agregados) y se importa cada audio
                if (!Directory.Exists(folder)) { Debug.LogWarning($"[Voces] No existe {folder}"); continue; }
                foreach (string diskPath in Directory.GetFiles(folder, "*.wav"))
                {
                    string path = diskPath.Replace('\\', '/');
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null) { Debug.LogWarning($"[Voces] Unity no pudo importar {path}"); continue; }
                    string file = Path.GetFileNameWithoutExtension(path);        // "vocal-a", "letra-ñ"
                    int dash = file.LastIndexOf('-');
                    if (dash < 0 || dash == file.Length - 1) { Debug.LogWarning($"[Voces] Nombre no reconocido: {file}"); continue; }
                    string letter = file.Substring(dash + 1);

                    // Voces cortas: mono y comprimidas (Vorbis) para que pesen poco en la compilación; se descomprimen al cargar
                    if (AssetImporter.GetAtPath(path) is AudioImporter importer)
                    {
                        var settings = importer.defaultSampleSettings;
                        if (!importer.forceToMono || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                        {
                            importer.forceToMono = true;
                            settings.compressionFormat = AudioCompressionFormat.Vorbis;
                            settings.quality = 0.6f;
                            settings.loadType = AudioClipLoadType.DecompressOnLoad;
                            importer.defaultSampleSettings = settings;
                            importer.SaveAndReimport();
                        }
                    }
                    library.Set(letter, AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                    count++;
                }
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            var letters = new System.Collections.Generic.List<string>();
            foreach (var entry in library.Entries) letters.Add(entry.letter);
            Debug.Log($"[Voces] LetterVoices.asset con {count} letras: {string.Join(", ", letters)}");
        }
    }
}
