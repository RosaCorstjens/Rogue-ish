using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // static reference to this instance 
    // so it can be accessed throughout the whole code base
    internal static GameManager instance = null;

    // game play settings
    [SerializeField] internal float levelStartDelay = 2f;
    [SerializeField] internal float restartLevelDelay = 1f;
    [SerializeField] internal float scale = 0.16f;
    [SerializeField] internal int finalFloor = 50;

    // references to content
    internal DungeonGenerator dungeon;
    internal List<Creature> enemies;

    // references to UI elements
    private GameObject fullscreenImage;
    private Text fullscreenText;

    // whether or not we're busy
    // others are supposed to wait if we are
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
        fullscreenImage = GameObject.Find("LevelImage");
        fullscreenText = fullscreenImage.GetComponentInChildren<Text>();
        fullscreenText.text = "Floor " + floor;
        fullscreenImage.gameObject.SetActive(true);

        dungeon.GenerateDungeon(floor);

        yield return new WaitForSeconds(levelStartDelay);

        fullscreenImage.gameObject.SetActive(false);
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
        // we finished the last floor!
        if(floor == finalFloor)
        {
            Win();
            return;
        }

        StartCoroutine(GotoNextFloor());
    }

    private IEnumerator GotoNextFloor()
    {
        yield return new WaitForSeconds(restartLevelDelay);

        SceneManager.LoadScene(0);
    }

    internal void Win()
    {
        fullscreenText.text = "You beated the dungeon and slayed " + enemiesKilled + " on the way!";
        fullscreenImage.gameObject.SetActive(true);

        enabled = false;
    }

    internal void GameOver()
    {
        fullscreenText.text = "You made it to floor " + floor + "!";
        fullscreenImage.gameObject.SetActive(true);

        enabled = false;
    }
}