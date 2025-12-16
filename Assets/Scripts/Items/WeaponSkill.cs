using UnityEngine;

/// <summary>
/// 무기 스킬의 기본 추상 클래스
/// 각 무기는 이 클래스를 상속받아 고유한 스킬을 구현합니다.
/// </summary>
public abstract class WeaponSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float range = 5f;

    protected Transform playerTransform;
    
    // 쿨타임 관리
    protected float skillCooldown = 1f;
    protected float currentCooldownTimer = 0f;

    /// <summary>
    /// 스킬을 실행합니다. (내부 구현용)
    /// </summary>
    /// <param name="direction">스킬 발동 방향 (마우스 위치 기준)</param>
    protected abstract void ExecuteSkill(Vector2 direction);

    /// <summary>
    /// 스킬 초기화 (무기 장착 시 호출)
    /// </summary>
    public virtual void Initialize(Transform player, float skillCooldown, float skillDamage, float skillRange)
    {
        playerTransform = player;
        damage = skillDamage;
        range = skillRange;
        this.skillCooldown = skillCooldown;
        this.currentCooldownTimer = 0f;
    }

    /// <summary>
    /// 스킬 사용 시도 (쿨타임 체크 포함)
    /// </summary>
    /// <param name="direction">스킬 발동 방향 (마우스 위치 기준)</param>
    /// <returns>스킬이 실행되었으면 true</returns>
    public virtual bool TryExecuteSkill(Vector2 direction)
    {
        // 쿨타임 체크
        if (currentCooldownTimer > 0f)
        {
            return false;
        }

        // 스킬 실행
        ExecuteSkill(direction);
        
        // 쿨타임 적용
        ApplyCooldown();
        
        return true;
    }

    /// <summary>
    /// 쿨타임을 적용합니다. (오버라이드 가능)
    /// </summary>
    protected virtual void ApplyCooldown()
    {
        currentCooldownTimer = skillCooldown;
    }

    /// <summary>
    /// 쿨타임을 업데이트합니다. (Update에서 호출)
    /// </summary>
    public virtual void UpdateCooldown()
    {
        if (currentCooldownTimer > 0f)
        {
            currentCooldownTimer -= Time.deltaTime;
            if (currentCooldownTimer < 0f)
            {
                currentCooldownTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 쿨타임을 동적으로 설정합니다.
    /// </summary>
    /// <param name="newCooldown">새로운 쿨타임 값</param>
    public virtual void SetCooldown(float newCooldown)
    {
        skillCooldown = newCooldown;
        // 현재 쿨타임이 새로운 쿨타임보다 크면 조정
        if (currentCooldownTimer > skillCooldown)
        {
            currentCooldownTimer = skillCooldown;
        }
    }

    /// <summary>
    /// 스킬 쿨타임 진행도 반환 (0~1)
    /// </summary>
    public virtual float GetCooldownProgress()
    {
        if (skillCooldown <= 0f) return 1f;
        return 1f - (currentCooldownTimer / skillCooldown);
    }
}
