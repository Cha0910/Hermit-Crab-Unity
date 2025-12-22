using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 검기 투사체
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class SwordProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private float damage;
    private float knockbackForce;
    private LayerMask enemyLayer;
    private Vector2 startPosition;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>(); // 이미 맞은 적 추적

    public void Initialize(Vector2 dir, float spd, float maxDist, float dmg, LayerMask layer, float knockback = 5f)
    {
        direction = dir.normalized;
        speed = spd;
        maxDistance = maxDist;
        damage = dmg;
        knockbackForce = knockback;
        enemyLayer = layer;
        startPosition = transform.position;
    }

    private void Update()
    {
        // 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 최대 거리 체크
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
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
                // 이미 맞은 적이면 무시
                if (hitEnemies.Contains(enemy))
                {
                    return;
                }

                // 넉백 방향 계산 (투사체 이동 방향)
                Vector2 knockbackDirection = direction;
                
                // 넉백 효과와 함께 데미지 적용
                enemy.TakeDamage(damage, knockbackDirection, knockbackForce);
                hitEnemies.Add(enemy);
            }
        }
    }
}
