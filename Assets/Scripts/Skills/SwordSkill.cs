using UnityEngine;

/// <summary>
///  테스트용 스킬
/// </summary>
public class SwordSkill : WeaponSkill
{
    [Header("Sword Skill Settings")]
    [SerializeField] private float width = 0.5f; // 직사각형의 너비
    [SerializeField] private float height = 3f; // 직사각형의 높이
    [SerializeField] private LayerMask enemyLayer; // 적 레이어

    public override void ExecuteSkill(Vector2 direction)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 Transform이 없습니다.");
            return;
        }

        if (!CanUse())
        {
            return;
        }

        // 쿨타임 시작
        StartCooldown();

        // 플레이어 위치에서 스킬 방향으로 직사각형 히트박스 생성
        Vector2 startPosition = playerTransform.position;
        Vector2 endPosition = startPosition + direction * range;

        // 직사각형의 중심 위치 계산
        Vector2 boxCenter = (startPosition + endPosition) * 0.5f;

        // 직사각형의 각도 계산 (direction 방향으로 회전)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 직사각형 크기 (세로로 긴 직사각형)
        Vector2 boxSize = new Vector2(width, height);

        // 직사각형 범위 내의 모든 적 탐지
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, enemyLayer);

        // 적에게 데미지 적용
        foreach (Collider2D hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"검 스킬로 적에게 {damage} 데미지를 입혔습니다!");
            }
        }

        Debug.Log($"검 스킬 발동! 방향: {direction}, 범위: {range}");
    }
}

