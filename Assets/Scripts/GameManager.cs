using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

[Serializable]
public class SaveData
{
    public int lastSeed = 0;
    public int floor = 0;
    public int heroHealth = 5;
    public int enemiesKilled = 0;
}

public class GameManager : MonoBehaviour
{
    // static reference to this instance 
    // so it can be accessed throughout the whole code base
    internal static GameManager instance = null;

    // game play settings
    [SerializeField] internal float levelStartDelay = 2f;
    [SerializeField] internal float restartLevelDelay = 1f;
    [SerializeField] internal float turnDelay = 0.1f;
    [SerializeField] internal float scale = 0.16f;
    [SerializeField] internal int finalFloor = 50;

    [Space]

    [SerializeField] internal string itemDatabasePath = "/items.json";
    [SerializeField] internal string saveDataPath = "/saveData.json";

    [Space]

    [SerializeField] internal Color activeTurnColor = Color.yellow;
    [SerializeField] internal Color targetedColor = Color.red;
    
    // save data
    internal SaveData saveData;

    // references to content
    internal DungeonGenerator dungeon;
    internal List<Enemy> enemies;
    internal Hero hero;
    
    // references to UI elements
    private GameObject fullscreenImage;
    private Text fullscreenText;

    // whether or not we're busy
    // others are supposed to wait if we are
    private bool doingSetup;

    // whether or not it's the hero's turn
    internal bool herosTurn;
    private bool enemiesMoving;

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

        // read save data
        if(File.Exists(Application.dataPath + saveDataPath))
        {
            string json = File.ReadAllText(Application.dataPath + saveDataPath);
            if (json != null)
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                saveData.floor--;       // reduce by 1, since it's gonna increase when starting next floor
            }
        }
        else
            saveData = new SaveData();

        // set up enemies list
        enemies = new List<Enemy>();

        // let's get started
        StartCoroutine(StartFloor(true));
    }

    private void OnApplicationQuit()
    {
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(Application.dataPath + saveDataPath, json);
    }

    private IEnumerator StartFloor(bool first)
    {
        // while doing setup, no interaction allowed
        doingSetup = true;

        // increase floor
        saveData.floor++;

        // find references
        dungeon = GameObject.Find("Dungeon").GetComponent<DungeonGenerator>();
        dungeon.LoadAssets();
        fullscreenImage = GameObject.Find("LevelImage");
        fullscreenText = fullscreenImage.GetComponentInChildren<Text>();
        fullscreenText.text = "Floor " + saveData.floor;
        fullscreenImage.gameObject.SetActive(true);

        dungeon.GenerateDungeon(saveData.floor, first);

        yield return new WaitForSeconds(levelStartDelay);

        fullscreenImage.gameObject.SetActive(false);
        doingSetup = false;
        herosTurn = true;
    }

    private void OnLevelWasLoaded(int level)
    {
        if (this != instance)
            return;

        StartCoroutine(StartFloor(false));
    }

    internal void NextFloor()
    {
        // we finished the last floor!
        if(saveData.floor == finalFloor)
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
        fullscreenText.text = "You beated the dungeon and slayed " + saveData.enemiesKilled + " on the way!";
        fullscreenImage.gameObject.SetActive(true);

        enabled = false;
    }

    internal void GameOver()
    {
        fullscreenText.text = "You made it to floor " + saveData.floor + "!";
        fullscreenImage.gameObject.SetActive(true);

        enabled = false;
    }

    private void Update()
    {
        // don't update if we're waiting for hero or enemies
        if (herosTurn || enemiesMoving)
            return;

        StartCoroutine(UpdateEnemies());
    }

    IEnumerator UpdateEnemies()
    {
        enemiesMoving = true;

        yield return new WaitForSeconds(turnDelay);

        if (enemies.Count == 0)
        {
            yield return new WaitForSeconds(turnDelay);
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].DoUpdate())
                continue;

            yield return new WaitForSeconds(enemies[i].moveTime);
        }

        herosTurn = true;
        enemiesMoving = false;
    }

}