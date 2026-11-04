using UnityEngine;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>Al tocar este objeto (por ejemplo, el pulpo) se cambia de escena con pantalla de carga mientras se baja el progreso.</summary>
    public class TapToLoadScene : TapInteractable
    {
        [SerializeField] private string sceneName = "ActivitySelect";
        [Tooltip("Texto de la transición")]
        [SerializeField] private string message = "Cargando tu progreso...";

        protected override bool CanReceiveTap => !LoadingOverlay.IsBusy;

        protected override void OnTapped() => Open();

        /// <summary>Hace lo mismo que tocar el objeto (también lo usa el signo "!" de misión nueva).</summary>
        public void Open()
        {
            // Se recuerda dónde estaba el personaje: al volver de "Tu ruta" aparece en este mismo punto, no en el inicio.
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) PortalReturnState.Save(player.transform.position, player.transform.rotation);

            // Con tarjeta de carga: mientras se descarga el progreso del backend el niño ve qué está pasando, igual que al salir.
            LoadingOverlay.LoadScene(sceneName, message, showCard: true, beforeLoad: ProgressSyncService.Fetch(),
                minSeconds: 1.0f, workTip: "Buscando lo que ya aprendiste...");
        }
    }
}
