using UnityEngine;

public class BricksRow : MonoBehaviour
{
    public float m_FloorPosition = -3.8f;

    public Brick[] m_Bricks;
    public ScoreBall[] m_ScoreBalls;

    public bool isminy;

    private void Awake()
    {
        m_Bricks = GetComponentsInChildren<Brick>();
        m_ScoreBalls = GetComponentsInChildren<ScoreBall>();
    }

    private void OnEnable()
    {
        if (transform.localPosition.y < m_FloorPosition)
            GoToTop();

        HideAll();
        GoToTop();

        MoveDown(BrickSpawner.Instance.m_SpawningDistance, -1000f);

        // make only one score ball available for this row randomly
        m_ScoreBalls[Random.Range(0, m_ScoreBalls.Length)].gameObject.SetActive(true);

        // try to enable bricks randomly except at the score ball's position
        for (int i = 0; i < m_Bricks.Length; i++)
        {
            if(m_ScoreBalls[i].gameObject.activeInHierarchy)
                m_Bricks[i].gameObject.SetActive(false);
            else
                m_Bricks[i].gameObject.SetActive(Random.Range(0, 2) == 1 ? true : false);
        }

        // make at least one brick available if there was not any one before
        bool hasNoBrick = true;
        for (int i = 0; i < m_Bricks.Length; i++)
            if (m_Bricks[i].gameObject.activeInHierarchy)
            {
                hasNoBrick = false;
                break;
            }

        if (hasNoBrick)
            for (int i = 0; i < m_Bricks.Length; i++)
                if (!m_ScoreBalls[i].gameObject.activeInHierarchy)
                {
                    m_Bricks[i].gameObject.SetActive(true);
                    break;
                }
    }

    private void Update()
    {
        if(transform.localPosition.y <= m_FloorPosition)
        {
            if(HasActiveBricks())
                GameManager.Instance.m_GameState = GameManager.GameState.GameOver;
            else if (HasActiveScoreBall())
            {
                GoToTop();
                gameObject.SetActive(false);
            }
            else
            {
                GoToTop();
                gameObject.SetActive(false);
            }
        }
    }

    private void HideAll()
    {
        for (int i = 0; i < m_Bricks.Length; i++)
        {
            m_Bricks[i].gameObject.SetActive(false);
            m_ScoreBalls[i].gameObject.SetActive(false);
        }
    }

    private void GoToTop()
    {
        HideAll();
        transform.localPosition = new Vector3(0, BrickSpawner.Instance.m_SpawningTopPosition, 0);
    }

    public void TellGMActionComplete()
    {
        if (transform.localPosition.y <= m_FloorPosition)
        {
            if (HasActiveBricks())
            {
                GameManager.Instance.m_GameState = GameManager.GameState.GameOver;
                //GameManager.Instance.CompleteAction();
            }
        }
        if (isminy)
        { 
            GameManager.Instance.CompleteAction();
            isminy = false;
        }
        Debug.Log("Finished Moving Down");
    }

    public void MoveDown(float howMuch, float miny)
    {
        for (int i = 0; i < m_Bricks.Length; i++)
            if (m_Bricks[i].gameObject.activeInHierarchy)
                m_Bricks[i].ChangeColor();

        var finalposition = new Vector3(transform.position.x, transform.position.y - howMuch, transform.position.z);

        isminy = false;

        if (transform.position.y == miny)
            isminy = true;

        iTween.MoveTo(gameObject, iTween.Hash("position", finalposition, "time", 0.25f, "oncomplete", "TellGMActionComplete", "oncompletetarget", this.gameObject));
        
        //iTween.MoveTo(gameObject, new Vector3(transform.position.x, transform.position.y - howMuch, transform.position.z), 0.25f);
    }

    public void CheckBricksActivation()
    {
        int deactiveObjects = 0;

        for (int i = 0; i < m_Bricks.Length; i++)
            if (!m_Bricks[i].gameObject.activeInHierarchy && !m_ScoreBalls[i].gameObject.activeInHierarchy)
                deactiveObjects++;

        if (deactiveObjects == m_Bricks.Length)
        {
            gameObject.SetActive(false);
            GoToTop();
        }
    }

    public bool HasActiveBricks()
    {
        bool hasActiveBrick = false;

        for (int i = 0; i < m_Bricks.Length; i++)
        {
            if (m_Bricks[i].gameObject.activeInHierarchy)
            {
                hasActiveBrick = true;
                break;
            }
        }

        return hasActiveBrick;
    }

    public bool HasActiveScoreBall()
    {
        bool hasActiveScoreBall = false;

        for (int i = 0; i < m_ScoreBalls.Length; i++)
        {
            if (m_ScoreBalls[i].gameObject.activeInHierarchy)
            {
                m_ScoreBalls[i].PlayParticle();
                BallLauncher.Instance.IncreaseBallsAmountFromOutSide(1);

                hasActiveScoreBall = true;
                break;
            }
        }

        return hasActiveScoreBall;
    }
}