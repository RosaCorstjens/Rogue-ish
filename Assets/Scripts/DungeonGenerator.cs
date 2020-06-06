using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public enum Direction
{
    North, East, South, West
}

[Serializable]
public struct Range
{
    public int min, max;

    public Range(int min, int max)
    {
        this.min = min;
        this.max = max;
    }

    public int GetRndm()
    {
        return Random.Range(min, max);
    }
}

public struct Coordinate
{
    internal int x;
    internal int y;

    internal static Coordinate zero = new Coordinate(0, 0);

    internal Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    internal static float Distance(Coordinate from, Coordinate to)
    {
        return Mathf.Sqrt(Mathf.Pow(from.x - to.x, 2) + Mathf.Pow(from.y - to.y, 2));
    }
}

public class DungeonGenerator : MonoBehaviour
{ 
    public enum Theme { Default, Ice, Fire }

    [Serializable]
    public struct ThematicAssetData
    {
        [SerializeField] internal int startFloor;
        [SerializeField] internal int endFloor;

        [SerializeField] internal GameObject[] floorTiles;
        [SerializeField] internal GameObject[] wallTiles;
        [SerializeField] internal GameObject exit;
    }

    [Serializable]
    public struct EnemyAssetData
    {
        [SerializeField] internal List<Theme> validThemes;
        [SerializeField] internal GameObject enemy;
    }

    public class Room
    {
        internal int x, y;
        internal int width, height;
        internal Direction enteringCorridor;

        // used for first room, since no entering corridor is required
        internal void SetupRoom(Range widthRange, Range heightRange)
        {
            // get a rndm width and height
            width = widthRange.GetRndm();
            height = heightRange.GetRndm();

            // place it roughly in the center
            x = Mathf.RoundToInt(GameManager.instance.dungeon.width / 2f - width / 2f);
            y = Mathf.RoundToInt(GameManager.instance.dungeon.height / 2f - height / 2f);
        }

        // used for all other rooms
        internal void SetupRoom(Range widthRange, Range heightRange, Corridor corridor)
        {
            // set entering corridor
            enteringCorridor = corridor.direction;

            // set rndm width and height
            width = widthRange.GetRndm();
            height = heightRange.GetRndm();

            switch (corridor.direction)
            {
                // if corridor is going north ...
                case Direction.North:
                    // ... height mustn't go beyond grid
                    height = Mathf.Clamp(height, 1, GameManager.instance.dungeon.height - 1 - corridor.EndY);

                    // ... y position is at the end of the corridor
                    y = corridor.EndY;

                    // ... x position randomly between allowed range
                    x = Random.Range(corridor.EndX - width + 1, corridor.EndX - 1);

                    // ... and clamp the x position to keep it on the grid
                    x = Mathf.Clamp(x, 1, GameManager.instance.dungeon.width - 1 - width);
                    break;
                case Direction.East:
                    width = Mathf.Clamp(width, 1, GameManager.instance.dungeon.width - 1 - corridor.EndX);
                    x = corridor.EndX;
                    y = Random.Range(corridor.EndY - height + 1, corridor.EndY);
                    y = Mathf.Clamp(y, 1, GameManager.instance.dungeon.height - 1 - height);
                    break;
                case Direction.South:
                    height = Mathf.Clamp(height, 2, corridor.EndY);
                    y = corridor.EndY - height + 1;
                    x = Random.Range(corridor.EndX - width + 1, corridor.EndX);
                    x = Mathf.Clamp(x, 1, GameManager.instance.dungeon.width - 1 - width);
                    break;
                case Direction.West:
                    width = Mathf.Clamp(width, 2, corridor.EndX);
                    x = corridor.EndX - width + 1;
                    y = Random.Range(corridor.EndY - height + 1, corridor.EndY);
                    y = Mathf.Clamp(y, 1, GameManager.instance.dungeon.height - 1 - height);
                    break;
            }
        }

        internal Coordinate GetRandomTile()
        {
            return new Coordinate(x + Random.Range(0, width - 1), y + Random.Range(0, height - 1));
        }

        internal Coordinate GetCenterTile()
        {
            return new Coordinate(Mathf.RoundToInt(x + width / 2f), Mathf.RoundToInt(y + height / 2f));
        }
    }

    public class Corridor
    {
        internal int startX, startY;
        internal int length;
        internal Direction direction;

        internal int EndX
        {
            get
            {
                if (direction == Direction.North || direction == Direction.South)
                    return startX;
                if (direction == Direction.East)
                    return startX + length - 1;
                return startX - length + 1;
            }
        }

