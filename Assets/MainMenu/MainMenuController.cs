using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject resumeButton;
    [SerializeField] TMP_Text highScoreText;

    private void Start()
    {
        //Allow for expansion into advanced save system later
        switch(PlayerPrefs.GetInt("SaveVersion", 0))
        {
            case 1:
                break;
            default:
                {
                    PlayerPrefs.SetInt("CanLoad", 0);
                    PlayerPrefs.SetInt("SaveVersion", 1);
                    PlayerPrefs.Save();
                    break;
                }
        }
        switch (PlayerPrefs.GetInt("SettingsVersion", 0))
        {
            case 1:
                break;
            default:
                {
                    PlayerPrefs.SetInt("SettingsVersion", 1);
                    PlayerPrefs.Save();
                    break;
                }
        }
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighestLevel", 0)}";
        resumeButton.SetActive(PlayerPrefs.GetInt("CanLoad", 0) == 1);
    }

    public void NewRun()
    {
        SceneManager.LoadScene("MapGenerationTestScene");
    }

    public void ResumeRun()
    {
        ExplorationManager.Instance.Load();
        CharacterManager.Instance.Load();
        SceneManager.LoadScene("MapGenerationTestScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
