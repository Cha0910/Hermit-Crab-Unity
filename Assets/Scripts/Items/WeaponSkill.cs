using UnityEngine;

/// <summary>
/// 무기 스킬의 기본 추상 클래스
/// 각 무기는 이 클래스를 상속받아 고유한 스킬을 구현합니다.
/// </summary>
public abstract class WeaponSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] protected float cooldown = 1f;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float range = 5f;

    protected float currentCooldownTimer = 0f;
    protected Transform playerTransform;

    /// <summary>
    /// 스킬 사용 가능 여부를 반환합니다.
    /// </summary>
    public bool CanUse()
    {
        return currentCooldownTimer <= 0f;
    }

    /// <summary>
    /// 스킬을 실행합니다.
    /// </summary>
    /// <param name="direction">스킬 발동 방향 (마우스 위치 기준)</param>
    public abstract void ExecuteSkill(Vector2 direction);

    /// <summary>
    /// 스킬 초기화 (무기 장착 시 호출)
    /// </summary>
    public virtual void Initialize(Transform player, float skillCooldown, float skillDamage, float skillRange)
    {
        playerTransform = player;
        cooldown = skillCooldown;
        damage = skillDamage;
        range = skillRange;
        currentCooldownTimer = 0f;
    }

    /// <summary>
    /// 쿨타임 업데이트 (Update에서 호출)
    /// </summary>
    protected virtual void Update()
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
    /// 쿨타임 시작
    /// </summary>
    protected void StartCooldown()
    {
        currentCooldownTimer = cooldown;
    }

    /// <summary>
    /// 남은 쿨타임 비율 반환 (0~1)
    /// </summary>
    public float GetCooldownProgress()
    {
        if (cooldown <= 0f) return 1f;
        return 1f - (currentCooldownTimer / cooldown);
    }
}

