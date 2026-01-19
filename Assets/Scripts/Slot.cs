using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    public void OccupySlot(Matchable matchable)
    {
        IsOccupied = true;
        matchable.transform.position = transform.position;
        matchable.OccupyingSlot = this;
    }

    public void ClearSlot()
    {
        IsOccupied = false;
    }
}