        internal int EndY
        {
            get
            {
                if (direction == Direction.East || direction == Direction.West)
                    return startY;
                if (direction == Direction.North)
                    return startY + length - 1;
                return startY - length + 1;
            }
        }

        internal void SetupCorridor(Room room, Range lengthRange, Range roomWidthRange, Range roomHeightRange, bool first = false)
        {
            // set rndm direction
            direction = (Direction)Random.Range(0, 4);

            // find opposite direction
            Direction oppositeDirection = (Direction)(((int)room.enteringCorridor + 2) % 4);

            // if not the first and going back on the last room
            // rotate the direction so it's not going back
            if (!first && direction == oppositeDirection)
                direction = (Direction)(((int)direction + 1) % 4);

            // set rndm length
            length = lengthRange.GetRndm();

            // make sure we're not going of the grid
            int maxLength = lengthRange.max;

            switch (direction)
            {
                // if going north (up) ... 
                case Direction.North:
                    // ... start on x-axis within room width
                    startX = Random.Range(room.x, room.x + room.width - 1);

                    // ... start on y-axis is top of the room
                    startY = room.y + room.height;

                    // ... and make sure the max length doesn't go off the grid
                    // ... and allows for another room
                    maxLength = GameManager.instance.dungeon.height - 1 - startY - roomHeightRange.min;
                    break;
                case Direction.East:
                    startX = room.x + room.width;
                    startY = Random.Range(room.y, room.y + room.height - 1);
                    maxLength = GameManager.instance.dungeon.width - 1 - startX - roomWidthRange.min;
                    break;
                case Direction.South:
                    startX = Random.Range(room.x, room.x + room.width);
                    startY = room.y;
                    maxLength = startY - 1 - roomHeightRange.min;
                    break;
                case Direction.West:
                    startX = room.x;
                    startY = Random.Range(room.y, room.y + room.height);
                    maxLength = startX - 1 - roomWidthRange.min;
                    break;
            }

            // clamp the length so it's in bounds
            length = Mathf.Clamp(length, 1, maxLength);
        }
    }

    [Header("Seed Settings")]
    [SerializeField] private bool randomizeSeed = false;

    [Header("Dungeon Settings")]
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 50;
    [SerializeField] private Range numRooms = new Range(5, 10);
    [SerializeField] private Range roomWidth = new Range(3, 6);
    [SerializeField] private Range roomHeight = new Range(3, 6);
    [SerializeField] private Range corridorLength = new Range(2, 5);

    [Header("Difficulty Settings")]
    [SerializeField] private Range baseEnemyCount = new Range(5, 15);
    [SerializeField] private int enemyIncreasePerFloor = 2;

    [Header("Assets")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] internal EnemyAssetData[] enemyAssetDatas;
    [SerializeField] private GameObject itemPrefab;

    private List<GameObject> validEnemies;
    private Dictionary<Theme, ThematicAssetData> thematicAssets;
    
    private GameObject dungeonParent;

    private bool[,] grid;
    private Room[] rooms;
    private Corridor[] corridors;
    private Theme theme;

    internal void LoadAssets()
    {
        thematicAssets = new Dictionary<Theme, ThematicAssetData>();
        validEnemies = new List<GameObject>();

        ThematicAssetData data = default;

        foreach (Theme theme in Enum.GetValues(typeof(Theme)))
        {
            // load all assets
            data.floorTiles = Resources.LoadAll<GameObject>("Prefabs/Dungeon/"+theme.ToString()+"/Floors/");
            data.wallTiles = Resources.LoadAll<GameObject>("Prefabs/Dungeon/" + theme.ToString() + "/Walls/");
            data.exit = Resources.Load<GameObject>("Prefabs/Dungeon/" + theme.ToString() + "/Exit");

            // set the start and end floor hard coded 
            // (could be loaded from file or something like that)
            switch (theme)
            {
                case Theme.Default:
                    data.startFloor = 0;
                    data.endFloor = 10;
                    break;
                case Theme.Ice:
                    data.startFloor = 10;
                    data.endFloor = 20;
                    break;
                case Theme.Fire:
                    data.startFloor = 20;
                    data.endFloor = 50;
                    break;
            }

            thematicAssets.Add(theme, data);
        }
    }

    internal void GenerateDungeon(int floor, bool first)
    {
        // setup the seed 
        if (randomizeSeed && !first)
            GameManager.instance.saveData.lastSeed = Random.Range(0, 10000);
        Random.InitState(GameManager.instance.saveData.lastSeed);

        // setup grid
        grid = new bool[width, height];
        dungeonParent = this.gameObject;
        
        // execute the generation
        CreateRoomsAndCorridors();
        SetTileValues();
        PickTheme();
        InstantiateDungeon();
        SpawnContent();
    }

