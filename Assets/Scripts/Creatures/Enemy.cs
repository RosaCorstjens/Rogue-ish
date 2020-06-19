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

    private static Coordinate[] possibleMoveCoordinates = new Coordinate[] { new Coordinate(0, 1),
                                                                             new Coordinate(1, 0),
                                                                             new Coordinate(0, -1),
                                                                             new Coordinate(-1, 0)};
    private bool[] allowedMoveCoordinates = new bool[4];

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

        // hier 4 directions
        for (int i = 0; i < allowedMoveCoordinates.Length; i++)
            allowedMoveCoordinates[i] = false;

        // check hit in ieder
        RaycastHit2D hit;
        Vector2 start, end;
        start = transform.position;

        for (int i = 0; i < possibleMoveCoordinates.Length; i++)
        {
            end = start + new Vector2(possibleMoveCoordinates[i].x * GameManager.instance.scale, possibleMoveCoordinates[i].y * GameManager.instance.scale);

            // linecast for hitting the blocking layer
            hit = Physics2D.Linecast(center, end, blockingLayer);

            // check for hit
            if (hit.transform != null && hit.transform.tag == "Player")
                allowedMoveCoordinates[i] = true;
            else
                allowedMoveCoordinates[i] = hit.transform == null;
        }

        // kunnen we uberhaupt een kant op?
        bool canMove = false;
        for (int i = 0; i < allowedMoveCoordinates.Length; i++)
        {
            if (allowedMoveCoordinates[i])
            {
                canMove = true;
                break;
            }
        }

        if (!canMove)
            return false;

        // bepaal welke we t liefst op willen 
        Coordinate moveDirection = Coordinate.zero;

        // voorkeur: match player op x as first, anders op y
        if (Mathf.Abs(target.tile.x - tile.x) < 1)
        {
            moveDirection.y = target.tile.y > tile.y ? 1 : -1;
        }
        else
        {
            moveDirection.x = target.tile.x > tile.x ? 1 : -1;
        }

        // mag ik deze kant op?
        canMove = false;
        for (int i = 0; i < possibleMoveCoordinates.Length; i++)
        {
            if (possibleMoveCoordinates[i].Equals(moveDirection))
            {
                canMove = allowedMoveCoordinates[i];
                break;
            }
        }

        if (!canMove)
        {
            moveDirection = Coordinate.zero;

            // voorkeur: match player op y as first, anders op x
            if (Mathf.Abs(target.tile.y - tile.y) < 1)
            {
                moveDirection.x = target.tile.x > tile.x ? 1 : -1;
            }
            else
            {
                moveDirection.y = target.tile.y > tile.y ? 1 : -1;
            }

            // mag ik deze kant op?
            canMove = false;
            for (int i = 0; i < possibleMoveCoordinates.Length; i++)
            {
                if (possibleMoveCoordinates[i].Equals(moveDirection))
                {
                    canMove = allowedMoveCoordinates[i];
                    break;
                }
            }
        }

        // hij was niet valid 
        if (!canMove)
        {
            for (int i = 0; i < possibleMoveCoordinates.Length; i++)
            {
                if (allowedMoveCoordinates[i])
                {
                    moveDirection = possibleMoveCoordinates[i];
                    break;
                }
            }
        }

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
        else 
        {
            Debug.Log("Hit: " + hit.transform.tag);
            yield return null;
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
