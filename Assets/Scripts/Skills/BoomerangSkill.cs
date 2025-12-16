using UnityEngine;

/// <summary>
/// 부메랑 스킬
/// </summary>
public class BoomerangSkill : WeaponSkill
{
    [Header("Boomerang Skill Settings")]
    [SerializeField] private GameObject projectilePrefab; // 부메랑 투사체 프리팹
    [SerializeField] private float projectileSpeed = 8f; // 부메랑 속도
    [SerializeField] private LayerMask enemyLayer; // 적 레이어

    [Header("Cooldown Settings")]
    [SerializeField] private float successCooldown = 1f; // 플레이어가 부메랑을 받았을 때 쿨타임
    [SerializeField] private float failCooldown = 5f; // 플레이어가 부메랑을 받지 못했을 때 쿨타임

    private BoomerangProjectile currentProjectile; // 현재 활성화된 부메랑

    public override bool TryExecuteSkill(Vector2 direction)
    {
        // 부메랑이 날아가고 있으면 쿨타임과 관계없이 강제 복귀
        if (currentProjectile != null)
        {
            currentProjectile.ForceReturn();
            return true; // 강제 복귀는 쿨타임 적용 안 함
        }

        // 일반적인 쿨타임 체크
        if (currentCooldownTimer > 0f)
        {
            return false;
        }

        // 스킬 실행 (쿨타임은 부메랑이 사라질 때 적용)
        ExecuteSkill(direction);
        
        return true;
    }

    protected override void ExecuteSkill(Vector2 direction)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 Transform이 없습니다.");
            return;
        }

        // 이미 부메랑이 날아가고 있으면 강제로 돌아오게 함
        if (currentProjectile != null)
        {
            currentProjectile.ForceReturn();
            return;
        }

        // 부메랑 생성
        CreateProjectile(direction);
    }

    private void CreateProjectile(Vector2 direction)
    {
        // 프리팹이 없으면 경고
        if (projectilePrefab == null)
        {
            Debug.LogError("프리팹이 설정되지 않았습니다! BoomerangSkill의 Projectile Prefab 필드에 프리팹을 할당해주세요.");
            return;
        }

        // 부메랑 프리팹 인스턴스화
        GameObject projectileObj = Instantiate(projectilePrefab, playerTransform.position, 
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));

        // BoomerangProjectile 컴포넌트 가져오기
        BoomerangProjectile projectile = projectileObj.GetComponent<BoomerangProjectile>();
        if (projectile == null)
        {
            Debug.LogError("프리팹에 BoomerangProjectile 컴포넌트가 없습니다!");
            Destroy(projectileObj);
            return;
        }

        // 부메랑 초기화
        projectile.Initialize(direction, projectileSpeed, range, damage, enemyLayer, playerTransform, this);
        
        // 현재 부메랑 추적
        currentProjectile = projectile;
    }

    /// <summary>
    /// 부메랑이 사라질 때 호출되는 콜백 메서드
    /// </summary>
    /// <param name="wasPlayerHit">플레이어가 부메랑을 받았는지 여부</param>
    public void OnProjectileDestroyed(bool wasPlayerHit)
    {
        // 현재 부메랑 참조 제거
        currentProjectile = null;

        // 쿨타임 설정 및 적용 (플레이어가 받았으면 짧게, 못 받았으면 길게)
        float newCooldown = wasPlayerHit ? successCooldown : failCooldown;
        SetCooldown(newCooldown);
        ApplyCooldown(); // 부메랑이 사라진 후 쿨타임 시작
    }
}
