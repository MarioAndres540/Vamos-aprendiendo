using UnityEngine;
using UnityEngine.EventSystems;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Mantiene una pantalla diseñada para un tamaño fijo (1920x1080) completa en cualquier proporción: el contenido se
    /// escala de forma uniforme para caber en el Canvas y queda centrado, sin recortarse ni solaparse. En pantallas más
    /// anchas o más altas que 16:9 sobra espacio a los lados o arriba y abajo, que ocupa el fondo.
    /// El script va en un objeto que ocupa todo el Canvas; el contenido es su hijo "Content".
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class DesignFrame : UIBehaviour
    {
        [SerializeField] private Vector2 designSize = new Vector2(1920f, 1080f);
        [SerializeField] private RectTransform content;

        public void Configure(RectTransform contentRect, Vector2 size)
        {
            content = contentRect;
            designSize = size;
            Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        private void Refresh()
        {
            if (content == null) return;
            var self = (RectTransform)transform;
            Vector2 available = self.rect.size;
            if (available.x <= 0f || available.y <= 0f) return;

            float scale = Mathf.Min(available.x / designSize.x, available.y / designSize.y);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = designSize;
            content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
