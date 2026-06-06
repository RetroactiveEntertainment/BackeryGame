using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class ButtonPressAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float clickPopScale = 1.08f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.18f;
    [SerializeField] private Color pressedTextColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    private Vector3 originalScale;
    private TMP_Text[] childTexts;
    private Color[] originalTextColors;
    private Color[] originalFaceColors;
    private bool isPressed;

    private void Awake()
    {
        originalScale = transform.localScale;
        CacheTextColors();
    }

    public void CopySettingsFrom(ButtonPressAnimator source)
    {
        if (source == null)
            return;

        pressedScale = source.pressedScale;
        clickPopScale = source.clickPopScale;
        pressDuration = source.pressDuration;
        releaseDuration = source.releaseDuration;
        pressedTextColor = source.pressedTextColor;
    }

    private void CacheTextColors()
    {
        childTexts = GetComponentsInChildren<TMP_Text>(true);
        originalTextColors = new Color[childTexts.Length];
        originalFaceColors = new Color[childTexts.Length];

        for (int i = 0; i < childTexts.Length; i++)
        {
            TMP_Text text = childTexts[i];
            originalTextColors[i] = text.color;

            Material fontMaterial = text.fontMaterial;
            originalFaceColors[i] = fontMaterial != null && fontMaterial.HasProperty(ShaderUtilities.ID_FaceColor)
                ? fontMaterial.GetColor(ShaderUtilities.ID_FaceColor)
                : text.color;
        }
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
        RestoreTextColors();
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        AnimateScale(originalScale * pressedScale, pressDuration, Ease.OutQuad);
        SetTextColor(pressedTextColor, pressedTextColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
            return;

        isPressed = false;
        AnimateScale(originalScale, releaseDuration, Ease.OutBack);
        RestoreTextColors();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed)
            return;

        isPressed = false;
        AnimateScale(originalScale, releaseDuration, Ease.OutBack);
        RestoreTextColors();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RestoreTextColors();
        transform.DOKill();
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(originalScale * clickPopScale, releaseDuration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(transform.DOScale(originalScale, releaseDuration).SetEase(Ease.OutBack));
    }

    private void AnimateScale(Vector3 targetScale, float duration, Ease ease)
    {
        transform.DOKill();
        transform.DOScale(targetScale, duration).SetEase(ease);
    }

    private void SetTextColor(Color vertexColor, Color faceColor)
    {
        if (childTexts == null)
            return;

        for (int i = 0; i < childTexts.Length; i++)
        {
            ApplyTextColor(childTexts[i], vertexColor, faceColor);
        }
    }

    private void RestoreTextColors()
    {
        if (childTexts == null || originalTextColors == null || originalFaceColors == null)
            return;

        for (int i = 0; i < childTexts.Length && i < originalTextColors.Length && i < originalFaceColors.Length; i++)
        {
            ApplyTextColor(childTexts[i], originalTextColors[i], originalFaceColors[i]);
        }
    }

    private static void ApplyTextColor(TMP_Text text, Color vertexColor, Color faceColor)
    {
        if (text == null)
            return;

        text.color = vertexColor;

        Material fontMaterial = text.fontMaterial;
        if (fontMaterial != null && fontMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
        {
            fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, faceColor);
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
