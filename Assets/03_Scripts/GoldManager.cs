using UnityEngine;

public class GoldManager : MonoBehaviour
{
    [SerializeField] private int playergold;
    public static GoldManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

            DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
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
    }
    public int GetGold()
    {
        return playergold;
    }
}
