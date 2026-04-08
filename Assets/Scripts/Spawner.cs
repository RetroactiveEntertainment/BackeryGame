using UnityEngine;
using UnityEngine.Splines;
using Dreamteck.Splines;

public class Spawner : MonoBehaviour
{
    [SerializeField] private int spawnerIndex;
    [SerializeField] private float spawnRate;
    [SerializeField] private float splineCompletionTime = 5f;
    [SerializeField] private float initialSpawnDelay;
    [SerializeField] private LevelDataSO levelData;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private SplineComputer splineComputer;
    [SerializeField] private Animator animator;


    [SerializeField] private MatchManager matchManager;
    private int _spawnCount = 0;

    private void Start()
    {
        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer not assigned");
            return;
        }

        InvokeRepeating(nameof(Spawn), initialSpawnDelay, spawnRate);
    }

    private void Spawn()
    {
        var targetList = levelData.LevelData[spawnerIndex].PrefabsToSpawn;
        if (_spawnCount >= targetList.Count)
            return;

        //Debug.Log($"Spawning {targetList[_spawnCount].name}");
        animator.Play("furnaceShot");
        GameObject spawnedGo = Instantiate(targetList[_spawnCount]);
        //Debug.Log($"Spawned {spawnedGo.name}");

        if (!spawnedGo.TryGetComponent(out Matchable matchable))
        {
            Debug.LogError($"No Matchable component found on {spawnedGo.name}");
            return;
        }

        matchable.Initialize(matchManager, splineContainer, splineCompletionTime, splineComputer);
        _spawnCount++;

        spawnedGo.SetActive(true);
    }
}