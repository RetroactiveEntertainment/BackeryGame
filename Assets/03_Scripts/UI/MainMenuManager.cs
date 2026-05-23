using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
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

    [Header("Size Settings")]
    public float selectedSize = 80f;
    public float deselectedSize = 60f;
    public float animationDuration = 0.3f;
    public float overlap = 32f;
    public float edgeBleed = 24f;
    public float selectedIconYOffset = 8f;

    [Header("Navbar Art")]
    public Sprite navbarContainerSprite;
    public Sprite navbarSelectedSprite;
    public Sprite navbarIconSprite;
    public List<Sprite> navbarIconSprites;
    public Sprite navbarSeparatorSprite;
    public string[] tabLabels = { "Store", "Leaderboard", "Home", "Settings" };
    public float navBarHeight = 190f;
    public float navIconDeselectedSize = 118f;
    public float navIconSelectedSize = 190f;
    public float navSelectedBackgroundWidth = 320f;
    public float navSelectedBackgroundHeight = 300f;
    public float navSelectedBackgroundYOffset = 72f;
    public float navSelectedBottomBleed = 8f;
    public float navSelectedIconYOffset = 132f;
    public float navDeselectedIconYOffset = 0f;
    public float navLabelYOffset = -52f;
    public float navNormalSlotWeight = 1f;
    public float navSelectedSlotWeight = 1.65f;
    public float navSideBleed = 32f;
    public float navSeparatorWidth = 18f;
    public float navSeparatorHeightRatio = 0.78f;

    private int currentSelectedIndex = 2;
    private HorizontalLayoutGroup bottomBarLayoutGroup;
    private RectTransform bottomBarLayoutRect;
    private RectTransform screenRoot;
    private readonly List<RectTransform> screenRects = new List<RectTransform>();
    private readonly List<NavbarItemVisual> navbarItems = new List<NavbarItemVisual>();
    private readonly List<RectTransform> navbarSeparators = new List<RectTransform>();
    private float runtimeDeselectedSize;
    private float runtimeSelectedSize;
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
        ConfigureScreens();
        RecalculateButtonSizes();
        ConfigurePlayButton();

        for (int i = 0; i < buttons.Count; i++)
        {
            SetButtonSize(buttons[i], runtimeDeselectedSize, 0f);
            SetNavbarItemState(i, i == currentSelectedIndex, 0f);

            Button btn = buttons[i].GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => SwitchButton(index));
        }

        if (buttons.Count > 0 && currentSelectedIndex < buttons.Count)
        {
            SetButtonSize(buttons[currentSelectedIndex], runtimeSelectedSize, 0f);
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

        RefreshNavbarPreview();
    }

    public void SwitchButton(int selectedIndex)
    {
        if (selectedIndex == currentSelectedIndex)
            return;

        SetButtonSize(buttons[currentSelectedIndex], runtimeDeselectedSize, animationDuration);
        SetNavbarItemState(currentSelectedIndex, false, animationDuration);

        PositionNavbarButtons(selectedIndex);
        PositionNavbarSeparators(selectedIndex);

        SetButtonSize(buttons[selectedIndex], runtimeSelectedSize, animationDuration);
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

        RecalculateButtonSizes();
        for (int i = 0; i < buttons.Count; i++)
        {
            SetButtonSize(buttons[i], i == currentSelectedIndex ? runtimeSelectedSize : runtimeDeselectedSize, 0f);
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
            RecalculateButtonSizes();
            ConfigureNavbarVisuals();

            for (int i = 0; i < buttons.Count; i++)
            {
                SetButtonSize(buttons[i], runtimeDeselectedSize, 0f);
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
        if (panels == null || panels.Count == 0 || panels[0] == null)
            return null;

        return panels[0].GetComponent<RectTransform>();
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
        selectedBackground.offsetMin = new Vector2(0f, -navSelectedBottomBleed);
        selectedBackground.offsetMax = new Vector2(0f, navSelectedBackgroundHeight - navSelectedBottomBleed);
        selectedBackground.SetAsFirstSibling();

        RectTransform icon = GetOrCreateChild(button, "NavbarIcon", typeof(Image));
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
        labelText.text = index < tabLabels.Length ? tabLabels[index] : string.Empty;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 48f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
        label.sizeDelta = new Vector2(navSelectedBackgroundWidth, 68f);
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

        return navbarIconSprite;
    }

    private RectTransform GetOrCreateChild(RectTransform parent, string childName, params System.Type[] components)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing as RectTransform;

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
            navbarSeparators[i].anchoredPosition = new Vector2((normalizedDivider - 0.5f) * width, 0f);
        }
    }

    private void SetNavbarItemState(int index, bool selected, float duration)
    {
        if (index < 0 || index >= navbarItems.Count)
            return;

        NavbarItemVisual item = navbarItems[index];
        float iconSize = selected ? navIconSelectedSize : navIconDeselectedSize;
        float iconY = selected ? navSelectedIconYOffset : navDeselectedIconYOffset;
        float alpha = selected ? 1f : 0f;
        float selectedScale = selected ? 1f : 0.88f;

        if (duration > 0f)
        {
            item.Icon.DOSizeDelta(new Vector2(iconSize, iconSize), duration).SetEase(Ease.OutBack);
            item.Icon.DOAnchorPosY(iconY, duration).SetEase(Ease.OutBack);
            item.SelectedBackground.DOSizeDelta(new Vector2(0f, navSelectedBackgroundHeight), duration).SetEase(Ease.OutBack);
            item.SelectedBackground.DOScale(selectedScale, duration).SetEase(Ease.OutBack);
            item.SelectedGroup.DOFade(alpha, duration * 0.6f);
            item.Label.DOAnchorPosY(navLabelYOffset, duration).SetEase(Ease.OutBack);
            item.LabelGroup.DOFade(alpha, duration * 0.6f);
        }
        else
        {
            item.Icon.sizeDelta = new Vector2(iconSize, iconSize);
            item.Icon.anchoredPosition = new Vector2(item.Icon.anchoredPosition.x, iconY);
            item.SelectedBackground.offsetMin = new Vector2(0f, -navSelectedBottomBleed);
            item.SelectedBackground.offsetMax = new Vector2(0f, navSelectedBackgroundHeight - navSelectedBottomBleed);
            item.SelectedBackground.localScale = Vector3.one * selectedScale;
            item.SelectedGroup.alpha = alpha;
            item.Label.anchoredPosition = new Vector2(0f, navLabelYOffset);
            item.LabelGroup.alpha = alpha;
        }
    }

    private void RecalculateButtonSizes()
    {
        runtimeDeselectedSize = deselectedSize;
        runtimeSelectedSize = selectedSize;

        if (bottomBarLayoutRect == null || buttons == null || buttons.Count == 0 || deselectedSize <= 0f)
            return;

        float layoutWidth = bottomBarLayoutRect.rect.width;
        if (layoutWidth <= 0f)
            return;

        float buttonScale = Mathf.Max(0.001f, buttons[0].localScale.x);
        runtimeDeselectedSize = layoutWidth / (buttons.Count * buttonScale);
        runtimeSelectedSize = runtimeDeselectedSize;
    }

    private void SetButtonSize(RectTransform button, float size, float duration)
    {
        if (button == null)
            return;

        button.sizeDelta = Vector2.zero;
        RebuildBottomBarLayout();
    }

    private void SetIconYOffset(RectTransform button, float yOffset, float duration)
    {
        if (button == null || button.childCount == 0)
            return;

        RectTransform icon = button.GetChild(0) as RectTransform;
        if (icon == null)
            return;

        if (duration > 0)
        {
            icon.DOAnchorPosY(yOffset, duration).SetEase(Ease.OutBack);
        }
        else
        {
            Vector2 anchoredPosition = icon.anchoredPosition;
            anchoredPosition.y = yOffset;
            icon.anchoredPosition = anchoredPosition;
        }
    }

    private void RebuildBottomBarLayout()
    {
        if (bottomBarLayoutRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomBarLayoutRect);
        }
    }
}
