using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string levelScenePath = "Assets/02_Scenes/Level01.unity";
    [SerializeField] private float minimumDisplayTime = 0.5f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(minimumDisplayTime);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelScenePath);
        if (loadOperation == null)
            yield break;

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }
}
