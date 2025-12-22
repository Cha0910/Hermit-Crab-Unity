using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BasicEnemy : Enemy
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [Header("Attack Settings")]
    [SerializeField] private float knockbackForce = 8f; // 플레이어 넉백 힘
    private SpriteRenderer spriteRenderer;
    private bool facingRight = true;
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isDead && isPlayerInRange)
        {
            Move();
            UpdateFlip();
        }
    }

    protected override void TrackPlayer()
    {
        // 플레이어 추적은 Move()에서 처리
    }

    protected override void Move()
    {
        if (playerTransform == null) return;
        if (isKnockedBack) return; // 넉백 중에는 이동 중지
        if (isPlayerInAttackRange) return; // 공격 범위 내에서는 이동 중지

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 공격 범위보다 멀리 있을 때만 이동
        if (distanceToPlayer > attackRange)
        {
            // 이동 적용
            Vector2 velocity = rb.linearVelocity;
            velocity.x = direction.x * moveSpeed;
            rb.linearVelocity = velocity;
        }
        else
        {
            // 공격 범위 내에서는 정지
            Vector2 velocity = rb.linearVelocity;
            velocity.x = 0f;
            rb.linearVelocity = velocity;
        }
    }

    protected override void Attack()
    {
        if (playerTransform == null) return;

        // 플레이어에게 데미지 주기
        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player != null && !player.IsDead())
        {
            // 적의 위치에서 플레이어 위치로의 방향 계산
            Vector2 knockbackDirection = (playerTransform.position - transform.position).normalized;
            
            // 넉백 효과와 함께 데미지 적용
            player.TakeDamage(attackDamage, knockbackDirection, knockbackForce);
        }

        // 공격 애니메이션 트리거 
        // animator.SetTrigger("Attack");
    }

    private void UpdateFlip()
    {
        if (playerTransform == null) return;

        // 플레이어 방향으로 스프라이트 뒤집기
        bool shouldFaceRight = playerTransform.position.x > transform.position.x;
        
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}

