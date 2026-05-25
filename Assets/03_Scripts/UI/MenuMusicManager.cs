using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public class MenuMusicManager : MonoBehaviour
{
    private static MenuMusicManager instance;

    [SerializeField] private AudioClip menuTheme;
    [SerializeField, Range(0f, 1f)] private float volume = 0.55f;
    [SerializeField] private string stopOnSceneName = "LoadingScreen";

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = menuTheme;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;

        if (menuTheme != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnValidate()
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(stopOnSceneName) && scene.name == stopOnSceneName)
        {
            StopAndDestroy();
        }
    }

    private void StopAndDestroy()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (instance == this)
        {
            instance = null;
        }

        Destroy(gameObject);
    }
}
