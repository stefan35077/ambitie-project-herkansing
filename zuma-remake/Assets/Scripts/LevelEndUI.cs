using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelEndUI : MonoBehaviour
{
    [Header("Refs")]
    public ChainController chain;

    [Header("Screens")]
    public GameObject winScreen;
    public GameObject loseScreen;

    [Header("Win UI")]
    public Image[] starImages;
    public TMP_Text scoreText;
    public Button winMenuButton;
    public Button nextLevelButton;

    [Header("Lose UI")]
    public Button loseMenuButton;
    public Button retryButton;

    [Header("Scene Flow")]
    public string menuSceneName = "Menu";
    public string nextSceneName = "";

    bool ended;

    void Awake()
    {
        if (winScreen) winScreen.SetActive(false);
        if (loseScreen) loseScreen.SetActive(false);

        if (winMenuButton) winMenuButton.onClick.AddListener(GoMenu);
        if (loseMenuButton) loseMenuButton.onClick.AddListener(GoMenu);
        if (retryButton) retryButton.onClick.AddListener(Retry);
        if (nextLevelButton) nextLevelButton.onClick.AddListener(NextLevel);
    }

    void OnEnable()
    {
        if (chain)
        {
            chain.OnLevelWon += OnWin;
            chain.OnLevelLost += OnLose;
        }
    }

    void OnDisable()
    {
        if (chain)
        {
            chain.OnLevelWon -= OnWin;
            chain.OnLevelLost -= OnLose;
        }
    }

    void OnWin()
    {
        if (ended) return;
        ended = true;

        FreezeGame();

        if (loseScreen) loseScreen.SetActive(false);
        if (winScreen) winScreen.SetActive(true);

        if (scoreText) scoreText.text = chain.score.ToString();

        int stars = chain.GetStars();
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (!starImages[i]) continue;

                if (i < stars)
                    starImages[i].color = Color.white;
                else
                    starImages[i].color = Color.black;
            }
        }
    }

    void OnLose()
    {
        if (ended) return;
        ended = true;

        FreezeGame();

        if (winScreen) winScreen.SetActive(false);
        if (loseScreen) loseScreen.SetActive(true);
    }

    void FreezeGame()
    {
        // simplest: freeze time
        Time.timeScale = 0f;

        // optional: unlock cursor for UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void UnfreezeGame()
    {
        Time.timeScale = 1f;
    }

    void Retry()
    {
        UnfreezeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoMenu()
    {
        UnfreezeGame();
        SceneManager.LoadScene(menuSceneName);
    }

    void NextLevel()
    {
        UnfreezeGame();

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        int i = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(i + 1);
    }
}