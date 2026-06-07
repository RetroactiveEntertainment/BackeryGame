using DG.Tweening;
using Dreamteck.Splines;
using System;
using UnityEngine;

public class Matchable : MonoBehaviour, IMatchable
{
    //[SerializeField] private SplineAnimate splineAnimate;
    [SerializeField] private SplineFollower splineFollower;

    private MatchManager m_matchManager;
    private Spawner spawner;
    private int matchableID;
    private bool _destroyedByMatch;
    private bool _lostReported;
    private bool _isRegisteringTouch;
    private float _touchAllowedAtTime;
    private Tween _merchTween;
    private Vector3 baseScale = Vector3.one;
    public Slot OccupyingSlot { get; set; }

    public void OnDestroy()
    {
        _merchTween?.Kill();

        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();

        if (IsTouched || _destroyedByMatch)
            m_matchManager?.RemoveMatchable(this);
    }

    [field: SerializeField] public MatchColor Color { get; set; }
    public bool IsTouched { get; set; } = false;

    public void OnLost()
    {
        ReportLost();
    }

    public void OnMatched(Transform point, Action onCompleteCallback = null)
    {
        _destroyedByMatch = true;

        if (OccupyingSlot)
            OccupyingSlot.ClearSlot();

        Vector3 targetPos = point.position;
        transform.localScale = baseScale;
        Transform stretchRoot = CreateDirectionalStretchRoot(targetPos);
        Vector3 originalScale = stretchRoot.localScale;
        Vector3 stretchScale = Vector3.Scale(originalScale, AmplifyScaleMultiplier(GetDirectionalStretchMultiplier()));
        Vector3 squashScale = Vector3.Scale(originalScale, AmplifyScaleMultiplier(GetDirectionalSquashMultiplier()));

        _merchTween = DOTween.Sequence()
            .Append(stretchRoot.DOScale(squashScale, m_matchManager.MatchableAnticipationDuration).SetEase(Ease.InQuad))
            .Append(stretchRoot.DOScale(stretchScale, m_matchManager.MatchableStretchDuration).SetEase(Ease.OutBack, 2.4f))
            .Join(stretchRoot.DOMove(targetPos, m_matchManager.MatchableMoveDuration).SetEase(Ease.InQuad))
            .Append(stretchRoot.DOScale(squashScale, m_matchManager.MatchableSquashDuration).SetEase(Ease.OutQuad))
            .Append(stretchRoot.DOScale(originalScale, m_matchManager.MatchableReboundDuration).SetEase(Ease.OutBack, 2.8f))
            .Append(stretchRoot.DOScale(Vector3.zero, m_matchManager.MatchableShrinkDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                onCompleteCallback?.Invoke();
                Destroy(stretchRoot.gameObject);
            });
    }

    private Vector3 AmplifyScaleMultiplier(Vector3 multiplier)
    {
        return Vector3.one + ((multiplier - Vector3.one) * Mathf.Max(0f, m_matchManager.MatchableSquashStretchIntensity));
    }

    private Vector3 GetDirectionalStretchMultiplier()
    {
        return new Vector3(0.55f, 0.55f, 1.6f);
    }

    private Vector3 GetDirectionalSquashMultiplier()
    {
        return new Vector3(1.55f, 1.55f, 0.45f);
    }

    private Transform CreateDirectionalStretchRoot(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        GameObject root = new GameObject($"{name}_MatchStretchRoot");
        Transform rootTransform = root.transform;
        rootTransform.position = transform.position;
        rootTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        rootTransform.localScale = Vector3.one;

        Transform originalParent = transform.parent;
        rootTransform.SetParent(originalParent, true);
        transform.SetParent(rootTransform, true);

        return rootTransform;
    }

    public bool OnTouched()
    {
        if (IsTouched || _isRegisteringTouch || Time.unscaledTime < _touchAllowedAtTime || m_matchManager == null)
            return false;

        _isRegisteringTouch = true;
        bool wasFollowing = splineFollower != null && splineFollower.follow;

        try
        {
            if (splineFollower != null)
                splineFollower.follow = false;

            if (!m_matchManager.RegisterMatchable(this, out bool completedMatch))
            {
                if (splineFollower != null)
                    splineFollower.follow = wasFollowing;
                return false;
            }

            if (!completedMatch)
                HapticFeedback.PlaySelection();

            IsTouched = true;
            return true;
        }
        catch (Exception exception)
        {
            if (splineFollower != null)
                splineFollower.follow = wasFollowing;

            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            _isRegisteringTouch = false;
        }
    }

    public void OnSplineEnd()
    {
        ReportLost();
        Destroy(gameObject);
    }

    private void OnSplineAnimateCompleted()
    {
        ReportLost();
        Destroy(gameObject);
    }

    public void Initialize(MatchManager matchManager, SplineComputer splineComputer, Spawner spawnerID, int ID)
    {
        spawner = spawnerID;
        matchableID = ID;
        m_matchManager = matchManager;
        splineFollower.spline = splineComputer;
        IsTouched = false;
        _destroyedByMatch = false;
        _lostReported = false;
        _isRegisteringTouch = false;
        _touchAllowedAtTime = 0f;
        baseScale = transform.localScale;
    }

    public void DelayTouch(float delay)
    {
        _touchAllowedAtTime = Time.unscaledTime + Mathf.Max(0f, delay);
    }

    private void ReportLost()
    {
        if (_lostReported || IsTouched || _destroyedByMatch)
            return;

        _lostReported = true;
        _touchAllowedAtTime = float.PositiveInfinity;

        Debug.Log("You lost a point!");
        m_matchManager?.LostCondition(spawner, matchableID);
    }

    public Vector3 BaseScale => baseScale;
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
    public bool OnTouched();
}

public interface IMatchable : ITouchable
{
    public MatchColor Color { get; set; }
    public void OnMatched(Transform point, Action onCompleteCallback = null);
    public void OnLost();
}
