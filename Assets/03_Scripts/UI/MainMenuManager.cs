using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using LeTai.TrueShadow;
using TMPro;

[ExecuteAlways]
public class MainMenuManager : MonoBehaviour
{
    [Header("Bottom Bar Buttons")]
    public List<RectTransform> buttons;

    [Header("Panels")]
    public List<GameObject> panels;
    public float screenSlideDuration = 0.35f;

    [Header("Play Flow")]
    public Button playButton;
    public string loadingSceneName = "LoadingScreen";

    [Header("Animation")]
    public float animationDuration = 0.3f;

    [Header("Navbar Art")]
    public Sprite navbarContainerSprite;
    public Sprite navbarSelectedSprite;
    public List<Sprite> navbarIconSprites;
    public Sprite navbarSeparatorSprite;
    public string[] tabLabels = { "Store", "Leaderboard", "Home", "Settings" };
    public TMP_FontAsset navbarLabelFont;
    public float navbarLabelFontSize = 48f;
    public float navbarLabelMinFontSize = 28f;
    public bool navbarLabelAutoSize = true;
    public float navbarLabelHeight = 92f;
    public float navBarHeight = 190f;
    public float navIconDeselectedSize = 118f;
    public float navIconSelectedSize = 190f;
    [Tooltip("Optional per-navbar-icon deselected sizes. Leave an entry at 0 to use Nav Icon Deselected Size.")]
    public List<float> navIconDeselectedSizeOverrides = new List<float>();
    [Tooltip("Optional per-navbar-icon selected sizes. Leave an entry at 0 to use Nav Icon Selected Size.")]
    public List<float> navIconSelectedSizeOverrides = new List<float>();
    public float navSelectedBackgroundWidth = 320f;
    public float navSelectedBackgroundHeight = 300f;
    public float navSelectedBottomBleed = 8f;
    public float navSelectedHorizontalInset = 18f;
    public float navSelectedBackgroundXOffset = 0f;
    public float navSelectedIconYOffset = 132f;
    public float navDeselectedIconYOffset = 0f;
    public float navLabelYOffset = -52f;
    public float navNormalSlotWeight = 1f;
    public float navSelectedSlotWeight = 1.65f;
    public float navSideBleed = 32f;
    public float navSeparatorWidth = 18f;
    public float navSeparatorHeightRatio = 0.78f;
    public float navSeparatorYOffset = 0f;
    [Range(0f, 1f)]
    public float navSeparatorOpacity = 1f;

    [Header("Navbar Main Container Shadow")]
    public bool navbarMainContainerShadowEnabled = true;
    public Color navbarMainContainerShadowColor = new Color(0f, 0f, 0f, 0.35f);
    public float navbarMainContainerShadowSize = 18f;
    [Range(0f, 1f)]
    public float navbarMainContainerShadowSpread = 0.05f;
    public float navbarMainContainerShadowOffsetAngle = 270f;
    public float navbarMainContainerShadowOffsetDistance = 6f;

    [Header("Top Bar Art")]
    public RectTransform topBarRoot;
    public Sprite topBarContainerSprite;
    public Sprite topBarProfileSprite;
    public Sprite topBarSettingsSprite;
    public Sprite topBarPlusSprite;
    public Sprite topBarHeartSprite;
    public Sprite topBarGoldSprite;
    public Sprite topBarButtonSprite;
    public TMP_FontAsset topBarFont;
    public bool autoBuildTopBar = true;
    public float topBarHeight = 150f;
    public float topBarTopOffset = 58f;

    [HideInInspector]
    public RectTransform settingsPanelRoot;
    [HideInInspector]
    public Sprite settingsContainerSprite;
    [HideInInspector]
    public Sprite settingsSmallContainerSprite;
    [HideInInspector]
    public Sprite settingsTitleContainerSprite;
    [HideInInspector]
    public Sprite settingsSoundIconSprite;
    [HideInInspector]
    public Sprite settingsMusicIconSprite;
    [HideInInspector]
    public Sprite settingsSliderBorderSprite;
    [HideInInspector]
    public Sprite settingsSliderOnSprite;
    [HideInInspector]
    public Sprite settingsSliderOffSprite;
    [HideInInspector]
    public Sprite settingsSliderHandleSprite;
    [HideInInspector]
    public bool notificationsEnabled = true;
    [HideInInspector]
    public bool soundsEnabled = true;
    [HideInInspector]
    public bool musicEnabled = true;

