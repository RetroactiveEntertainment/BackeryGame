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
    private const int WinRewardSparkleTextureSize = 512;
    private const int WinRewardSparkleRenderLayer = 5;
    private const float WinRewardSparkleAreaScale = 1.9f;
    private const float WinRewardSparkleFadeoutDelay = 0.8f;
    private const float WinRewardSparkleRenderDepth = 12f;
    private const float WinRewardSparkleRenderOrthoSize = 6f;
    private const float WinIntroDuration = 1.35f;
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
    [SerializeField] private float winRewardSparkleInterval = 0.85f;
    [SerializeField] private float winRewardSparkleLifetime = 1.65f;
    [SerializeField] private float winRewardSparkleScale = 0.55f;
    [Header("Win Celebration")]
    [SerializeField] private TextMeshProUGUI winPerfectText;
    [SerializeField] private GameObject winFireworkObject;

    private Sequence lostPanelSequence;
    private Sequence winRewardSequence;
    private Sequence winRewardSparkleLoop;
    private Sequence winCelebrationSequence;
    private RectTransform winIntroOverlay;
    private RectTransform winRewardSparkleHost;
    private RawImage winRewardSparkleImage;
    private Material winRewardSparkleImageMaterial;
    private Camera winRewardSparkleCamera;
    private RenderTexture winRewardSparkleTexture;
    private GameObject winRewardSparkleRenderRoot;
    private Vector3 winPanelOriginalScale = Vector3.one;
    private Vector3 winPerfectOriginalScale = Vector3.one;
    private Vector2 winPerfectOriginalPosition;
    private Vector3 winRewardCoinOriginalScale = Vector3.one;
    private Vector3 winRewardAmountOriginalScale = Vector3.one;
    private RectTransform activePerfectLetterContainer;
    private readonly List<RectTransform> activePerfectLetters = new List<RectTransform>();
    private readonly List<GameObject> activeWinRewardSparkles = new List<GameObject>();
    private Coroutine countdownCoroutine;
    private Tween countdownTimeScaleTween;
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
        StopCountdownCoroutine();
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();
        StopWinRewardSparkleLoop();
    }

    private void OnDestroy()
    {
        CleanupWinRewardSparkleResources();
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

            StopCountdownCoroutine();
            countdownCoroutine = StartCoroutine(CountdownWithRealTime());
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

        CloseBlockingPanelsForWin();
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
        CleanupWinIntroOverlay();
        CleanupPerfectLetterContainer();

        WinGamePanel.SetActive(false);

        RectTransform overlay = CreateWinIntroOverlay(out TextMeshProUGUI logoText);
        if (winFireworkObject != null)
        {
            if (winFireworkObject.scene.IsValid())
            {
                ParticleSystem[] particleSystems = winFireworkObject.GetComponentsInChildren<ParticleSystem>(true);
                winFireworkObject.SetActive(true);

                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Clear(false);
                }

                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    particleSystem.Play(false);
                }
            }
            else
            {
                Debug.LogWarning("Win firework object must be a scene instance, not a prefab asset.");
            }
        }

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

    private void CloseBlockingPanelsForWin()
    {
        lostPanelSequence?.Kill();
        lostPanelSequence = null;
        StopCountdownCoroutine();

        if (LostGamePanel != null)
        {
            LostGamePanel.SetActive(false);

            CanvasGroup lostCanvasGroup = LostGamePanel.GetComponent<CanvasGroup>();
            if (lostCanvasGroup != null)
                lostCanvasGroup.alpha = 1f;

            RectTransform lostRect = LostGamePanel.GetComponent<RectTransform>();
            if (lostRect != null)
                lostRect.localScale = Vector3.one;
        }

        if (CountdownPanel != null)
            CountdownPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void StopCountdownCoroutine()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownTimeScaleTween?.Kill();
        countdownTimeScaleTween = null;
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
        if (winRewardCoinRoot == null || winRewardSparklePrefab == null)
            return;

        if (!EnsureWinRewardSparkleHost())
            return;

        GameObject sparkle = Instantiate(winRewardSparklePrefab, winRewardSparkleRenderRoot.transform);
        sparkle.transform.localPosition = Vector3.zero;
        sparkle.transform.localRotation = Quaternion.identity;
        sparkle.transform.localScale = Vector3.one * Mathf.Max(0.0001f, winRewardSparkleScale);
        SetLayerRecursively(sparkle, WinRewardSparkleRenderLayer);
        activeWinRewardSparkles.Add(sparkle);

        ParticleSystem[] particleSystems = sparkle.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
        }

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play(false);
        }

        float lifetime = Mathf.Max(0.1f, winRewardSparkleLifetime);
        DOTween.Sequence()
            .SetTarget(sparkle)
            .AppendInterval(lifetime)
            .AppendCallback(() =>
            {
                if (sparkle == null)
                    return;

                ParticleSystem[] systems = sparkle.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particleSystem in systems)
                {
                    particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            })
            .AppendInterval(WinRewardSparkleFadeoutDelay)
            .OnComplete(() =>
            {
                activeWinRewardSparkles.Remove(sparkle);
                if (sparkle != null)
                    Destroy(sparkle);
            });
    }

    private void StartWinRewardSparkleLoop()
    {
        StopWinRewardSparkleLoop();
        SpawnWinRewardSparkles();

        winRewardSparkleLoop = DOTween.Sequence()
            .AppendInterval(Mathf.Max(0.08f, winRewardSparkleInterval))
            .AppendCallback(SpawnWinRewardSparkles)
            .SetLoops(-1)
            .OnKill(() => winRewardSparkleLoop = null);
    }

    private void StopWinRewardSparkleLoop()
    {
        winRewardSparkleLoop?.Kill();
        winRewardSparkleLoop = null;
        ClearActiveWinRewardSparkles();
    }

    private void ClearActiveWinRewardSparkles()
    {
        for (int i = activeWinRewardSparkles.Count - 1; i >= 0; i--)
        {
            GameObject sparkle = activeWinRewardSparkles[i];
            if (sparkle == null)
                continue;

            DOTween.Kill(sparkle);

            ParticleSystem[] particleSystems = sparkle.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(false);
            }

            Destroy(sparkle);
        }

        activeWinRewardSparkles.Clear();
    }

    private bool EnsureWinRewardSparkleHost()
    {
        if (winRewardCoinRoot == null || winRewardCoinRoot.parent == null)
            return false;

        if (winRewardSparkleTexture == null)
        {
            winRewardSparkleTexture = new RenderTexture(WinRewardSparkleTextureSize, WinRewardSparkleTextureSize, 0, RenderTextureFormat.ARGB32)
            {
                name = "WinRewardSparkleTexture",
                useMipMap = false,
                autoGenerateMips = false
            };
            winRewardSparkleTexture.Create();
        }

        if (winRewardSparkleRenderRoot == null)
        {
            winRewardSparkleRenderRoot = new GameObject("WinRewardSparkleRenderRoot");
            winRewardSparkleRenderRoot.transform.position = new Vector3(10000f, 10000f, 0f);
            SetLayerRecursively(winRewardSparkleRenderRoot, WinRewardSparkleRenderLayer);
        }

        if (winRewardSparkleCamera == null)
        {
            GameObject cameraObject = new GameObject("WinRewardSparkleCamera");
            cameraObject.layer = WinRewardSparkleRenderLayer;
            cameraObject.transform.position = winRewardSparkleRenderRoot.transform.position - Vector3.forward * WinRewardSparkleRenderDepth;
            cameraObject.transform.rotation = Quaternion.identity;
            cameraObject.transform.SetParent(winRewardSparkleRenderRoot.transform, true);
            winRewardSparkleCamera = cameraObject.AddComponent<Camera>();
            winRewardSparkleCamera.clearFlags = CameraClearFlags.SolidColor;
            winRewardSparkleCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            winRewardSparkleCamera.orthographic = true;
            winRewardSparkleCamera.orthographicSize = WinRewardSparkleRenderOrthoSize;
            winRewardSparkleCamera.nearClipPlane = 0.1f;
            winRewardSparkleCamera.farClipPlane = WinRewardSparkleRenderDepth * 2f;
            winRewardSparkleCamera.cullingMask = 1 << WinRewardSparkleRenderLayer;
            winRewardSparkleCamera.targetTexture = winRewardSparkleTexture;
            winRewardSparkleCamera.allowHDR = true;
            winRewardSparkleCamera.allowMSAA = false;
        }

        if (winRewardSparkleHost == null)
        {
            GameObject hostObject = new GameObject("WinRewardSparkleHost", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            winRewardSparkleHost = hostObject.GetComponent<RectTransform>();

            winRewardSparkleImage = hostObject.GetComponent<RawImage>();
            winRewardSparkleImage.texture = winRewardSparkleTexture;
            winRewardSparkleImage.material = GetWinRewardSparkleImageMaterial();
            winRewardSparkleImage.color = Color.white;
            winRewardSparkleImage.raycastTarget = false;
        }

        if (winRewardSparkleHost.parent != winRewardCoinRoot.parent)
            winRewardSparkleHost.SetParent(winRewardCoinRoot.parent, false);

        UpdateWinRewardSparkleHostLayout();
        return true;
    }

    private void UpdateWinRewardSparkleHostLayout()
    {
        if (winRewardSparkleHost == null || winRewardCoinRoot == null)
            return;

        winRewardSparkleHost.anchorMin = winRewardCoinRoot.anchorMin;
        winRewardSparkleHost.anchorMax = winRewardCoinRoot.anchorMax;
        winRewardSparkleHost.pivot = winRewardCoinRoot.pivot;
        winRewardSparkleHost.anchoredPosition = winRewardCoinRoot.anchoredPosition;
        winRewardSparkleHost.sizeDelta = winRewardCoinRoot.rect.size * WinRewardSparkleAreaScale;
        winRewardSparkleHost.localScale = Vector3.one;
        winRewardSparkleHost.localRotation = Quaternion.identity;
        MoveWinRewardSparkleHostBehindCoin();
    }

    private void MoveWinRewardSparkleHostBehindCoin()
    {
        if (winRewardSparkleHost == null || winRewardCoinRoot == null || winRewardSparkleHost.parent != winRewardCoinRoot.parent)
            return;

        int coinSiblingIndex = winRewardCoinRoot.GetSiblingIndex();
        if (winRewardSparkleHost.GetSiblingIndex() < coinSiblingIndex)
            coinSiblingIndex--;

        winRewardSparkleHost.SetSiblingIndex(Mathf.Max(0, coinSiblingIndex));
    }

    private Material GetWinRewardSparkleImageMaterial()
    {
        if (winRewardSparkleImageMaterial != null)
            return winRewardSparkleImageMaterial;

        Shader shader = Shader.Find("UI/TrueShadow-Additive");
        if (shader == null)
            shader = Shader.Find("UI/Additive");
        if (shader == null)
            shader = Shader.Find("Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader == null)
            return null;

        winRewardSparkleImageMaterial = new Material(shader)
        {
            name = "WinRewardSparkleAdditive"
        };

        return winRewardSparkleImageMaterial;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void CleanupWinRewardSparkleResources()
    {
        ClearActiveWinRewardSparkles();

        if (winRewardSparkleImage != null)
        {
            winRewardSparkleImage.texture = null;
            winRewardSparkleImage.material = null;
        }

        if (winRewardSparkleImageMaterial != null)
        {
            Destroy(winRewardSparkleImageMaterial);
            winRewardSparkleImageMaterial = null;
        }

        if (winRewardSparkleTexture != null)
        {
            winRewardSparkleTexture.Release();
            Destroy(winRewardSparkleTexture);
            winRewardSparkleTexture = null;
        }

        if (winRewardSparkleHost != null)
        {
            Destroy(winRewardSparkleHost.gameObject);
            winRewardSparkleHost = null;
            winRewardSparkleImage = null;
        }

        if (winRewardSparkleRenderRoot != null)
        {
            Destroy(winRewardSparkleRenderRoot);
            winRewardSparkleRenderRoot = null;
            winRewardSparkleCamera = null;
        }
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
        if (CountdownPanel != null)
            CountdownPanel.SetActive(true);

        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        MatchManager.Instance.TryAgain();
        Debug.Log("CountDownEnds Here");
        Time.timeScale = 0.25f;
        if (CountdownPanel != null)
            CountdownPanel.SetActive(false);

        countdownTimeScaleTween?.Kill();
        countdownTimeScaleTween = DOTween.To(
            () => Time.timeScale,
            x => Time.timeScale = x,
            1f,
            0.5f
        )
        .SetEase(Ease.InOutQuad)
        .OnKill(() => countdownTimeScaleTween = null);
        countdownCoroutine = null;
    }
}
