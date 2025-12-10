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

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private enum PlayerState { Idle, Move, Jump, Fall }

    [SerializeField] private PlayerState _state = PlayerState.Idle;
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _jumpQueued;
    private bool _facingRight = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        ApplyMovement();
        ApplyJump();
        UpdateState();
        UpdateAnimator();
        UpdateFlip();
    }

    private void ApplyMovement()
    {
        var velocity = _rb.linearVelocity;
        velocity.x = _moveInput.x * moveSpeed;
        _rb.linearVelocity = velocity;
    }

    private void ApplyJump()
    {
        if (!_jumpQueued) return;
        _jumpQueued = false;
        if (!_isGrounded) return;

        var velocity = _rb.linearVelocity;
        velocity.y = jumpForce;
        _rb.linearVelocity = velocity;
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null) return;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateState()
    {
        var vy = _rb.linearVelocity.y;

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
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        _jumpQueued = true;
    }
}
