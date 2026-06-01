using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI costText, goldText, countdownText;
    [SerializeField] private GameObject LostGamePanel, WinGamePanel, CountdownPanel;
    [SerializeField] private int secondChanceGoldCost = 20;

    void Start()
    {
        SetGoldText();
    }

    void SetGoldText()
    {
        var playergold = GoldManager.Instance.GetGold();
        goldText.text = playergold.ToString();
    }
    public void OpenWinPanel()
    {
        WinGamePanel.SetActive(true);
    }

    public void ReturnMain()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenLostPanel()
    {

        if (LostGamePanel == null)
        {
            Debug.Log("LostGamePanel is NULL - not assigned in inspector!");
            return;
        }
        if (!LostGamePanel.activeInHierarchy)
        {
            Debug.Log("LostGamePanel is not active in hierarchy");
        }
        costText.text = "Ödenecek miktar " + secondChanceGoldCost.ToString();
        LostGamePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ContinueGame()
    {
        var playergold = GoldManager.Instance.GetGold();

        if (playergold >= secondChanceGoldCost)
        {
            playergold -= secondChanceGoldCost;
            secondChanceGoldCost *= 2;

            GoldManager.Instance.SetGold(playergold);
            //Time.timeScale = 1f;
            LostGamePanel.SetActive(false);

            StartCoroutine(CountdownWithRealTime());
            //MatchManager.Instance.TryAgain();
            SetGoldText();
        }
        else
        {
            // gold yok popup, reklam izle cartcurt

        }
    }

    IEnumerator CountdownWithRealTime()
    {
        CountdownPanel.SetActive(true);
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        MatchManager.Instance.TryAgain();
        Debug.Log("CountDownEnds Here");
        Time.timeScale = 0.25f;
        CountdownPanel.SetActive(false);
         DOTween.To(
            () => Time.timeScale,
            x => Time.timeScale = x,
            1f,
            0.5f
        )
        .SetEase(Ease.InOutQuad);
    }
}
