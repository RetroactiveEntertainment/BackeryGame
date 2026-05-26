using UnityEngine;
using UnityEngine.Splines;
using Dreamteck.Splines;
using TMPro;

public class Spawner : MonoBehaviour
{
    [SerializeField] private int spawnerIndex;
    [SerializeField] private float spawnRate;
    [SerializeField] private float initialSpawnDelay;
    [SerializeField] private LevelDataSO levelData;
    [SerializeField] private SplineComputer splineComputer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem smokeVfx;
    [SerializeField] private ParticleSystem smokeVentVfx;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int arrowMaterialIndex = 2;
    [SerializeField] private float arrowTextureScrollSpeed = 2f;

    [SerializeField] bool IsFrozen = false;
    [SerializeField] int requiredHeat = 0;
    [SerializeField] TextMeshProUGUI freezeStatusText;
    [SerializeField] GameObject frozenObject;

    [SerializeField] private MatchManager matchManager;
    private int _spawnCount = 0;
    private Material arrowMaterial;

    private void Start()
    {
        if (splineComputer == null)
        {
            Debug.LogError("splineComputer not assigned");
            return;
        }

        CacheArrowMaterial();

        if(requiredHeat > 0)
        {
            freezeStatusText.text = requiredHeat.ToString();
            animator.enabled = false;
            smokeVentVfx.gameObject.SetActive(false);
            IsFrozen = true;
            return;
        }

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


        animator.Play("furnaceShot");
        smokeVfx.Play();
        audioSource.Play();
        GameObject spawnedGo = Instantiate(targetList[_spawnCount]);

        if (!spawnedGo.TryGetComponent(out Matchable matchable))
        {
            Debug.LogError($"No Matchable component found on {spawnedGo.name}");
            return;
        }

        matchable.Initialize(matchManager, splineComputer);
        _spawnCount++;

        spawnedGo.SetActive(true);
    }

    public void RemoveIce()
    {
        if(IsFrozen == false)
            return;    
        
        --requiredHeat;
        freezeStatusText.text = requiredHeat.ToString();

        if(requiredHeat <= 0)
        {
            
            frozenObject.SetActive(false);
            freezeStatusText.gameObject.SetActive(false);
            smokeVentVfx.gameObject.SetActive(true);
            IsFrozen = false;
             animator.enabled = true;
            InvokeRepeating(nameof(Spawn), initialSpawnDelay, spawnRate); 
        }

    }
}
