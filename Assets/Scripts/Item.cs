using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Item : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprRender;

    private ItemData itemData;

    public void Initialize(ItemData itemData)
    {
        this.itemData = itemData;

        // set vars from the item data
        gameObject.name = itemData.name;
        sprRender.sprite = Resources.Load<Sprite>("Sprites/" + itemData.spritePath);
    }
}