using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Pantalla de actividades ("Tu ruta"): muestra el progreso de cada tarjeta, abre la actividad elegida y, al volver,
    /// guarda el progreso en el backend mostrando la pantalla de carga.
    /// </summary>
    public class ActivitySelectUI : MonoBehaviour
    {
        [SerializeField] private ActivityCardView[] cards;
        [SerializeField] private Button backButton;
        [SerializeField] private string backScene = "BaseScene";
        [SerializeField] private string numbersScene = "NumberTrace_2D";
        [SerializeField] private string lettersScene = "LetterTrace_2D";
        [SerializeField] private string thisScene = "ActivitySelect";

        private void Awake()
        {
            foreach (var card in cards)
            {
                var c = card;
                if (c.Button != null) c.Button.onClick.AddListener(() => Open(c.Kind));
            }
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        private void OnEnable()
        {
            TraceProgress.Changed += RefreshAnimated;
        }

        private void OnDisable()
        {
            TraceProgress.Changed -= RefreshAnimated;
        }

        private void Start()
        {
            RefreshAll(false);
            // Al abrir se pone al día con el servidor sin bloquear la pantalla: las barras suben cuando llegan los datos.
            StartCoroutine(ProgressSyncService.Fetch());
        }

        private void RefreshAnimated() => RefreshAll(true);

        private void RefreshAll(bool animate)
        {
            foreach (var card in cards) card.Refresh(animate);
        }

        private void Open(ActivityKind kind)
        {
            if (LoadingOverlay.IsBusy) return;

            string scene;
            switch (kind)
            {
                case ActivityKind.Numbers:
                    scene = numbersScene;
                    break;
                case ActivityKind.LowercaseVowels:
                    scene = lettersScene;
                    ActivityRoute.Case = TraceCase.Lowercase;
                    break;
                case ActivityKind.UppercaseVowels:
                    scene = lettersScene;
                    ActivityRoute.Case = TraceCase.Uppercase;
                    break;
                case ActivityKind.Consonants:
                    scene = lettersScene;
                    ActivityRoute.Case = TraceCase.Both;
                    ActivityRoute.StartOnConsonants = true;
                    break;
                default:
                    return;
            }
            // No se abre el reto directo: se vuelve al mundo y el personaje camina hasta el portal de esa actividad.
            ActivityRoute.PendingPortalScene = scene;
            LoadingOverlay.LoadScene(backScene, "Llevándote al portal...", showCard: true, beforeLoad: ProgressSyncService.Push(),
                minSeconds: 0.8f);
        }

        private void OnBack()
        {
            if (LoadingOverlay.IsBusy) return;
            // Se guarda el progreso en el backend antes de salir, con la pantalla de carga.
            LoadingOverlay.LoadScene(backScene, "Guardando tu progreso...", showCard: true, beforeLoad: ProgressSyncService.Push());
        }
    }
}
