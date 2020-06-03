using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] internal int totalHealth = 5;
    internal int currentHealth;
    internal bool dead = false;

    internal void Initialize()
    {
        currentHealth = totalHealth;
    }

    internal void SetHealth(int health)
    {
        currentHealth = health;
        currentHealth = Mathf.Clamp(currentHealth, 0, totalHealth);
    }

    internal void ChangeHealth(int change)
    {
        currentHealth += change;
        currentHealth = Mathf.Clamp(currentHealth, 0, totalHealth);

        SendMessage("OnHealthChanged", SendMessageOptions.RequireReceiver);

        CheckDead();
    }

    private void CheckDead()
    {
        if (currentHealth <= 0)
        {
            dead = true;
            SendMessage("OnDie", SendMessageOptions.RequireReceiver);
        }
    }
}
