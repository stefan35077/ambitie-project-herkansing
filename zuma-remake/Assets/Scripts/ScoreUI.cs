using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public ChainController chain;
    public TMP_Text scoreText;

    void Start()
    {
        if (!chain || !scoreText) return;

        scoreText.text = chain.score.ToString();
        chain.OnScoreChanged += s => scoreText.text = s.ToString();
    }
}