using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]

public abstract class Creature : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] internal float moveTime = 0.5f;
    [SerializeField] private LayerMask blockingLayer;

    private GameObject targetGameObject;
    private SpriteRenderer targetSpriteRender;

    protected bool moving = false;
    private float inverseMoveTime;

    private new BoxCollider2D collider;
    private Rigidbody2D rigidBody;
    protected Animator anim;
    protected Health health;

    internal Vector2 center { get { return collider.bounds.center; } }
    internal Coordinate tile { get { return GameManager.instance.dungeon.GetGridPosition(center); } }

    protected virtual void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        anim = transform.Find("Creature").GetComponent<Animator>();
        health = GetComponent<Health>();
        health.Initialize();

        targetGameObject = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Target"), this.transform);
        targetSpriteRender = targetGameObject.GetComponent<SpriteRenderer>();
        targetGameObject.SetActive(false);

        inverseMoveTime = 1f / moveTime;
    }

    internal virtual void ChangeHealth(int change)
    {
        health.ChangeHealth(change);
    }

    protected abstract void OnHealthChanged();
    protected abstract void OnDie();

    protected void StartTurn()
    {
        targetGameObject.SetActive(true);
        targetSpriteRender.color = GameManager.instance.activeTurnColor;
    }

    protected virtual void EndTurn()
    {
        targetGameObject.SetActive(false);
    }

    protected void SetTargeted(bool active)
    {
        targetGameObject.SetActive(active);
        if (active)
            targetSpriteRender.color = GameManager.instance.targetedColor;
    }

    #region MOVE
    // returns true if it is able to move 
    protected bool Move(Coordinate direction, out RaycastHit2D hit)
    {
        if (moving)
        {
            hit = default;
            return false;
        }

        Vector2 start = transform.position;
        Vector2 end = start + new Vector2(direction.x * GameManager.instance.scale, direction.y * GameManager.instance.scale);

        // disable collider so that linecast doesn't hit this object's own collider
        //collider.enabled = false;

        // linecast for hitting the blocking layer
        hit = Physics2D.Linecast(center, end, blockingLayer);

        // re-enable collider after linecast
        //collider.enabled = true;

        // check for hit
        if (hit.transform == null)
        {
            // start moving
            StartCoroutine(SmoothMovement(end));

            // moved succesfully
            return true;
        }

        // didn't move
        return false;
    }

    protected IEnumerator SmoothMovement(Vector3 end)
    {
        // calculate remaining distance to move
        float sqrRemainingDistance = (transform.position - end).sqrMagnitude;

        anim.SetTrigger("move");
        moving = true;

        // keep moving until we're very close
        while (sqrRemainingDistance > float.Epsilon)
        {
            // move 
            rigidBody.MovePosition(Vector3.MoveTowards(rigidBody.position, end, inverseMoveTime * Time.deltaTime));

            // recalculate remaining distance 
            sqrRemainingDistance = (transform.position - end).sqrMagnitude;

            // return and loop until end reached
            yield return null;
        }

        EndTurn();

        moving = false;
    }

    // takes generic parameter to specify the type of component expected to interact with if blocked (e.g. Player for Enemies, Wall for Player)
    protected virtual bool AttemptMove(Coordinate direction)
    {
        RaycastHit2D hit;

        // store whether we can move in given direction
        bool canMove = Move(direction, out hit);

        // check for hit and return if so
        if (hit.transform == null)
            return canMove;

        // get a reference to the hit creature 
        Creature hitComponent = hit.transform.GetComponent<Creature>();

        // if we can't move and we found something to interact with
        // deal with the consequences!@
        if (!canMove && hitComponent != null)
            StartCoroutine(OnCantMove(hitComponent));

        return canMove;
    }

    // will be overriden by functions in the child classes
    protected abstract IEnumerator OnCantMove(Creature other);
    #endregion
}
