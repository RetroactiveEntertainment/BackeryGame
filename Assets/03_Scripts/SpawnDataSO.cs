using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnDataSO", menuName = "Scriptable Objects/SpawnDataSO")]
public class SpawnDataSO : ScriptableObject
{
    public List<GameObject> PrefabsToSpawn = new();
}