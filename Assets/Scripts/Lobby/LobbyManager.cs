using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameType
{
    Soccer,
    Dummy,
    Max,
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField]
    private GameObject gameHolder;

    [SerializeField]
    private TMP_Text gameName;

    private GameType selectedGameType;

    public void OnGameSelected(int gameIndex)
    {
        if (gameIndex >= (int)GameType.Max)
        {
            throw new System.SystemException("�ùٸ��� ���� GameType");
        }
        selectedGameType = (GameType)gameIndex;
        gameName.text = selectedGameType + " Game";
    }

    public void OnStartButtonClicked()
    {
        switch(selectedGameType)
        {
            case GameType.Soccer:
                {
                    SceneManager.LoadScene("BattleScene");
                }
                break;
            case GameType.Dummy:
                {
                    Debug.Log("���� ���� ����");
                }
                break;
            default:
                {
                    throw new System.SystemException("�ùٸ��� ���� GameType");
                }
        }
    }

    public void DebugButton()
    {
        Debug.Log("�����");
    }

    // Start is called before the first frame update
    void Start()
    {
        OnGameSelected((int)GameType.Soccer);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