    private int currentSelectedIndex = 2;
    private HorizontalLayoutGroup bottomBarLayoutGroup;
    private RectTransform bottomBarLayoutRect;
    private RectTransform screenRoot;
    private readonly List<RectTransform> screenRects = new List<RectTransform>();
    private readonly List<NavbarItemVisual> navbarItems = new List<NavbarItemVisual>();
    private readonly List<RectTransform> navbarSeparators = new List<RectTransform>();
    private Tween screenSlideTween;
    private bool refreshingNavbarPreview;

    private class NavbarItemVisual
    {
        public RectTransform Button;
        public RectTransform Icon;
        public RectTransform SelectedBackground;
        public CanvasGroup SelectedGroup;
        public RectTransform Label;
        public CanvasGroup LabelGroup;
    }

    private void Awake()
    {
        if (buttons != null && buttons.Count > 0 && buttons[0] != null)
        {
            bottomBarLayoutGroup = buttons[0].GetComponentInParent<HorizontalLayoutGroup>();
            bottomBarLayoutRect = bottomBarLayoutGroup != null ? bottomBarLayoutGroup.transform as RectTransform : null;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        Canvas.ForceUpdateCanvases();
        ConfigureBottomBarLayout();
        ConfigureNavbarVisuals();
        ConfigureTopBarVisuals();
        ConfigureScreens();
        ConfigurePlayButton();

        for (int i = 0; i < buttons.Count; i++)
        {
            SetNavbarItemState(i, i == currentSelectedIndex, 0f);

            Button btn = buttons[i].GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => SwitchButton(index));
        }

        if (buttons.Count > 0 && currentSelectedIndex < buttons.Count)
        {
            SetNavbarItemState(currentSelectedIndex, true, 0f);
            PositionScreens(currentSelectedIndex);
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        RefreshNavbarPreview();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled)
            return;

        SyncNavbarIconSizeOverrideLists();
        RefreshNavbarPreview();
    }

    public void SwitchButton(int selectedIndex)
    {
        if (selectedIndex == currentSelectedIndex)
            return;

        SetNavbarItemState(currentSelectedIndex, false, animationDuration);

        PositionNavbarButtons(selectedIndex);
        PositionNavbarSeparators(selectedIndex);

        SetNavbarItemState(selectedIndex, true, animationDuration);

        SlideToScreen(selectedIndex);

        currentSelectedIndex = selectedIndex;
    }

    private void ConfigurePlayButton()
    {
        if (playButton == null)
            return;

        playButton.onClick.RemoveListener(OpenLoadingScreen);
        playButton.onClick.AddListener(OpenLoadingScreen);
    }

    private void OpenLoadingScreen()
    {
        if (playButton != null)
        {
            playButton.interactable = false;
        }

        SceneManager.LoadScene(loadingSceneName);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (buttons == null || buttons.Count == 0)
            return;

        if (!Application.isPlaying)
        {
            RefreshNavbarPreview();
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            SetNavbarItemState(i, i == currentSelectedIndex, 0f);
        }

        PositionNavbarButtons(currentSelectedIndex);
        PositionNavbarSeparators(currentSelectedIndex);
        PositionScreens(currentSelectedIndex);
    }

    private void RefreshNavbarPreview()
    {
        if (refreshingNavbarPreview)
            return;

        if (buttons == null || buttons.Count == 0)
            return;

        refreshingNavbarPreview = true;
        try
        {
            if (bottomBarLayoutGroup == null || bottomBarLayoutRect == null)
            {
                bottomBarLayoutGroup = buttons[0] != null ? buttons[0].GetComponentInParent<HorizontalLayoutGroup>() : null;
                bottomBarLayoutRect = bottomBarLayoutGroup != null ? bottomBarLayoutGroup.transform as RectTransform : null;
            }

            Canvas.ForceUpdateCanvases();
            ConfigureBottomBarLayout();
            ConfigureNavbarVisuals();
            ConfigureTopBarVisuals();

            for (int i = 0; i < buttons.Count; i++)
            {
                SetNavbarItemState(i, i == currentSelectedIndex, 0f);
            }

            PositionNavbarButtons(currentSelectedIndex);
            PositionNavbarSeparators(currentSelectedIndex);
        }
        finally
        {
            refreshingNavbarPreview = false;
        }
    }

    private void ConfigureScreens()
    {
        if (buttons == null || buttons.Count == 0)
            return;

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        GameObject rootGo = new GameObject("ScreenRoot", typeof(RectTransform));
        rootGo.layer = gameObject.layer;
        screenRoot = rootGo.GetComponent<RectTransform>();
        screenRoot.SetParent(transform, false);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = Vector2.zero;
        screenRoot.offsetMax = Vector2.zero;
        screenRoot.SetAsFirstSibling();

        RectTransform homeScreen = GetHomeScreen();
        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform screen = GetConfiguredScreen(i, homeScreen);
            if (screen == null)
            {
                screen = CreateColorScreen(i);
            }

            screen.SetParent(screenRoot, false);
            StretchToParent(screen);
            screen.gameObject.SetActive(true);
            screenRects.Add(screen);
        }

