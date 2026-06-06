using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    private const float LostPanelAnimationDuration = 0.3f;
    private const float LostPanelStartScale = 0.85f;
    private const int WinRewardSparkleCount = 3;
    private const float WinRewardSparkleRadius = 95f;
    private const float WinRewardSparkleInterval = 0.35f;
    private const float WinIntroDuration = 1.35f;
    private const float WinConfettiDistanceFromCamera = 8f;
    private const float WinConfettiScale = 0.25f;
    private const float WinConfettiLifetime = 2f;
    private const float WinPanelAnimationDuration = 0.34f;
    private const float WinPanelStartScale = 0.82f;
    private const float PerfectLetterDelay = 0.12f;
    private const float PerfectSmashSquashScale = 0.58f;
    private const float PerfectSmashOvershootScale = 1.45f;
    private const float PerfectLetterStartScale = 1.75f;
    private const float PerfectLetterIdleJumpHeight = 22f;
    private const float PerfectLetterIdleDelay = 0.075f;
    private const float PerfectLetterIdleLoopDelay = 0.85f;

    [SerializeField] private TextMeshProUGUI costText, goldText, countdownText;
    [SerializeField] private GameObject LostGamePanel, WinGamePanel, CountdownPanel;
    [SerializeField] private int secondChanceGoldCost = 20;
    [Header("Win Reward")]
    [SerializeField] private RectTransform winRewardCoinRoot;
    [SerializeField] private TextMeshProUGUI winRewardAmountText;
    [SerializeField] private GameObject winRewardSparklePrefab;
    [SerializeField] private Sprite winRewardSparkleSprite;
    [Header("Win Celebration")]
    [SerializeField] private TextMeshProUGUI winPerfectText;
    [SerializeField] private GameObject winConfettiPrefab;

    private Sequence lostPanelSequence;
    private Sequence winRewardSequence;
    private Sequence winRewardSparkleLoop;
    private Sequence winCelebrationSequence;
    private RectTransform winIntroOverlay;
    private GameObject activeWinConfetti;
    private Vector3 winPanelOriginalScale = Vector3.one;
    private Vector3 winPerfectOriginalScale = Vector3.one;
    private Vector2 winPerfectOriginalPosition;
    private Vector3 winRewardCoinOriginalScale = Vector3.one;
    private Vector3 winRewardAmountOriginalScale = Vector3.one;
    private RectTransform activePerfectLetterContainer;
    private readonly List<RectTransform> activePerfectLetters = new List<RectTransform>();
    private Sequence perfectIdleSequence;

    void Start()
    {
        SetGoldText();
    }

    private void OnDisable()
    {
        lostPanelSequence?.Kill();
        winRewardSequence?.Kill();
        winCelebrationSequence?.Kill();
        perfectIdleSequence?.Kill();
        CleanupWinConfetti();
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();
        StopWinRewardSparkleLoop();
    }

    void SetGoldText()
    {
        var playergold = GoldManager.Instance.GetGold();
        goldText.text = playergold.ToString();
    }
    public void OpenWinPanel()
    {
        PlayWinCelebrationSequence();
    }

    public void ReturnMain()
    {
        winCelebrationSequence?.Kill();
        CleanupWinConfetti();
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();
        StopWinRewardSparkleLoop();
        SceneManager.LoadScene(1);
    }

    public void OpenLostPanel()
    {

        if (LostGamePanel == null)
        {
            Debug.Log("LostGamePanel is NULL - not assigned in inspector!");
            return;
        }
        if (!LostGamePanel.activeInHierarchy)
        {
            Debug.Log("LostGamePanel is not active in hierarchy");
        }
        costText.text = secondChanceGoldCost.ToString();
        LostGamePanel.SetActive(true);
        PlayLostPanelAnimation();
        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        winCelebrationSequence?.Kill();
        CleanupWinConfetti();
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();
        StopWinRewardSparkleLoop();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ContinueGame()
    {
        var playergold = GoldManager.Instance.GetGold();

        if (playergold >= secondChanceGoldCost)
        {
            playergold -= secondChanceGoldCost;
            secondChanceGoldCost *= 2;

            GoldManager.Instance.SetGold(playergold);
            //Time.timeScale = 1f;
            lostPanelSequence?.Kill();
            LostGamePanel.SetActive(false);

            StartCoroutine(CountdownWithRealTime());
            //MatchManager.Instance.TryAgain();
            SetGoldText();
        }
        else
        {
            // gold yok popup, reklam izle cartcurt

        }
    }

    private void PlayWinCelebrationSequence()
    {
        if (WinGamePanel == null)
            return;

        EnsureWinRewardReferences();
        EnsureWinPerfectText();
        if (winPerfectText != null)
        {
            winPerfectOriginalScale = winPerfectText.rectTransform.localScale;
            winPerfectOriginalPosition = winPerfectText.rectTransform.anchoredPosition;
        }
        RectTransform panelRect = WinGamePanel.GetComponent<RectTransform>();
        if (panelRect != null)
            winPanelOriginalScale = panelRect.localScale;
        StopWinRewardSparkleLoop();
        winRewardSequence?.Kill();
        winCelebrationSequence?.Kill();
        CleanupWinConfetti();
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();

        WinGamePanel.SetActive(false);

        RectTransform overlay = CreateWinIntroOverlay(out TextMeshProUGUI logoText);
        SpawnWinIntroConfetti();

        winCelebrationSequence = DOTween.Sequence()
            .Append(logoText.rectTransform.DOScale(1f, 0.32f).From(0.35f).SetEase(Ease.OutBack, 2.4f))
            .Join(logoText.DOFade(1f, 0.18f).From(0f))
            .AppendInterval(Mathf.Max(0f, WinIntroDuration - 0.62f))
            .Append(logoText.rectTransform.DOScale(0.82f, 0.18f).SetEase(Ease.InBack))
            .Join(logoText.DOFade(0f, 0.16f))
            .AppendCallback(() =>
            {
                CleanupWinIntroOverlay();
                PlayWinPanelReveal();
            })
            .OnKill(() => winCelebrationSequence = null);
    }

    private RectTransform CreateWinIntroOverlay(out TextMeshProUGUI logoText)
    {
        Canvas sourceCanvas = WinGamePanel.GetComponentInParent<Canvas>();
        Transform parent = sourceCanvas != null ? sourceCanvas.transform : WinGamePanel.transform.parent;

        GameObject overlayObject = new GameObject("WinIntroOverlay", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform overlay = overlayObject.GetComponent<RectTransform>();
        overlay.SetParent(parent, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
        winIntroOverlay = overlay;

        GameObject logoObject = new GameObject("LogoPlaceholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform logoRect = logoObject.GetComponent<RectTransform>();
        logoRect.SetParent(overlay, false);
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(520f, 170f);
        logoRect.anchoredPosition = Vector2.zero;

        logoText = logoObject.GetComponent<TextMeshProUGUI>();
        logoText.text = "BAKERY\nGAME";
        logoText.alignment = TextAlignmentOptions.Center;
        logoText.fontSize = 72f;
        logoText.fontStyle = FontStyles.Bold;
        logoText.color = Color.white;
        logoText.raycastTarget = false;
        if (goldText != null && goldText.font != null)
            logoText.font = goldText.font;

        return overlay;
    }

    private void CleanupWinIntroOverlay()
    {
        if (winIntroOverlay != null)
        {
            Destroy(winIntroOverlay.gameObject);
            winIntroOverlay = null;
        }
    }

    private void SpawnWinIntroConfetti()
    {
        if (winConfettiPrefab == null)
            return;

        Camera camera = Camera.main;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (camera != null)
        {
            position = camera.transform.position + camera.transform.forward * WinConfettiDistanceFromCamera;
            rotation = camera.transform.rotation;
        }

        activeWinConfetti = Instantiate(winConfettiPrefab, position, rotation);
        activeWinConfetti.transform.localScale = Vector3.one * WinConfettiScale;
        Destroy(activeWinConfetti, WinConfettiLifetime);
    }

    private void CleanupWinConfetti()
    {
        if (activeWinConfetti != null)
        {
            Destroy(activeWinConfetti);
            activeWinConfetti = null;
        }
    }

    private void PlayWinPanelReveal()
    {
        WinGamePanel.SetActive(true);

        RectTransform panelRect = WinGamePanel.GetComponent<RectTransform>();
        CanvasGroup panelGroup = WinGamePanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
            panelGroup = WinGamePanel.AddComponent<CanvasGroup>();

        panelGroup.alpha = 0f;
        if (panelRect != null)
            panelRect.localScale = winPanelOriginalScale * WinPanelStartScale;

        PrepareWinUiForReveal();

        Sequence sequence = DOTween.Sequence()
            .Append(panelGroup.DOFade(1f, WinPanelAnimationDuration * 0.8f).SetEase(Ease.OutQuad));

        if (panelRect != null)
            sequence.Join(panelRect.DOScale(winPanelOriginalScale, WinPanelAnimationDuration).SetEase(Ease.OutBack, 2.2f));

        sequence.AppendCallback(RevealWinUi)
            .AppendInterval(0.18f)
            .AppendCallback(PlayWinRewardPresentation)
            .AppendInterval(0.48f)
            .AppendCallback(PlayPerfectLetterReveal)
            .AppendInterval(GetPerfectSmashDuration());
    }

    private void PrepareWinUiForReveal()
    {
        if (winPerfectText != null)
        {
            winPerfectText.maxVisibleCharacters = winPerfectText.text.Length;
            SetGraphicAlpha(winPerfectText, 0f);
            winPerfectText.rectTransform.localScale = winPerfectOriginalScale;
            winPerfectText.rectTransform.anchoredPosition = winPerfectOriginalPosition;
        }

        if (winRewardAmountText != null)
            SetGraphicAlpha(winRewardAmountText, 0f);

        Graphic[] graphics = WinGamePanel.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || graphic == winPerfectText || graphic == winRewardAmountText)
                continue;

            SetGraphicAlpha(graphic, 0f);
        }
    }

    private void PlayPerfectLetterReveal()
    {
        if (winPerfectText == null)
            return;

        winPerfectText.transform.SetAsLastSibling();
        CleanupPerfectLetterContainer();
        winPerfectText.ForceMeshUpdate();
        int characterCount = winPerfectText.textInfo.characterCount;
        activePerfectLetterContainer = CreatePerfectLetterContainer();

        Sequence perfectSequence = DOTween.Sequence();
        int visibleLetterIndex = 0;
        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo characterInfo = winPerfectText.textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
                continue;

            RectTransform letterRect = CreatePerfectLetter(characterInfo, visibleLetterIndex);
            activePerfectLetters.Add(letterRect);
            Vector2 targetPosition = letterRect.anchoredPosition;

            letterRect.anchoredPosition = targetPosition;
            letterRect.localScale = Vector3.zero;

            float delay = visibleLetterIndex * PerfectLetterDelay;
            perfectSequence.Insert(delay, letterRect.DOScale(Vector3.one * PerfectLetterStartScale, 0.09f).SetEase(Ease.OutBack, 4.2f));
            perfectSequence.Insert(delay, letterRect.DORotate(new Vector3(0f, 0f, Random.Range(-12f, 12f)), 0.09f).SetEase(Ease.OutQuad));
            perfectSequence.Insert(delay + 0.09f, letterRect.DOScale(new Vector3(1.38f, PerfectSmashSquashScale, 1f), 0.075f).SetEase(Ease.InOutQuad));
            perfectSequence.Insert(delay + 0.165f, letterRect.DOScale(new Vector3(0.74f, PerfectSmashOvershootScale, 1f), 0.095f).SetEase(Ease.OutBack, 3.2f));
            perfectSequence.Insert(delay + 0.26f, letterRect.DOScale(new Vector3(1.12f, 0.9f, 1f), 0.07f).SetEase(Ease.InOutSine));
            perfectSequence.Insert(delay + 0.33f, letterRect.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutElastic, 1.18f, 0.32f));
            perfectSequence.Insert(delay + 0.33f, letterRect.DORotate(Vector3.zero, 0.16f).SetEase(Ease.OutBack, 2.2f));
            visibleLetterIndex++;
        }

        perfectSequence.AppendInterval(0.05f);
        perfectSequence.Append(activePerfectLetterContainer.DOPunchScale(Vector3.one * 0.18f, 0.24f, 2, 0.55f));
        perfectSequence.AppendCallback(StartPerfectIdleLoop);
    }

    private void StartPerfectIdleLoop()
    {
        perfectIdleSequence?.Kill();
        perfectIdleSequence = DOTween.Sequence();

        for (int i = 0; i < activePerfectLetters.Count; i++)
        {
            RectTransform letter = activePerfectLetters[i];
            if (letter == null)
                continue;

            perfectIdleSequence.AppendCallback(() => PlayPerfectLetterIdleHop(letter));
            perfectIdleSequence.AppendInterval(PerfectLetterIdleDelay);
        }

        perfectIdleSequence.AppendInterval(PerfectLetterIdleLoopDelay);
        perfectIdleSequence.SetLoops(-1);
    }

    private void PlayPerfectLetterIdleHop(RectTransform letter)
    {
        if (letter == null)
            return;

        letter.DOKill();
        Vector2 basePosition = letter.anchoredPosition;
        float tilt = UnityEngine.Random.Range(-7f, 7f);

        DOTween.Sequence()
            .Append(letter.DOAnchorPos(basePosition + Vector2.up * PerfectLetterIdleJumpHeight, 0.07f).SetEase(Ease.OutCubic))
            .Join(letter.DOScale(new Vector3(0.9f, 1.16f, 1f), 0.07f).SetEase(Ease.OutCubic))
            .Join(letter.DORotate(new Vector3(0f, 0f, tilt), 0.07f).SetEase(Ease.OutCubic))
            .Append(letter.DOAnchorPos(basePosition, 0.095f).SetEase(Ease.InCubic))
            .Join(letter.DOScale(new Vector3(1.12f, 0.88f, 1f), 0.055f).SetEase(Ease.InOutSine))
            .Append(letter.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutBack, 2.2f))
            .Join(letter.DORotate(Vector3.zero, 0.08f).SetEase(Ease.OutBack, 2f));
    }

    private RectTransform CreatePerfectLetterContainer()
    {
        GameObject containerObject = new GameObject("PerfectLetterContainer", typeof(RectTransform));
        RectTransform container = containerObject.GetComponent<RectTransform>();
        RectTransform sourceRect = winPerfectText.rectTransform;
        container.SetParent(sourceRect, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.pivot = sourceRect.pivot;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        container.localPosition = Vector3.zero;
        container.localScale = Vector3.one;
        container.localRotation = Quaternion.identity;
        container.SetAsLastSibling();
        return container;
    }

    private RectTransform CreatePerfectLetter(TMP_CharacterInfo characterInfo, int letterIndex)
    {
        GameObject letterObject = new GameObject("PerfectLetter_" + letterIndex, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform letterRect = letterObject.GetComponent<RectTransform>();
        letterRect.SetParent(activePerfectLetterContainer, false);
        letterRect.anchorMin = new Vector2(0.5f, 0.5f);
        letterRect.anchorMax = new Vector2(0.5f, 0.5f);
        letterRect.pivot = new Vector2(0.5f, 0.5f);
        Vector3 bottomLeft = characterInfo.bottomLeft;
        Vector3 topRight = characterInfo.topRight;
        Vector2 characterSize = topRight - bottomLeft;
        letterRect.sizeDelta = new Vector2(characterSize.x * 1.45f, characterSize.y * 1.45f);

        Vector2 localCenter = (bottomLeft + topRight) * 0.5f;
        Vector3 worldCenter = winPerfectText.rectTransform.TransformPoint(localCenter);
        Vector3 containerLocalCenter = activePerfectLetterContainer.InverseTransformPoint(worldCenter);
        letterRect.localPosition = new Vector3(containerLocalCenter.x, containerLocalCenter.y, 0f);

        TextMeshProUGUI letterText = letterObject.GetComponent<TextMeshProUGUI>();
        letterText.text = characterInfo.character.ToString();
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.font = winPerfectText.font;
        letterText.fontSharedMaterial = winPerfectText.fontSharedMaterial;
        letterText.fontSize = winPerfectText.fontSize;
        letterText.fontStyle = winPerfectText.fontStyle;
        Color letterColor = winPerfectText.color;
        letterColor.a = 1f;
        letterText.color = letterColor;
        letterText.raycastTarget = false;
        letterText.enableWordWrapping = false;
        return letterRect;
    }

    private void CleanupPerfectLetterContainer()
    {
        perfectIdleSequence?.Kill();
        perfectIdleSequence = null;
        activePerfectLetters.Clear();

        if (activePerfectLetterContainer != null)
        {
            Destroy(activePerfectLetterContainer.gameObject);
            activePerfectLetterContainer = null;
        }
    }

    private void RevealWinUi()
    {
        Graphic[] graphics = WinGamePanel.GetComponentsInChildren<Graphic>(true);
        int revealIndex = 0;
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || graphic == winPerfectText || graphic == winRewardAmountText)
                continue;

            float delay = revealIndex * 0.025f;
            graphic.DOFade(1f, 0.28f).SetEase(Ease.OutQuad).SetDelay(delay);
            revealIndex++;
        }
    }

    private float GetPerfectRevealDuration()
    {
        if (winPerfectText == null)
            return 0.5f;

        return Mathf.Max(0.32f, winPerfectText.text.Length * PerfectLetterDelay);
    }

    private float GetPerfectSmashDuration()
    {
        return GetPerfectRevealDuration() + 0.5f;
    }

    private void EnsureWinPerfectText()
    {
        if (winPerfectText != null || WinGamePanel == null)
            return;

        TextMeshProUGUI[] texts = WinGamePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.text.Contains("PERFECT"))
            {
                winPerfectText = text;
                return;
            }
        }
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void PlayWinRewardPresentation()
    {
        EnsureWinRewardReferences();

        int rewardAmount = 0;
        if (GoldManager.Instance != null)
            rewardAmount = Mathf.Max(0, GoldManager.Instance.GetGold() - GoldManager.Instance.GetPreviousGold());

        if (winRewardAmountText != null)
        {
            winRewardAmountText.text = rewardAmount.ToString();
            winRewardAmountText.gameObject.SetActive(true);
            SetGraphicAlpha(winRewardAmountText, 1f);
            winRewardAmountText.transform.SetAsLastSibling();
        }

        winRewardSequence?.Kill();
        StopWinRewardSparkleLoop();
        if (winRewardCoinRoot != null)
        {
            winRewardCoinRoot.DOKill();
            winRewardCoinRoot.localScale = winRewardCoinOriginalScale * 0.92f;
        }

        if (winRewardAmountText != null)
        {
            winRewardAmountText.rectTransform.DOKill();
            winRewardAmountText.rectTransform.localScale = Vector3.zero;
        }

        winRewardSequence = DOTween.Sequence()
            .AppendInterval(0.08f);

        if (winRewardCoinRoot != null)
            winRewardSequence.Join(winRewardCoinRoot.DOScale(winRewardCoinOriginalScale, 0.32f).SetEase(Ease.OutBack, 2.2f));

        if (winRewardAmountText != null)
            winRewardSequence.Join(winRewardAmountText.rectTransform.DOScale(winRewardAmountOriginalScale, 0.34f).SetEase(Ease.OutBack, 2.5f));

        winRewardSequence.AppendCallback(StartWinRewardSparkleLoop)
            .OnKill(() => winRewardSequence = null);
    }

    private void EnsureWinRewardReferences()
    {
        if (WinGamePanel == null)
            return;

        if (winRewardCoinRoot == null)
            winRewardCoinRoot = FindLargestImageRect(WinGamePanel.transform);

        if (winRewardAmountText == null)
            winRewardAmountText = CreateWinRewardAmountText();

        if (winRewardCoinRoot != null)
            winRewardCoinOriginalScale = winRewardCoinRoot.localScale;

        if (winRewardAmountText != null)
            winRewardAmountOriginalScale = winRewardAmountText.rectTransform.localScale;
    }

    private RectTransform FindLargestImageRect(Transform root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        RectTransform bestRect = null;
        float bestArea = 0f;

        foreach (Image image in images)
        {
            if (image == null || image.sprite == null)
                continue;

            RectTransform rect = image.rectTransform;
            float area = rect.rect.width * rect.rect.height;
            if (area > bestArea && area < 260000f)
            {
                bestArea = area;
                bestRect = rect;
            }
        }

        return bestRect;
    }

    private TextMeshProUGUI CreateWinRewardAmountText()
    {
        Transform parent = winRewardCoinRoot != null ? winRewardCoinRoot.parent : WinGamePanel.transform;
        GameObject textObject = new GameObject("RewardAmountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 130f);
        rect.anchoredPosition = winRewardCoinRoot != null
            ? winRewardCoinRoot.anchoredPosition + new Vector2(165f, -125f)
            : new Vector2(165f, -80f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 110f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        if (goldText != null && goldText.font != null)
            text.font = goldText.font;

        return text;
    }

    private void SpawnWinRewardSparkles()
    {
        if (winRewardCoinRoot == null)
            return;

        for (int i = 0; i < WinRewardSparkleCount; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * WinRewardSparkleRadius;
            SpawnPrefabSparkle(offset);
            SpawnUiImageSparkle(offset);
        }
    }

    private void StartWinRewardSparkleLoop()
    {
        StopWinRewardSparkleLoop();
        SpawnWinRewardSparkles();

        winRewardSparkleLoop = DOTween.Sequence()
            .AppendInterval(WinRewardSparkleInterval)
            .AppendCallback(SpawnWinRewardSparkles)
            .SetLoops(-1)
            .OnKill(() => winRewardSparkleLoop = null);
    }

    private void StopWinRewardSparkleLoop()
    {
        winRewardSparkleLoop?.Kill();
        winRewardSparkleLoop = null;
    }

    private void SpawnPrefabSparkle(Vector2 offset)
    {
        if (winRewardSparklePrefab == null)
            return;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(winRewardCoinRoot), winRewardCoinRoot.position) + offset;
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(camera.transform.position.z) + 5f));
        GameObject sparkle = Instantiate(winRewardSparklePrefab, worldPosition, Quaternion.identity);
        sparkle.transform.localScale = Vector3.one * 0.75f;
        Destroy(sparkle, 2f);
    }

    private void SpawnUiImageSparkle(Vector2 offset)
    {
        if (winRewardSparkleSprite == null)
            return;

        GameObject sparkleObject = new GameObject("RewardImageSparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = sparkleObject.GetComponent<RectTransform>();
        rect.SetParent(winRewardCoinRoot.parent, false);
        rect.anchorMin = winRewardCoinRoot.anchorMin;
        rect.anchorMax = winRewardCoinRoot.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * Random.Range(36f, 72f);
        rect.anchoredPosition = winRewardCoinRoot.anchoredPosition + offset;
        rect.localScale = Vector3.zero;
        rect.SetAsLastSibling();

        Image image = sparkleObject.GetComponent<Image>();
        image.sprite = winRewardSparkleSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 0.95f, 0.45f, 0f);

        DOTween.Sequence()
            .Append(rect.DOScale(Random.Range(0.75f, 1.1f), 0.18f).SetEase(Ease.OutBack))
            .Join(image.DOFade(1f, 0.12f))
            .Join(rect.DORotate(new Vector3(0f, 0f, Random.Range(80f, 180f)), 0.6f, RotateMode.FastBeyond360))
            .AppendInterval(0.14f)
            .Append(image.DOFade(0f, 0.22f))
            .Join(rect.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack))
            .OnComplete(() => Destroy(sparkleObject));
    }

    private Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private void PlayLostPanelAnimation()
    {
        RectTransform panelRect = LostGamePanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        CanvasGroup canvasGroup = LostGamePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = LostGamePanel.AddComponent<CanvasGroup>();

        lostPanelSequence?.Kill();

        panelRect.localScale = Vector3.one * LostPanelStartScale;
        canvasGroup.alpha = 0f;

        lostPanelSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(panelRect.DOScale(Vector3.one, LostPanelAnimationDuration).SetEase(Ease.OutBack))
            .Join(canvasGroup.DOFade(1f, LostPanelAnimationDuration * 0.75f).SetEase(Ease.OutQuad))
            .OnKill(() => lostPanelSequence = null);
    }

    IEnumerator CountdownWithRealTime()
    {
        CountdownPanel.SetActive(true);
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        MatchManager.Instance.TryAgain();
        Debug.Log("CountDownEnds Here");
        Time.timeScale = 0.25f;
        CountdownPanel.SetActive(false);
         DOTween.To(
            () => Time.timeScale,
            x => Time.timeScale = x,
            1f,
            0.5f
        )
        .SetEase(Ease.InOutQuad);
    }
}
