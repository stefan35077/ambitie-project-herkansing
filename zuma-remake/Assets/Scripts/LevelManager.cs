using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public ChainController chain;

    void Start()
    {
        chain.OnLevelWon += HandleWin;
        chain.OnLevelLost += HandleLose;
    }

    void HandleWin()
    {
        int stars = chain.GetStars();
        Debug.Log($"LEVEL COMPLETE! Stars: {stars}  Score: {chain.score}");

        // TODO: show win UI, save progress, load next level
        Time.timeScale = 0f; // pause (optional)
    }

    void HandleLose()
    {
        Debug.Log($"LEVEL FAILED. Score: {chain.score}");

        // TODO: show fail UI, retry button
        Time.timeScale = 0f; // pause (optional)
    }
}