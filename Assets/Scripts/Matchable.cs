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
        splineAnimate.Completed += OnSplineAnimateCompleted;
    }

    public void OnDestroy()
    {
        if (!_isTouched)
        {
            Debug.Log("You lost a point!");
            return;
        }

        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();
        matchManager.RemoveMatchable(this);
    }

    private void OnSplineAnimateCompleted()
    {
        EventBus<LosePointEvent>.Raise(new LosePointEvent());
        Destroy(gameObject);
    }


    public void OnTouched()
    {
        if (_isTouched)
            return;

        splineAnimate.Pause();
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