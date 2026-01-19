using System;
using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public const int REQUIRED_AMOUNT_TO_MATCH = 3;
    public static MatchManager Instance { get; private set; }
    [SerializeField] private Slot[] slots;

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
            if (!slot.IsOccupied) return index;
        }

        return -1;
    }

    public Slot GetSlot(int index) => slots[index];

    public Slot GetFirstEmptySlot()
    {
        int index = GetEmptySlotIndex();
        return index >= 0 ? slots[index] : null;
    }

    public void RegisterMatchable(Matchable matchable)
    {
        List<Matchable> targetMatchableList = _matchableInfoDict[matchable.Color];

        if (targetMatchableList.Count == targetMatchableList.Capacity)
        {
            Debug.LogError($"Tried adding while capacity for {matchable.Color} was already reached!");
            return;
        }

        _matchableInfoDict[matchable.Color].Add(matchable);
        Transform targetSlotTransform = GetFirstEmptySlot().transform;
        matchable.transform.position = targetSlotTransform.position;

        if (targetMatchableList.Count == targetMatchableList.Capacity)
        {
            foreach (Matchable matchableObject in targetMatchableList)
            {
                Destroy(matchableObject.gameObject);
            }
        }
    }

    public void RemoveMatchable(Matchable matchable)
    {
        _matchableInfoDict[matchable.Color].Remove(matchable);
    }
}