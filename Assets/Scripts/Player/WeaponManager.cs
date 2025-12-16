using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 무기 관리 및 스킬 실행을 담당하는 컴포넌트
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class WeaponManager : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponItem currentWeapon;
    [SerializeField] private WeaponSkill currentSkill;

    private Transform playerTransform;
    private GameObject skillInstance; // 현재 스킬 인스턴스

    private void Awake()
    {
        playerTransform = transform;
    }

    private void Update()
    {
        // 스킬의 쿨타임 업데이트
        if (currentSkill != null)
        {
            currentSkill.UpdateCooldown();
        }
    }

    /// <summary>
    /// 무기를 장착합니다. 기존 무기가 있으면 해제하고 새 무기를 장착합니다.
    /// </summary>
    /// <param name="weapon">장착할 무기</param>
    /// <param name="dropPreviousWeapon">기존 무기를 떨어뜨릴지 여부 (기본값: false)</param>
    /// <param name="dropPosition">무기를 떨어뜨릴 위치 (dropPreviousWeapon이 true일 때만 사용)</param>
    /// <param name="itemPickupPrefab">떨어진 무기를 생성할 프리팹 (dropPreviousWeapon이 true일 때만 사용)</param>
    /// <returns>떨어뜨린 무기 (없으면 null)</returns>
    public WeaponItem EquipWeapon(WeaponItem weapon, bool dropPreviousWeapon = false, Vector3? dropPosition = null, GameObject itemPickupPrefab = null)
    {
        if (weapon == null)
        {
            Debug.LogWarning("장착하려는 무기가 null입니다.");
            return null;
        }

        WeaponItem previousWeapon = currentWeapon;

        // 기존 무기 해제
        UnequipWeapon();

        // 새 무기 장착
        currentWeapon = weapon;
        Debug.Log($"무기 장착: {weapon.WeaponName}");

        // 스킬 인스턴스 생성
        CreateSkillInstance();

        // 기존 무기를 떨어뜨려야 하는 경우
        if (dropPreviousWeapon && previousWeapon != null && dropPosition.HasValue)
        {
            DropWeaponAtPosition(previousWeapon, dropPosition.Value, itemPickupPrefab);
            return previousWeapon;
        }

        return previousWeapon;
    }

    /// <summary>
    /// 현재 장착한 무기를 해제합니다.
    /// </summary>
    public void UnequipWeapon()
    {
        if (currentSkill != null)
        {
            Destroy(currentSkill.gameObject);
            currentSkill = null;
        }

        if (skillInstance != null)
        {
            Destroy(skillInstance);
            skillInstance = null;
        }

        if (currentWeapon != null)
        {
            Debug.Log($"무기 해제: {currentWeapon.WeaponName}");
            currentWeapon = null;
        }

    }

    /// <summary>
    /// 스킬 인스턴스를 생성합니다.
    /// </summary>
    private void CreateSkillInstance()
    {
        if (currentWeapon == null || currentWeapon.SkillPrefab == null)
        {
            Debug.LogWarning("스킬 프리팹이 없습니다.");
            return;
        }

        // 스킬 오브젝트 생성 (플레이어의 자식으로)
        skillInstance = Instantiate(currentWeapon.SkillPrefab, playerTransform);
        skillInstance.name = $"{currentWeapon.WeaponName}_Skill";

        // WeaponSkill 컴포넌트 가져오기
        currentSkill = skillInstance.GetComponent<WeaponSkill>();
        if (currentSkill == null)
        {
            Debug.LogError($"스킬 프리팹에 WeaponSkill 컴포넌트가 없습니다: {currentWeapon.WeaponName}");
            Destroy(skillInstance);
            return;
        }

        // 스킬 초기화
        currentSkill.Initialize(
            playerTransform,
            currentWeapon.SkillCooldown,
            currentWeapon.SkillDamage,
            currentWeapon.SkillRange
        );
    }

    /// <summary>
    /// 스킬을 사용합니다. (외부에서 호출 가능)
    /// </summary>
    public void TryUseSkill()
    {
        if (currentWeapon == null || currentSkill == null)
        {
            return;
        }

        // 마우스 위치를 월드 좌표로 변환
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
            return;
        }

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane)
        );
        mouseWorldPos.z = playerTransform.position.z;

        // 플레이어 위치에서 마우스 방향 계산
        Vector2 skillDirection = (mouseWorldPos - playerTransform.position).normalized;

        // 스킬 실행 (쿨타임 체크는 스킬 내부에서 처리)
        currentSkill.TryExecuteSkill(skillDirection);
    }

    /// <summary>
    /// PlayerController의 OnSkill에서 호출됩니다.
    /// 스킬 액션이 Input System에 추가되면 PlayerController에 OnSkill 메서드를 추가하고 여기를 호출하세요.
    /// </summary>
    public void OnSkill(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryUseSkill();
        }
    }

    /// <summary>
    /// 현재 장착한 무기 정보 반환
    /// </summary>
    public WeaponItem GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// 무기를 장착하고 있는지 확인
    /// </summary>
    public bool HasWeapon()
    {
        return currentWeapon != null;
    }

    /// <summary>
    /// 스킬 쿨타임 진행도 반환 (0~1)
    /// </summary>
    public float GetSkillCooldownProgress()
    {
        if (currentSkill == null) return 1f;
        return currentSkill.GetCooldownProgress();
    }

    /// <summary>
    /// 지정된 위치에 무기를 떨어뜨립니다.
    /// </summary>
    /// <param name="weapon">떨어뜨릴 무기</param>
    /// <param name="position">떨어뜨릴 위치</param>
    /// <param name="itemPickupPrefab">아이템 픽업 프리팹 (null이면 자동 생성)</param>
    private void DropWeaponAtPosition(WeaponItem weapon, Vector3 position, GameObject itemPickupPrefab = null)
    {
        if (weapon == null) return;

        GameObject dropItem;
        if (itemPickupPrefab != null)
        {
            dropItem = Instantiate(itemPickupPrefab, position, Quaternion.identity);
        }
        else
        {
            // 프리팹이 없으면 기본 오브젝트 생성
            dropItem = new GameObject($"Dropped_{weapon.WeaponName}");
            dropItem.transform.position = position;

            // 필요한 컴포넌트 추가
            SpriteRenderer sr = dropItem.AddComponent<SpriteRenderer>();
            if (weapon.WeaponIcon != null)
            {
                sr.sprite = weapon.WeaponIcon;
            }

            CircleCollider2D col = dropItem.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            dropItem.AddComponent<ItemPickup>();
        }

        // Item 레이어 설정
        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer != -1)
        {
            dropItem.layer = itemLayer;
        }
        else
        {
            Debug.LogWarning("'Item' 레이어를 찾을 수 없습니다. Edit > Project Settings > Tags and Layers에서 'Item' 레이어를 추가해주세요.");
        }

        // ItemPickup 컴포넌트에 무기 설정
        ItemPickup pickupComponent = dropItem.GetComponent<ItemPickup>();
        if (pickupComponent != null)
        {
            pickupComponent.SetWeaponItem(weapon);
        }
        else
        {
            Debug.LogWarning("떨어진 무기 오브젝트에 ItemPickup 컴포넌트가 없습니다.");
        }

        Debug.Log($"무기 떨어뜨림: {weapon.WeaponName} at {position}");
    }
}
