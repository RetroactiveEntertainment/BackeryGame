using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SettingsToggleSwitch : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isOn = true;
    [SerializeField] private bool applyPreviewInEditMode;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private RectTransform stateTextRect;
    [SerializeField] private RectTransform handleRect;

    [Header("Sprites")]
    [SerializeField] private Sprite onFillSprite;
    [SerializeField] private Sprite offFillSprite;

    [Header("Text")]
    [SerializeField] private string onText = "ON";
    [SerializeField] private string offText = "OFF";
    [SerializeField] private Vector2 onTextAnchoredPosition = new Vector2(-48f, 2f);
    [SerializeField] private Vector2 offTextAnchoredPosition = new Vector2(48f, 2f);

    [Header("Fill")]
    [SerializeField] private Vector2 onFillAnchoredPosition = new Vector2(-9.9f, -13.2f);
    [SerializeField] private Vector2 offFillAnchoredPosition = new Vector2(-9.9f, -13.2f);
    [SerializeField] private Vector2 onFillSizeDelta = new Vector2(250.4f, 124.3f);
    [SerializeField] private Vector2 offFillSizeDelta = new Vector2(250.4f, 124.3f);

    [Header("Handle")]
    [SerializeField] private Vector2 onHandleAnchoredPosition = new Vector2(82f, 0f);
    [SerializeField] private Vector2 offHandleAnchoredPosition = new Vector2(-82f, 0f);

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
            button.onClick.AddListener(Toggle);
        }

        ApplyState();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
        }
    }

    private void OnValidate()
    {
        CacheReferences();

        if (!Application.isPlaying && !applyPreviewInEditMode)
            return;

        ApplyState();
    }

    [ContextMenu("Capture Current Layout As ON")]
    private void CaptureCurrentLayoutAsOn()
    {
        CacheReferences();
        CaptureCurrentLayout(true);
        isOn = true;
    }

    [ContextMenu("Capture Current Layout As OFF")]
    private void CaptureCurrentLayoutAsOff()
    {
        CacheReferences();
        CaptureCurrentLayout(false);
        isOn = false;
    }

    [ContextMenu("Preview ON Layout")]
    private void PreviewOnLayout()
    {
        isOn = true;
        ApplyState();
    }

    [ContextMenu("Preview OFF Layout")]
    private void PreviewOffLayout()
    {
        isOn = false;
        ApplyState();
    }

    private void Toggle()
    {
        isOn = !isOn;
        ApplyState();
    }

    private void CacheReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (fillRect == null)
            fillRect = transform.Find("Fill") as RectTransform;

        if (fillImage == null && fillRect != null)
            fillImage = fillRect.GetComponent<Image>();

        if (stateTextRect == null)
            stateTextRect = transform.Find("State") as RectTransform;

        if (stateText == null && stateTextRect != null)
            stateText = stateTextRect.GetComponent<TextMeshProUGUI>();

        if (handleRect == null)
            handleRect = transform.Find("Handle") as RectTransform;
    }

    private void ApplyState()
    {
        if (fillImage != null)
            fillImage.sprite = isOn ? onFillSprite : offFillSprite;

        if (fillRect != null)
        {
            fillRect.anchoredPosition = isOn ? onFillAnchoredPosition : offFillAnchoredPosition;
            fillRect.sizeDelta = isOn ? onFillSizeDelta : offFillSizeDelta;
        }

        if (stateText != null)
            stateText.text = isOn ? onText : offText;

        if (stateTextRect != null)
            stateTextRect.anchoredPosition = isOn ? onTextAnchoredPosition : offTextAnchoredPosition;

        if (handleRect != null)
            handleRect.anchoredPosition = isOn ? onHandleAnchoredPosition : offHandleAnchoredPosition;
    }

    private void CaptureCurrentLayout(bool captureOnLayout)
    {
        if (fillRect != null)
        {
            if (captureOnLayout)
            {
                onFillAnchoredPosition = fillRect.anchoredPosition;
                onFillSizeDelta = fillRect.sizeDelta;
            }
            else
            {
                offFillAnchoredPosition = fillRect.anchoredPosition;
                offFillSizeDelta = fillRect.sizeDelta;
            }
        }

        if (stateTextRect != null)
        {
            if (captureOnLayout)
            {
                onTextAnchoredPosition = stateTextRect.anchoredPosition;
            }
            else
            {
                offTextAnchoredPosition = stateTextRect.anchoredPosition;
            }
        }

        if (handleRect != null)
        {
            if (captureOnLayout)
            {
                onHandleAnchoredPosition = handleRect.anchoredPosition;
            }
            else
            {
                offHandleAnchoredPosition = handleRect.anchoredPosition;
            }
        }
    }
}
