using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Sprite fullHeartImage;
    [SerializeField] private Sprite emptyHeartImage;

    private Image[] hearts;

    internal void Initialize(int health, int totalHealth)
    {
        hearts = new Image[totalHealth];

        for (int i = 0; i < totalHealth; i++)
            hearts[i] = GameObject.Instantiate(heartPrefab, this.transform)
                .GetComponent<Image>();

        UpdateHearts(health);
    }

    internal void UpdateHearts(int health)
    {
        for(int i = 0; i < hearts.Length; i++)
        {
            if (health <= i)
                hearts[i].sprite = emptyHeartImage;
            else
                hearts[i].sprite = fullHeartImage;
        }
    }
}
