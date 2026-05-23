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
    [SerializeField] private ParticleSystem smokeVfx;
    [SerializeField] private int arrowMaterialIndex = 1;
    [SerializeField] private float arrowTextureScrollSpeed = 2f;


    [SerializeField] private MatchManager matchManager;
    private int _spawnCount = 0;
    private Material arrowMaterial;

    private void Start()
    {
        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer not assigned");
            return;
        }

        CacheArrowMaterial();
        InvokeRepeating(nameof(Spawn), initialSpawnDelay, spawnRate);
       

    }
    private void Update()
    {
        MoveArrowChannel();
    }
    private void MoveArrowChannel()
    {
        if (arrowMaterial == null)
            return;

        Vector2 currentOffset = arrowMaterial.HasProperty("_BaseMap")
            ? arrowMaterial.GetTextureOffset("_BaseMap")
            : arrowMaterial.GetTextureOffset("_MainTex");
        currentOffset.y -= arrowTextureScrollSpeed * Time.deltaTime;
        SetArrowTextureOffset(currentOffset);
    }

    private void CacheArrowMaterial()
    {
        if (splineComputer == null)
            return;

        MeshRenderer meshRenderer = splineComputer.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        Material[] materials = meshRenderer.materials;
        if (arrowMaterialIndex < 0 || arrowMaterialIndex >= materials.Length)
            return;

        arrowMaterial = materials[arrowMaterialIndex];
    }

    private void SetArrowTextureOffset(Vector2 offset)
    {
        if (arrowMaterial.HasProperty("_BaseMap"))
        {
            arrowMaterial.SetTextureOffset("_BaseMap", offset);
        }

        if (arrowMaterial.HasProperty("_MainTex"))
        {
            arrowMaterial.SetTextureOffset("_MainTex", offset);
        }
    }

    private void Spawn()
    {
        var targetList = levelData.LevelData[spawnerIndex].PrefabsToSpawn;
        if (_spawnCount >= targetList.Count)
            return;

        //Debug.Log($"Spawning {targetList[_spawnCount].name}");
        animator.Play("furnaceShot");
        smokeVfx.Play();
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
