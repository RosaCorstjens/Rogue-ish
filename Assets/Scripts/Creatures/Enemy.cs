using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Creature
{
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;
    private bool attacking;

    [SerializeField] private float noticeTargetRange = 5;

    private Creature target;

    protected override void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Hero>();
        GameManager.instance.enemies.Add(this);

        base.Start();
    }

    internal bool DoAction()
    {
        // we can't do anymore actions in this turn
        if (currentActionPoints <= 0)
            return false;

        // only attempt to move if we're not busy and the target is close enough
        if (moving || attacking || target == null || Coordinate.Distance(target.tile, tile) > noticeTargetRange)
            return false;

        StartAction();

        // determine direction
        Coordinate moveDirection = Coordinate.zero;

        // reached target on x axis?
        // if so move on y axis
        if (Mathf.Abs(target.tile.x - tile.x) < 1)
            moveDirection.y = target.tile.y > tile.y ? 1 : -1;
        else
            moveDirection.x = target.tile.x > tile.x ? 1 : -1;

        // attempt to move if we found a direction
        if (!moveDirection.Equals(Coordinate.zero))
            AttemptMove(moveDirection);

        return true;
    }

    protected override IEnumerator OnHitAfterMoveAttempt(RaycastHit2D hit)
    {
        // deal damage to the player
        if (hit.transform.tag == "Player")
        {
            hit.transform.GetComponent<Hero>().ChangeHealth(-attackDamage);

            attacking = inAction = true;

            yield return new WaitForSeconds(attackCooldown);

            attacking = inAction = false;
        }

        // should we just end the action
        EndAction();

        yield return null;
    }

    #region HEALTH
    protected override void OnDie()
    {
        GameManager.instance.saveData.enemiesKilled++;
        GameManager.instance.enemies.Remove(this);

        Destroy(this.gameObject);
    }

    protected override void OnHealthChanged()
    {
        // nothing to see here!
    }
    #endregion
}
