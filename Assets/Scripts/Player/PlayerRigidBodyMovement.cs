using System;
using UnityEngine;
using Fusion;
[OrderAfter(typeof(NetworkPhysicsSimulation2D))]
public class PlayerRigidBodyMovement : NetworkBehaviour
{
    [Header("Movement")]
    private PlayerBehaviour _behaviour;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _EnemyLayer;
    private NetworkRigidbody2D _rb;
    private InputController _inputController;

    [SerializeField] float _speed = 10f;
    [SerializeField] float _jumpForce = 10f;
    [SerializeField] float _maxVelocity = 8f;

    [SerializeField] private float fallMultiplier = 3.3f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    private readonly float wallSlidingMultiplier = 1f;

    private bool lastMoveDirection = true; //0 left, 1 right

    private Vector2 _groundHorizontalDragVector = new Vector2(.1f, 1);
    private Vector2 _airHorizontalDragVector = new Vector2(.98f, 1);
    private Vector2 _horizontalSpeedReduceVector = new Vector2(.95f, 1);
    private Vector2 _verticalSpeedReduceVector = new Vector2(1, .95f);

    private Collider2D _collider;
    [Networked]
    private NetworkBool IsGrounded { get; set; }
    private bool _wallSliding;
    private Vector2 _wallSlidingNormal;

    private float _jumpBufferThreshold = .2f;
    private float _jumpBufferTime;

    [Networked]
    private float CoyoteTimeThreshold { get; set; } = .1f;
    [Networked]
    private float TimeLeftGrounded { get; set; }
    [Networked]
    private NetworkBool CoyoteTimeCD { get; set; }
    [Networked]
    private NetworkBool WasGrounded { get; set; }

    [Space()]
    [Header("Particle")]
    [SerializeField] private ParticleManager _particleManager;

    [Space()]
    [Header("Sound")]
    [SerializeField] private SoundChannelSO _sfxChannel;
    [SerializeField] private SoundSO _jumpSound;
    [SerializeField] private AudioSource _playerSource;

    private int playerLife = 100;

    public StatusPublisher statusPublisher;
    void Awake()
    {
        _rb = GetComponent<NetworkRigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
        _behaviour = GetBehaviour<PlayerBehaviour>();
        _inputController = GetBehaviour<InputController>();
        statusPublisher = new StatusPublisher();
    }

    public override void Spawned()
    {
        Runner.SetPlayerAlwaysInterested(Object.InputAuthority, Object, true);
    }
    public void TakeDamage()
    {
        Debug.Log("TEST");
        statusPublisher.Broadcast(StatusPublisher.StatusType.Hurt);
        lastMoveDirection = true;
        playerLife -= 10;
        if (playerLife <= 0)
        {
            Death();
        }

    }
    public void Death()
    {
        _behaviour.RespawnPlayer();
        playerLife = 100;
    }
    public void AddStatusSubscriber(EventHandler<StatusPublisher.StatusType> statusEventHandler)
    {
        statusPublisher.StatusEvent += statusEventHandler;


    }
    /// <summary>
    /// Detects grounded and wall sliding state
    /// </summary>
    private void DetectGroundAndWalls()
    {
        WasGrounded = IsGrounded;
        IsGrounded = default;
        _wallSliding = default;
        IsGrounded = (bool)Runner.GetPhysicsScene2D().OverlapBox((Vector2)transform.position + Vector2.down * (_collider.bounds.extents.y - 0.3f), Vector2.one * 1.1f, 0, _groundLayer);

        if (IsGrounded)
        {
            CoyoteTimeCD = false;
            return;
        }

        if (WasGrounded)
        {
            if (CoyoteTimeCD)
            {
                CoyoteTimeCD = false;
            }
            else
            {
                TimeLeftGrounded = Runner.SimulationTime;
            }
        }

        _wallSliding = Runner.GetPhysicsScene2D().OverlapCircle(transform.position + Vector3.right * (_collider.bounds.extents.x), .1f, _groundLayer);
        if (_wallSliding)
        {
            _wallSlidingNormal = Vector2.left;
            return;
        }
        else
        {
            _wallSliding = Runner.GetPhysicsScene2D().OverlapCircle(transform.position - Vector3.right * (_collider.bounds.extents.x), .1f, _groundLayer);
            if (_wallSliding)
            {
                _wallSlidingNormal = Vector2.right;
            }
        }

    }

