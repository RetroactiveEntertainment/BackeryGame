using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GoldCoinFlyToTopBarAnimator : MonoBehaviour
{
    private const float GoldFillStartDistancePixels = 96f;

    [Header("References")]
    [SerializeField] private RectTransform goldTargetIcon;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Camera coinCamera;
    [SerializeField] private RectTransform sourcePoint;

    [Header("Coin")]
    [SerializeField] private Vector3 coinScale = Vector3.one * 0.2f;
    [SerializeField, Range(0.05f, 1f)] private float targetCoinScaleMultiplier = 0.25f;
    [SerializeField] private float distanceFromCamera = 10f;
    [SerializeField] private int coinCount = 3;

    [Header("Flight")]
    [SerializeField] private float scatterRadiusPixels = 115f;
    [SerializeField] private float arcHeightPixels = 150f;
    [SerializeField] private float arcHeightWorld = 1.25f;
    [SerializeField] private float collectDelay = 0.2f;
    [SerializeField] private float stagger = 0.1f;
    [SerializeField] private float spreadDuration = 0.22f;
    [SerializeField] private float flyDuration = 0.62f;

    [Header("Target")]
    [SerializeField] private float targetPunchScale = 0.16f;
    [SerializeField] private float targetPunchDuration = 0.18f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip goldFillSfx;
    [SerializeField] private AudioClip goldFinishSfx;
    [SerializeField, Range(0f, 1f)] private float goldFillVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float goldFinishVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float goldFillLoopPitch = 1f;

    private readonly List<Tween> activeTweens = new List<Tween>();
    private int activeCoinFlights;
    private bool goldFillLoopPlaying;

    private Camera CoinCamera => coinCamera != null ? coinCamera : Camera.main;

    private void OnDisable()
    {
        KillActiveTweens();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (goldFillSfx == null)
            goldFillSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/01_Art/Sound FX/Coin/Gold_SFX1.wav");

        if (goldFinishSfx == null)
            goldFinishSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/01_Art/Sound FX/Coin/Gold_SFX2.mp3");
    }
#endif

    public void Play(int coinAmount)
    {
        RectTransform source = sourcePoint != null ? sourcePoint : transform as RectTransform;
        if (source == null)
            return;
        coinCount = coinAmount;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(source), source.position);
        PlayFromScreenPosition(screenPosition, coinCount);
    }

    public void PlayFromWorldPoint(Vector3 worldPoint, Camera sourceCamera, int coinCount)
    {
        Camera camera = sourceCamera != null ? sourceCamera : CoinCamera;
        if (camera == null)
            return;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
        PlayFromScreenPosition(screenPosition, coinCount);
    }

    public void PlayFromScreenPosition(Vector2 sourceScreenPosition, int coinCount)
    {
        Camera camera = CoinCamera;
        if (coinPrefab == null || goldTargetIcon == null || camera == null || coinCount <= 0)
            return;

        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), goldTargetIcon.position);

        for (int i = 0; i < coinCount; i++)
        {
            SpawnCoin(camera, sourceScreenPosition, targetScreenPosition, i);
        }
    }

    private void SpawnCoin(Camera camera, Vector2 sourceScreenPosition, Vector2 targetScreenPosition, int index)
    {
        Vector2 scatterScreenPosition = sourceScreenPosition + Random.insideUnitCircle * scatterRadiusPixels;
        Vector2 arcScreenPosition = Vector2.Lerp(scatterScreenPosition, targetScreenPosition, 0.55f) + Vector2.up * arcHeightPixels;

        Vector3 sourceWorldPosition = ScreenToWorld(camera, sourceScreenPosition);
        Vector3 scatterWorldPosition = ScreenToWorld(camera, scatterScreenPosition);
        Vector3 arcWorldPosition = ScreenToWorld(camera, arcScreenPosition) + camera.transform.up * arcHeightWorld;
        Vector3 targetWorldPosition = ScreenToWorld(camera, targetScreenPosition);

        GameObject coin = Instantiate(coinPrefab, sourceWorldPosition, Quaternion.identity);
        Transform coinTransform = coin.transform;
        coinTransform.localScale = Vector3.zero;
        activeCoinFlights++;

        Sequence sequence = DOTween.Sequence();
        sequence.SetDelay(index * stagger);
        sequence.Append(coinTransform.DOScale(coinScale, spreadDuration).SetEase(Ease.OutBack));
        sequence.Join(coinTransform.DOMove(scatterWorldPosition, spreadDuration).SetEase(Ease.OutQuad));
        sequence.AppendInterval(collectDelay);
        Tween flyTween = coinTransform.DOPath(
            new[] { arcWorldPosition, targetWorldPosition },
            flyDuration,
            PathType.CatmullRom,
            PathMode.Full3D)
            .SetEase(Ease.InOutCubic)
            .OnUpdate(() =>
            {
                Vector2 coinScreenPosition = camera.WorldToScreenPoint(coinTransform.position);
                if (!goldFillLoopPlaying && Vector2.Distance(coinScreenPosition, targetScreenPosition) <= GoldFillStartDistancePixels)
                    PlayGoldFillLoop();
            });

        sequence.Append(flyTween);
        sequence.Join(coinTransform.DORotate(new Vector3(0f, 720f, -360f), flyDuration, RotateMode.FastBeyond360).SetRelative());
        sequence.Join(coinTransform.DOScale(coinScale * targetCoinScaleMultiplier, flyDuration).SetEase(Ease.InQuad));
        sequence.AppendCallback(() =>
        {
            PlayGoldFillLoop();
            PunchTargetIcon();
            Destroy(coin);
            activeCoinFlights = Mathf.Max(0, activeCoinFlights - 1);
            if (activeCoinFlights == 0)
            {
                StopGoldFillLoop();
                PlaySfx(goldFinishSfx, goldFinishVolume);
            }
        });
        sequence.OnKill(() => activeTweens.Remove(sequence));

        activeTweens.Add(sequence);
    }

    private Vector3 ScreenToWorld(Camera camera, Vector2 screenPosition)
    {
        return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
    }

    private Camera GetCanvasCamera()
    {
        Canvas canvas = goldTargetIcon.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private void PunchTargetIcon()
    {
        goldTargetIcon.DOKill();
        goldTargetIcon.localScale = Vector3.one;
        goldTargetIcon.DOPunchScale(Vector3.one * targetPunchScale, targetPunchDuration, 1, 0.45f);
    }

    private void KillActiveTweens()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = activeTweens[i];
            tween?.Kill();
        }

        activeTweens.Clear();
        activeCoinFlights = 0;
        StopGoldFillLoop();
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        AudioSource source = GetSfxSource();
        if (source == null)
            return;

        source.volume = 1f;
        source.PlayOneShot(clip, volume);
    }

    private void PlayGoldFillLoop()
    {
        if (goldFillSfx == null || goldFillLoopPlaying)
            return;

        AudioSource source = GetSfxSource();
        if (source == null)
            return;

        source.clip = goldFillSfx;
        source.volume = goldFillVolume;
        source.pitch = goldFillLoopPitch;
        source.loop = true;
        source.time = 0f;
        source.Play();
        goldFillLoopPlaying = true;
    }

    private void StopGoldFillLoop()
    {
        if (!goldFillLoopPlaying || sfxSource == null)
            return;

        sfxSource.Stop();
        sfxSource.volume = 1f;
        sfxSource.loop = false;
        sfxSource.pitch = 1f;
        sfxSource.clip = null;
        goldFillLoopPlaying = false;
    }

    private AudioSource GetSfxSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        return sfxSource;
    }
}
