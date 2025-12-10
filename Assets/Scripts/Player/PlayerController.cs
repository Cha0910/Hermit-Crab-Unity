using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
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
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _jumpQueued;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        ApplyMovement();
        ApplyJump();
        UpdateState();
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