    public bool GetGrounded()
    {
        return IsGrounded;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput<InputData>(out var input))
        {
            var pressed = input.GetButtonPressed(_inputController.PrevButtons);
            _inputController.PrevButtons = input.Buttons;

            UpdateMovement(input);
            Jump(pressed);
            BetterJumpLogic(input);
            Attack(pressed);

        }
    }

    void UpdateMovement(InputData input)
    {
        DetectGroundAndWalls();

        if (input.GetButton(InputButton.LEFT) && _behaviour.InputsAllowed)
        {
            //Reset x velocity if start moving in oposite direction.
            if (_rb.Rigidbody.velocity.x > 0 && IsGrounded)
            {
                _rb.Rigidbody.velocity *= Vector2.up;
            }
            _rb.Rigidbody.AddForce(Vector2.left * _speed * Runner.DeltaTime, ForceMode2D.Force);
            lastMoveDirection = false;

        }
        else if (input.GetButton(InputButton.RIGHT) && _behaviour.InputsAllowed)
        {
            //Reset x velocity if start moving in oposite direction.
            if (_rb.Rigidbody.velocity.x < 0 && IsGrounded)
            {
                _rb.Rigidbody.velocity *= Vector2.up;
            }
            _rb.Rigidbody.AddForce(Vector2.right * _speed * Runner.DeltaTime, ForceMode2D.Force);
            lastMoveDirection = true;
        }
        else
        {
            //Different horizontal drags depending if grounded or not.
            if (IsGrounded)
                _rb.Rigidbody.velocity *= _groundHorizontalDragVector;
            else
                _rb.Rigidbody.velocity *= _airHorizontalDragVector;
        }

        LimitSpeed();
    }

    private void LimitSpeed()
    {
        //Limit horizontal velocity
        if (Mathf.Abs(_rb.Rigidbody.velocity.x) > _maxVelocity)
        {
            _rb.Rigidbody.velocity *= _horizontalSpeedReduceVector;
        }

        if (Mathf.Abs(_rb.Rigidbody.velocity.y) > _maxVelocity * 2)
        {
            _rb.Rigidbody.velocity *= _verticalSpeedReduceVector;
        }
    }

    #region Jump
    private void Jump(NetworkButtons pressedButtons)
    {
        //Jump
        if (pressedButtons.IsSet(InputButton.JUMP) || CalculateJumpBuffer())
        {
            if (_behaviour.InputsAllowed)
            {
                if (!IsGrounded && pressedButtons.IsSet(InputButton.JUMP))
                {
                    _jumpBufferTime = Runner.SimulationTime;
                }

                if (IsGrounded || CalculateCoyoteTime())
                {
                    _rb.Rigidbody.velocity *= Vector2.right; //Reset y Velocity
                    _rb.Rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
                    CoyoteTimeCD = true;
                    if (Runner.Simulation.IsLocalPlayerFirstExecution && Object.HasInputAuthority)
                    {
                        RPC_PlayJumpEffects((Vector2)transform.position - Vector2.up * .5f);
                    }
                }
                else if (_wallSliding)
                {
                    _rb.Rigidbody.velocity *= Vector2.zero; //Reset y and x Velocity
                    _rb.Rigidbody.AddForce((Vector2.up + (_wallSlidingNormal)) * _jumpForce, ForceMode2D.Impulse);
                    CoyoteTimeCD = true;
                    if (Runner.Simulation.IsLocalPlayerFirstExecution && Object.HasInputAuthority)
                    {
                        RPC_PlayJumpEffects((Vector2)transform.position - _wallSlidingNormal * .5f);
                    }
                }
            }
        }
    }

    private bool CalculateJumpBuffer()
    {
        return (Runner.SimulationTime <= _jumpBufferTime + _jumpBufferThreshold) && IsGrounded;
    }

    private bool CalculateCoyoteTime()
    {
        return (Runner.SimulationTime <= TimeLeftGrounded + CoyoteTimeThreshold);
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayJumpEffects(Vector2 particlePos)
    {
        PlayJumpSound();
        PlayJumpParticle(particlePos);
    }

    private void PlayJumpSound()
    {
        _sfxChannel.CallSoundEvent(_jumpSound, Object.HasInputAuthority ? null : _playerSource);
    }

    private void PlayJumpParticle(Vector2 pos)
    {
        _particleManager.Get(ParticleManager.ParticleID.Jump).transform.position = pos;
    }

    /// <summary>
    /// Increases gravity force on the player based on input and current fall progress.
    /// </summary>
    /// <param name="input"></param>
    private void BetterJumpLogic(InputData input)
    {
        if (IsGrounded) { return; }
        if (_rb.Rigidbody.velocity.y < 0)
        {
            if (_wallSliding && input.AxisPressed())
            {
                _rb.Rigidbody.velocity += Vector2.up * Physics2D.gravity.y * (wallSlidingMultiplier - 1) * Runner.DeltaTime;
            }
            else
            {
                _rb.Rigidbody.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Runner.DeltaTime;
            }
        }
        else if (_rb.Rigidbody.velocity.y > 0 && !input.GetButton(InputButton.JUMP))
        {
            _rb.Rigidbody.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Runner.DeltaTime;
        }
    }
    #endregion

    #region Attack
    private void Attack(NetworkButtons pressedButton)
    {
        if (pressedButton.IsSet(InputButton.ATTACK))
        {
            TryAttackEnemy();
            statusPublisher.Broadcast(StatusPublisher.StatusType.Attack);
        }
    }
    private void TryAttackEnemy()
    {
        Vector2 attackPoint = default;
        Vector2 damageSize = new Vector2(2f, 3f);
        SpriteRenderer _renderer = GetComponentInChildren<SpriteRenderer>();
        if (lastMoveDirection)
        {
            attackPoint = _rb.ReadPosition() + Vector3.right * (_collider.bounds.extents.x) * 2;
        }
        else
        {
            attackPoint = _rb.ReadPosition() + Vector3.left * (_collider.bounds.extents.x) * 2;
        }
        Collider2D enemyGetDamage = Runner.GetPhysicsScene2D().OverlapBox(attackPoint, damageSize, 0f, _EnemyLayer);
        if (enemyGetDamage)
        {
            EnemyAI enemyAI = enemyGetDamage.GetComponentInParent<EnemyAI>();
            Vector2 attackForce = ((Vector2)enemyGetDamage.transform.position - (Vector2)_rb.transform.position).normalized;
            enemyAI.TakeDamage(10, attackForce * 8000);
        }



    }
    void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Vector2 attackPoint = default;
        Vector2 damageSize = new Vector2(2f, 3f);
        SpriteRenderer _renderer = GetComponentInChildren<SpriteRenderer>();
        if (lastMoveDirection)
        {
            attackPoint = _rb.ReadPosition() + Vector3.right * (_collider.bounds.extents.x) * 2;
        }
        else
        {
            attackPoint = _rb.ReadPosition() + Vector3.left * (_collider.bounds.extents.x) * 2;
        }
        //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
        Gizmos.DrawWireCube(attackPoint, damageSize);
    }
    #endregion
}
