using DG.Tweening;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public Matchable OccupyingMatchable { get; private set; }

    public void OccupySlot(Matchable matchable, bool animate = false, float moveDuration = 0f, float popScale = 1f)
    {
        IsOccupied = true;
        OccupyingMatchable = matchable;
        matchable.OccupyingSlot = this;

        matchable.transform.DOKill();
        if (!animate || moveDuration <= 0f)
        {
            matchable.transform.position = transform.position;
            return;
        }

        Vector3 originalScale = matchable.BaseScale;
        DOTween.Sequence()
            .Append(matchable.transform.DOMove(transform.position, moveDuration).SetEase(Ease.OutBack))
            .Join(matchable.transform.DOScale(originalScale * popScale, moveDuration * 0.65f).SetEase(Ease.OutQuad))
            .Append(matchable.transform.DOScale(originalScale, moveDuration * 0.35f).SetEase(Ease.OutQuad));
        //Debug.Log($" Matchable: {matchable.name} is occupying slot {name}");
    }

    public void ClearSlot()
    {
        IsOccupied = false;
        OccupyingMatchable = null;
    }
}
