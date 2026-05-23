using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchableColorSorter : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Slot[] ghostSlots;
    
    private List<Matchable> _lastKnownSlotsMatchables = new List<Matchable>();
    private List<Matchable> _lastKnownGhostMatchables = new List<Matchable>();

    private void LateUpdate()
    {
        HandleSorting(slots, ref _lastKnownSlotsMatchables);
        HandleSorting(ghostSlots, ref _lastKnownGhostMatchables);
    }

    private void HandleSorting(Slot[] targetSlots, ref List<Matchable> lastKnown)
    {
        if (targetSlots == null || targetSlots.Length == 0) return;

        // 1. Get all matchables currently in slots
        List<Matchable> currentMatchables = new List<Matchable>();
        foreach (var slot in targetSlots)
        {
            if (slot.IsOccupied && slot.OccupyingMatchable != null)
            {
                currentMatchables.Add(slot.OccupyingMatchable);
            }
        }

        // 2. Check if they have changed since last check
        if (!AreMatchableListsEqual(currentMatchables, lastKnown))
        {
            // 3. If changed, sort them by color
            SortMatchables(targetSlots, currentMatchables);
            
            // 4. Update the last known state after sorting
            lastKnown = GetCurrentMatchablesState(targetSlots);
        }
    }

    private List<Matchable> GetCurrentMatchablesState(Slot[] targetSlots)
    {
        List<Matchable> list = new List<Matchable>();
        foreach (var slot in targetSlots)
        {
            if (slot.IsOccupied && slot.OccupyingMatchable != null)
                list.Add(slot.OccupyingMatchable);
        }
        return list;
    }

    private bool AreMatchableListsEqual(List<Matchable> list1, List<Matchable> list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    private void SortMatchables(Slot[] targetSlots, List<Matchable> matchables)
    {
        if (matchables.Count <= 1) return;

        // Group matchables by color
        var sortedMatchables = matchables.OrderBy(m => m.Color).ToList();

        // Check if they are already in this order in the slots
        if (AreMatchableListsEqual(matchables, sortedMatchables)) return;

        // Clear all target slots first
        foreach (var slot in targetSlots)
        {
            slot.ClearSlot();
        }

        // Fill slots in sorted order
        for (int i = 0; i < sortedMatchables.Count; i++)
        {
            if (i < targetSlots.Length)
            {
                targetSlots[i].OccupySlot(sortedMatchables[i]);
            }
        }
    }
}
