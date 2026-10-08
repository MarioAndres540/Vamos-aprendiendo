using UnityEngine;
using UnityEngine.EventSystems;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>Zona que recibe los toques del niño y los reenvía al reto de trazado.</summary>
    public class TraceInputSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private TraceChallenge challenge;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (challenge != null) challenge.OnSurfaceDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (challenge != null) challenge.OnSurfaceDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // La validación es manual (botón Comprobar): una forma puede tener varios trazos.
        }
    }
}
