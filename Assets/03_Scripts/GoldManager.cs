using UnityEngine;

public class GoldManager : MonoBehaviour
{
    [SerializeField] private int playergold;
    public static GoldManager Instance { get; private set; }

    private bool rewardShow = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(this.gameObject);

        GetPlayerStat();
    }

    private void GetPlayerStat()
    {
        playergold = PlayerPrefs.GetInt("PlayerGold");
    }

    public void SetGold(int gold)
    {
        playergold = gold;
        PlayerPrefs.SetInt("PlayerGold", playergold);

        rewardShow = true;
    }
    public int GetGold()
    {
        return playergold;
    }

    public bool GetRewardStatus()
    {
        return rewardShow;
    }

    public void RewardShown()
    {
        rewardShow = false;
    }
}
