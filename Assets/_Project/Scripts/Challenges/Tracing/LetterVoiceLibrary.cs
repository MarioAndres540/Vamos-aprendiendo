using System;
using System.Collections.Generic;
using UnityEngine;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Audios de cada letra (vocales y consonantes) para el reto de trazo. La misma voz sirve para la mayúscula y la minúscula.
    /// Se genera con Tools/Audio/Build Letter Voice Library a partir de Audio/SFX/vocales y Audio/SFX/consonantes y vive en
    /// Resources para que el juego la encuentre; los clips quedan en sus carpetas.
    /// </summary>
    [CreateAssetMenu(menuName = "Vamos Aprendiendo/Letter Voice Library", fileName = "LetterVoices")]
    public class LetterVoiceLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string letter;   // en minúscula: "a", "ñ"
            public AudioClip clip;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, AudioClip> _byLetter;

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Agrega o reemplaza la voz de una letra (lo usa el constructor del editor).</summary>
        public void Set(string letter, AudioClip clip)
        {
            letter = letter.ToLowerInvariant();
            var existing = entries.Find(e => e.letter == letter);
            if (existing != null) existing.clip = clip;
            else entries.Add(new Entry { letter = letter, clip = clip });
            _byLetter = null;
        }

        public void Clear()
        {
            entries.Clear();
            _byLetter = null;
        }

        /// <summary>Voz de un ejercicio por su id de glifo ("lo_a", "up_A", "up_Ñ"); null si no hay audio (por ejemplo los números).</summary>
        public AudioClip GetClip(string glyphId)
        {
            if (string.IsNullOrEmpty(glyphId)) return null;
            int underscore = glyphId.IndexOf('_');
            if (underscore < 0 || glyphId.StartsWith("d_", StringComparison.Ordinal)) return null;

            if (_byLetter == null)
            {
                _byLetter = new Dictionary<string, AudioClip>();
                foreach (var e in entries)
                    if (e != null && e.clip != null && !string.IsNullOrEmpty(e.letter)) _byLetter[e.letter.ToLowerInvariant()] = e.clip;
            }
            _byLetter.TryGetValue(glyphId.Substring(underscore + 1).ToLowerInvariant(), out var clip);
            return clip;
        }
    }
}