    private void CreateRoomsAndCorridors()
    {
        // create arrays
        rooms = new Room[numRooms.GetRndm()];
        corridors = new Corridor[rooms.Length - 1];

        // create the first
        rooms[0] = new Room();
        corridors[0] = new Corridor();

        // setup first room
        rooms[0].SetupRoom(roomWidth, roomHeight);

        // setup first corridor using first room
        corridors[0].SetupCorridor(rooms[0], corridorLength, roomWidth, roomHeight, true);

        // start at one because we already did the first room
        // and create the rest of the dungeon
        for(int i = 1; i < rooms.Length; i++)
        {
            // create room
            rooms[i] = new Room();

            // setup based on last corridor
            rooms[i].SetupRoom(roomWidth, roomHeight, corridors[i - 1]);

            // if there are still more corridors to be placed
            // this way we won't place for the last room
            if(i < corridors.Length)
            {
                // create corridor
                corridors[i] = new Corridor();

                // set up based on just created room
                corridors[i].SetupCorridor(rooms[i], corridorLength, roomWidth, roomHeight);
            }
        }
    }

    private void SetTileValues()
    {
        // set all tiles that are part of a room to true
        // meaning they are part of the dungeon
        Room currentRoom = null;
        for(int i = 0; i < rooms.Length; i++)
        {
            // get the room
            currentRoom = rooms[i];
            
            // loop through it's width ... 
            for(int j = 0; j < currentRoom.width; j++)
            {
                int x = currentRoom.x + j;

                // ... and height
                for(int k = 0; k < currentRoom.height; k++)
                {
                    // get coordinate of this tile
                    int y = currentRoom.y + k;

                    // and mark it
                    grid[x, y] = true;
                }
            }
        }

        // do the same for corridors
        Corridor currentCorridor = null;
        for(int i = 0; i < corridors.Length; i++)
        {
            currentCorridor = corridors[i];

            for(int j = 0; j < currentCorridor.length; j++)
            {
                int x = currentCorridor.startX;
                int y = currentCorridor.startY;

                switch (currentCorridor.direction)
                {
                    case Direction.North:
                        y += j;
                        break;
                    case Direction.East:
                        x += j;
                        break;
                    case Direction.South:
                        y -= j;
                        break;
                    case Direction.West:
                        x -= j;
                        break;
                }

                grid[x, y] = true;
            }
        }
    }

    private void PickTheme()
    {
        List<Theme> validThemes = new List<Theme>();

        // find all theme's that may occur on this floor
        foreach(KeyValuePair<Theme, ThematicAssetData> t in thematicAssets)
        {
            if(t.Value.startFloor <= GameManager.instance.saveData.floor && 
                t.Value.endFloor >= GameManager.instance.saveData.floor)
            {
                validThemes.Add(t.Key);
            }
        }

        // mssg if we didn't find a valid theme
        if(validThemes.Count == 0)
        {
            Debug.LogError("No valid theme for floor " + GameManager.instance.saveData.floor);

            // and add default as valid so we can continue
            validThemes.Add(Theme.Default);
        }

        // pick a random theme
        theme = validThemes[Random.Range(0, validThemes.Count)];

        // find all enemies for this theme
        validEnemies.Clear();

        for(int i = 0; i < enemyAssetDatas.Length; i++)
        {
            if (enemyAssetDatas[i].validThemes.Contains(theme))
                validEnemies.Add(enemyAssetDatas[i].enemy);
        }
    }

    private void InstantiateDungeon()
    {
        // loop through the grid to place floor tiles
        // with GetLength() you get the length of a given dimension 
        Coordinate coordinate = Coordinate.zero;

        for(int x = 0; x < grid.GetLength(0); x++)
        {
            for(int y = 0; y < grid.GetLength(1); y++)
            {
                coordinate.x = x;
                coordinate.y = y;

                // if this is a floor tile
                if (grid[x, y])
                    InstantiateRandom(thematicAssets[theme].floorTiles, coordinate);

                // if this is an edge
                if (IsEdge(coordinate))
                    InstantiateRandom(thematicAssets[theme].wallTiles, coordinate);
            }
        }
    }

