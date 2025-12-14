using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float minJumpForce = 4f;
    [SerializeField] private float jumpHoldForce = 10f;
    [SerializeField] private float maxJumpHoldTime = 0.35f;
    [SerializeField] private float jumpBufferTime = 0.15f; // 점프 버퍼 시간
    [SerializeField] private float coyoteTime = 0.15f; // 땅 코요테 타임
    [SerializeField] private float wallCoyoteTime = 0.15f; // 벽 코요테 타임

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Wall Settings")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallClimbSpeed = 3f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallMaxDuration = 1.5f;
    [SerializeField] private float wallJumpHorizontalForce = 7f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackAngle = 90f; // 부채꼴 각도
    [SerializeField] private float attackCooldown = 0.3f; // 공격 쿨타임
    [SerializeField] private LayerMask enemyLayer;

    private enum PlayerState { Idle, Move, Jump, Fall, Dash, WallIdle, WallMove, Attack, Dead }

    [SerializeField] private PlayerState _state = PlayerState.Idle;
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;
    private Vector2 _moveInput;
    private Vector2 _lastAttackDirection; // 마지막 공격 방향
    private bool _isAttacking; // 공격 중인지
    private float _attackTimer; // 공격 타이머
    private bool _isGrounded;
    private bool _jumpQueued;
    private bool _isJumpHeld;
    private bool _isJumping;
    private float _jumpHoldTimer;
    private float _jumpBufferTimer;
    private float _coyoteTimer;
    private float _wallCoyoteTimer;
    private int _lastWallDirection; // 마지막으로 벽에 닿았던 방향 (코요테 타임용)
    private bool _facingRight = true;
    private bool _isDashing;
    private bool _dashQueued;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDirection;
    private float _originalGravityScale;
    private bool _isOnWall;
    private int _wallDirection; // -1 = left, 1 = right
    private bool _facingDown;
    private float _wallTimer;
    private bool _dashAvailable = true;
    private bool _isDead;

    private void Awake()
    {
        // 싱글톤 패턴 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("PlayerController 인스턴스가 이미 존재합니다. 중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }

        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
        _originalGravityScale = _rb.gravityScale;
        currentHealth = maxHealth;
        _isDead = false;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        UpdateGrounded();
        UpdateWallContact();
        HandleDash();
        TickWallTimer();
        TickDashCooldown();
        TickJumpBuffer();
        TickCoyoteTime();
        TickAttackTimer();
        ApplyMovement();
        ApplyJump();
        UpdateState();
        UpdateAnimator();
        UpdateFlip();
    }

    private void ApplyMovement()
    {
        if (_isDashing) return;

        var velocity = _rb.linearVelocity;
        if (_isOnWall)
        {
            // 벽에 붙은 동안은 수직 이동만 허용
            velocity.x = 0f;
            velocity.y = _moveInput.y * wallClimbSpeed;
            _rb.linearVelocity = velocity;
            return;
        }

        velocity.x = _moveInput.x * moveSpeed;
        _rb.linearVelocity = velocity;
    }

    private void ApplyJump()
    {
        if (_isDashing) return;

        // 코요테 타임 체크: 땅이나 벽에서 떠난 후 짧은 시간 동안 점프 가능
        bool canJumpFromGround = _isGrounded || _coyoteTimer > 0f;
        bool canJumpFromWall = _isOnWall || _wallCoyoteTimer > 0f;

        // 점프 버퍼 체크: 버퍼가 활성화되어 있고 땅이나 벽에 닿거나 코요테 타임 내에 있으면 점프 실행
        if (_jumpBufferTimer > 0f && (canJumpFromGround || canJumpFromWall))
        {
            _jumpBufferTimer = 0f;
            ExecuteJump();
            return;
        }

        // 초기 점프 실행
        if (_jumpQueued)
        {
            _jumpQueued = false;
            if (!canJumpFromGround && !canJumpFromWall) return;
            ExecuteJump();
        }

        // 점프 중이고 스페이스바를 누르고 있는 동안 추가 힘 적용
        if (_isJumping && _isJumpHeld)
        {
            // 제한 시간 내에만 추가 힘 적용
            if (_jumpHoldTimer < maxJumpHoldTime)
            {
                // 위로 올라가는 중일 때만 힘 추가
                if (_rb.linearVelocity.y > 0f)
                {
                    _jumpHoldTimer += Time.fixedDeltaTime;
                    _rb.AddForce(Vector2.up * jumpHoldForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void ExecuteJump()
    {
        // 최소 힘으로 점프 시작
        var velocity = _rb.linearVelocity;
        velocity.y = minJumpForce;
        
        // 벽 점프 시 벽 반대 방향으로 수평 힘 부여 (코요테 타임 포함)
        if (_isOnWall || _wallCoyoteTimer > 0f)
        {
            int wallDir = _isOnWall ? _wallDirection : _lastWallDirection;
            if (wallDir != 0)
            {
                velocity.x = -wallDir * wallJumpHorizontalForce;
                _facingRight = velocity.x > 0f;
            }
        }
        _rb.linearVelocity = velocity;

        // 벽에서 점프하면 벽 상태 해제
        if (_isOnWall)
        {
            ExitWall();
        }

        // 점프 시작
        _isJumping = true;
        _jumpHoldTimer = 0f;
        _jumpBufferTimer = 0f; // 점프 실행 시 버퍼 초기화
        _coyoteTimer = 0f; // 점프 실행 시 코요테 타이머 초기화
        _wallCoyoteTimer = 0f; // 점프 실행 시 벽 코요테 타이머 초기화
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null) return;
        bool previousGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (_isGrounded)
        {
            // 착지 시 벽 타기/대시 충전
            _wallTimer = wallMaxDuration;
            _dashAvailable = true;
            _coyoteTimer = 0f; // 착지 시 코요테 타이머 초기화
            
            // 이전 프레임에 공중에 있었다가 착지했거나, 속도가 아래로 향하고 있을 때만 점프 상태 초기화
            if (!previousGrounded || _rb.linearVelocity.y <= 0.1f)
            {
                _isJumping = false;
                _jumpHoldTimer = 0f;
            }
        }
        else if (previousGrounded)
        {
            // 땅에서 떠난 순간 코요테 타이머 시작
            _coyoteTimer = coyoteTime;
        }
    }

    private void UpdateWallContact()
    {
        bool wasOnWall = _isOnWall;

        if (_isGrounded || _isDashing)
        {
            ExitWall();
            if (wasOnWall && !_isGrounded)
            {
                // 벽에서 떠난 순간 벽 코요테 타이머 시작
                _wallCoyoteTimer = wallCoyoteTime;
            }
            return;
        }

        if (_wallTimer <= 0f)
        {
            ExitWall();
            if (wasOnWall)
            {
                // 벽에서 떠난 순간 벽 코요테 타이머 시작
                _wallCoyoteTimer = wallCoyoteTime;
            }
            return;
        }

        // 좌우로 벽 탐색
        bool hitRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, wallLayer);
        bool hitLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, wallLayer);

        if (hitRight || hitLeft)
        {
            int newWallDir = hitRight ? 1 : -1;

            // 반대 입력 시 즉시 탈출
            if (_moveInput.x != 0 && Mathf.Sign(_moveInput.x) != newWallDir)
            {
                ExitWall();
                if (wasOnWall)
                {
                    // 벽에서 떠난 순간 벽 코요테 타이머 시작
                    _wallCoyoteTimer = wallCoyoteTime;
                }
                return;
            }

            if (!_isOnWall)
            {
                // 벽에 처음 닿는 순간 코요테 타이머 초기화
                _wallCoyoteTimer = 0f;
            }
            EnterWall(newWallDir);
            _lastWallDirection = newWallDir; // 마지막 벽 방향 저장
            return;
        }

        ExitWall();
        if (wasOnWall)
        {
            // 벽에서 떠난 순간 벽 코요테 타이머 시작
            _wallCoyoteTimer = wallCoyoteTime;
        }
    }

    private void EnterWall(int direction)
    {
        _isOnWall = true;
        _wallDirection = direction;
        _lastWallDirection = direction; // 마지막 벽 방향 저장
        _rb.gravityScale = 0f;
        _dashQueued = false; // 벽에서 대시 금지
    }

    private void ExitWall()
    {
        if (!_isOnWall) return;
        _isOnWall = false;
        _rb.gravityScale = _originalGravityScale;
        _facingDown = false;
    }

    private void HandleDash()
    {
        if (_isOnWall)
        {
            _dashQueued = false; // 벽에서 대시 불가
        }

        if (_dashQueued && !_isDashing)
        {
            if (_dashAvailable && _dashCooldownTimer <= 0f)
            {
                _dashQueued = false;
                StartDash();
            }
            else
            {
                // 쿨타임 중 입력은 버림
                _dashQueued = false;
            }
        }

        if (!_isDashing) return;

        // 반대 키 입력 시 대시 취소
        if (_moveInput.x != 0 && Mathf.Sign(_moveInput.x) != Mathf.Sign(_dashDirection))
        {
            EndDash();
            return;
        }

        _dashTimer -= Time.fixedDeltaTime;
        var velocity = _rb.linearVelocity;
        velocity.x = _dashDirection * dashSpeed;
        velocity.y = 0f; // 중력 무시
        _rb.linearVelocity = velocity;

        if (_dashTimer <= 0f)
        {
            EndDash();
        }
    }

    private void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _dashAvailable = false;
        _dashDirection = _facingRight ? 1f : -1f;
        _rb.gravityScale = 0f;

        var velocity = _rb.linearVelocity;
        velocity.x = _dashDirection * dashSpeed;
        velocity.y = 0f;
        _rb.linearVelocity = velocity;
    }

    private void EndDash()
    {
        _isDashing = false;
        _rb.gravityScale = _originalGravityScale;
    }

    private void TickDashCooldown()
    {
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= Time.fixedDeltaTime;
            if (_dashCooldownTimer < 0f) _dashCooldownTimer = 0f;
        }
    }

    private void TickJumpBuffer()
    {
        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.fixedDeltaTime;
            if (_jumpBufferTimer < 0f) _jumpBufferTimer = 0f;
        }
    }

    private void TickCoyoteTime()
    {
        if (_coyoteTimer > 0f)
        {
            _coyoteTimer -= Time.fixedDeltaTime;
            if (_coyoteTimer < 0f) _coyoteTimer = 0f;
        }

        if (_wallCoyoteTimer > 0f)
        {
            _wallCoyoteTimer -= Time.fixedDeltaTime;
            if (_wallCoyoteTimer < 0f) _wallCoyoteTimer = 0f;
        }
    }

    private void TickAttackTimer()
    {
        if (_isAttacking && _attackTimer > 0f)
        {
            _attackTimer -= Time.fixedDeltaTime;
            if (_attackTimer <= 0f)
            {
                _isAttacking = false;
                _lastAttackDirection = Vector2.zero; // 공격 종료 시 방향 초기화
            }
        }
    }

    private void TickWallTimer()
    {
        if (_isGrounded) return;
        if (!_isOnWall) return;

        if (_wallTimer > 0f)
        {
            _wallTimer -= Time.fixedDeltaTime;
        }

        if (_wallTimer <= 0f && _isOnWall)
        {
            ExitWall();
        }
    }

    private void UpdateState()
    {
        // 사망 상태가 가장 우선순위가 높음
        if (_isDead)
        {
            _state = PlayerState.Dead;
            return;
        }

        var vy = _rb.linearVelocity.y;

        // 공격 상태 체크
        if (_isAttacking)
        {
            _state = PlayerState.Attack;
            return;
        }

        if (_isDashing)
        {
            _state = PlayerState.Dash;
            return;
        }

        if (_isOnWall)
        {
            _state = Mathf.Abs(_moveInput.y) > 0.01f ? PlayerState.WallMove : PlayerState.WallIdle;
            return;
        }

        if (!_isGrounded)
        {
            _state = vy >= 0.1f ? PlayerState.Jump : PlayerState.Fall;
            return;
        }

        _state = Mathf.Abs(_moveInput.x) > 0.01f ? PlayerState.Move : PlayerState.Idle;
    }

    private void UpdateAnimator()
    {
        if (_anim == null) return;

        var velocity = _rb.linearVelocity;
        _anim.SetFloat("Speed", Mathf.Abs(velocity.x));
        _anim.SetBool("IsGrounded", _isGrounded);
        _anim.SetFloat("VerticalVel", velocity.y);
        _anim.SetBool("IsDashing", _isDashing);
        _anim.SetBool("IsOnWall", _isOnWall);
        _anim.SetFloat("WallVerticalVel", _isOnWall ? Mathf.Abs(velocity.y) : 0f);
        _anim.SetBool("IsAttacking", _isAttacking);
        _anim.SetBool("IsDead", _isDead);
    }

    private void UpdateFlip()
    {
        if (_sr == null) return;

        // flipX 방향 결정: 공격 중이면 공격 방향, 아니면 이동 입력
        if (_isAttacking && _lastAttackDirection.magnitude > 0.01f && !_isOnWall)
        {
            // 바닥에서 공격 중: 공격 방향의 x 값에 따라 좌/우 flip
            _facingRight = _lastAttackDirection.x > 0.01f;
        }
        else
        {
            // 이동 입력에 따라 좌/우 flip
            if (_moveInput.x > 0.01f)
            {
                _facingRight = true;
            }
            else if (_moveInput.x < -0.01f)
            {
                _facingRight = false;
            }
        }
        _sr.flipX = !_facingRight;

        // flipY 방향 결정: 벽 상태일 때만 적용
        if (_isOnWall)
        {
            if (_isAttacking && _lastAttackDirection.magnitude > 0.01f)
            {
                // 벽에서 공격 중: 공격 방향의 y 값에 따라 위/아래 flip
                _facingDown = _lastAttackDirection.y < -0.01f;
            }
            else
            {
                // 벽 상태에서 이동 입력에 따라 위/아래 flip
                if (_moveInput.y < -0.01f)
                {
                    _facingDown = true;
                }
                else if (_moveInput.y > 0.01f)
                {
                    _facingDown = false;
                }
            }
            _sr.flipY = _facingDown;
        }
        else
        {
            _sr.flipY = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 스페이스바를 누르면 점프 시작
            _isJumpHeld = true;
            // 코요테 타임 포함하여 점프 가능 여부 체크
            bool canJumpFromGround = _isGrounded || _coyoteTimer > 0f;
            bool canJumpFromWall = _isOnWall || _wallCoyoteTimer > 0f;
            
            if (canJumpFromGround || canJumpFromWall)
            {
                _jumpQueued = true;
            }
            else
            {
                // 공중에 있을 때는 점프 버퍼 활성화
                _jumpBufferTimer = jumpBufferTime;
            }
        }
        else if (context.canceled)
        {
            // 스페이스바를 떼면 추가 힘 적용 중단
            _jumpHoldTimer = maxJumpHoldTime;
            _isJumpHeld = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        _dashQueued = true;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || _isDead) return;
        PerformAttack();
    }

    private void PerformAttack()
    {
        // 공격 중이면 중복 공격 방지
        if (_isAttacking) return;

        // 마우스 위치를 월드 좌표로 변환
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
            return;
        }

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        mouseWorldPos.z = transform.position.z; // Z 좌표를 플레이어와 동일하게 설정

        // 플레이어 위치에서 마우스 방향 계산
        Vector2 attackDirection = (mouseWorldPos - transform.position).normalized;

        // 공격 상태 시작
        _isAttacking = true;
        _attackTimer = attackCooldown;
        _lastAttackDirection = attackDirection; // 공격 방향 저장 (UpdateFlip에서 사용)

        // 공격 범위 내의 모든 적 탐지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        foreach (Collider2D hitCollider in hitColliders)
        {
            // 적의 방향 계산
            Vector2 toEnemy = (hitCollider.transform.position - transform.position).normalized;

            // 각도 계산 (내적을 사용하여 각도 확인)
            float angle = Vector2.Angle(attackDirection, toEnemy);

            // 90도 부채꼴 범위 내에 있는지 확인 (각도가 절반인 45도 이내)
            if (angle <= attackAngle * 0.5f)
            {
                // 적에게 데미지 적용
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                    Debug.Log($"적에게 {attackDamage} 데미지를 입혔습니다!");
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log($"플레이어가 데미지를 받았습니다! 현재 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _state = PlayerState.Dead;
        
        // 사망 시 물리 효과 중지
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = 0f;
        
        Debug.Log("플레이어가 사망했습니다!");
        
        // 게임오버 처리 (나중에 확장)
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return _isDead;
    }
}
