using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

[ExecuteAlways]
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string levelScenePath = "Assets/02_Scenes/Level01.unity";
    [SerializeField] private float minimumDisplayTime = 3f;
    [SerializeField] private Sprite loadingScreenSprite;
    [SerializeField] private TMP_FontAsset loadingFont;
    [SerializeField] private CanvasGroup previewCanvasGroup;
    [SerializeField] private RectTransform previewImageRect;
    [SerializeField] private RectTransform previewTextRect;
    [SerializeField] private float imageOverscan = 160f;
    [SerializeField] private float loadingTextBottomOffset = 150f;
    [SerializeField] private float loadingTextFontSize = 54f;

    private CanvasGroup canvasGroup;
    private RectTransform imageRect;
    private RectTransform textRect;
    private Tween textPulseTween;
    private bool refreshingPreview;
    private bool loadingViewBuilt;

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        BuildLoadingView();
        canvasGroup.alpha = 1f;
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        RefreshEditorPreview();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled)
            return;

        RefreshEditorPreview();
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying)
            yield break;

        BuildLoadingView();
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(canvasGroup.gameObject);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelScenePath);
        if (loadOperation == null)
            yield break;

        loadOperation.allowSceneActivation = false;

        float elapsed = 0f;
        textPulseTween = textRect.DOScale(1.06f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        while (loadOperation.progress < 0.9f || elapsed < minimumDisplayTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        textPulseTween?.Kill();
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        textPulseTween?.Kill();
        if (canvasGroup != null)
        {
            Destroy(canvasGroup.gameObject);
        }

        Destroy(gameObject);
    }

    private void BuildLoadingView()
    {
        if (loadingViewBuilt && canvasGroup != null)
            return;

        TryFindPreviewObjects();

        if (previewCanvasGroup != null && previewImageRect != null)
        {
            canvasGroup = previewCanvasGroup;
            imageRect = previewImageRect;
            textRect = previewTextRect != null ? previewTextRect : CreateLoadingText(canvasGroup.transform);
            ApplyLoadingViewLayout();
            canvasGroup.gameObject.SetActive(true);
        }
        else
        {
            GameObject canvasObject = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            GameObject imageObject = new GameObject("LoadingImage", typeof(RectTransform), typeof(Image));
            imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(canvasObject.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = loadingScreenSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            GameObject textObject = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(canvasObject.transform, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            ConfigureLoadingText(text);

            ApplyLoadingViewLayout();
        }

        canvasGroup.alpha = 0f;
        loadingViewBuilt = true;
    }

    private void RefreshEditorPreview()
    {
        if (refreshingPreview)
            return;

        refreshingPreview = true;
        try
        {
            BuildLoadingView();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (imageRect != null)
            {
                imageRect.localScale = Vector3.one;
            }

            if (textRect != null)
            {
                textRect.localScale = Vector3.one;
                textRect.anchoredPosition = new Vector2(0f, loadingTextBottomOffset);
            }
        }
        finally
        {
            refreshingPreview = false;
        }
    }

    private void TryFindPreviewObjects()
    {
        if (previewCanvasGroup != null && previewImageRect != null)
            return;

        GameObject existingCanvas = GameObject.Find("LoadingCanvas");
        if (existingCanvas == null)
            return;

        previewCanvasGroup = existingCanvas.GetComponent<CanvasGroup>();
        Transform image = existingCanvas.transform.Find("LoadingImage");
        Transform text = existingCanvas.transform.Find("LoadingText");
        previewImageRect = image != null ? image as RectTransform : existingCanvas.transform as RectTransform;
        previewTextRect = text as RectTransform;
    }

    private RectTransform CreateLoadingText(Transform parent)
    {
        GameObject textObject = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        ConfigureLoadingText(textObject.GetComponent<TextMeshProUGUI>());
        previewTextRect = rectTransform;
        return rectTransform;
    }

    private void ConfigureLoadingText(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        text.text = "Loading";
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }

    private void ApplyLoadingViewLayout()
    {
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(-imageOverscan, -imageOverscan);
        imageRect.offsetMax = new Vector2(imageOverscan, imageOverscan);
        imageRect.localScale = Vector3.one * 1.08f;

        Image image = imageRect.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = loadingScreenSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(520f, 90f);
        textRect.anchoredPosition = new Vector2(0f, loadingTextBottomOffset);

        TextMeshProUGUI text = textRect.GetComponent<TextMeshProUGUI>();
        if (text == null)
            return;

        text.fontSize = loadingTextFontSize;
        if (loadingFont != null)
        {
            text.font = loadingFont;
        }
    }

}
