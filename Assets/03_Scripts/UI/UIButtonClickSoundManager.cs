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
    [SerializeField] private ButtonPressAnimator pressAnimationTemplate;
    [SerializeField] private float runtimeButtonScanInterval = 2f;

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
    private AudioSource audioSource;
    private Coroutine continuousRegistrationCoroutine;

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
        if (continuousRegistrationCoroutine != null)
        {
            StopCoroutine(continuousRegistrationCoroutine);
            continuousRegistrationCoroutine = null;
        }
    }

    private void Start()
    {
        RegisterAllButtons();
        StartCoroutine(RegisterButtonsForInitialFrames());
        continuousRegistrationCoroutine = StartCoroutine(RegisterButtonsContinuously());
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

    private IEnumerator RegisterButtonsContinuously()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(Mathf.Max(0.25f, runtimeButtonScanInterval));
        while (true)
        {
            yield return wait;
            RemoveMissingButtons();
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
        if (button == null)
            return;

        EnsurePressAnimation(button);

        if (registeredButtons.Contains(button))
            return;

        button.onClick.AddListener(PlayClick);
        registeredButtons.Add(button);
    }

    private void EnsurePressAnimation(Button button)
    {
        ButtonPressAnimator animator = button.GetComponent<ButtonPressAnimator>();
        if (animator == null)
        {
            animator = button.gameObject.AddComponent<ButtonPressAnimator>();
        }

        ButtonPressAnimator template = GetPressAnimationTemplate(animator);
        if (template != null && template != animator)
        {
            animator.CopySettingsFrom(template);
        }
    }

    private ButtonPressAnimator GetPressAnimationTemplate(ButtonPressAnimator fallback)
    {
        if (pressAnimationTemplate != null)
            return pressAnimationTemplate;

        ButtonPressAnimator[] animators = FindObjectsOfType<ButtonPressAnimator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i] != fallback)
            {
                pressAnimationTemplate = animators[i];
                return pressAnimationTemplate;
            }
        }

        return fallback;
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
