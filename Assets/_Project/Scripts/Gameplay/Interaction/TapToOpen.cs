using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>Abre (y vuelve a cerrar) un <see cref="HingeOpener"/> al tocarlo o hacerle clic.</summary>
    public class TapToOpen : TapInteractable
    {
        [SerializeField] private HingeOpener opener;
        [Tooltip("Si es verdadero, un segundo toque lo vuelve a cerrar")]
        [SerializeField] private bool allowClose = true;

        protected override bool CanReceiveTap => opener != null && (!opener.IsOpen || allowClose);

        protected override void OnTapped() => opener.Toggle();
    }
}
