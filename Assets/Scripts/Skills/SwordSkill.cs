using UnityEngine;

/// <summary>
/// 검기 스킬
/// </summary>
public class SwordSkill : WeaponSkill
{
    [Header("Sword Skill Settings")]
    [SerializeField] private GameObject projectilePrefab; // 투사체 프리팹
    [SerializeField] private float projectileSpeed = 10f; // 투사체 속도
    [SerializeField] private LayerMask enemyLayer; // 적 레이어

    public override void ExecuteSkill(Vector2 direction)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 Transform이 없습니다.");
            return;
        }

        // 검기 생성
        CreateProjectile(direction);
    }

    private void CreateProjectile(Vector2 direction)
    {
        // 프리팹이 없으면 경고
        if (projectilePrefab == null)
        {
            Debug.LogError("프리팹이 설정되지 않았습니다! SwordSkill의 Projectile Prefab 필드에 프리팹을 할당해주세요.");
            return;
        }

        // 검기 프리팹 인스턴스화
        GameObject projectileObj = Instantiate(projectilePrefab, playerTransform.position, 
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        // SwordProjectile 컴포넌트 가져오기
        SwordProjectile projectile = projectileObj.GetComponent<SwordProjectile>();
        if (projectile == null)
        {
            Debug.LogError("프리팹에 SwordProjectile 컴포넌트가 없습니다!");
            Destroy(projectileObj);
            return;
        }

        // 검기 초기화
        projectile.Initialize(direction, projectileSpeed, range, damage, enemyLayer);
    }
}
