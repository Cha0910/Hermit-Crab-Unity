using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float minJumpForce = 4f;
    [SerializeField] private float jumpHoldForce = 15f;
    [SerializeField] private float maxJumpHoldTime = 0.3f;

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

    private enum PlayerState { Idle, Move, Jump, Fall, Dash, WallIdle, WallMove }

    [SerializeField] private PlayerState _state = PlayerState.Idle;
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _jumpQueued;
    private bool _isJumpHeld;
    private bool _isJumping;
    private float _jumpHoldTimer;
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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
        _originalGravityScale = _rb.gravityScale;
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        UpdateWallContact();
        HandleDash();
        TickWallTimer();
        TickDashCooldown();
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

        // 초기 점프 실행
        if (_jumpQueued)
        {
            _jumpQueued = false;
            if (!_isGrounded && !_isOnWall) return;

            // 최소 힘으로 점프 시작
            var velocity = _rb.linearVelocity;
            velocity.y = minJumpForce;
            
            // 벽 점프 시 벽 반대 방향으로 수평 힘 부여
            if (_isOnWall)
            {
                velocity.x = -_wallDirection * wallJumpHorizontalForce;
                _facingRight = velocity.x > 0f;
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
            
            // 이전 프레임에 공중에 있었다가 착지했거나, 속도가 아래로 향하고 있을 때만 점프 상태 초기화
            if (!previousGrounded || _rb.linearVelocity.y <= 0.1f)
            {
                _isJumping = false;
                _jumpHoldTimer = 0f;
            }
        }
    }

    private void UpdateWallContact()
    {
        if (_isGrounded || _isDashing)
        {
            ExitWall();
            return;
        }

        if (_wallTimer <= 0f)
        {
            ExitWall();
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
                return;
            }

            EnterWall(newWallDir);
            return;
        }

        ExitWall();
    }

    private void EnterWall(int direction)
    {
        _isOnWall = true;
        _wallDirection = direction;
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
        var vy = _rb.linearVelocity.y;

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
    }

    private void UpdateFlip()
    {
        if (_sr == null) return;

        if (_moveInput.x > 0.01f)
        {
            _facingRight = true;
        }
        else if (_moveInput.x < -0.01f)
        {
            _facingRight = false;
        }

        _sr.flipX = !_facingRight;

        if (_isOnWall)
        {
            // 벽 상태에서 한 번 아래로 이동하면 플립 유지, 위로 이동 시 해제
            if (_moveInput.y < -0.01f)
            {
                _facingDown = true;
            }
            else if (_moveInput.y > 0.01f)
            {
                _facingDown = false;
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
            if (_isGrounded || _isOnWall)
            {
                _jumpQueued = true;
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
}
