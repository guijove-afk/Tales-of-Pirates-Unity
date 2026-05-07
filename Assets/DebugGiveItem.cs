using UnityEngine;

public class DebugGiveItem : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int itemId = 1001;
    [SerializeField] private int quantity = 1;
    [SerializeField] private KeyCode giveKey = KeyCode.G;

    void Start()
    {
        Debug.Log("[DebugGiveItem] ====== INICIANDO TESTE DO ITEMDATABASE ======");

        var db = ItemDatabase.Instance;
        if (db == null)
        {
            Debug.LogError("[DebugGiveItem] ❌ ItemDatabase.Instance é NULL!");
            return;
        }

        Debug.Log("[DebugGiveItem] ✅ ItemDatabase.Instance encontrado!");

        var item = db.GetItem(itemId);
        if (item == null)
            Debug.LogError("[DebugGiveItem] ❌ GetItem(" + itemId + ") retornou NULL!");
        else
            Debug.Log("[DebugGiveItem] ✅ GetItem(" + itemId + "): " + item.itemName);

        var equip = db.GetEquipment(itemId);
        if (equip == null)
            Debug.LogError("[DebugGiveItem] ❌ GetEquipment(" + itemId + ") retornou NULL!");
        else
            Debug.Log("[DebugGiveItem] ✅ GetEquipment(" + itemId + "): " + equip.itemName + " | Slot: " + equip.slot);

        Debug.Log("[DebugGiveItem] 📦 Itens no banco: " + db.GetAllItems().Count);
        Debug.Log("[DebugGiveItem] ⚔️ Equipamentos no banco: " + db.GetAllEquipment().Count);
        Debug.Log("[DebugGiveItem] ====== FIM DO TESTE ======");
    }

    void Update()
    {
        if (Input.GetKeyDown(giveKey))
        {
            GiveItem();
        }
    }

    private void GiveItem()
    {
        PlayerInventory playerInventory = FindAnyObjectByType<PlayerInventory>();

        if (playerInventory == null)
        {
            Debug.LogError("[DebugGiveItem] ❌ PlayerInventory não encontrado na cena!");
            return;
        }

        Debug.Log("[DebugGiveItem] Tentando adicionar item " + itemId + "...");
        Debug.Log("[DebugGiveItem] PlayerInventory: " + playerInventory.name + " | isServer: " + playerInventory.isServer);

        if (playerInventory.isServer)
        {
            // 🔧 CORREÇÃO: AddItem retorna void, não bool
            // Verificar se tem slot vazio antes
            int emptySlot = playerInventory.FindEmptySlot();
            if (emptySlot != -1)
            {
                playerInventory.AddItem(itemId, quantity);
                Debug.Log("[DebugGiveItem] ✅ Item " + itemId + " adicionado ao inventário!");
            }
            else
            {
                Debug.LogWarning("[DebugGiveItem] ❌ Inventário cheio!");
            }
        }
        else
        {
            // 🔧 CORREÇÃO: No cliente, usar CmdAddItemDebug (Command)
            Debug.Log("[DebugGiveItem] Enviando comando para servidor...");
            playerInventory.CmdAddItemDebug(itemId, quantity);
        }
    }
}