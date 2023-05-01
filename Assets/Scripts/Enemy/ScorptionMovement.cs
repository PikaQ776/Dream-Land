using UnityEngine;
using Fusion;
using Pathfinding;
using System;
// using static StatusKeeper;
[OrderAfter(typeof(NetworkPhysicsSimulation2D))]
public class ScorptionMovement : EnemyAI
{
    [SerializeField] private LayerMask _playerLayer;

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] float _speed = 400f;
    [SerializeField] float _defaultGravity = 1f;
    private NetworkRigidbody2D _rb;
    private Collider2D _collider;
    private ScorptionBehavior _behaviour;

    [Header("Chase")]
    public float nextWaypointDistance = 3f;
    Seeker _seeker;
    Path path;
    int currentWaypoint = 0;
    private Vector3 targetPosition;


    [Header("Attack")]
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackCooldown = 4f;
    [SerializeField] int attackDamage = 10;
    private float lastAttackTime;


    private StatusKeeper statusKeeper;
    public StatusPublisher statusPublisher;

    private int life = 20;
    public Vector3 getTargetPosition()
    {
        return targetPosition;
    }

    public Vector2 get2DMoveDirection()
    {
        if (path == null)
        {
            return new Vector2();
        }
        return ((Vector2)path.vectorPath[currentWaypoint] - (Vector2)_rb.ReadPosition()).normalized;
    }
    void Start()
    {

    }
    void Awake()
    {

        _seeker = GetComponent<Seeker>();
        _rb = GetComponent<NetworkRigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
        _behaviour = GetBehaviour<ScorptionBehavior>();
        statusKeeper = new StatusKeeper();
        statusPublisher = new StatusPublisher();
    }

    public override void Spawned()
    {
        InvokeRepeating("SetNearestPlayerToTarget", 2f, 2f);
    }
    public override void TakeDamage(int damage, Vector2 force)
    {
        statusPublisher.Broadcast(StatusPublisher.StatusType.Hurt);
        _rb.Rigidbody.AddForce(force * Runner.DeltaTime, ForceMode2D.Force);
        life -= 10;
        if (life <= 0)
        {
            _behaviour.SetInputsAllowed(false);
            statusPublisher.Broadcast(StatusPublisher.StatusType.Die);
        }

    }
    public void Death()
    {
        _behaviour.RespawnsScorption();
        life = 20;
    }
    public void AddStatusSubscriber(EventHandler<StatusPublisher.StatusType> statusEventHandler)
    {
        statusPublisher.StatusEvent += statusEventHandler;
    }
    private void SetNearestPlayerToTarget()
    {


        GameObject[] targetCandidates = GameObject.FindGameObjectsWithTag("Player");
        GameObject targetObject = GetNearestObj(targetCandidates);
        targetPosition = targetObject.transform.position;
        if (targetPosition != default(Vector3) && _seeker.IsDone())
        {
            _seeker.StartPath(_rb.ReadPosition(), targetPosition, OnPathComplete);
        }
    }

    private GameObject GetNearestObj(GameObject[] objArr)
    {
        if (objArr.Length == 0)
            return null;

        float minDistance = float.MaxValue;
        GameObject nearestObj = objArr[0];
        Vector2 selfPosition = (Vector2)_rb.ReadPosition();

        foreach (GameObject obj in objArr)
        {
            float currentDistance = Vector2.Distance((Vector2)obj.transform.position, selfPosition);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                nearestObj = obj;
            }
        }
        return nearestObj;
    }
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }
    public override void FixedUpdateNetwork()
    {
        ChaseAndAttackTarget();
    }
    private void ChaseAndAttackTarget()
    {
        if (path == null && !_behaviour.InputsAllowed)
        {
            return;
        }

        if (getRestDistanceToTarget() < attackRange)
        {
            Attack();
            return;
        }
        ChaseTarget();


    }
    private float getRestDistanceToTarget()
    {
        return Vector2.Distance(_rb.ReadRigidbodyPosition(), targetPosition);
    }
    private void Attack()
    {
        if (Time.time > lastAttackTime + attackCooldown)
        {

            // statusKeeper.SetStatus(StatusKeeper.StatusType.Attack);
            statusPublisher.Broadcast(StatusPublisher.StatusType.Attack);
            lastAttackTime = Time.time;
            TryAttackPlayer();
        }
        else
        {
            statusPublisher.Broadcast(StatusPublisher.StatusType.Idle);
        }
    }
    private void TryAttackPlayer()
    {
        Vector2 attackPoint = default;
        Vector2 damageSize = new Vector2(1f, 3f);
        if (get2DMoveDirection().x > 0)
        {
            attackPoint = _rb.ReadPosition() + Vector3.right * (_collider.bounds.extents.x);
        }
        else
        {
            attackPoint = _rb.ReadPosition() + Vector3.left * (_collider.bounds.extents.x);
        }
        Collider2D playersGetDamage = Runner.GetPhysicsScene2D().OverlapBox(attackPoint, damageSize, _playerLayer);
        if (playersGetDamage)
        {
            PlayerRigidBodyMovement playerGetDamageRb = playersGetDamage.GetComponentInParent<PlayerRigidBodyMovement>();
            if (playerGetDamageRb)
                playerGetDamageRb.TakeDamage();
        }
    }
    private void ChaseTarget()
    {
        MoveToCurrentWaypoint();
        UpdateCurrentWaypoint();
    }

    private void MoveToCurrentWaypoint()
    {
        statusKeeper.SetStatus(StatusKeeper.StatusType.Run);
        Vector2 moveDirection = get2DMoveDirection();

        if (moveDirection.y > 0 && DetectWall())
        {
            _rb.Rigidbody.gravityScale = 0;
            MoveTop();
        }
        else if (moveDirection.y < 0 && DetectWall())
        {

            _rb.Rigidbody.gravityScale = 0;
            MoveDown();
        }
        else if (moveDirection.x > 0)
        {
            _rb.Rigidbody.gravityScale = _defaultGravity;
            MoveRight();
        }
        else if (moveDirection.x < 0)
        {
            _rb.Rigidbody.gravityScale = _defaultGravity;
            MoveLeft();
        }
        else
        {
            _rb.Rigidbody.gravityScale = _defaultGravity;
        }
    }
    private bool DetectWall()
    {

        bool hasWall = Runner.GetPhysicsScene2D().OverlapCircle(transform.position + Vector3.right * (_collider.bounds.extents.x), .1f, _groundLayer);

        if (!hasWall)
        {
            hasWall = Runner.GetPhysicsScene2D().OverlapCircle(transform.position + Vector3.left * (_collider.bounds.extents.x), .1f, _groundLayer);
        }
        return hasWall;
    }

    public StatusKeeper.StatusType GetStatus()
    {
        return statusKeeper.CurrentStatus;
    }
    private void MoveRight()
    {
        _rb.Rigidbody.AddForce(Vector2.right * _speed * Runner.DeltaTime, ForceMode2D.Force);
    }
    private void MoveLeft()
    {
        _rb.Rigidbody.AddForce(Vector2.left * _speed * Runner.DeltaTime, ForceMode2D.Force);
    }
    private void MoveTop()
    {
        _rb.Rigidbody.AddForce(Vector2.up * _speed * Runner.DeltaTime, ForceMode2D.Force);
    }
    private void MoveDown()
    {
        _rb.Rigidbody.AddForce(Vector2.down * _speed * Runner.DeltaTime, ForceMode2D.Force);
    }
    private void UpdateCurrentWaypoint()
    {
        float restDistance = Vector2.Distance((Vector2)_rb.ReadPosition(), path.vectorPath[currentWaypoint]);
        // 
        if (restDistance < nextWaypointDistance && currentWaypoint < path.vectorPath.Count - 1)
        {
            currentWaypoint++;
        }
    }



}
