using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public const int REQUIRED_AMOUNT_TO_MATCH = 3;
    public static MatchManager Instance { get; private set; }
    [SerializeField] private Transform merchPoint;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Slot[] ghostSlots;
    private Coroutine _sortCoroutine;

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


        if (targetMatchableList.Count == REQUIRED_AMOUNT_TO_MATCH)
        {
            foreach (Matchable matchableObject in targetMatchableList)
            {
                matchableObject.OnMatched(merchPoint);
            }

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

            targetEmptySlot.OccupySlot(matchable);
            if (_sortCoroutine != null) StopCoroutine(_sortCoroutine);
            _sortCoroutine = StartCoroutine(SortBoard());
        }
    }

    private IEnumerator SortBoard()
    {
        yield return null; // Wait for the next frame so the objects are definitely destroyed. 
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
                ghostSlots[index].OccupySlot(targetMatchable);
            }
        }
    }
}

/* New Slot Power Logic
 * For every matchable -> If the ghost slot in front of it is empty, move it there */