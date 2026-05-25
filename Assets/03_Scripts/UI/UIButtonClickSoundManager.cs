using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class UIButtonClickSoundManager : MonoBehaviour
{
    private static UIButtonClickSoundManager instance;

    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
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

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RegisterAllButtons();
        StartCoroutine(RegisterButtonsForInitialFrames());
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveMissingButtons();
        RegisterAllButtons();
        StartCoroutine(RegisterButtonsForInitialFrames());
    }

    private IEnumerator RegisterButtonsForInitialFrames()
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            RegisterAllButtons();
        }
    }

    private void RegisterAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            RegisterButton(button);
        }
    }

    private void RegisterButton(Button button)
    {
        if (button == null || registeredButtons.Contains(button))
            return;

        button.onClick.AddListener(PlayClick);
        registeredButtons.Add(button);
    }

    private void RemoveMissingButtons()
    {
        registeredButtons.RemoveWhere(button => button == null);
    }

    private void PlayClick()
    {
        if (clickClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clickClip, volume);
    }
}
