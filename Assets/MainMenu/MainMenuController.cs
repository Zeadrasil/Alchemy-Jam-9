using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject resumeButton;
    [SerializeField] TMP_Text highScoreText;

    private void Start()
    {
        //if(PlayerPrefs.GetInt(""))
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighestLevel", 0)}";
    }

    public void NewRun()
    {
        SceneManager.LoadScene("MapGenerationTestScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
