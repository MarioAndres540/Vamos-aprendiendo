#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    public static class LoginSceneGenerator
    {
        private const string SCENE_DIR = "Assets/_Project/Scenes";
        private const string SCENE_PATH = "Assets/_Project/Scenes/LoginScene.unity";
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string IMAGES_DIR = "Assets/_Project/Art/images";

        [MenuItem("Tools/Scenes/Create or Rebuild Login Scene", false, 10)]
        public static void GenerateLoginScene()
        {
            if (!Directory.Exists(SCENE_DIR))
            {
                Directory.CreateDirectory(SCENE_DIR);
                AssetDatabase.Refresh();
            }

            // Create new clean 2D/UI scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera (Solid background color matching palette)
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.92f, 0.95f, 0.99f, 1f); // #EBF2FC
            cam.orthographic = true;
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0, 0, -10);

            // 2. EventSystem
            GameObject eventSysObj = new GameObject("EventSystem");
            eventSysObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModule = eventSysObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                inputModule.actionsAsset = actionsAsset;
            }
            inputModule.AssignDefaultActions();

            // 3. Canvas (Responsive for Mobile, Tablet and PC)
            GameObject canvasObj = new GameObject("LoginCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Full HD Base Reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balanced scaling for phones, tablets & desktop

            canvasObj.AddComponent<GraphicRaycaster>();

            // Controller script
            LoginUI loginUI = canvasObj.AddComponent<LoginUI>();

            // Load Sprites
            Sprite fondoSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/fondo.png");
            Sprite avatarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/avatar.png");
            Sprite boySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/boy.png");
            Sprite girlSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/gril.png");
            Sprite menSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/men.png");
            Sprite womanSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/woman.png");
            Sprite oldMenSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/old-men.png");
            Sprite oldWomanSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/old-woman.png");

            // 4. Background Image (Full Stretch)
            GameObject bgObj = CreateUIObject("Background_Image", canvasObj.transform);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            Image bgImg = bgObj.AddComponent<Image>();
            if (fondoSprite != null)
            {
                bgImg.sprite = fondoSprite;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
            }
            bgImg.color = Color.white;

            // 5. Left & Right Characters Carousel (Anchored to Left & Right hills)
            GameObject carouselRoot = CreateUIObject("Character_Carousel_Root", canvasObj.transform);
            SetFullStretch(carouselRoot.GetComponent<RectTransform>());
            CharacterCarouselUI carousel = carouselRoot.AddComponent<CharacterCarouselUI>();

            // Left Character Slot (Positioned over the left hill)
            GameObject leftContainer = CreateUIObject("Left_Character_Slot", carouselRoot.transform);
            RectTransform leftRect = leftContainer.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0.04f, 0.05f);
            leftRect.anchorMax = new Vector2(0.28f, 0.75f);
            leftRect.pivot = new Vector2(0.5f, 0f);
            leftRect.anchoredPosition = new Vector2(182f, 0f);
            leftRect.sizeDelta = Vector2.zero;

            GameObject leftPrimary = CreateUIObject("Left_Primary_Image", leftContainer.transform);
            SetFullStretch(leftPrimary.GetComponent<RectTransform>());
            Image leftPriImg = leftPrimary.AddComponent<Image>();
            leftPriImg.sprite = boySprite;
            leftPriImg.preserveAspect = true;
            CanvasGroup leftPriCG = leftPrimary.AddComponent<CanvasGroup>();
            leftPriCG.alpha = 1f;

            GameObject leftSecondary = CreateUIObject("Left_Secondary_Image", leftContainer.transform);
            SetFullStretch(leftSecondary.GetComponent<RectTransform>());
            Image leftSecImg = leftSecondary.AddComponent<Image>();
            leftSecImg.preserveAspect = true;
            CanvasGroup leftSecCG = leftSecondary.AddComponent<CanvasGroup>();
            leftSecCG.alpha = 0f;

            // Right Character Slot (Positioned over the right hill)
            GameObject rightContainer = CreateUIObject("Right_Character_Slot", carouselRoot.transform);
            RectTransform rightRect = rightContainer.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.72f, 0.05f);
            rightRect.anchorMax = new Vector2(0.96f, 0.75f);
            rightRect.pivot = new Vector2(0.5f, 0f);
            rightRect.anchoredPosition = new Vector2(-154f, 0f);
            rightRect.sizeDelta = Vector2.zero;

            GameObject rightPrimary = CreateUIObject("Right_Primary_Image", rightContainer.transform);
            SetFullStretch(rightPrimary.GetComponent<RectTransform>());
            Image rightPriImg = rightPrimary.AddComponent<Image>();
            rightPriImg.sprite = girlSprite;
            rightPriImg.preserveAspect = true;
            CanvasGroup rightPriCG = rightPrimary.AddComponent<CanvasGroup>();
            rightPriCG.alpha = 1f;

            GameObject rightSecondary = CreateUIObject("Right_Secondary_Image", rightContainer.transform);
            SetFullStretch(rightSecondary.GetComponent<RectTransform>());
            Image rightSecImg = rightSecondary.AddComponent<Image>();
            rightSecImg.preserveAspect = true;
            CanvasGroup rightSecCG = rightSecondary.AddComponent<CanvasGroup>();
            rightSecCG.alpha = 0f;

            // Configure Character Pairs (Niños -> Adultos -> Adultos Mayores)
            List<CharacterPair> pairs = new List<CharacterPair>
            {
                new CharacterPair { categoryName = "Niños", leftSprite = boySprite, rightSprite = girlSprite },
                new CharacterPair { categoryName = "Adultos", leftSprite = menSprite, rightSprite = womanSprite },
                new CharacterPair { categoryName = "Adultos Mayores", leftSprite = oldMenSprite, rightSprite = oldWomanSprite }
            };

            // Setup Carousel Serialized Fields
            SerializedObject soCarousel = new SerializedObject(carousel);
            soCarousel.FindProperty("leftPrimaryImage").objectReferenceValue = leftPriImg;
            soCarousel.FindProperty("leftSecondaryImage").objectReferenceValue = leftSecImg;
            soCarousel.FindProperty("leftPrimaryCanvasGroup").objectReferenceValue = leftPriCG;
            soCarousel.FindProperty("leftSecondaryCanvasGroup").objectReferenceValue = leftSecCG;
            soCarousel.FindProperty("leftContainer").objectReferenceValue = leftRect;

            soCarousel.FindProperty("rightPrimaryImage").objectReferenceValue = rightPriImg;
            soCarousel.FindProperty("rightSecondaryImage").objectReferenceValue = rightSecImg;
            soCarousel.FindProperty("rightPrimaryCanvasGroup").objectReferenceValue = rightPriCG;
            soCarousel.FindProperty("rightSecondaryCanvasGroup").objectReferenceValue = rightSecCG;
            soCarousel.FindProperty("rightContainer").objectReferenceValue = rightRect;

            soCarousel.FindProperty("displayDuration").floatValue = 4.0f;
            soCarousel.FindProperty("transitionDuration").floatValue = 0.8f;
            soCarousel.ApplyModifiedProperties();

            carousel.SetPairs(pairs);

            // 6. Header Section (Logo + App Title + Subtitle)
            GameObject headerObj = CreateUIObject("Header_Section", canvasObj.transform);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 0.5f);
            headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.pivot = new Vector2(0.5f, 0.5f);
            headerRect.sizeDelta = new Vector2(600, 150);
            headerRect.anchoredPosition = new Vector2(0, 360);

            // Avatar / Logo
            if (avatarSprite != null)
            {
                GameObject avatarObj = CreateUIObject("Logo_Avatar", headerObj.transform);
                RectTransform avRect = avatarObj.GetComponent<RectTransform>();
                avRect.anchorMin = new Vector2(0.5f, 1f);
                avRect.anchorMax = new Vector2(0.5f, 1f);
                avRect.pivot = new Vector2(0.5f, 1f);
                avRect.sizeDelta = new Vector2(85, 85);
                avRect.anchoredPosition = new Vector2(0, 131f);
                avatarObj.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);

                Image avImg = avatarObj.AddComponent<Image>();
                avImg.sprite = avatarSprite;
                avImg.preserveAspect = true;
            }

            // Title "Vamos Aprendiendo"
            GameObject titleObj = CreateUIObject("App_Title", headerObj.transform);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0f);
            titleRect.anchorMax = new Vector2(0.5f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.sizeDelta = new Vector2(600, 42);
            titleRect.anchoredPosition = new Vector2(0, 24);

            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<b><color=#1E293B>Vamos</color> <color=#4338CA>Aprendiendo</color></b>";
            titleTMP.fontSize = 34;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.enableWordWrapping = false;

            // Subtitle
            GameObject subTitleObj = CreateUIObject("App_Subtitle", headerObj.transform);
            RectTransform subTitleRect = subTitleObj.GetComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0.5f, 0f);
            subTitleRect.anchorMax = new Vector2(0.5f, 0f);
            subTitleRect.pivot = new Vector2(0.5f, 0f);
            subTitleRect.sizeDelta = new Vector2(600, 22);
            subTitleRect.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI subTitleTMP = subTitleObj.AddComponent<TextMeshProUGUI>();
            subTitleTMP.text = "Aprender es crecer cada día";
            subTitleTMP.fontSize = 16;
            subTitleTMP.color = new Color(0.42f, 0.48f, 0.58f, 1f);
            subTitleTMP.alignment = TextAlignmentOptions.Center;

            // 7. Central Card (Compact, Crisp, Beautifully Proportionate)
            GameObject cardObj = CreateUIObject("Login_Card", canvasObj.transform);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(460, 560);
            cardRect.anchoredPosition = new Vector2(0, -35);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.color = Color.white;
            
            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.85f, 0.89f, 0.95f, 0.9f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Card Welcome Title
            GameObject cardWelcome = CreateTextObject("Card_Welcome_Title", cardObj.transform, "¡Bienvenido de nuevo!", 22, new Color(0.09f, 0.12f, 0.18f), FontStyles.Bold);
            RectTransform cwRect = cardWelcome.GetComponent<RectTransform>();
            cwRect.anchorMin = new Vector2(0.5f, 1f);
            cwRect.anchorMax = new Vector2(0.5f, 1f);
            cwRect.pivot = new Vector2(0.5f, 1f);
            cwRect.sizeDelta = new Vector2(400, 28);
            cwRect.anchoredPosition = new Vector2(0, -22);

            // Card Subtitle
            GameObject cardSubtitle = CreateTextObject("Card_Subtitle", cardObj.transform, "Inicia sesión para continuar aprendiendo", 13, new Color(0.45f, 0.5f, 0.58f), FontStyles.Normal);
            RectTransform csRect = cardSubtitle.GetComponent<RectTransform>();
            csRect.anchorMin = new Vector2(0.5f, 1f);
            csRect.anchorMax = new Vector2(0.5f, 1f);
            csRect.pivot = new Vector2(0.5f, 1f);
            csRect.sizeDelta = new Vector2(400, 20);
            csRect.anchoredPosition = new Vector2(0, -52);

            // User Field Label
            GameObject userLabel = CreateTextObject("Label_Usuario", cardObj.transform, "Usuario", 13, new Color(0.2f, 0.25f, 0.33f), FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform ulRect = userLabel.GetComponent<RectTransform>();
            ulRect.anchorMin = new Vector2(0.5f, 1f);
            ulRect.anchorMax = new Vector2(0.5f, 1f);
            ulRect.pivot = new Vector2(0.5f, 1f);
            ulRect.sizeDelta = new Vector2(380, 18);
            ulRect.anchoredPosition = new Vector2(0, -82);

            // User Input Field
            TMP_InputField userInput = CreateInputField("Input_Usuario", cardObj.transform, "Ingresa tu usuario", false);
            RectTransform uiRect = userInput.GetComponent<RectTransform>();
            uiRect.anchorMin = new Vector2(0.5f, 1f);
            uiRect.anchorMax = new Vector2(0.5f, 1f);
            uiRect.pivot = new Vector2(0.5f, 1f);
            uiRect.sizeDelta = new Vector2(380, 42);
            uiRect.anchoredPosition = new Vector2(0, -104);

            // Password Field Label
            GameObject passLabel = CreateTextObject("Label_Password", cardObj.transform, "Contraseña", 13, new Color(0.2f, 0.25f, 0.33f), FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform plRect = passLabel.GetComponent<RectTransform>();
            plRect.anchorMin = new Vector2(0.5f, 1f);
            plRect.anchorMax = new Vector2(0.5f, 1f);
            plRect.pivot = new Vector2(0.5f, 1f);
            plRect.sizeDelta = new Vector2(380, 18);
            plRect.anchoredPosition = new Vector2(0, -156);

            // Password Input Field
            TMP_InputField passInput = CreateInputField("Input_Password", cardObj.transform, "Ingresa tu contraseña", true);
            RectTransform piRect = passInput.GetComponent<RectTransform>();
            piRect.anchorMin = new Vector2(0.5f, 1f);
            piRect.anchorMax = new Vector2(0.5f, 1f);
            piRect.pivot = new Vector2(0.5f, 1f);
            piRect.sizeDelta = new Vector2(380, 42);
            piRect.anchoredPosition = new Vector2(0, -178);

            // Forgot Password Link
            Button forgotBtn = CreateLinkButton("Btn_ForgotPassword", cardObj.transform, "¿Olvidaste tu contraseña?", 12, new Color(0.25f, 0.42f, 0.88f), TextAlignmentOptions.Right);
            RectTransform fpRect = forgotBtn.GetComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.5f, 1f);
            fpRect.anchorMax = new Vector2(0.5f, 1f);
            fpRect.pivot = new Vector2(0.5f, 1f);
            fpRect.sizeDelta = new Vector2(380, 20);
            fpRect.anchoredPosition = new Vector2(0, -226);

            // Main Action Button "Iniciar sesión"
            Button loginBtn = CreateActionButton("Btn_IniciarSesion", cardObj.transform, "Iniciar sesión", 16, new Color(0.28f, 0.33f, 0.86f), Color.white);
            RectTransform lbRect = loginBtn.GetComponent<RectTransform>();
            lbRect.anchorMin = new Vector2(0.5f, 1f);
            lbRect.anchorMax = new Vector2(0.5f, 1f);
            lbRect.pivot = new Vector2(0.5f, 1f);
            lbRect.sizeDelta = new Vector2(380, 46);
            lbRect.anchoredPosition = new Vector2(0, -254);

            // Divider "o continúa con"
            GameObject divider = CreateTextObject("Divider_Text", cardObj.transform, "────────  o continúa con  ────────", 11, new Color(0.6f, 0.65f, 0.72f), FontStyles.Normal);
            RectTransform divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.5f, 1f);
            divRect.anchorMax = new Vector2(0.5f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.sizeDelta = new Vector2(380, 18);
            divRect.anchoredPosition = new Vector2(0, -310);

            // Google Button
            Button googleBtn = CreateActionButton("Btn_GoogleLogin", cardObj.transform, "Continuar con Google", 13, new Color(0.96f, 0.97f, 0.99f), new Color(0.18f, 0.22f, 0.3f));
            RectTransform gbRect = googleBtn.GetComponent<RectTransform>();
            gbRect.anchorMin = new Vector2(0.5f, 1f);
            gbRect.anchorMax = new Vector2(0.5f, 1f);
            gbRect.pivot = new Vector2(0.5f, 1f);
            gbRect.sizeDelta = new Vector2(380, 38);
            gbRect.anchoredPosition = new Vector2(0, -336);

            // Apple Button
            Button appleBtn = CreateActionButton("Btn_AppleLogin", cardObj.transform, "Continuar con Apple", 13, new Color(0.96f, 0.97f, 0.99f), new Color(0.18f, 0.22f, 0.3f));
            RectTransform abRect = appleBtn.GetComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.5f, 1f);
            abRect.anchorMax = new Vector2(0.5f, 1f);
            abRect.pivot = new Vector2(0.5f, 1f);
            abRect.sizeDelta = new Vector2(380, 38);
            abRect.anchoredPosition = new Vector2(0, -382);

            // Register Footer Link
            Button registerBtn = CreateLinkButton("Btn_Register", cardObj.transform, "¿No tienes cuenta?  <color=#4338CA><b>Regístrate</b></color>", 13, new Color(0.35f, 0.4f, 0.48f), TextAlignmentOptions.Center);
            RectTransform regRect = registerBtn.GetComponent<RectTransform>();
            regRect.anchorMin = new Vector2(0.5f, 0f);
            regRect.anchorMax = new Vector2(0.5f, 0f);
            regRect.pivot = new Vector2(0.5f, 0f);
            regRect.sizeDelta = new Vector2(380, 26);
            regRect.anchoredPosition = new Vector2(0, 18);

            // 8. Bottom Security Badge
            GameObject badgeObj = CreateUIObject("Bottom_Security_Badge", canvasObj.transform);
            RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0f);
            badgeRect.sizeDelta = new Vector2(380, 36);
            badgeRect.anchoredPosition = new Vector2(0, 24);

            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.color = new Color(0.92f, 0.98f, 0.94f, 0.95f);
            Outline badgeOutline = badgeObj.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.72f, 0.9f, 0.78f, 0.8f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);

            GameObject badgeText = CreateTextObject("Badge_Text", badgeObj.transform, "Entorno seguro y protegido para aprender", 13, new Color(0.14f, 0.42f, 0.28f), FontStyles.Bold);
            SetFullStretch(badgeText.GetComponent<RectTransform>());

            // 9. Feedback Toast (Floating Message)
            GameObject toastObj = CreateUIObject("Feedback_Toast", canvasObj.transform);
            RectTransform toastRect = toastObj.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.5f);
            toastRect.anchorMax = new Vector2(0.5f, 0.5f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(400, 42);
            toastRect.anchoredPosition = new Vector2(0, 260);

            Image toastBg = toastObj.AddComponent<Image>();
            toastBg.color = new Color(0.1f, 0.14f, 0.22f, 0.95f);
            CanvasGroup toastCG = toastObj.AddComponent<CanvasGroup>();
            toastCG.alpha = 0f;

            GameObject toastTextObj = CreateTextObject("Toast_Text", toastObj.transform, "", 14, Color.white, FontStyles.Normal);
            SetFullStretch(toastTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI toastTMP = toastTextObj.GetComponent<TextMeshProUGUI>();

            // 10. Loading Overlay (Glass Backdrop + Center Card + Spinner + 0-100% Progress Bar)
            GameObject loadingObj = CreateUIObject("Loading_Overlay", canvasObj.transform);
            RectTransform loadingRect = loadingObj.GetComponent<RectTransform>();
            SetFullStretch(loadingRect);
            Image loadingBg = loadingObj.AddComponent<Image>();
            loadingBg.color = new Color(0.06f, 0.09f, 0.16f, 0.94f); // Modern dark glass backdrop
            CanvasGroup loadingCG = loadingObj.AddComponent<CanvasGroup>();
            loadingCG.alpha = 0f;
            loadingCG.blocksRaycasts = false;
            loadingObj.SetActive(false);

            // Loading Center Card
            GameObject loadingCardObj = CreateUIObject("Loading_Card", loadingObj.transform);
            RectTransform cardRt = loadingCardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(440, 260);
            cardRt.anchoredPosition = Vector2.zero;

            Image cardBg = loadingCardObj.AddComponent<Image>();
            cardBg.color = new Color(0.11f, 0.16f, 0.28f, 0.98f);
            Outline loadingCardOutline = loadingCardObj.AddComponent<Outline>();
            loadingCardOutline.effectColor = new Color(0.28f, 0.45f, 0.85f, 0.5f);
            loadingCardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Spinner (Rotating Container)
            GameObject spinnerObj = CreateUIObject("Loading_Spinner", loadingCardObj.transform);
            RectTransform spinnerRt = spinnerObj.GetComponent<RectTransform>();
            spinnerRt.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRt.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRt.pivot = new Vector2(0.5f, 0.5f);
            spinnerRt.sizeDelta = new Vector2(54, 54);
            spinnerRt.anchoredPosition = new Vector2(0, 60);

            // Spinner outer decorative ring
            GameObject ringObj = CreateUIObject("Spinner_Ring", spinnerObj.transform);
            SetFullStretch(ringObj.GetComponent<RectTransform>());
            Image ringImg = ringObj.AddComponent<Image>();
            ringImg.color = new Color(0.2f, 0.32f, 0.55f, 0.35f);
            Outline ringOutline = ringObj.AddComponent<Outline>();
            ringOutline.effectColor = new Color(0.35f, 0.58f, 0.98f, 0.6f);
            ringOutline.effectDistance = new Vector2(2f, -2f);

            // Spinner top notch / indicator
            GameObject notchObj = CreateUIObject("Spinner_Notch", spinnerObj.transform);
            RectTransform notchRt = notchObj.GetComponent<RectTransform>();
            notchRt.anchorMin = new Vector2(0.5f, 1f);
            notchRt.anchorMax = new Vector2(0.5f, 1f);
            notchRt.pivot = new Vector2(0.5f, 0.5f);
            notchRt.sizeDelta = new Vector2(14, 14);
            notchRt.anchoredPosition = new Vector2(0, -6);
            Image notchImg = notchObj.AddComponent<Image>();
            notchImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Vibrant Sky Blue

            // Title
            GameObject loadingTitleObj = CreateTextObject("Loading_Title", loadingCardObj.transform, "Cargando Aventura...", 18, Color.white, FontStyles.Bold);
            RectTransform titleRt = loadingTitleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(380, 26);
            titleRt.anchoredPosition = new Vector2(0, 15);

            // Subtitle / Dynamic tip
            GameObject tipObj = CreateTextObject("Loading_Tip", loadingCardObj.transform, "Preparando actividades interactivas...", 12, new Color(0.65f, 0.75f, 0.88f), FontStyles.Normal);
            RectTransform tipRt = tipObj.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0.5f, 0.5f);
            tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0.5f, 0.5f);
            tipRt.sizeDelta = new Vector2(380, 22);
            tipRt.anchoredPosition = new Vector2(0, -8);
            TextMeshProUGUI tipTMP = tipObj.GetComponent<TextMeshProUGUI>();

            // Progress Bar Container
            GameObject barContainer = CreateUIObject("Progress_Bar_Container", loadingCardObj.transform);
            RectTransform barContRt = barContainer.GetComponent<RectTransform>();
            barContRt.anchorMin = new Vector2(0.5f, 0.5f);
            barContRt.anchorMax = new Vector2(0.5f, 0.5f);
            barContRt.pivot = new Vector2(0.5f, 0.5f);
            barContRt.sizeDelta = new Vector2(360, 16);
            barContRt.anchoredPosition = new Vector2(0, -42);

            Image barBgImg = barContainer.AddComponent<Image>();
            barBgImg.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
            Outline barOutline = barContainer.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.24f, 0.36f, 0.58f, 0.5f);
            barOutline.effectDistance = new Vector2(1f, -1f);

            // Progress Bar Fill
            GameObject barFillObj = CreateUIObject("Progress_Bar_Fill", barContainer.transform);
            RectTransform barFillRt = barFillObj.GetComponent<RectTransform>();
            SetFullStretch(barFillRt);
            Image fillImg = barFillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            fillImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Glowing sky blue

            // Percentage Text
            GameObject pctObj = CreateTextObject("Percentage_Text", loadingCardObj.transform, "0%", 14, new Color(0.22f, 0.74f, 0.97f), FontStyles.Bold);
            RectTransform pctRt = pctObj.GetComponent<RectTransform>();
            pctRt.anchorMin = new Vector2(0.5f, 0.5f);
            pctRt.anchorMax = new Vector2(0.5f, 0.5f);
            pctRt.pivot = new Vector2(0.5f, 0.5f);
            pctRt.sizeDelta = new Vector2(120, 22);
            pctRt.anchoredPosition = new Vector2(0, -72);
            TextMeshProUGUI pctTMP = pctObj.GetComponent<TextMeshProUGUI>();

            // Link LoginUI fields
            SerializedObject soLogin = new SerializedObject(loginUI);
            soLogin.FindProperty("targetSceneName").stringValue = "BaseScene";
            soLogin.FindProperty("minimumLoadingDuration").floatValue = 2.2f;
            soLogin.FindProperty("usernameInput").objectReferenceValue = userInput;
            soLogin.FindProperty("passwordInput").objectReferenceValue = passInput;
            soLogin.FindProperty("loginButton").objectReferenceValue = loginBtn;
            soLogin.FindProperty("forgotPasswordButton").objectReferenceValue = forgotBtn;
            soLogin.FindProperty("googleLoginButton").objectReferenceValue = googleBtn;
            soLogin.FindProperty("appleLoginButton").objectReferenceValue = appleBtn;
            soLogin.FindProperty("registerButton").objectReferenceValue = registerBtn;
            soLogin.FindProperty("feedbackText").objectReferenceValue = toastTMP;
            soLogin.FindProperty("feedbackCanvasGroup").objectReferenceValue = toastCG;
            soLogin.FindProperty("loadingOverlay").objectReferenceValue = loadingObj;
            soLogin.FindProperty("loadingCanvasGroup").objectReferenceValue = loadingCG;
            soLogin.FindProperty("spinnerTransform").objectReferenceValue = spinnerRt;
            soLogin.FindProperty("progressBarFill").objectReferenceValue = fillImg;
            soLogin.FindProperty("progressPercentageText").objectReferenceValue = pctTMP;
            soLogin.FindProperty("loadingStatusText").objectReferenceValue = tipTMP;
            soLogin.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            Debug.Log($"<color=#5ce65c>[LoginSceneGenerator] Escena creada exitosamente y adaptada para móvil, tablet y PC en '{SCENE_PATH}'.</color>");

            // 11. Configure Build Settings
            UpdateBuildSettings();
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            // Scene 0: LoginScene
            buildScenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));

            // Scene 1: BaseScene
            if (File.Exists(BASE_SCENE_PATH))
            {
                buildScenes.Add(new EditorBuildSettingsScene(BASE_SCENE_PATH, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#5ce65c>[LoginSceneGenerator] Build Settings actualizados con LoginScene (0) y BaseScene (1).</color>");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            return obj;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText, bool isPassword)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.97f, 0.98f, 1f, 1f);

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.88f, 0.93f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Text Area
            GameObject textArea = CreateUIObject("Text Area", obj.transform);
            RectTransform taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(14, 4);
            taRect.offsetMax = new Vector2(-14, -4);

            // Placeholder
            GameObject phObj = CreateTextObject("Placeholder", textArea.transform, placeholderText, 13, new Color(0.6f, 0.65f, 0.73f), FontStyles.Normal, TextAlignmentOptions.Left);
            SetFullStretch(phObj.GetComponent<RectTransform>());
            TextMeshProUGUI phTMP = phObj.GetComponent<TextMeshProUGUI>();

            // Text
            GameObject textObj = CreateTextObject("Text", textArea.transform, "", 13, new Color(0.15f, 0.2f, 0.28f), FontStyles.Normal, TextAlignmentOptions.Left);
            SetFullStretch(textObj.GetComponent<RectTransform>());
            TextMeshProUGUI textTMP = textObj.GetComponent<TextMeshProUGUI>();

            TMP_InputField inputField = obj.AddComponent<TMP_InputField>();
            inputField.textViewport = taRect;
            inputField.textComponent = textTMP;
            inputField.placeholder = phTMP;
            inputField.fontAsset = textTMP.font;

            if (isPassword)
            {
                inputField.contentType = TMP_InputField.ContentType.Password;
                inputField.inputType = TMP_InputField.InputType.Password;
            }
            else
            {
                inputField.contentType = TMP_InputField.ContentType.Standard;
            }

            return inputField;
        }

        private static Button CreateActionButton(string name, Transform parent, string label, float fontSize, Color bgColor, Color textColor)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = bgColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.85f, 0.92f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.08f;
            cb.pressedColor = bgColor * 0.92f;
            btn.colors = cb;

            GameObject textObj = CreateTextObject("Label", btnObj.transform, label, fontSize, textColor, FontStyles.Bold);
            SetFullStretch(textObj.GetComponent<RectTransform>());

            return btn;
        }

        private static Button CreateLinkButton(string name, Transform parent, string label, float fontSize, Color textColor, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.clear;
            cb.highlightedColor = Color.clear;
            cb.pressedColor = Color.clear;
            btn.colors = cb;

            GameObject textObj = CreateTextObject("Label", btnObj.transform, label, fontSize, textColor, FontStyles.Normal, align);
            SetFullStretch(textObj.GetComponent<RectTransform>());

            return btn;
        }
    }
}
#endif
