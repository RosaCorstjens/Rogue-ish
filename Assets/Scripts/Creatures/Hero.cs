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
        if (GameManager.instance.saveData.floor == 1)
            GameManager.instance.saveData.heroHealth = health.currentHealth;
        else
            health.SetHealth(GameManager.instance.saveData.heroHealth);

        healthUI = GameObject.FindObjectOfType<HealthUI>().GetComponent<HealthUI>();
        healthUI.Initialize(health.currentHealth, health.totalHealth);
    }

    private void OnDisable()
    {
        // store variables in game manager before changing levels
        GameManager.instance.saveData.heroHealth = health.currentHealth;
    }

    private void Update()
    {
        if (!GameManager.instance.herosTurn || inAction) return;

        StartAction();

        // get the input
        Coordinate moveDirection = Coordinate.zero;
        moveDirection.x = (int)Input.GetAxisRaw("Horizontal");
        moveDirection.y = (int)Input.GetAxisRaw("Vertical");

        // prevent from going diagonally
        if (moveDirection.x != 0)
            moveDirection.y = 0;

        // attempt a move if we have input
        if (!moveDirection.Equals(Coordinate.zero))
            AttemptMove(moveDirection);
    }

    protected override void EndAction()
    {
        base.EndAction();

        // make the hero wait the turn delay time in between actions
        inAction = true;

        StartCoroutine(WaitForEndAction());
    }

    private IEnumerator WaitForEndAction()
    {
        // wait 
        yield return new WaitForSeconds(GameManager.instance.turnDelay);

        // actually end the action
        inAction = false;
        GameManager.instance.OnActionEnded();

        if (currentActionPoints <= 0)
            GameManager.instance.herosTurn = false;

        yield return null;
    }

    internal override void OnActionEnded()
    {
        base.OnActionEnded();

        // if it's not our turn
        if (GameManager.instance.herosTurn)
            return;

        // check to see whether we should be marked with an attack target
        bool targeted = false;
        foreach(Enemy enemy in GameManager.instance.enemies)
        {
            // if the enemy is in 1 distance of us, he can attack us directly
            if(Coordinate.Distance(enemy.tile, this.tile) <= 1)
            {
                targeted = true;

                // we don't wanna search for other enemies anymore, 
                // we know we're in range of at least one
                break;
            }
        }

        // set the target marker accordingly
        SetTargeted(targeted);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // we only get here for objects that didn't block our movement
        // so at this point we basically walked on top of it

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

    #region HEALTH
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
    #endregion

    protected override IEnumerator OnHitAfterMoveAttempt(RaycastHit2D hit)
    {
        // if I collided with an enemy, 
        // deal self dmg and end action
        if(hit.transform.tag == "Enemy")
        {
            ChangeHealth(-1);
            EndAction();
        }

        // else, don't do anything, 
        // the player can still use this action point

        yield return null;
    }
}
