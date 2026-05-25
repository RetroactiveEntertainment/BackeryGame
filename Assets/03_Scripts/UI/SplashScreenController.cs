using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

[ExecuteAlways]
public class SplashScreenController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private float displayTime = 1.5f;
    [SerializeField] private Sprite companyLogoSprite;
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Vector2 logoSize = new Vector2(420f, 420f);
    [SerializeField] private CanvasGroup previewCanvasGroup;
    [SerializeField] private RectTransform previewLogoRect;
    [SerializeField] private float exitFadeDuration = 0.35f;

    private CanvasGroup canvasGroup;
    private RectTransform logoRect;
    private bool refreshingPreview;
    private bool splashViewBuilt;

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        BuildSplashView();
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

        BuildSplashView();
        canvasGroup.alpha = 1f;
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(canvasGroup.gameObject);

        yield return new WaitForSeconds(displayTime);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName);
        if (loadOperation == null)
            yield break;

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        yield return canvasGroup.DOFade(0f, exitFadeDuration).SetEase(Ease.InOutSine).WaitForCompletion();

        if (canvasGroup != null)
        {
            Destroy(canvasGroup.gameObject);
        }

        Destroy(gameObject);
    }

    private void BuildSplashView()
    {
        if (splashViewBuilt && canvasGroup != null)
        {
            if (logoRect == null && previewLogoRect != null)
            {
                logoRect = previewLogoRect;
            }

            ApplySplashViewLayout();
            return;
        }

        TryFindPreviewObjects();

        if (previewCanvasGroup != null && previewLogoRect != null)
        {
            canvasGroup = previewCanvasGroup;
            logoRect = previewLogoRect;
            ApplySplashViewLayout();
            canvasGroup.gameObject.SetActive(true);
        }
        else
        {
            GameObject canvasObject = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            GameObject logoObject = new GameObject("CompanyLogo", typeof(RectTransform), typeof(Image));
            logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.SetParent(canvasObject.transform, false);

            ApplySplashViewLayout();
        }

        canvasGroup.alpha = 1f;
        splashViewBuilt = true;
    }

    private void RefreshEditorPreview()
    {
        if (refreshingPreview)
            return;

        refreshingPreview = true;
        try
        {
            BuildSplashView();
        }
        finally
        {
            refreshingPreview = false;
        }
    }

    private void TryFindPreviewObjects()
    {
        if (previewCanvasGroup != null && previewLogoRect != null)
            return;

        GameObject existingCanvas = GameObject.Find("SplashCanvas");
        if (existingCanvas == null)
            return;

        previewCanvasGroup = existingCanvas.GetComponent<CanvasGroup>();
        Transform logo = existingCanvas.transform.Find("CompanyLogo");
        previewLogoRect = logo as RectTransform;
    }

    private void ApplySplashViewLayout()
    {
        RectTransform canvasRect = canvasGroup.transform as RectTransform;
        if (canvasRect != null)
        {
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }

        Image background = canvasGroup.GetComponent<Image>();
        if (background != null)
        {
            background.color = backgroundColor;
            background.raycastTarget = false;
        }

        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = logoSize;
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.localScale = Vector3.one;

        Image logo = logoRect.GetComponent<Image>();
        if (logo != null)
        {
            logo.sprite = companyLogoSprite;
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
        }
    }
}
