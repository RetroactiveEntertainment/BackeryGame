using DG.Tweening;
using Dreamteck.Splines;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class Matchable : MonoBehaviour, IMatchable
{
    [SerializeField] private SplineAnimate splineAnimate;
    [SerializeField] private SplineFollower splineFollower;

    private MatchManager m_matchManager;
    private bool _destroyedByMatch;
    private Tween _merchTween;
    public Slot OccupyingSlot { get; set; }

    private void Start()
    {
        splineAnimate.Completed += OnSplineAnimateCompleted;
    }

    public void OnDestroy()
    {
        _merchTween?.Kill();
        if (_destroyedByMatch)
            return;
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

    public void OnMatched(Transform point, Action onCompleteCallback = null)
    {
        _destroyedByMatch = true;
        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();

        Vector3 targetPos = point.position;
        _merchTween = DOTween.Sequence()
            .Append(transform.DOMove(targetPos, 0.5f).SetEase(Ease.InOutQuad))
            .Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                onCompleteCallback?.Invoke();
                Destroy(gameObject);
            });
    }

    public void OnTouched()
    {
        if (IsTouched)
            return;
        splineFollower.follow = false;
       // splineAnimate.Pause();
        RemoveSplineAnimate();
        m_matchManager.RegisterMatchable(this);
        IsTouched = true;
    }

    private void OnSplineAnimateCompleted()
    {
        EventBus<LosePointEvent>.Raise(new LosePointEvent());
        Destroy(gameObject);
    }

    public void Initialize(MatchManager matchManager, SplineContainer splineContainer, float splineCompleteDuration, SplineComputer splineComputer)
    {
        m_matchManager = matchManager;
        splineAnimate.Container = splineContainer;
        splineAnimate.Duration = splineCompleteDuration;
        splineFollower.spline = splineComputer;
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
    public void OnMatched(Transform point, Action onCompleteCallback = null);
    public void OnLost();
}