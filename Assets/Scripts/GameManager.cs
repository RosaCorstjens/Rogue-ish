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
    [SerializeField] internal float levelStartDelay = 5f;
    [SerializeField] internal float restartLevelDelay = 5f;
    [SerializeField] internal float turnDelay = 0.1f;

    [Space]

    [SerializeField] internal float scale = 0.16f;
    [SerializeField] internal int finalFloor = 10;

    [Space]

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
    internal bool herosTurn = false;
    private bool enemiesMoving = false;
    private Coroutine enemiesMovingRoutine;

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

        // read the save data, 
        // keeps a reference for further use
        ReadSaveData();

        // set up enemies list
        enemies = new List<Enemy>();

        // let's get started
        StartCoroutine(StartFloor(true));
    }
    
    private void OnApplicationQuit()
    {
        // make sure the save data 
        // is save in a file
        WriteSaveData();
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

        hero.StartTurn();
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

    #region UPDATE
    private void Update()
    {
        // don't update if we're waiting for something
        if (herosTurn || enemiesMoving || doingSetup)
            return;

        // if we're doing neither, 
        // start updating the enemies

        // if we we're still updating the enemies 
        // which can maybe happen if you switch floors and something goes wrong
        // end the previous coroutine
        if (enemiesMovingRoutine != null)
        {
            StopCoroutine(enemiesMovingRoutine);
            enemiesMovingRoutine = null;
        }
            
        // and start a new coroutine
        enemiesMovingRoutine = StartCoroutine(UpdateEnemies());
    }

    internal void OnActionEnded()
    {
        enemies.ForEach(e => e.OnActionEnded());
        hero.OnActionEnded();
    }

    IEnumerator UpdateEnemies()
    {
        // o.k. we're moving
        enemiesMoving = true;

        // start up for all the enemies
        enemies.ForEach(e => e.StartTurn());

        // wait for the turn delay 
        // from hero to enemies
        yield return new WaitForSeconds(turnDelay);

        // keep track of whether there are still enemies to update
        // always start assuming we're in the final action of this turn
        bool finalAction = true;

        // at least update once
        do
        {
            finalAction = true;

            // update each enemy one turn
            for (int i = 0; i < enemies.Count; i++)
            {
                // if the enemy can't update for some reason, 
                // (he ran out of action points for example)
                // go to the next one
                if (!enemies[i].DoAction())
                    continue;
                    
                // wait while the current enemy is busy
                while (enemies[i].inAction)
                    yield return new WaitForEndOfFrame();

                OnActionEnded();

                // if the enemy still has action points, 
                // this is not the final action
                if (enemies[i].currentActionPoints > 0)
                    finalAction = false;

                // and wait for the turn delay
                // from this enemy to the next enemy
                yield return new WaitForSeconds(turnDelay);
            }
        }
        // but stop when this is the final action
        while (!finalAction);

        // wait for the turn delay 
        // from enemies to hero
        yield return new WaitForSeconds(turnDelay);

        // and we're done moving, 
        // it's your turn now, hero!
        hero.StartTurn();
        herosTurn = true;
        enemiesMoving = false;
    }
    #endregion

    #region END_GAME
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
    #endregion

    #region SAVE_DATA
    private void ReadSaveData()
    {
        // read save data
        if (File.Exists(Application.dataPath + saveDataPath))
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
    }

    private void WriteSaveData()
    {
        File.WriteAllText(Application.dataPath + saveDataPath, JsonUtility.ToJson(saveData));
    }
    #endregion
}