using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 아이템 픽업을 처리하는 컴포넌트
/// 플레이어가 충돌 범위 내에서 Interact 키를 누르면 아이템을 획득합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private WeaponItem weaponItem;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer itemSpriteRenderer;
    [SerializeField] private GameObject pickupPrompt; // UI 프롬프트

    [Header("Drop Settings")]
    [SerializeField] private GameObject itemPickupPrefab; // 떨어진 아이템을 다시 생성할 때 사용할 프리팹

    private bool isPlayerInRange = false;
    private PlayerController playerController;
    private WeaponManager weaponManager;

    private void Awake()
    {
        // Collider2D가 Trigger로 설정되어 있는지 확인
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: Collider2D가 Trigger로 설정되어 있지 않습니다. Trigger로 설정해주세요.");
        }

        // 아이템 아이콘 설정
        if (itemSpriteRenderer != null && weaponItem != null && weaponItem.WeaponIcon != null)
        {
            itemSpriteRenderer.sprite = weaponItem.WeaponIcon;
        }

        // 프롬프트 초기화
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                weaponManager = playerController.GetWeaponManager();
                isPlayerInRange = true;

                // 프롬프트 표시
                if (pickupPrompt != null)
                {
                    pickupPrompt.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerController = null;
            weaponManager = null;

            // 프롬프트 숨기기
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
        }
    }

    /// <summary>
    /// PlayerController의 OnInteract에서 호출됩니다.
    /// 플레이어가 범위 내에 있을 때만 아이템을 획득합니다.
    /// </summary>
    public void OnInteract(InputAction.CallbackContext context)
    {
        // Hold interaction을 사용하므로 started도 체크
        if ((context.performed || context.started) && isPlayerInRange && weaponItem != null)
        {
            PickupItem();
        }
    }

    /// <summary>
    /// 아이템을 획득합니다.
    /// </summary>
    private void PickupItem()
    {
        if (weaponManager == null || weaponItem == null)
        {
            Debug.LogWarning("무기 매니저나 무기 아이템이 없습니다.");
            return;
        }

        // 현재 아이템의 위치 저장 (떨어뜨릴 위치로 사용)
        Vector3 dropPosition = transform.position;

        // 기존 무기가 있으면 떨어뜨리면서 새 무기 장착
        weaponManager.EquipWeapon(weaponItem, dropPreviousWeapon: weaponManager.HasWeapon(), dropPosition: dropPosition, itemPickupPrefab: itemPickupPrefab);

        Debug.Log($"아이템 획득: {weaponItem.WeaponName}");

        // 아이템 오브젝트 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 외부에서 무기 아이템을 설정할 수 있는 메서드 (떨어진 무기 생성 시 사용)
    /// </summary>
    public void SetWeaponItem(WeaponItem weapon)
    {
        weaponItem = weapon;

        // 스프라이트 업데이트
        if (itemSpriteRenderer != null && weapon != null && weapon.WeaponIcon != null)
        {
            itemSpriteRenderer.sprite = weapon.WeaponIcon;
        }
    }

    /// <summary>
    /// 현재 아이템 정보 반환
    /// </summary>
    public WeaponItem GetWeaponItem()
    {
        return weaponItem;
    }
}

