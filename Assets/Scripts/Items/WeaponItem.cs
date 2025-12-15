using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon Item")]
public class WeaponItem : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private Sprite weaponIcon;
    [TextArea(3, 5)]
    [SerializeField] private string description = "무기 설명";

    [Header("Skill Settings")]
    [SerializeField] private GameObject skillPrefab; // 스킬 실행용 프리팹
    [SerializeField] private float skillCooldown = 1f;
    [SerializeField] private float skillDamage = 10f;
    [SerializeField] private float skillRange = 5f;

    // 프로퍼티
    public string WeaponName => weaponName;
    public Sprite WeaponIcon => weaponIcon;
    public string Description => description;
    public GameObject SkillPrefab => skillPrefab;
    public float SkillCooldown => skillCooldown;
    public float SkillDamage => skillDamage;
    public float SkillRange => skillRange;
}

