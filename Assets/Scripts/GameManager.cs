using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject m_MainMenuPanel;
    public GameObject m_GameMenuPanel;
    public GameObject m_GameOverPanel;
    public GameObject m_Scores;
    public Text m_GameOverFinalScore;

    public int after;

    public enum GameState { MainMenu, Playable, GameOver, }
    private GameState m_State = GameState.MainMenu;

    public GameState m_GameState
    {
        set
        {
            m_State = value;

            switch(value)
            {
                case GameState.MainMenu:
                    m_MainMenuPanel.SetActive(true);
                    m_GameMenuPanel.SetActive(false);
                    m_GameOverPanel.SetActive(false);
                    m_Scores.SetActive(true);

                    BallLauncher.Instance.OnMainMenuActions();
                    BrickSpawner.Instance.HideAllBricksRows();
                    break;
                case GameState.Playable:
                    if(Saver.Instance.HasSave())
                    {

                    }
                    else
                    {
                        m_MainMenuPanel.SetActive(false);
                        m_GameMenuPanel.SetActive(true);
                        m_GameOverPanel.SetActive(false);
                        m_Scores.SetActive(true);
                    
                        BallLauncher.Instance.m_CanPlay = true;
                        BrickSpawner.Instance.m_LevelOfFinalBrick = 1;  // temporary (after save and load)

                        // reset score (probably by conditions)
                        ScoreManager.Instance.m_ScoreText.text = BrickSpawner.Instance.m_LevelOfFinalBrick.ToString();

                        BrickSpawner.Instance.SpawnNewBricks();
                    }
                    break;
                case GameState.GameOver:
                    m_MainMenuPanel.SetActive(false);
                    m_GameMenuPanel.SetActive(false);
                    m_GameOverPanel.SetActive(true);
                    m_Scores.SetActive(false);

                    m_GameOverFinalScore.text = "Final Score : " + (BrickSpawner.Instance.m_LevelOfFinalBrick - 1).ToString();
                    BallLauncher.Instance.m_CanPlay = false;
                    BallLauncher.Instance.ResetPositions();
                    ScoreManager.Instance.SubmitScoreToLeaderboard();
                    break;
            }
        }
        get
        {
            return m_State;
        }
    }
    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        m_GameState = GameState.MainMenu;

        after = 0;

        await ListenForLearner();
    }

    private TaskCompletionSource<int> actionPromise;

    private void ResetActionPromise()
    {
        actionPromise = new TaskCompletionSource<int>();
    }

    public void CompleteAction()
    {
        actionPromise.SetResult(0);
    }

    public int CalcBrickTotal()
    {
        int total = 0;

        for(int i = 0; i < BrickSpawner.Instance.m_BricksRow.Count; ++i)
        {
            if (BrickSpawner.Instance.m_BricksRow[i].gameObject.activeInHierarchy)
            {
                for (int j = 0; j < BrickSpawner.Instance.m_BricksRow[i].m_Bricks.Length; ++j)
                {
                    if (BrickSpawner.Instance.m_BricksRow[i].m_Bricks[j].gameObject.activeInHierarchy)
                    {
                        total += BrickSpawner.Instance.m_BricksRow[i].m_Bricks[j].m_Health;
                    }
                }
            }
        }
        return total;
    }

    private async Task ListenForLearner()
    {
        var listener = TcpListener.Create(10012);
        listener.Start();

        using (var client = await listener.AcceptTcpClientAsync())
        {
            var reader = new StreamReader(client.GetStream());
            var writer = new StreamWriter(client.GetStream());
            while (true)
            {
                string line = await reader.ReadLineAsync();
                string[] args = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                {
                    // IGNORE
                }
                else if (args[0] == "RESET")
                {
                    m_GameState = GameState.MainMenu;
                    m_GameState = GameState.Playable;
                    await writer.WriteAsync("DONE\n");
                    await writer.FlushAsync();
                }
                else if (args[0] == "SCREEN")
                {
                    Color[] observation = GrabScreen.Instance.mytexture.GetPixels();

                    var pixelString = observation.Select(color => ((int)(255 * color.r), (int)(255 * color.g), (int)(255 * color.b)))
                        .Select(color => string.Format("{0:D3} {0:D3} {0:D3}", color.Item1, color.Item2, color.Item3))
                        .Aggregate(new StringBuilder(), (total, item) => total.Append(" ").Append(item));

                    await writer.WriteAsync(pixelString + "\n");
                    await writer.FlushAsync();
                }
                else if (args[0] == "ACTION" && int.TryParse(args[1], out int direction))
                {
                    Debug.Log("direction: " + direction);
                    ResetActionPromise();

                    var tmpvec = new Vector3(Mathf.Tan(-1.35f + (direction - 1) * 0.09f), 1.0f, 0.0f);

                    BallLauncher.Instance.m_StartPosition = Vector3.zero;
                    BallLauncher.Instance.m_EndPosition = tmpvec.normalized;
                    BallLauncher.Instance.m_CanPlay = false;
                    int before = CalcBrickTotal();
                    BallLauncher.Instance.EndDrag();
                    

                    await actionPromise.Task;

                    float reward = ((float)(before - after)) / ((float)(before));
                    if (m_GameState == GameState.GameOver)
                        reward = -2.0f;

                    await writer.WriteAsync(String.Format("{0,-1:F7}", reward) + '\n');
                    await writer.FlushAsync();
                    Debug.Log("reward" + reward + " : " + "Ball position : " + BallLauncher.Instance.gameObject.transform.position);
                }
                else
                {
                    await writer.WriteAsync("Protocol Mismatch\n");
                    await writer.FlushAsync();
                }
            }
        }
    }
}