        screenRoot.SetAsFirstSibling();
        if (bottomBarLayoutRect != null && bottomBarLayoutRect.parent != null)
        {
            bottomBarLayoutRect.parent.SetAsLastSibling();
        }

        Canvas.ForceUpdateCanvases();
        PositionScreens(currentSelectedIndex);
    }

    private RectTransform GetHomeScreen()
    {
        if (panels == null || currentSelectedIndex < 0 || currentSelectedIndex >= panels.Count || panels[currentSelectedIndex] == null)
            return null;

        return panels[currentSelectedIndex].GetComponent<RectTransform>();
    }

    private RectTransform GetConfiguredScreen(int index, RectTransform homeScreen)
    {
        if (index == currentSelectedIndex)
            return homeScreen;

        if (panels == null || index < 0 || index >= panels.Count || panels[index] == null)
            return null;

        RectTransform configuredScreen = panels[index].GetComponent<RectTransform>();
        if (configuredScreen == homeScreen)
            return null;

        return configuredScreen;
    }

    private RectTransform CreateColorScreen(int index)
    {
        Color[] colors =
        {
            new Color(0.13f, 0.59f, 0.95f, 1f),
            new Color(0.08f, 0.24f, 0.72f, 1f),
            new Color(0.35f, 0.72f, 0.46f, 1f),
            new Color(0.86f, 0.38f, 0.62f, 1f),
            new Color(0.45f, 0.39f, 0.82f, 1f)
        };

        GameObject screenGo = new GameObject($"Screen {index + 1}", typeof(RectTransform), typeof(Image));
        screenGo.layer = gameObject.layer;
        Image image = screenGo.GetComponent<Image>();
        image.color = colors[index % colors.Length];
        return screenGo.GetComponent<RectTransform>();
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void PositionScreens(int selectedIndex)
    {
        if (screenRoot == null)
            return;

        float width = GetScreenWidth();
        for (int i = 0; i < screenRects.Count; i++)
        {
            screenRects[i].anchoredPosition = new Vector2((i - selectedIndex) * width, 0f);
        }
    }

    private void SlideToScreen(int selectedIndex)
    {
        if (screenRoot == null)
            return;

        screenSlideTween?.Kill();
        float width = GetScreenWidth();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < screenRects.Count; i++)
        {
            Vector2 targetPosition = new Vector2((i - selectedIndex) * width, 0f);
            sequence.Join(screenRects[i].DOAnchorPos(targetPosition, screenSlideDuration).SetEase(Ease.OutCubic));
        }

        screenSlideTween = sequence;
    }

    private float GetScreenWidth()
    {
        if (screenRoot == null)
            return 0f;

        return screenRoot.rect.width;
    }

    private void ConfigureBottomBarLayout()
    {
        if (bottomBarLayoutGroup == null)
            return;

        if (bottomBarLayoutRect != null && bottomBarLayoutRect.parent is RectTransform bottomBar)
        {
            bottomBar.sizeDelta = new Vector2(navSideBleed * 2f, navBarHeight);
            bottomBar.anchoredPosition = new Vector2(0f, bottomBar.anchoredPosition.y);
            bottomBarLayoutRect.offsetMin = new Vector2(-navSideBleed, 0f);
            bottomBarLayoutRect.offsetMax = new Vector2(navSideBleed, 0f);
        }

        bottomBarLayoutGroup.enabled = false;
        PositionNavbarButtons(currentSelectedIndex);
    }

    private void ConfigureNavbarVisuals()
    {
        navbarItems.Clear();
        navbarSeparators.Clear();

        if (bottomBarLayoutRect == null || buttons == null)
            return;

        RectTransform bottomBar = bottomBarLayoutRect.parent as RectTransform;
        if (bottomBar != null)
        {
            Image containerImage = bottomBar.GetComponent<Image>();
            if (containerImage == null)
            {
                containerImage = bottomBar.gameObject.AddComponent<Image>();
            }

            containerImage.sprite = navbarContainerSprite;
            containerImage.type = Image.Type.Sliced;
            containerImage.raycastTarget = false;
            ConfigureNavbarMainContainerShadow(bottomBar);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            NavbarItemVisual item = ConfigureNavbarButton(i);
            navbarItems.Add(item);
        }

        ConfigureNavbarSeparators(bottomBar);
        Canvas.ForceUpdateCanvases();
        PositionNavbarSeparators(currentSelectedIndex);
    }

    private void ConfigureNavbarMainContainerShadow(RectTransform bottomBar)
    {
        TrueShadow shadow = bottomBar.GetComponent<TrueShadow>();
        if (!navbarMainContainerShadowEnabled)
        {
            if (shadow != null)
            {
                shadow.enabled = false;
            }

            return;
        }

        if (shadow == null)
        {
            shadow = bottomBar.gameObject.AddComponent<TrueShadow>();
        }

        shadow.enabled = true;
        shadow.Color = navbarMainContainerShadowColor;
        shadow.Size = navbarMainContainerShadowSize;
        shadow.Spread = navbarMainContainerShadowSpread;
        shadow.UseGlobalAngle = false;
        shadow.OffsetAngle = navbarMainContainerShadowOffsetAngle;
        shadow.OffsetDistance = navbarMainContainerShadowOffsetDistance;
        shadow.BlendMode = BlendMode.Normal;
        shadow.UseCasterAlpha = true;
        shadow.IgnoreCasterColor = true;
        shadow.Inset = false;
        shadow.Cutout = false;
    }

    private void PositionNavbarButtons(int selectedIndex)
    {
        if (buttons == null || buttons.Count == 0)
            return;

        float totalWeight = GetNavbarTotalWeight(selectedIndex);
        float cursor = 0f;

        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform button = buttons[i];
            if (button == null)
                continue;

            float weight = GetNavbarSlotWeight(i, selectedIndex);
            float minX = cursor / totalWeight;
            cursor += weight;
            float maxX = cursor / totalWeight;
            button.anchorMin = new Vector2(minX, 0f);
            button.anchorMax = new Vector2(maxX, 1f);
            button.offsetMin = Vector2.zero;
            button.offsetMax = Vector2.zero;
            button.pivot = new Vector2(0.5f, 0.5f);
            button.localScale = Vector3.one;
            button.anchoredPosition = Vector2.zero;
            button.sizeDelta = Vector2.zero;
        }
    }

    private float GetNavbarSlotWeight(int index, int selectedIndex)
    {
        return index == selectedIndex ? Mathf.Max(0.1f, navSelectedSlotWeight) : Mathf.Max(0.1f, navNormalSlotWeight);
    }

    private float GetNavbarTotalWeight(int selectedIndex)
    {
        float totalWeight = 0f;
        for (int i = 0; i < buttons.Count; i++)
        {
            totalWeight += GetNavbarSlotWeight(i, selectedIndex);
        }

        return Mathf.Max(0.1f, totalWeight);
    }

    private NavbarItemVisual ConfigureNavbarButton(int index)
    {
        RectTransform button = buttons[index];
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color transparent = buttonImage.color;
            transparent.a = 0f;
            buttonImage.color = transparent;
            buttonImage.raycastTarget = true;
        }

        RectTransform selectedBackground = GetOrCreateChild(button, "SelectedBackground", typeof(Image), typeof(CanvasGroup));
        Image selectedImage = selectedBackground.GetComponent<Image>();
        selectedImage.sprite = navbarSelectedSprite;
        selectedImage.type = Image.Type.Sliced;
        selectedImage.raycastTarget = false;
        selectedBackground.localScale = Vector3.one;
        selectedBackground.anchorMin = new Vector2(0f, 0f);
        selectedBackground.anchorMax = new Vector2(1f, 0f);
        selectedBackground.pivot = new Vector2(0.5f, 0f);
        ApplySelectedBackgroundOffsets(selectedBackground);
        selectedBackground.SetAsFirstSibling();

        RectTransform icon = GetOrCreateChild(button, "NavbarIcon", typeof(Image));
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(0f, icon.anchoredPosition.y);
        icon.localScale = Vector3.one;
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = GetNavbarIconSprite(index);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        HideUnusedButtonImages(button, icon, selectedBackground);

        RectTransform label = GetOrCreateChild(button, "SelectedLabel", typeof(TextMeshProUGUI), typeof(CanvasGroup));
        label.localScale = Vector3.one;
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        if (navbarLabelFont != null)
        {
            labelText.font = navbarLabelFont;
        }

        labelText.text = index < tabLabels.Length ? tabLabels[index] : string.Empty;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = navbarLabelFontSize;
        labelText.fontSizeMin = navbarLabelMinFontSize;
        labelText.fontSizeMax = navbarLabelFontSize;
        labelText.enableAutoSizing = navbarLabelAutoSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.raycastTarget = false;
        label.sizeDelta = new Vector2(navSelectedBackgroundWidth - (navSelectedHorizontalInset * 2f), navbarLabelHeight);
        label.anchoredPosition = new Vector2(0f, navLabelYOffset);
        label.SetAsLastSibling();

        icon.SetAsLastSibling();

        return new NavbarItemVisual
        {
            Button = button,
            Icon = icon,
            SelectedBackground = selectedBackground,
            SelectedGroup = selectedBackground.GetComponent<CanvasGroup>(),
            Label = label,
            LabelGroup = label.GetComponent<CanvasGroup>()
        };
    }

    private void HideUnusedButtonImages(RectTransform button, RectTransform icon, RectTransform selectedBackground)
    {
        Image[] images = button.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            Transform imageTransform = image.transform;
            if (imageTransform == button || imageTransform == icon || imageTransform == selectedBackground)
                continue;

            image.enabled = false;
            image.raycastTarget = false;
        }
    }

    private Sprite GetNavbarIconSprite(int index)
    {
        if (navbarIconSprites != null && index >= 0 && index < navbarIconSprites.Count && navbarIconSprites[index] != null)
            return navbarIconSprites[index];

        return null;
    }

    private void ConfigureTopBarVisuals()
    {
        if (!autoBuildTopBar)
            return;

        RectTransform root = GetTopBarRoot();
        if (root == null)
            return;

        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -topBarTopOffset);
        root.sizeDelta = new Vector2(0f, topBarHeight);

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name != "TopBarGenerated")
            {
                child.gameObject.SetActive(false);
            }
        }

        RectTransform generatedRoot = GetOrCreateChild(root, "TopBarGenerated");
        generatedRoot.gameObject.SetActive(true);
        generatedRoot.anchorMin = Vector2.zero;
        generatedRoot.anchorMax = Vector2.one;
        generatedRoot.offsetMin = Vector2.zero;
        generatedRoot.offsetMax = Vector2.zero;
        generatedRoot.SetAsLastSibling();

        RectTransform profile = CreateTopBarImage(generatedRoot, "Profile", topBarProfileSprite, new Vector2(128f, 128f), new Vector2(-385f, -2f), false);
        profile.SetAsLastSibling();

        CreateTopBarCurrencyGroup(generatedRoot, "GoldGroup", topBarGoldSprite, "1000", string.Empty, new Vector2(-132f, -12f));
        CreateTopBarCurrencyGroup(generatedRoot, "HeartGroup", topBarHeartSprite, "5", "Full", new Vector2(142f, -12f));

        RectTransform settingsButton = CreateTopBarImage(generatedRoot, "SettingsButton", topBarButtonSprite, new Vector2(96f, 96f), new Vector2(385f, -2f), true);
        CreateTopBarImage(settingsButton, "SettingsIcon", topBarSettingsSprite, new Vector2(66f, 66f), Vector2.zero, false);
    }

    private void ConfigureSettingsVisuals()
    {
        RectTransform root = GetSettingsPanelRoot();
        if (root == null)
            return;

        Image panelImage = root.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.77f, 0.64f, 0.50f, 1f);
            panelImage.raycastTarget = false;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name != "SettingsGenerated")
            {
                child.gameObject.SetActive(false);
            }
        }

        RectTransform generated = GetOrCreateChild(root, "SettingsGenerated");
        generated.gameObject.SetActive(true);
        generated.anchorMin = Vector2.zero;
        generated.anchorMax = Vector2.one;
        generated.offsetMin = Vector2.zero;
        generated.offsetMax = Vector2.zero;
        generated.localScale = Vector3.one;
        generated.SetAsLastSibling();

        RectTransform panel = GetOrCreateChild(generated, "PanelContainer");
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(980f, 1220f);
        panel.anchoredPosition = new Vector2(0f, 80f);
        panel.localScale = Vector3.one;
        Image panelContainerImage = panel.GetComponent<Image>();
        if (panelContainerImage != null)
        {
            panelContainerImage.enabled = false;
            panelContainerImage.raycastTarget = false;
        }

        RectTransform titleContainer = CreateSettingsImage(panel, "TitleContainer", settingsTitleContainerSprite, new Vector2(980f, 252f), new Vector2(0f, 500f), false);
        titleContainer.SetAsFirstSibling();
        CreateSettingsText(titleContainer, "Title", "SETTINGS", new Vector2(0f, -6f), new Vector2(670f, 126f), 82f);

        RectTransform notifications = CreateSettingsImage(panel, "NotificationsContainer", settingsSmallContainerSprite, new Vector2(860f, 178f), new Vector2(0f, 250f), false);
        CreateSettingsText(notifications, "Label", "Notifications", new Vector2(-210f, 4f), new Vector2(500f, 102f), 54f);
        CreateSettingsToggle(notifications, "NotificationsToggle", notificationsEnabled, new Vector2(275f, 0f));

        RectTransform audioContainer = CreateSettingsImage(panel, "AudioContainer", settingsContainerSprite, new Vector2(860f, 482f), new Vector2(0f, -95f), false);
        CreateSettingsAudioButton(audioContainer, "Sounds", "Sounds", settingsSoundIconSprite, soundsEnabled, new Vector2(-185f, -25f));
        CreateSettingsAudioButton(audioContainer, "Music", "Music", settingsMusicIconSprite, musicEnabled, new Vector2(185f, -25f));
    }

    private RectTransform GetSettingsPanelRoot()
    {
        if (settingsPanelRoot != null)
            return settingsPanelRoot;

        Transform found = FindChildRecursive(transform, "SettingsPanel");
        settingsPanelRoot = found as RectTransform;
        return settingsPanelRoot;
    }

    private RectTransform CreateSettingsImage(RectTransform parent, string childName, Sprite sprite, Vector2 size, Vector2 anchoredPosition, bool sliced)
    {
        RectTransform rectTransform = GetOrCreateChild(parent, childName, typeof(Image));
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        Image image = rectTransform.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = !sliced;
        image.raycastTarget = false;

        return rectTransform;
    }

    private TextMeshProUGUI CreateSettingsText(RectTransform parent, string childName, string text, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        RectTransform rectTransform = GetOrCreateChild(parent, childName, typeof(TextMeshProUGUI), typeof(Shadow));
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        TextMeshProUGUI textComponent = rectTransform.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = navbarLabelFont != null ? navbarLabelFont : topBarFont;
        if (font != null)
        {
            textComponent.font = font;
        }

        textComponent.text = text;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = fontSize;
        textComponent.enableAutoSizing = true;
        textComponent.fontSizeMin = fontSize * 0.72f;
        textComponent.fontSizeMax = fontSize;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = false;
        textComponent.raycastTarget = false;

        Shadow shadow = rectTransform.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.29f, 0.08f, 0.04f, 1f);
        shadow.effectDistance = new Vector2(0f, -8f);
        shadow.useGraphicAlpha = true;

        return textComponent;
    }

    private RectTransform CreateSettingsToggle(RectTransform parent, string childName, bool isOn, Vector2 anchoredPosition)
    {
        RectTransform root = GetOrCreateChild(parent, childName, typeof(Image), typeof(Button));
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(300f, 118f);
        root.anchoredPosition = anchoredPosition;
        root.localScale = Vector3.one;

        Image hitArea = root.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;

        CreateSettingsImage(root, "Border", settingsSliderBorderSprite, root.sizeDelta, Vector2.zero, false);
        RectTransform fill = CreateSettingsImage(root, "Fill", isOn ? settingsSliderOnSprite : settingsSliderOffSprite, new Vector2(240f, 78f), new Vector2(-12f, 0f), false);
        fill.SetAsFirstSibling();
        CreateSettingsText(root, "State", isOn ? "ON" : "OFF", new Vector2(-48f, 2f), new Vector2(126f, 74f), 54f);
        CreateSettingsImage(root, "Handle", settingsSliderHandleSprite, new Vector2(98f, 98f), new Vector2(isOn ? 82f : -82f, 0f), false);

        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitArea;

        return root;
    }

    private void CreateSettingsAudioButton(RectTransform parent, string childName, string label, Sprite icon, bool enabled, Vector2 anchoredPosition)
    {
        RectTransform group = GetOrCreateChild(parent, childName);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.sizeDelta = new Vector2(260f, 330f);
        group.anchoredPosition = anchoredPosition;
        group.localScale = Vector3.one;

        CreateSettingsText(group, "Label", label, new Vector2(0f, 120f), new Vector2(240f, 78f), 52f);
        RectTransform buttonRect = CreateSettingsImage(group, "Button", topBarButtonSprite, new Vector2(200f, 200f), new Vector2(0f, -42f), true);
        Button button = buttonRect.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRect.gameObject.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        Image image = buttonRect.GetComponent<Image>();
        image.raycastTarget = true;

        RectTransform iconRect = CreateSettingsImage(buttonRect, "Icon", icon, new Vector2(125f, 125f), Vector2.zero, false);
        iconRect.localScale = enabled ? Vector3.one : Vector3.one * 0.88f;

        CanvasGroup groupAlpha = iconRect.GetComponent<CanvasGroup>();
        if (groupAlpha == null)
        {
            groupAlpha = iconRect.gameObject.AddComponent<CanvasGroup>();
        }

        groupAlpha.alpha = enabled ? 1f : 0.45f;
    }

    private RectTransform GetTopBarRoot()
    {
        if (topBarRoot != null)
            return topBarRoot;

        Transform found = FindChildRecursive(transform, "TopBar");
        topBarRoot = found as RectTransform;
        return topBarRoot;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void CreateTopBarCurrencyGroup(RectTransform parent, string groupName, Sprite iconSprite, string amount, string suffix, Vector2 anchoredPosition)
    {
        RectTransform group = GetOrCreateChild(parent, groupName);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.sizeDelta = new Vector2(245f, 88f);
        group.anchoredPosition = anchoredPosition;
        group.localScale = Vector3.one;

        RectTransform container = CreateTopBarImage(group, "Container", topBarContainerSprite, new Vector2(string.IsNullOrEmpty(suffix) ? 188f : 214f, 60f), new Vector2(32f, 0f), true);
        container.SetAsFirstSibling();

        RectTransform icon = CreateTopBarImage(group, "Icon", iconSprite, new Vector2(82f, 82f), new Vector2(-78f, 5f), false);
        icon.SetAsLastSibling();

        RectTransform plus = CreateTopBarImage(group, "Plus", topBarPlusSprite, new Vector2(38f, 38f), new Vector2(-58f, -25f), false);
        plus.SetAsLastSibling();

        TextMeshProUGUI amountText = CreateTopBarText(group, "Amount", amount, new Vector2(string.IsNullOrEmpty(suffix) ? 30f : 8f, 1f), new Vector2(118f, 48f), 34f);
        amountText.alignment = string.IsNullOrEmpty(suffix) ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;

        if (!string.IsNullOrEmpty(suffix))
        {
            TextMeshProUGUI suffixText = CreateTopBarText(group, "Suffix", suffix, new Vector2(86f, 1f), new Vector2(86f, 48f), 30f);
            suffixText.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            Transform suffixChild = group.Find("Suffix");
            if (suffixChild != null)
                suffixChild.gameObject.SetActive(false);
        }
    }

    private RectTransform CreateTopBarImage(RectTransform parent, string childName, Sprite sprite, Vector2 size, Vector2 anchoredPosition, bool sliced)
    {
        RectTransform rectTransform = GetOrCreateChild(parent, childName, typeof(Image));
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        Image image = rectTransform.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = !sliced;
        image.raycastTarget = false;

        return rectTransform;
    }

    private TextMeshProUGUI CreateTopBarText(RectTransform parent, string childName, string text, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        RectTransform rectTransform = GetOrCreateChild(parent, childName, typeof(TextMeshProUGUI));
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        TextMeshProUGUI textComponent = rectTransform.GetComponent<TextMeshProUGUI>();
        if (topBarFont != null)
        {
            textComponent.font = topBarFont;
        }

        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = new Color(0.28f, 0.13f, 0.08f, 1f);
        textComponent.raycastTarget = false;

        rectTransform.gameObject.SetActive(true);
        return textComponent;
    }

    private RectTransform GetOrCreateChild(RectTransform parent, string childName, params System.Type[] components)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            foreach (System.Type component in components)
            {
                if (component != typeof(RectTransform) && existing.GetComponent(component) == null)
                {
                    existing.gameObject.AddComponent(component);
                }
            }

            return existing as RectTransform;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        foreach (System.Type component in components)
        {
            if (component != typeof(RectTransform))
            {
                child.AddComponent(component);
            }
        }
        child.layer = parent.gameObject.layer;
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private void ConfigureNavbarSeparators(RectTransform bottomBar)
    {
        if (bottomBar == null || navbarSeparatorSprite == null || buttons.Count < 2)
            return;

        RectTransform separatorRoot = GetOrCreateChild(bottomBar, "NavbarSeparators");
        separatorRoot.anchorMin = Vector2.zero;
        separatorRoot.anchorMax = Vector2.one;
        separatorRoot.offsetMin = Vector2.zero;
        separatorRoot.offsetMax = Vector2.zero;
        separatorRoot.SetAsFirstSibling();
        bottomBarLayoutRect.SetAsLastSibling();
        for (int i = 1; i < buttons.Count; i++)
        {
            RectTransform separator = GetOrCreateChild(separatorRoot, $"Separator {i}", typeof(Image));
            separator.gameObject.SetActive(true);
            Image separatorImage = separator.GetComponent<Image>();
            separatorImage.sprite = navbarSeparatorSprite;
            separatorImage.type = Image.Type.Simple;
            separatorImage.color = new Color(1f, 1f, 1f, navSeparatorOpacity);
            separatorImage.raycastTarget = false;
            separator.sizeDelta = new Vector2(navSeparatorWidth, navBarHeight * navSeparatorHeightRatio);
            navbarSeparators.Add(separator);
        }

        HideExtraSeparators(separatorRoot, buttons.Count - 1);
    }

    private void HideExtraSeparators(RectTransform parent, int visibleCount)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(i < visibleCount);
        }
    }

    private void PositionNavbarSeparators(int selectedIndex)
    {
        if (bottomBarLayoutRect == null || buttons == null || buttons.Count < 2)
            return;

        RectTransform bottomBar = bottomBarLayoutRect.parent as RectTransform;
        if (bottomBar == null)
            return;

        float width = bottomBar.rect.width;
        if (width <= 0f)
            return;

        float totalWeight = GetNavbarTotalWeight(selectedIndex);
        float cursor = 0f;
        for (int i = 0; i < navbarSeparators.Count; i++)
        {
            cursor += GetNavbarSlotWeight(i, selectedIndex);
            float normalizedDivider = cursor / totalWeight;
            navbarSeparators[i].anchoredPosition = new Vector2((normalizedDivider - 0.5f) * width, navSeparatorYOffset);
        }
    }

    private void SetNavbarItemState(int index, bool selected, float duration)
    {
        if (index < 0 || index >= navbarItems.Count)
            return;

        NavbarItemVisual item = navbarItems[index];
        float iconSize = GetNavbarIconSize(index, selected);
        float iconY = selected ? navSelectedIconYOffset : navDeselectedIconYOffset;
        float alpha = selected ? 1f : 0f;
        float selectedScale = selected ? 1f : 0.88f;

        if (duration > 0f)
        {
            item.Icon.DOKill();
            item.Icon.DOSizeDelta(new Vector2(iconSize, iconSize), duration).SetEase(Ease.OutBack);
            item.Icon.DOAnchorPosY(iconY, duration).SetEase(Ease.OutBack);
            item.SelectedBackground.DOKill();
            item.SelectedGroup.DOKill();
            ApplySelectedBackgroundOffsets(item.SelectedBackground);
            item.SelectedGroup.alpha = alpha;

            if (selected)
            {
                item.SelectedBackground.localScale = Vector3.one;
            }
            else
            {
                item.SelectedBackground.localScale = Vector3.one * selectedScale;
            }

            item.Label.DOAnchorPosY(navLabelYOffset, duration).SetEase(Ease.OutBack);
            item.LabelGroup.DOKill();
            item.LabelGroup.alpha = alpha;
        }
        else
        {
            ApplyNavbarIconSize(item.Icon, iconSize);
            item.Icon.anchoredPosition = new Vector2(item.Icon.anchoredPosition.x, iconY);
            ApplySelectedBackgroundOffsets(item.SelectedBackground);
            item.SelectedBackground.localScale = Vector3.one * selectedScale;
            item.SelectedGroup.alpha = alpha;
            item.Label.anchoredPosition = new Vector2(0f, navLabelYOffset);
            item.LabelGroup.alpha = alpha;
        }
    }

    private void ApplySelectedBackgroundOffsets(RectTransform selectedBackground)
    {
        selectedBackground.offsetMin = new Vector2(navSelectedHorizontalInset + navSelectedBackgroundXOffset, -navSelectedBottomBleed);
        selectedBackground.offsetMax = new Vector2(-navSelectedHorizontalInset + navSelectedBackgroundXOffset, navSelectedBackgroundHeight - navSelectedBottomBleed);
    }

    private float GetNavbarIconSize(int index, bool selected)
    {
        List<float> overrides = selected ? navIconSelectedSizeOverrides : navIconDeselectedSizeOverrides;
        float fallbackSize = selected ? navIconSelectedSize : navIconDeselectedSize;

        if (overrides != null && index >= 0 && index < overrides.Count && overrides[index] > 0f)
            return overrides[index];

        return fallbackSize;
    }

    private void ApplyNavbarIconSize(RectTransform icon, float size)
    {
        icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        icon.sizeDelta = new Vector2(size, size);
    }

    private void SyncNavbarIconSizeOverrideLists()
    {
        int targetCount = buttons != null ? buttons.Count : 0;
        SyncFloatListCount(navIconDeselectedSizeOverrides, targetCount);
        SyncFloatListCount(navIconSelectedSizeOverrides, targetCount);
    }

    private void SyncFloatListCount(List<float> values, int targetCount)
    {
        if (values == null)
            return;

        while (values.Count < targetCount)
        {
            values.Add(0f);
        }

        while (values.Count > targetCount)
        {
            values.RemoveAt(values.Count - 1);
        }
    }
}
