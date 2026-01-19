using System;
using UnityEngine;
using UnityEngine.Splines;

public class Matchable : MonoBehaviour, ITouchable
{
    private MatchManager matchManager;
    public MatchColor Color;
    [SerializeField] private SplineAnimate splineAnimate;
    public Slot OccupyingSlot { get; set; }

    private void Start()
    {
        matchManager = MatchManager.Instance;
    }

    public void OnDestroy()
    {
        OccupyingSlot.ClearSlot();
        matchManager.RemoveMatchable(this);
    }


    public void OnTouched()
    {
        RemoveSplineAnimate();
        matchManager.RegisterMatchable(this);
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