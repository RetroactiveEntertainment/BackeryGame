using System;
using UnityEngine;
using UnityEngine.Splines;

public class Matchable : MonoBehaviour, IMatchable
{
    [SerializeField] private SplineAnimate splineAnimate;
    private MatchManager m_matchManager;
    public Slot OccupyingSlot { get; set; }

    private void Start()
    {
        splineAnimate.Completed += OnSplineAnimateCompleted;
    }

    public void OnDestroy()
    {
        if (!IsTouched)
        {
            Debug.Log("You lost a point!");
            return;
        }

        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();
        m_matchManager.RemoveMatchable(this);
    }

    [field: SerializeField] public MatchColor Color { get; set; }
    public bool IsTouched { get; set; } = false;

    public void OnLost()
    {
        if (IsTouched) return;
        Debug.Log("You lost a point!");
    }

    public void OnMatched()
    {
        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();
        //m_matchManager.RemoveMatchable(this);

        Destroy(gameObject);
    }


    public void OnTouched()
    {
        if (IsTouched)
            return;

        splineAnimate.Pause();
        RemoveSplineAnimate();
        m_matchManager.RegisterMatchable(this);
        IsTouched = true;
    }

    private void OnSplineAnimateCompleted()
    {
        EventBus<LosePointEvent>.Raise(new LosePointEvent());
        Destroy(gameObject);
    }

    public void Initialize(MatchManager matchManager, SplineContainer splineContainer, float splineCompleteDuration)
    {
        m_matchManager = matchManager;
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
    public bool IsTouched { get; set; }
    public void OnTouched();
}

public interface IMatchable : ITouchable
{
    public MatchColor Color { get; set; }
    public void OnMatched();
    public void OnLost();
}