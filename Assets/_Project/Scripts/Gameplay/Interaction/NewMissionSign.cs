using UnityEngine;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Signo "!" de misión nueva, junto al pulpo. Aparece al empezar (cuando todavía no se hizo ninguna ruta) y cada vez que
    /// se termina la ruta en la que se estaba y queda otra por empezar; se oculta mientras el jugador está en una ruta y cuando
    /// ya las completó todas. Rebota y brilla para llamar la atención; al tocarlo abre la pantalla de rutas, igual que el pulpo.
    /// </summary>
    public class NewMissionSign : MissionMarker
    {
        [Tooltip("El pulpo: tocar la '!' hace lo mismo que tocarlo (abrir la pantalla de rutas)")]
        [SerializeField] private TapToLoadScene octopus;

        protected override bool ShouldShow() => MissionRewards.HasNewMissionAvailable();

        protected override bool CanTapNow => octopus != null && !LoadingOverlay.IsBusy;

        protected override void OnTapped() => octopus.Open();
    }
}
