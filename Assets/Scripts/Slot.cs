using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public Matchable OccupyingMatchable { get; private set; }
    [SerializeField] private MatchManager matchManager;

    public void OccupySlot(Matchable matchable)
    {
        IsOccupied = true;
        OccupyingMatchable = matchable;
        matchable.transform.position = transform.position;
        matchable.OccupyingSlot = this;
        //Debug.Log($" Matchable: {matchable.name} is occupying slot {name}");
    }

    public void ClearSlot()
    {
        IsOccupied = false;
        OccupyingMatchable = null;
    }
}