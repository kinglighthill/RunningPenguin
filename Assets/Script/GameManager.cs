using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GooglePlayGames;
using GooglePlayGames.BasicApi.SavedGame;

public class GameManager : MonoBehaviour
{
    private const int COIN_SCORE_AMOUNT = 5;

    public static GameManager Instance { set; get; }

    public bool IsDead { set; get; }
    private bool isGameStarted = false;
    private PlayerMotor motor;

    public Animator gameCanvas, menuAnim, diamondAnim;
    public TMP_Text scoreText, coinText, modifierText, highScoreText, totalCoinText;
    public float score, modifierScore;
    private int lastScore, coinScore, totalCoin;

    public Animator deathMenuAnim;
    public TMP_Text deadScoreText, deadCoinText;

    public GameObject connectedMenu, disconnectedMenu;

    private void Awake()
    {
        Instance = this;
        modifierScore = 1;
        motor = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMotor>();

        modifierText.text = "x" + modifierScore.ToString("0.0");
        coinText.text = coinScore.ToString("0");
        scoreText.text = score.ToString("0");
        highScoreText.text = PlayerPrefs.GetInt("Highscore").ToString();

        //GooglePlayGames.BasicApi.PlayGamesClientConfiguration config =
        //    new GooglePlayGames.BasicApi.PlayGamesClientConfiguration.Builder().EnableSavedGames().Build();
        //PlayGamesPlatform.InitializeInstance(config);
        //PlayGamesPlatform.Activate();
        //OnConnectionResponse(PlayGamesPlatform.Instance.localUser.authenticated);
    }

    public void OnStartClick()
    {

    }

    private void Update()
    {
        Debug.Log(MobileInput.Instance.Tap);
        if (MobileInput.Instance.Tap && !isGameStarted)
        {
            isGameStarted = true;
            motor.StartRunning();
            FindObjectOfType<GlacierSpawner>().IsScrolling = true;
            FindObjectOfType<CameraMotor>().IsMoving = true;
            gameCanvas.SetTrigger("Show");
            menuAnim.SetTrigger("Hide");
        }

        if (isGameStarted && !IsDead)
        {
            score += (Time.deltaTime * modifierScore);

            if (lastScore != (score))
            {
                lastScore = (int)score;
                scoreText.text = score.ToString("0");
            }
        }
    }

    public void GetCoin()
    {
        diamondAnim.SetTrigger("Collect");
        coinScore++;

        switch(coinScore)
        {
            case 50:
                UnlockAchievement("achievement_collect_50_coins");
                break;
            case 100:
                UnlockAchievement("achievement_collect_100_coins");
                break;
            case 150:
                UnlockAchievement("achievement_collect_150_coins");
                break;
            case 200:
                UnlockAchievement("achievement_collect_200_coins");
                break;
            default:
                break;
        }

        coinText.text = coinScore.ToString("0"); ;
        score += COIN_SCORE_AMOUNT;
        scoreText.text = score.ToString("0");
    }

    public void UpdateModifier(float modifierAmount)
    {
        modifierScore = 1.0f + modifierAmount;
        modifierText.text = "x" + modifierScore.ToString("0.0");
    }

    public void OnPlayButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void OnDeath()
    {
        IsDead = true;
        FindObjectOfType<GlacierSpawner>().IsScrolling = false;
        deadScoreText.text = score.ToString("0");
        deadCoinText.text = coinScore.ToString("0");
        deathMenuAnim.SetTrigger("Dead");
        gameCanvas.SetTrigger("Hide");

        ReportScore((int) score, "leaderboard_highscore");

        if (score > PlayerPrefs.GetInt("HighScore"))
        {
            float s = score;

            if (s % 1 == 0)
            {
                s += 1;
            }

            PlayerPrefs.SetInt("Highscore", (int) s);
        }

        totalCoin += coinScore;

        OpenSave(true);
    }

    private string GetSaveString()
    {
        return PlayerPrefs.GetInt("Highscore").ToString()+ "|" + totalCoin.ToString();
    }

    private void LoadSaveString(string save)
    {
        string[] data = save.Split("|");
        PlayerPrefs.SetInt("Highscore", int.Parse(data[0]));
        totalCoin = int.Parse(data[1]);

        totalCoinText.text = totalCoin.ToString();
    }

    private void OnConnectionResponse(bool authenticated)
    {
        connectedMenu.SetActive(authenticated);
        disconnectedMenu.SetActive(!authenticated);

        if (authenticated)
        {
            UnlockAchievement("achievement_login");
            OpenSave(false);
        }
    }

    public void OnConnectClick()
    {
        Social.localUser.Authenticate((bool success) =>
        {
            OnConnectionResponse(success);
        });
    }

    public void UnlockAchievement(string achievementID)
    {
        Social.ReportProgress(achievementID, 100.0f, (bool success) =>
        {

        });
    }

    public void OnAchievementClick()
    {
        if(Social.localUser.authenticated)
        {
            Social.ShowAchievementsUI();
        }
    }

    public void OnLeaderboardClick()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowLeaderboardUI();
        }
    }

    public void ReportScore(int score, string leaderboardID)
    {
        Social.ReportScore(score, leaderboardID, (bool success) =>
        {

        });
    }

    private bool isSaving = false;

    public void OpenSave(bool saving)
    {
        if (Social.localUser.authenticated)
        {
            isSaving = saving;
            ((PlayGamesPlatform)Social.Active).SavedGame
                .OpenWithAutomaticConflictResolution(
                "RunningPenguin", GooglePlayGames.BasicApi.DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, SaveGameOpened);
        }
    }

    private void SaveGameOpened(SavedGameRequestStatus status, ISavedGameMetadata metadata)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            if (isSaving)
            {
                byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes(GetSaveString());
                SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                    .WithUpdatedDescription("Saved at " + DateTime.Now.ToString()).Build();

                ((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(metadata,update, data, SaveUpdate);
            }
            else
            {
                ((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(metadata, SaveRead);
            }
        }
    }

    private void SaveUpdate(SavedGameRequestStatus status, ISavedGameMetadata metadata)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            // string savedData = System.Text.ASCIIEncoding.ASCII.GetString(data);
        }
    }

    private void SaveRead(SavedGameRequestStatus status, byte[] data)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            string savedData = System.Text.ASCIIEncoding.ASCII.GetString(data);
            LoadSaveString(savedData);
        }
    }
}
