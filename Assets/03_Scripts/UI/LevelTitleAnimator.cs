using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class LevelTitleAnimator : MonoBehaviour
{
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private float enterDuration = 0.45f;
    [SerializeField] private float punchDuration = 0.35f;
    [SerializeField] private float startYOffset = 45f;
    [SerializeField] private float startScale = 0.65f;
    [SerializeField] private float punchScale = 0.16f;
    [SerializeField] private bool loopAttention = true;
    [SerializeField] private float loopInterval = 1.4f;
    [SerializeField] private float loopPunchScale = 0.1f;
    [SerializeField] private float loopPunchDuration = 0.35f;

    private RectTransform rectTransform;
    private TextMeshProUGUI label;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Sequence loopSequence;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        label = GetComponent<TextMeshProUGUI>();

        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
        }
    }

    private IEnumerator Start()
    {
        if (rectTransform == null)
            yield break;

        yield return new WaitForSeconds(startDelay);

        rectTransform.DOKill();
        if (label != null)
        {
            label.DOKill();
            Color color = label.color;
            color.a = 0f;
            label.color = color;
        }

        rectTransform.anchoredPosition = originalPosition + new Vector2(0f, startYOffset);
        rectTransform.localScale = originalScale * startScale;

        Sequence sequence = DOTween.Sequence();
        if (label != null)
        {
            sequence.Join(label.DOFade(1f, enterDuration * 0.75f));
        }

        sequence.Join(rectTransform.DOAnchorPos(originalPosition, enterDuration).SetEase(Ease.OutBack));
        sequence.Join(rectTransform.DOScale(originalScale, enterDuration).SetEase(Ease.OutBack));
        sequence.Append(rectTransform.DOPunchScale(Vector3.one * punchScale, punchDuration, 6, 0.7f));
        sequence.OnComplete(StartAttentionLoop);
    }

    private void OnDisable()
    {
        loopSequence?.Kill();
        if (rectTransform != null)
        {
            rectTransform.DOKill();
        }

        if (label != null)
        {
            label.DOKill();
        }
    }

    private void StartAttentionLoop()
    {
        if (!loopAttention || rectTransform == null)
            return;

        loopSequence?.Kill();
        loopSequence = DOTween.Sequence();
        loopSequence.AppendInterval(loopInterval);
        loopSequence.Append(rectTransform.DOPunchScale(Vector3.one * loopPunchScale, loopPunchDuration, 5, 0.7f));
        loopSequence.Join(rectTransform.DOAnchorPosY(originalPosition.y + 5f, loopPunchDuration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
        loopSequence.SetLoops(-1);
    }
}
