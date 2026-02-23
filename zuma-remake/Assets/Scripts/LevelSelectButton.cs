using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Level Load")]
    [Tooltip("Scene name to load (recommended).")]
    public string sceneName;

    [Tooltip("Optional: if sceneName is empty, load by build index instead.")]
    public int buildIndex = -1;

    [Header("Stars UI")]
    public Image[] starImages;
    public string starsKeyPrefix = "LevelStars_";

    void Start()
    {
        UpdateStarsUI();
    }

    void UpdateStarsUI()
    {
        if (starImages == null || starImages.Length == 0)
            return;

        int levelId = GetLevelId();
        int stars = PlayerPrefs.GetInt($"{starsKeyPrefix}{levelId}", 0);

        for (int i = 0; i < starImages.Length; i++)
        {
            if (!starImages[i]) continue;

            if (i < stars)
            {
                starImages[i].color = Color.white;
                starImages[i].transform.localScale = Vector3.one * 1.05f;
            }
            else
            {
                starImages[i].color = Color.black;
                starImages[i].transform.localScale = Vector3.one;
            }
        }
    }

    int GetLevelId()
    {
        if (buildIndex >= 0)
            return buildIndex;

        if (!string.IsNullOrEmpty(sceneName))
        {
            return SceneUtility.GetBuildIndexByScenePath(sceneName);
        }

        return -1;
    }

    public void LoadLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else if (buildIndex >= 0)
            SceneManager.LoadScene(buildIndex);
        else
            Debug.LogError("LevelSelectButton: sceneName empty AND buildIndex < 0");
    }
}