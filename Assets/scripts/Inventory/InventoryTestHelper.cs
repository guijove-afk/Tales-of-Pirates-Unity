using UnityEngine;
using Mirror;

public class InventoryTestHelper : MonoBehaviour
{
    [Header("Test Items")]
    [SerializeField] private int testItemId = 1;
    [SerializeField] private int testQuantity = 1;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode addItemKey = KeyCode.F1;
    [SerializeField] private KeyCode removeItemKey = KeyCode.F2;

    private PlayerInventory playerInventory;
    private bool initialized = false;

    void Start()
    {
        Invoke(nameof(FindPlayer), 1f);
    }

    void FindPlayer()
    {
        var players = FindObjectsByType<PlayerInventory>(FindObjectsInactive.Include);
        foreach (var p in players)
        {
            if (p.isLocalPlayer)
            {
                playerInventory = p;
                initialized = true;
                Debug.Log("[InventoryTestHelper] PlayerInventory local encontrado!");
                break;
            }
        }

        if (!initialized)
        {
            Debug.LogWarning("[InventoryTestHelper] Nenhum PlayerInventory local. Tentando novamente...");
            Invoke(nameof(FindPlayer), 2f);
        }
    }

    void Update()
    {
        if (!initialized || playerInventory == null) return;

        if (Input.GetKeyDown(addItemKey))
        {
            Debug.Log($"[InventoryTestHelper] Adicionando item {testItemId} x{testQuantity}");
            playerInventory.CmdAddItemDebug(testItemId, testQuantity);
        }

        if (Input.GetKeyDown(removeItemKey))
        {
            Debug.Log($"[InventoryTestHelper] Removendo item {testItemId}");
            playerInventory.CmdRemoveItemDebug(testItemId, testQuantity);
        }
    }
}