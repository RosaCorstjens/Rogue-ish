using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]

public abstract class Creature : MonoBehaviour
{
    // movement variabels
    [Header("Movement Settings")]
    [SerializeField] internal float moveTime = 0.5f;
    [SerializeField] protected LayerMask blockingLayer;
    protected bool moving = false;
    private float inverseMoveTime;

    // action points variabels
    [Header("Action Point Settings")]
    [SerializeField] protected int totalActionPoints = 1;
    public int currentActionPoints { get; protected set; }

    // whether or not we're currently busy
    public bool inAction { get; protected set; }

    // references to the highlight 
    // used for turn indication and targeted indication
    private GameObject highlightGameObject;
    private SpriteRenderer highlightSpriteRender;

    // references to components
    private new BoxCollider2D collider;
    private Rigidbody2D rigidBody;
    protected Animator anim;
    protected Health health;

    // handy properties 
    internal Vector2 center { get { return collider.bounds.center; } }
    internal Coordinate tile { get { return GameManager.instance.dungeon.GetGridPosition(center); } }

    protected virtual void Start()
    {
        // get components
        collider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        anim = transform.Find("Creature").GetComponent<Animator>();
        health = GetComponent<Health>();
        health.Initialize();

        // create highlight
        highlightGameObject = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Target"), this.transform);
        highlightSpriteRender = highlightGameObject.GetComponent<SpriteRenderer>();
        highlightGameObject.SetActive(false);

        inverseMoveTime = 1f / moveTime;
    }

    #region HEALTH
    internal virtual void ChangeHealth(int change)
    {
        health.ChangeHealth(change);
    }

    protected abstract void OnHealthChanged();
    protected abstract void OnDie();
    #endregion

    #region TURN_BASED_SYSTEM
    internal virtual void StartTurn()
    {
        // set action points
        currentActionPoints = totalActionPoints;
    }

    protected virtual void StartAction()
    {
        // set the highlight object to 'its my turn'
        highlightGameObject.SetActive(true);
        highlightSpriteRender.color = GameManager.instance.activeTurnColor;
    }

    protected virtual void EndAction()
    {
        // decreate action points per turn
        // if actions have been performed the cost more action points
        // that action will have removed them
        currentActionPoints--;

        // set the highlight object
        highlightGameObject.SetActive(false);
    }

    internal virtual void OnActionEnded()
    {

    }
    #endregion

    protected void SetTargeted(bool active)
    {
        // set the highlighted object as if targeted
        highlightGameObject.SetActive(active);
        if (active)
            highlightSpriteRender.color = GameManager.instance.targetedColor;
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

        // linecast for hitting the blocking layer
        hit = Physics2D.Linecast(center, end, blockingLayer);

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
        moving = inAction = true;

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

        moving = inAction = false;

        EndAction();
    }

    protected virtual bool AttemptMove(Coordinate direction)
    {
        RaycastHit2D hit;

        // store whether we can move in given direction
        bool canMove = Move(direction, out hit);

        // if we can't move and we found something to interact with
        // deal with the consequences!@
        if (!canMove && hit.transform != null)
            StartCoroutine(OnHitAfterMoveAttempt(hit));

        // and deal whether we could move
        return canMove;
    }

    // will be overriden by functions in the child classes
    protected abstract IEnumerator OnHitAfterMoveAttempt(RaycastHit2D hit);
    #endregion
}