    private void SpawnContent()
    {
        // spawn start and exit
        SpawnHero();
        SpawnExit();

        // calculate amount of enemies based on floor
        int enemyAmount = GameManager.instance.saveData.floor * enemyIncreasePerFloor + baseEnemyCount.GetRndm();

        // spawn enemies randomly, but not in start / end room
        for(int i = 0; i < enemyAmount; i++)
        {
            // determine position
            Coordinate coordinate = GetRandomRoom(true).GetRandomTile();

            // spawn a random enemy
            // and add it to the game managers enemies
            InstantiateRandom(validEnemies.ToArray(), coordinate).GetComponent<Enemy>();
        }

        // spawn a random item in the same room as the hero
        Item item = GameObject.Instantiate(itemPrefab, GetWorldPosition(rooms[0].GetRandomTile()), Quaternion.identity).GetComponent<Item>();
        item.Initialize(GameManager.instance.itemDatabase.items[Random.Range(0, GameManager.instance.itemDatabase.items.Length)]);
    }

    private void SpawnHero()
    {
        Coordinate startPos = rooms[0].GetCenterTile();
        GameObject.Instantiate(heroPrefab, GetWorldPosition(startPos), Quaternion.identity);
    }

    private void SpawnExit()
    {
        Coordinate endPos = rooms[rooms.Length - 1].GetCenterTile();
        GameObject.Instantiate(thematicAssets[theme].exit, GetWorldPosition(endPos), Quaternion.identity);
    }

    private GameObject InstantiateRandom(GameObject[] options, Coordinate coordinate)
    {
        // pick one at random
        int index = Random.Range(0, options.Length);

        // create the object
        GameObject gameObject = GameObject.Instantiate(options[index],
                                                       GetWorldPosition(coordinate),
                                                       Quaternion.identity,
                                                       dungeonParent.transform);

        return gameObject;
    }

    internal Vector2 GetWorldPosition(Coordinate coordinate)
    {
        return new Vector2(coordinate.x * GameManager.instance.scale, coordinate.y * GameManager.instance.scale);
    }

    internal Coordinate GetGridPosition(Vector2 pos)
    {
        return new Coordinate(Mathf.RoundToInt(pos.x / GameManager.instance.scale), Mathf.RoundToInt(pos.y / GameManager.instance.scale));
    }

    private bool IsEdge(Coordinate coordinate)
    {
        // this is an edge if ... 
        // ... it's not a floor tile
        // ... and if it has at least one floor tile as neighbour
        if (!grid[coordinate.x, coordinate.y] && HasNeighbours(coordinate))
            return true;

        return false;
    }

    private bool HasNeighbours(Coordinate coordinate)
    {
        int x = coordinate.x;
        int y = coordinate.y;

        if (x - 1 >= 0)
        {
            if (grid[x - 1, y])
                return true;

            // and diagonals
            if (y - 1 >= 0)
                if (grid[x - 1, y - 1])
                    return true;
            if (y + 1 < height)
                if (grid[x - 1, y + 1])
                    return true;
        }

        // to the right
        if (x + 1 < width)
        {
            if (grid[x + 1, y])
                return true;

            // and diagonals
            if (y - 1 >= 0)
                if (grid[x + 1, y - 1])
                    return true;
            if (y + 1 < height)
                if (grid[x + 1, y + 1])
                    return true;
        }

        // and up and down
        if (y - 1 >= 0)
            if (grid[x, y - 1])
                return true;

        if (y + 1 < height)
            if (grid[x, y + 1])
                return true;

        // still no neighbour? well, return false 
        return false;
    }

    private List<Coordinate> GetNeighbours(Coordinate coordinate)
    {
        List<Coordinate> neighbours = new List<Coordinate>();

        int x = coordinate.x;
        int y = coordinate.y;

        // find and add all eight neighbours
        // if they exist

        // to the left
        if (x - 1 >= 0)
        {
            neighbours.Add(new Coordinate(x - 1, y));

            // and diagonals
            if (y - 1 >= 0)
                neighbours.Add(new Coordinate(x - 1, y - 1));
            if (y + 1 < height)
                neighbours.Add(new Coordinate(x - 1, y + 1));
        }
        
        // to the right
        if (x + 1 < width)
        {
            neighbours.Add(new Coordinate(x + 1, y));

            // and diagonals
            if (y - 1 >= 0)
                neighbours.Add(new Coordinate(x + 1, y - 1));
            if (y + 1 < height)
                neighbours.Add(new Coordinate(x + 1, y + 1));
        }
         
        // and up and down
        if (y - 1 >= 0)
            neighbours.Add(new Coordinate(x, y - 1));
        if (y + 1 < height)
            neighbours.Add(new Coordinate(x, y + 1));

        return neighbours;
    }

    private Room GetRandomRoom(bool excludeStartEnd = false)
    {
        if(!excludeStartEnd)
            return rooms[Random.Range(0, rooms.Length - 1)];
        else
            return rooms[Random.Range(1, rooms.Length - 2)];
    }
}
