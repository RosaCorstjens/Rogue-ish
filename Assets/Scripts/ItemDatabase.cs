using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ItemDatabase 
{
    public ItemData[] items;
}

[Serializable]
public class ItemData
{
    public string name;
    public string description;
    public string spritePath;
}

