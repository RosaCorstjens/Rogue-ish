using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    internal static GameManager instance = null;

    internal DungeonGenerator dungeon;
    internal List<Creature> enemies;

    [SerializeField] internal float levelStartDelay = 2f;
    [SerializeField] internal float restartLevelDelay = 1f;
    [SerializeField] internal float scale = 0.16f;

    private GameObject levelImage;
    private Text levelText;

    private bool doingSetup;

    // settings to remember when switching levels
    internal int heroHealth = 5;
    internal int enemiesKilled = 0;
    internal int floor = 0;

    private void Awake()
    {
        // set the static instance if it doens't exist yet
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // don't destroy when we reload
        DontDestroyOnLoad(gameObject);

        // set up enemies list
        enemies = new List<Creature>();

        // let's get started
        StartCoroutine(StartFloor());
    }

    private IEnumerator StartFloor()
    {
        // while doing setup, no interaction allowed
        doingSetup = true;

        // increase floor
        floor++;

        // find references
        dungeon = GameObject.Find("Dungeon").GetComponent<DungeonGenerator>();
        dungeon.LoadAssets();
        levelImage = GameObject.Find("LevelImage");
        levelText = levelImage.GetComponentInChildren<Text>();
        levelText.text = "Floor " + floor;
        levelImage.gameObject.SetActive(true);

        dungeon.GenerateDungeon(floor);

        yield return new WaitForSeconds(levelStartDelay);

        levelImage.gameObject.SetActive(false);
        doingSetup = false;
    }

    private void OnLevelWasLoaded(int level)
    {
        if (this != instance)
            return;

        StartCoroutine(StartFloor());
    }

    internal void NextFloor()
    {
        StartCoroutine(GotoNextFloor());
    }

    private IEnumerator GotoNextFloor()
    {
        yield return new WaitForSeconds(restartLevelDelay);

        SceneManager.LoadScene(0);
    }

    internal void Win()
    {
        levelText.text = "You beated the dungeon and slayed " + enemiesKilled + " on the way!";
        levelImage.gameObject.SetActive(true);

        enabled = false;
    }

    internal void GameOver()
    {
        levelText.text = "You made it to floor " + floor + "!";
        levelImage.gameObject.SetActive(true);

        enabled = false;
    }
}