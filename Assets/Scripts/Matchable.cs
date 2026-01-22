using System;
using UnityEngine;
using UnityEngine.Splines;

public class Matchable : MonoBehaviour, ITouchable
{
    private MatchManager matchManager;
    public MatchColor Color;
    [SerializeField] private SplineAnimate splineAnimate;
    public Slot OccupyingSlot { get; set; }
    private bool _isTouched = false;

    private void Start()
    {
        matchManager = MatchManager.Instance;
    }

    public void OnDestroy()
    {
        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();
        matchManager.RemoveMatchable(this);
    }


    public void OnTouched()
    {
        if (_isTouched)
            return;
        RemoveSplineAnimate();
        matchManager.RegisterMatchable(this);
        _isTouched = true;
    }

    public void Initialize(SplineContainer splineContainer, float splineCompleteDuration)
    {
        splineAnimate.Container = splineContainer;
        splineAnimate.Duration = splineCompleteDuration;
    }


    public void RemoveSplineAnimate() => Destroy(splineAnimate);
}

public enum MatchColor
{
    None = -1,
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Orange
}

public interface ITouchable
{
    public void OnTouched();
}