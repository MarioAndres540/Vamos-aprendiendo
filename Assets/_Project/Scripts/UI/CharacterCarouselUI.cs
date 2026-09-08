using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    [System.Serializable]
    public struct CharacterPair
    {
        public string categoryName;
        public Sprite leftSprite;
        public Sprite rightSprite;
    }

    public class CharacterCarouselUI : MonoBehaviour
    {
        [Header("Character Slots")]
        [SerializeField] private Image leftPrimaryImage;
        [SerializeField] private Image leftSecondaryImage;
        [SerializeField] private CanvasGroup leftPrimaryCanvasGroup;
        [SerializeField] private CanvasGroup leftSecondaryCanvasGroup;
        [SerializeField] private RectTransform leftContainer;

        [SerializeField] private Image rightPrimaryImage;
        [SerializeField] private Image rightSecondaryImage;
        [SerializeField] private CanvasGroup rightPrimaryCanvasGroup;
        [SerializeField] private CanvasGroup rightSecondaryCanvasGroup;
        [SerializeField] private RectTransform rightContainer;

        [Header("Configuration")]
        [SerializeField] private float displayDuration = 4.0f;
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private bool enableIdleBreathing = true;
        [SerializeField] private float breathingSpeed = 1.5f;
        [SerializeField] private float breathingAmplitude = 8f;

        [Header("Pairs Data (Niños -> Adultos -> Adultos Mayores)")]
        [SerializeField] private List<CharacterPair> pairs = new List<CharacterPair>();

        private int _currentIndex = 0;
        private Coroutine _carouselCoroutine;
        private Vector2 _leftOriginalPos;
        private Vector2 _rightOriginalPos;
        private float _breathingTimer;

        public void SetPairs(List<CharacterPair> newPairs)
        {
            pairs = newPairs;
            InitializeCarousel();
        }

        private void Awake()
        {
            if (leftContainer != null) _leftOriginalPos = leftContainer.anchoredPosition;
            if (rightContainer != null) _rightOriginalPos = rightContainer.anchoredPosition;
        }

        private void Start()
        {
            InitializeCarousel();
        }

        public void InitializeCarousel()
        {
            if (pairs == null || pairs.Count == 0) return;

            _currentIndex = 0;
            ApplyInitialSprites();

            if (_carouselCoroutine != null)
            {
                StopCoroutine(_carouselCoroutine);
            }
            _carouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void ApplyInitialSprites()
        {
            var firstPair = pairs[0];

            if (leftPrimaryImage != null)
            {
                leftPrimaryImage.sprite = firstPair.leftSprite;
                leftPrimaryImage.enabled = firstPair.leftSprite != null;
            }
            if (leftPrimaryCanvasGroup != null) leftPrimaryCanvasGroup.alpha = 1f;
            if (leftSecondaryCanvasGroup != null) leftSecondaryCanvasGroup.alpha = 0f;

            if (rightPrimaryImage != null)
            {
                rightPrimaryImage.sprite = firstPair.rightSprite;
                rightPrimaryImage.enabled = firstPair.rightSprite != null;
            }
            if (rightPrimaryCanvasGroup != null) rightPrimaryCanvasGroup.alpha = 1f;
            if (rightSecondaryCanvasGroup != null) rightSecondaryCanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!enableIdleBreathing) return;

            _breathingTimer += Time.deltaTime * breathingSpeed;
            float offset = Mathf.Sin(_breathingTimer) * breathingAmplitude;

            if (leftContainer != null)
            {
                leftContainer.anchoredPosition = _leftOriginalPos + new Vector2(0, offset);
            }
            if (rightContainer != null)
            {
                rightContainer.anchoredPosition = _rightOriginalPos + new Vector2(0, Mathf.Cos(_breathingTimer) * breathingAmplitude * 0.85f);
            }
        }

        private IEnumerator CarouselRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(displayDuration);

                if (pairs.Count <= 1) continue;

                int nextIndex = (_currentIndex + 1) % pairs.Count;
                yield return StartCoroutine(TransitionToPair(nextIndex));
                _currentIndex = nextIndex;
            }
        }

        private IEnumerator TransitionToPair(int targetIndex)
        {
            var targetPair = pairs[targetIndex];

            // Setup secondary images with incoming sprites
            if (leftSecondaryImage != null)
            {
                leftSecondaryImage.sprite = targetPair.leftSprite;
                leftSecondaryImage.enabled = targetPair.leftSprite != null;
            }
            if (rightSecondaryImage != null)
            {
                rightSecondaryImage.sprite = targetPair.rightSprite;
                rightSecondaryImage.enabled = targetPair.rightSprite != null;
            }

            float elapsed = 0f;
            Vector3 leftStartScale = Vector3.one * 0.94f;
            Vector3 leftEndScale = Vector3.one;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                // Fade Primary out, Secondary in
                if (leftPrimaryCanvasGroup != null) leftPrimaryCanvasGroup.alpha = 1f - t;
                if (leftSecondaryCanvasGroup != null) leftSecondaryCanvasGroup.alpha = t;

                if (rightPrimaryCanvasGroup != null) rightPrimaryCanvasGroup.alpha = 1f - t;
                if (rightSecondaryCanvasGroup != null) rightSecondaryCanvasGroup.alpha = t;

                // Subtle scale / slide interpolation for premium fluid feel
                if (leftSecondaryImage != null)
                    leftSecondaryImage.transform.localScale = Vector3.Lerp(leftStartScale, leftEndScale, t);
                if (rightSecondaryImage != null)
                    rightSecondaryImage.transform.localScale = Vector3.Lerp(leftStartScale, leftEndScale, t);

                yield return null;
            }

            // Finalize swap
            if (leftPrimaryImage != null)
            {
                leftPrimaryImage.sprite = targetPair.leftSprite;
                leftPrimaryImage.enabled = targetPair.leftSprite != null;
                leftPrimaryImage.transform.localScale = Vector3.one;
            }
            if (leftPrimaryCanvasGroup != null) leftPrimaryCanvasGroup.alpha = 1f;
            if (leftSecondaryCanvasGroup != null) leftSecondaryCanvasGroup.alpha = 0f;

            if (rightPrimaryImage != null)
            {
                rightPrimaryImage.sprite = targetPair.rightSprite;
                rightPrimaryImage.enabled = targetPair.rightSprite != null;
                rightPrimaryImage.transform.localScale = Vector3.one;
            }
            if (rightPrimaryCanvasGroup != null) rightPrimaryCanvasGroup.alpha = 1f;
            if (rightSecondaryCanvasGroup != null) rightSecondaryCanvasGroup.alpha = 0f;
        }
    }
}
