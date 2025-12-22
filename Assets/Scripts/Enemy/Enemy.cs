using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;

    [Header("Detection Settings")]
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float attackRange = 1f;

    [Header("Attack Settings")]
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;

    [Header("Knockback Settings")]
    [Range(0f, 10f)]
    [SerializeField] protected float knockbackResistance = 0f; // 넉백 저항 (0~10 퍼센트)
    [SerializeField] protected float knockbackDuration = 0.3f; // 넉백 지속 시간

    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected float attackTimer;
    protected bool isPlayerInRange;
    protected bool isPlayerInAttackRange;
    protected bool isDead;
    protected bool isKnockedBack;
    protected float knockbackTimer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        attackTimer = 0f;
        isDead = false;
        isKnockedBack = false;
        knockbackTimer = 0f;
    }

    protected virtual void Start()
    {
        // 싱글톤을 통해 플레이어 참조 가져오기
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogWarning("PlayerController 인스턴스를 찾을 수 없습니다.");
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // 플레이어 참조가 없으면 다시 시도
        if (playerTransform == null && PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }

        // 공격 쿨다운 업데이트
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // 넉백 타이머 업데이트
        TickKnockbackTimer();

        // 플레이어 감지
        DetectPlayer();
        
        // 플레이어 추적
        if (isPlayerInRange)
        {
            TrackPlayer();
        }

        // 공격
        if (isPlayerInAttackRange && attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }
    }

    protected virtual void DetectPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        isPlayerInRange = distanceToPlayer <= detectionRange;
        isPlayerInAttackRange = distanceToPlayer <= attackRange;
    }

    protected virtual void TrackPlayer()
    {
        // 자식 클래스에서 구현
    }

    protected virtual void Move()
    {
        // 자식 클래스에서 구현
    }

    protected virtual void Attack()
    {
        // 자식 클래스에서 구현
    }

    public virtual void TakeDamage(float damage, Vector2? knockbackDirection = null, float knockbackForce = 0f)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        // 넉백 적용
        if (knockbackDirection.HasValue && knockbackForce > 0f)
        {
            ApplyKnockback(knockbackDirection.Value, knockbackForce);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void ApplyKnockback(Vector2 direction, float force)
    {
        // 넉백 저항 적용 (0~10 퍼센트)
        float resistanceMultiplier = 1f - (knockbackResistance / 100f);
        float actualForce = force * resistanceMultiplier;

        // 넉백 방향 정규화
        Vector2 knockbackDir = direction.normalized;

        // AddForce로 넉백 적용
        rb.AddForce(knockbackDir * actualForce, ForceMode2D.Impulse);

        // 넉백 상태 설정
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
    }

    protected virtual void TickKnockbackTimer()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                knockbackTimer = 0f;
                isKnockedBack = false;
            }
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        // 사망 처리 (애니메이션, 이펙트 등)
        // 자식 클래스에서 오버라이드하여 추가 동작 가능
        
        // 게임오브젝트 삭제
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // 추적 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

