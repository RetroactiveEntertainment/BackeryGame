using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour
{
    public const int REQUIRED_AMOUNT_TO_MATCH = 3;
    public static MatchManager Instance { get; private set; }
    [SerializeField] private Transform merchPoint, matchEndPoint;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Slot[] ghostSlots;
    [SerializeField] private GameObject[] matchRewards;
    [SerializeField] private Spawner[] frozenFurnace;
    [SerializeField] private LevelDataSO leveldata;
    [SerializeField] private GameUIManager gameUIManager;
    private int levelmatchableCount = 0;
    private int matchedCount = 0;
    private Spawner secondChanceSpawner;
    private int secondChanceMatchableID;

    [Header("Match SFX")]
    [SerializeField] private AudioClip matchableClickSfx;
    [SerializeField] private float matchableClickSfxVolume = 1f;
    [SerializeField] private AudioClip matchCompleteSfx;
    [SerializeField] private float matchCompleteSfxVolume = 1f;
    [Header("Level Music")]
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private float levelMusicVolume = 0.22f;
    [Header("Matchable Animation")]
    [SerializeField] private float matchableMoveDuration = 0.28f;
    [SerializeField] private float matchableShrinkDuration = 0.1f;
    [SerializeField] private float matchableSquashStretchIntensity = 2.3f;
    [Header("Slot Appear Animation")]
    [SerializeField] private float slotAppearDuration = 0.25f;
    [SerializeField] private float slotAppearPopScale = 1.15f;
    [Header("Reward Animation")]
    [SerializeField] private float rewardPopDuration = 0.45f;
    [SerializeField] private float rewardMoveDuration = 0.45f;
    [SerializeField] private float rewardShrinkDuration = 0.25f;
    [SerializeField] private float rewardPopScale = 1f;
    private Coroutine _sortCoroutine;
    private AudioSource levelMusicSource;

    private int _matchCallbackCounter = 0;
    private MatchColor _currentMatchColor = MatchColor.None;

    public float MatchableMoveDuration => matchableMoveDuration;
    public float MatchableAnticipationDuration => matchableMoveDuration * 0.16f;
    public float MatchableStretchDuration => matchableMoveDuration * 0.28f;
    public float MatchableSquashDuration => matchableMoveDuration * 0.25f;
    public float MatchableReboundDuration => matchableMoveDuration * 0.16f;
    public float MatchableShrinkDuration => matchableShrinkDuration;
    public float MatchableSquashStretchIntensity => matchableSquashStretchIntensity;

    public void PlayMatchableClickSfx()
    {
        PlayOneShotSfx(matchableClickSfx, matchableClickSfxVolume, "MatchableClickSfx");
    }

    private Dictionary<MatchColor, List<Matchable>> _matchableInfoDict = new Dictionary<MatchColor, List<Matchable>>()
    {
        { MatchColor.Red, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) },
        { MatchColor.Blue, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) },
        { MatchColor.Green, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) },
        { MatchColor.Yellow, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) },
        { MatchColor.Purple, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) },
        { MatchColor.Orange, new List<Matchable>(REQUIRED_AMOUNT_TO_MATCH) }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        PlayLevelMusic();
    }

    private void OnDisable()
    {
        levelMusicSource?.Stop();
    }

    public int GetEmptySlotIndex()
    {
        for (var index = 0; index < slots.Length; index++)
        {
            var slot = slots[index];
            if (!slot.IsOccupied)
            {
                //Debug.Log($"Found empty slot at index {index}");
                return index;
            }
        }

        return -1;
    }

    public Slot GetSlot(int index) => slots[index];

    public Slot GetFirstEmptySlot()
    {
        int index = GetEmptySlotIndex();
        return index >= 0 ? slots[index] : null;
    }

    private List<Matchable> GetAllMatchablesInSlots()
    {
        List<Matchable> matchableList = new List<Matchable>();
        foreach (var slot in slots)
        {
            if (slot.IsOccupied && slot.OccupyingMatchable) matchableList.Add(slot.OccupyingMatchable);
        }

        return matchableList;
    }

    public void RegisterMatchable(Matchable matchable)
    {
        List<Matchable> targetMatchableList = _matchableInfoDict[matchable.Color];

        if (targetMatchableList.Count == REQUIRED_AMOUNT_TO_MATCH)
        {
            Debug.LogError($"Tried adding while capacity for {matchable.Color} was already reached!");
            return;
        }

        _matchableInfoDict[matchable.Color].Add(matchable);

        matchable.transform.rotation = Quaternion.Euler(0, -120, 0);

        if (targetMatchableList.Count == REQUIRED_AMOUNT_TO_MATCH)
        {
            _matchCallbackCounter = 0;
            _currentMatchColor = matchable.Color;

            foreach (Matchable matchableObject in targetMatchableList)
            {
                matchableObject.OnMatched(merchPoint, OnMatchableDestroyed);
            }
            BreakIce();
            targetMatchableList.Clear();
            if (_sortCoroutine != null) StopCoroutine(_sortCoroutine);
            _sortCoroutine = StartCoroutine(SortBoard());
        }
        else
        {
            Slot targetEmptySlot = GetFirstEmptySlot();
            if (targetEmptySlot == null)
            {
                Debug.LogError("No empty slots found!");
                return;
            }

            targetEmptySlot.OccupySlot(matchable, true, slotAppearDuration, slotAppearPopScale);
            if (_sortCoroutine != null) StopCoroutine(_sortCoroutine);
            _sortCoroutine = StartCoroutine(SortBoard());
        }
    }

    private void OnMatchableDestroyed()
    {
        _matchCallbackCounter++;

        if (_matchCallbackCounter == 1)
        {
            SpawnReward(_currentMatchColor);
        }
    }

    private IEnumerator SortBoard()
    {
        yield return new WaitForSeconds(slotAppearDuration);
        var slottedMatchables = GetAllMatchablesInSlots();

        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }

        for (var index = 0; index < slottedMatchables.Count; index++)
        {
            slots[index].OccupySlot(slottedMatchables[index]);
        }

        _sortCoroutine = null;
    }

    public void RemoveMatchable(Matchable matchable) => _matchableInfoDict[matchable.Color].Remove(matchable);

    public void MoveToGhostSlot()
    {
        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index].IsOccupied && !ghostSlots[index].IsOccupied)
            {
                Matchable targetMatchable = slots[index].OccupyingMatchable;
                RemoveMatchable(targetMatchable);
                targetMatchable.IsTouched = false;
                slots[index].ClearSlot();
                ghostSlots[index].OccupySlot(targetMatchable, true, slotAppearDuration, slotAppearPopScale);
            }
        }
    }

    private void SpawnReward(MatchColor color)
    {
        Debug.Log("Match color = " + color);
        PlayMatchCompleteSfx();
        GameObject rewardDessert = Instantiate(matchRewards[(int)color], merchPoint);

        Vector3 rewardScale = Vector3.one * rewardPopScale;
        Vector3 squashScale = Vector3.Scale(rewardScale, new Vector3(1.35f, 0.55f, 1.35f));
        Vector3 popScale = rewardScale * 1.18f;
        Vector3 upPos = matchEndPoint.position;

        rewardDessert.transform.localScale = squashScale;

        DOTween.Sequence()
            .Append(rewardDessert.transform.DOScale(popScale, rewardPopDuration * 0.55f).SetEase(Ease.OutBack, 2.6f))
            .Append(rewardDessert.transform.DOScale(rewardScale, rewardPopDuration * 0.45f).SetEase(Ease.OutQuad))
            .Append(rewardDessert.transform.DOMove(upPos, rewardMoveDuration).SetEase(Ease.OutQuad))
            .Append(rewardDessert.transform.DOScale(Vector3.zero, rewardShrinkDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                Destroy(rewardDessert);
                CheckGameStatus();
            });
    }

    private void PlayMatchCompleteSfx()
    {
        PlayOneShotSfx(matchCompleteSfx, matchCompleteSfxVolume, "MatchCompleteSfx");
    }

    private void PlayOneShotSfx(AudioClip clip, float volume, string objectName)
    {
        if (clip == null)
            return;

        GameObject audioObject = new GameObject(objectName);
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(audioObject, clip.length);
    }

    private void PlayLevelMusic()
    {
        if (levelMusic == null)
            return;

        GameObject audioObject = new GameObject("LevelMusic");
        audioObject.transform.SetParent(transform, false);
        levelMusicSource = audioObject.AddComponent<AudioSource>();
        levelMusicSource.clip = levelMusic;
        levelMusicSource.volume = levelMusicVolume;
        levelMusicSource.loop = true;
        levelMusicSource.spatialBlend = 0f;
        levelMusicSource.priority = 200;
        levelMusicSource.playOnAwake = false;
        levelMusicSource.Play();

        ReadLevelData();
    }

    private void BreakIce()
    {
        if (frozenFurnace.Count() > 0)
        {
            for (int i = 0; i < frozenFurnace.Count(); i++)
            {
                frozenFurnace[i].RemoveIce();
            }
        }
    }

    public void LostCondition(Spawner spawner, int matchableID)
    {
        gameUIManager.OpenLostPanel();

        secondChanceSpawner = spawner;
        secondChanceMatchableID = matchableID;
    }

    private void LevelEnd()
    {
        var playergold = GoldManager.Instance.GetGold();
        playergold += 5;
        GoldManager.Instance.SetGold(playergold);

        gameUIManager.OpenWinPanel();
    }

    public void TryAgain()
    {
        if (secondChanceSpawner != null)
        {
            secondChanceSpawner.RespawnMatchable(secondChanceMatchableID);
        }

    }

    private void ReadLevelData()
    {
        Time.timeScale = 1f;
        foreach (var item in leveldata.LevelData)
        {
            levelmatchableCount += item.PrefabsToSpawn.Count();
        }

        levelmatchableCount = levelmatchableCount / 3;
        Debug.Log("Level matchable count = " + levelmatchableCount);
    }

    private void CheckGameStatus()
    {
        ++matchedCount;
        Debug.Log("Matched object = " + matchedCount);

        if (levelmatchableCount > matchedCount)
            return;

        LevelEnd();
    }
}

/* New Slot Power Logic
 * For every matchable -> If the ghost slot in front of it is empty, move it there */
