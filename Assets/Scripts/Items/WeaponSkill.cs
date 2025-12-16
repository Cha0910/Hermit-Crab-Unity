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
        damage = skillDamage;
        range = skillRange;
    }
}
