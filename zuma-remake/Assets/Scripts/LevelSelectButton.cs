using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectButton : MonoBehaviour
{
    [Tooltip("Scene name to load (recommended).")]
    public string sceneName;

    [Tooltip("Optional: if sceneName is empty, load by build index instead.")]
    public int buildIndex = -1;

    public void LoadLevel()
    {
        Time.timeScale = 1f; // just in case you froze time on win/lose

        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else if (buildIndex >= 0)
            SceneManager.LoadScene(buildIndex);
        else
            Debug.LogError("LevelSelectButton: sceneName empty AND buildIndex < 0");
    }
}