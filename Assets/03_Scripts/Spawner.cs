using UnityEngine;
using UnityEngine.Splines;
using Dreamteck.Splines;
using TMPro;
using DG.Tweening;

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
    [SerializeField] private AudioClip spawnSfxClip;
    [SerializeField] private float spawnSfxVolume = 1f;
    [Header("Working Loop SFX")]
    [SerializeField] private AudioClip railWorkingSfx;
    [SerializeField] private float railWorkingVolume = 0.04f;
    [SerializeField] private AudioClip furnaceWorkingSfx;
    [SerializeField] private float furnaceWorkingVolume = 0.035f;
    [SerializeField] private int arrowMaterialIndex = 2;
    [SerializeField] private float arrowTextureScrollSpeed = 2f;
    [Header("Spawn Pop Animation")]
    [SerializeField] private float spawnPopDuration = 0.18f;
    [SerializeField] private float spawnPopScale = 1.18f;

    [SerializeField] bool IsFrozen = false;
    [SerializeField] int requiredHeat = 0;
    [SerializeField] TextMeshProUGUI freezeStatusText;
    [SerializeField] GameObject frozenObject;

    [SerializeField] private MatchManager matchManager;
    private int _spawnCount = 0;
    private Material arrowMaterial;
    private AudioSource railWorkingSource;
    private AudioSource furnaceWorkingSource;

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

    private void OnDisable()
    {
        railWorkingSource?.Stop();
        furnaceWorkingSource?.Stop();
    }

    private void Update()
    {
        bool working = IsSpawnerWorking();

        if (working)
            MoveArrowChannel();

        UpdateWorkingLoopSfx(working);
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
        PlaySpawnSfx();
        GameObject spawnedGo = Instantiate(targetList[_spawnCount]);

        if (!spawnedGo.TryGetComponent(out Matchable matchable))
        {
            Debug.LogError($"No Matchable component found on {spawnedGo.name}");
            return;
        }

        matchable.Initialize(matchManager, splineComputer, this, _spawnCount);
        PlaySpawnPop(spawnedGo.transform);
        _spawnCount++;

        spawnedGo.SetActive(true);
    }

    private void PlaySpawnPop(Transform spawnedTransform)
    {
        Matchable matchable = spawnedTransform.GetComponent<Matchable>();
        Vector3 originalScale = matchable != null ? matchable.BaseScale : spawnedTransform.localScale;
        spawnedTransform.DOKill();
        spawnedTransform.localScale = Vector3.zero;

        DOTween.Sequence()
            .Append(spawnedTransform.DOScale(originalScale * spawnPopScale, spawnPopDuration * 0.55f).SetEase(Ease.OutBack))
            .Append(spawnedTransform.DOScale(originalScale, spawnPopDuration * 0.45f).SetEase(Ease.OutQuad));
    }

    private void PlaySpawnSfx()
    {
        AudioClip clip = GetSpawnSfxClip();
        if (clip == null)
            return;

        GameObject audioObject = new GameObject("SpawnSfx");
        AudioSource oneShotSource = audioObject.AddComponent<AudioSource>();
        oneShotSource.clip = clip;
        oneShotSource.outputAudioMixerGroup = audioSource != null ? audioSource.outputAudioMixerGroup : null;
        oneShotSource.volume = (audioSource != null ? audioSource.volume : 1f) * spawnSfxVolume;
        oneShotSource.pitch = 1f;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.priority = 0;
        oneShotSource.ignoreListenerPause = true;
        oneShotSource.playOnAwake = false;
        oneShotSource.Play();
        Destroy(audioObject, clip.length + 0.1f);
    }

    private void UpdateWorkingLoopSfx(bool working)
    {
        UpdateLoopSource(ref railWorkingSource, "RailWorkingSfx", railWorkingSfx, railWorkingVolume, working);
        UpdateLoopSource(ref furnaceWorkingSource, "FurnaceWorkingSfx", furnaceWorkingSfx, furnaceWorkingVolume, working);
    }

    private void UpdateLoopSource(ref AudioSource source, string objectName, AudioClip clip, float volume, bool shouldPlay)
    {
        if (clip == null)
            return;

        if (source == null)
        {
            GameObject audioObject = new GameObject(objectName);
            audioObject.transform.SetParent(transform, false);
            source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 128;
        }

        source.volume = volume;

        if (shouldPlay)
        {
            if (!source.isPlaying)
                source.Play();
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    private AudioClip GetSpawnSfxClip()
    {
        if (spawnSfxClip != null)
            return spawnSfxClip;

        return audioSource != null ? audioSource.clip : null;
    }

    private bool IsSpawnerWorking()
    {
        return !IsFrozen && HasMoreToSpawn();
    }

    private bool HasMoreToSpawn()
    {
        if (levelData == null || spawnerIndex < 0 || spawnerIndex >= levelData.LevelData.Count)
            return false;

        var spawnData = levelData.LevelData[spawnerIndex];
        return spawnData != null && _spawnCount < spawnData.PrefabsToSpawn.Count;
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

    public void RespawnMatchable(int ID)
    {
        var targetList = levelData.LevelData[spawnerIndex].PrefabsToSpawn;
        --_spawnCount;

        animator.Play("furnaceShot");
        smokeVfx.Play();
        PlaySpawnSfx();
        GameObject spawnedGo = Instantiate(targetList[ID]);

        if (!spawnedGo.TryGetComponent(out Matchable matchable))
        {
            Debug.LogError($"No Matchable component found on {spawnedGo.name}");
            return;
        }

        matchable.Initialize(matchManager, splineComputer, this, _spawnCount);
        PlaySpawnPop(spawnedGo.transform);
        _spawnCount++;

        spawnedGo.SetActive(true);
    }
}
