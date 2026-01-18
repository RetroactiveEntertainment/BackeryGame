using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    public void OccupySlot()
    {
        IsOccupied = true;
    }

    public void ClearSlot()
    {
        IsOccupied = false;
    }
}