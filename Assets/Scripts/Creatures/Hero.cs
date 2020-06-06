using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : Creature
{
    private HealthUI healthUI;

    protected override void Start()
    {
        base.Start();

        GameManager.instance.hero = this;

        // set the health for the first floor, 
        // retrieve it for all the others
        if (GameManager.instance.floor == 1)
            GameManager.instance.heroHealth = health.currentHealth;
        else
            health.SetHealth(GameManager.instance.heroHealth);

        healthUI = GameObject.FindObjectOfType<HealthUI>().GetComponent<HealthUI>();
        healthUI.Initialize(health.currentHealth, health.totalHealth);
    }

    private void OnDisable()
    {
        // store variables in game manager before changing levels
        GameManager.instance.heroHealth = health.currentHealth;
    }

    private void Update()
    {
        if (!GameManager.instance.herosTurn) return;

        StartTurn();

        // get the input
        Coordinate moveDirection = Coordinate.zero;
        moveDirection.x = (int)Input.GetAxisRaw("Horizontal");
        moveDirection.y = (int)Input.GetAxisRaw("Vertical");

        // prevent from going diagonally
        if (moveDirection.x != 0)
            moveDirection.y = 0;

        // attempt a move if we have input
        // TODO: interface for interaction and gone be the generic shit
        if (!moveDirection.Equals(Coordinate.zero))
            AttemptMove(moveDirection);
    }

    protected override void EndTurn()
    {
        base.EndTurn();
        GameManager.instance.herosTurn = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // check with what we collided with
        if(collision.tag == "Exit")
        {
            // end the level
            GameManager.instance.NextFloor();
            enabled = false;
        }
        else if (collision.tag == "HealthPotion")
        {
            // gain health yay!
            health.ChangeHealth(1);
            Destroy(collision.gameObject);
        }
    }

    internal override void ChangeHealth(int change)
    {
        if(change < 0)
            anim.SetTrigger("hit");
        base.ChangeHealth(change);
    }
    
    protected override void OnHealthChanged()
    {
        healthUI.UpdateHearts(health.currentHealth);
    }

    protected override void OnDie()
    {
        GameManager.instance.GameOver();
    }

    protected override IEnumerator OnCantMove(Creature other)
    {
        // TODO: deal damage?
        EndTurn();

        yield return null;
    }
}
