using UnityEngine;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Al cargar el nivel, si el jugador vuelve de un portal lo coloca junto a él en lugar del inicio.
    /// </summary>
    public class PlayerSpawnRestorer : MonoBehaviour
    {
        private void Awake()
        {
            if (!PortalReturnState.HasPoint) return;

            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            transform.SetPositionAndRotation(PortalReturnState.Position, PortalReturnState.Rotation);

            if (controller != null) controller.enabled = true;
            PortalReturnState.Clear();
        }
    }
}
