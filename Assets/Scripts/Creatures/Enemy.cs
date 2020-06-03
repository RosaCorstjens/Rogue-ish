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

    private void Update()
    {
        if (moving || attacking || target == null) return;

        // only attempt to move if the target is close enough
        if (Coordinate.Distance(target.tile, tile) > noticeTargetRange)
            return;

        // determine direction
        Coordinate moveDirection = Coordinate.zero;

        // reached target on x axis?
        // if so move on y axis
        if (Mathf.Abs(target.tile.x - tile.x) < 1)
            moveDirection.y = target.tile.y > tile.y ? 1 : -1;
        else
            moveDirection.x = target.tile.x > tile.x ? 1 : -1;

        if (!moveDirection.Equals(Coordinate.zero))
            AttemptMove(moveDirection);
    }

    protected override IEnumerator OnCantMove(Creature other)
    {
        // deal damage to the player
        if (other.gameObject.tag == "Player")
        {
            other.ChangeHealth(-attackDamage);

            attacking = true;

            yield return new WaitForSeconds(attackCooldown);

            attacking = false;
        }
            
        yield return null;
    }

    protected override void OnDie()
    {
        GameManager.instance.enemiesKilled++;
        GameManager.instance.enemies.Remove(this);

        Destroy(this.gameObject);
    }

    protected override void OnHealthChanged()
    {
        // nothing to see here!
    }
}
