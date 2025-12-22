using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 부메랑 투사체 - 나갔다가 플레이어에게 돌아옴
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BoomerangProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private float damage;
    private float knockbackForce;
    private LayerMask enemyLayer;
    private Vector2 startPosition;
    private Transform playerTransform;
    private bool isReturning = false; // 돌아오는 중인지 여부
    private Vector2 returnDirection; // 돌아올 때의 고정된 방향
    private bool isPlayerHit = false; // 플레이어와 충돌했는지 여부
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>(); // 이미 맞은 적 추적
    private float returnDistanceThreshold = 0.5f; // 플레이어에게 돌아왔을 때 사라지는 거리
    private BoomerangSkill boomerangSkill; // BoomerangSkill 참조

    public void Initialize(Vector2 dir, float spd, float maxDist, float dmg, LayerMask layer, Transform player, BoomerangSkill skill, float knockback = 5f)
    {
        direction = dir.normalized;
        speed = spd;
        maxDistance = maxDist;
        damage = dmg;
        knockbackForce = knockback;
        enemyLayer = layer;
        startPosition = transform.position;
        playerTransform = player;
        boomerangSkill = skill;
        isReturning = false;
        isPlayerHit = false;
        hitEnemies.Clear();
    }

    /// <summary>
    /// 부메랑을 강제로 돌아오게 함
    /// </summary>
    public void ForceReturn()
    {
        if (!isReturning)
        {
            isReturning = true;
            // 돌아올 때의 방향을 현재 위치에서 플레이어로의 방향으로 설정
            returnDirection = (playerTransform.position - transform.position).normalized;
            // 돌아올 때는 이미 맞은 적 목록 초기화 (다시 맞출 수 있도록)
            hitEnemies.Clear();
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            OnDestroyed();
            return;
        }

        // 돌아오는 중이 아니고 최대 거리에 도달했으면 돌아오기 시작
        if (!isReturning)
        {
            float distanceTraveled = Vector2.Distance(startPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                isReturning = true;
                // 돌아올 때의 방향을 현재 위치에서 플레이어로의 방향으로 고정
                returnDirection = (playerTransform.position - transform.position).normalized;
                // 돌아올 때는 이미 맞은 적 목록 초기화 (다시 맞출 수 있도록)
                hitEnemies.Clear();
            }
        }

        // 이동 처리
        if (isReturning)
        {
            // 고정된 방향으로 이동 (플레이어를 추적하지 않음)
            transform.position += (Vector3)(returnDirection * speed * Time.deltaTime);

            // 플레이어와의 거리 체크
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            // 플레이어에게 가까워지면 성공으로 처리
            if (distanceToPlayer <= returnDistanceThreshold)
            {
                isPlayerHit = true;
                OnDestroyed();
            }
            // 최대 거리를 넘어서면 실패로 처리
            else if (distanceToPlayer >= maxDistance * 1.5)
            {
                isPlayerHit = false;
                OnDestroyed();
            }
        }
        else
        {
            // 앞으로 이동
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 레이어인지 확인
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            // Enemy 컴포넌트 가져오기
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 이미 맞은 적이면 무시 (돌아올 때는 다시 맞출 수 있음)
                if (hitEnemies.Contains(enemy))
                {
                    return;
                }

                // 넉백 방향 계산 (나갈 때는 direction, 돌아올 때는 returnDirection)
                Vector2 knockbackDirection = isReturning ? returnDirection : direction;
                
                // 넉백 효과와 함께 데미지 적용
                enemy.TakeDamage(damage, knockbackDirection, knockbackForce);
                hitEnemies.Add(enemy);
            }
        }
    }

    /// <summary>
    /// 부메랑이 사라질 때 호출되는 메서드
    /// </summary>
    private void OnDestroyed()
    {
        // BoomerangSkill에 콜백 전달
        if (boomerangSkill != null)
        {
            boomerangSkill.OnProjectileDestroyed(isPlayerHit);
        }
        
        Destroy(gameObject);
    }
}
