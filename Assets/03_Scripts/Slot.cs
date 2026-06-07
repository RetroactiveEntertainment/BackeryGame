using DG.Tweening;
using System;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public Matchable OccupyingMatchable { get; private set; }

    public void OccupySlot(Matchable matchable, bool animate = false, float moveDuration = 0f, float popScale = 1f, Action onPlaced = null, float onPlacedLeadTime = 0f)
    {
        if (matchable == null)
        {
            ClearSlot();
            return;
        }

        if (matchable.OccupyingSlot != null && matchable.OccupyingSlot != this)
            matchable.OccupyingSlot.ClearSlot();

        if (OccupyingMatchable != null && OccupyingMatchable != matchable && OccupyingMatchable.OccupyingSlot == this)
            OccupyingMatchable.OccupyingSlot = null;

        IsOccupied = true;
        OccupyingMatchable = matchable;
        matchable.OccupyingSlot = this;

        matchable.transform.DOKill();
        if (!animate || moveDuration <= 0f)
        {
            matchable.transform.position = transform.position;
            onPlaced?.Invoke();
            return;
        }

        Vector3 originalScale = matchable.BaseScale;
        bool placedCallbackInvoked = false;
        void InvokePlacedCallback()
        {
            if (placedCallbackInvoked)
                return;

            placedCallbackInvoked = true;
            onPlaced?.Invoke();
        }

        float callbackTime = Mathf.Max(0f, moveDuration - Mathf.Max(0f, onPlacedLeadTime));

        DOTween.Sequence()
            .Append(matchable.transform.DOMove(transform.position, moveDuration).SetEase(Ease.OutBack))
            .Join(matchable.transform.DOScale(originalScale * popScale, moveDuration * 0.65f).SetEase(Ease.OutQuad))
            .InsertCallback(callbackTime, InvokePlacedCallback)
            .Append(matchable.transform.DOScale(originalScale, moveDuration * 0.35f).SetEase(Ease.OutQuad))
            .OnKill(() =>
            {
                if (matchable == null)
                    return;

                if (!placedCallbackInvoked && Vector3.Distance(matchable.transform.position, transform.position) <= 0.01f)
                    InvokePlacedCallback();
            });
        //Debug.Log($" Matchable: {matchable.name} is occupying slot {name}");
    }

    public void ClearSlot()
    {
        if (OccupyingMatchable != null && OccupyingMatchable.OccupyingSlot == this)
            OccupyingMatchable.OccupyingSlot = null;

        IsOccupied = false;
        OccupyingMatchable = null;
    }
}
