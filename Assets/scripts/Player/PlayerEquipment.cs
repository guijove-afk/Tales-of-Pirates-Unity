using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlayerEquipment : NetworkBehaviour
{
    [System.Serializable]
    public struct EquipmentSlotItem
    {
        public EquipmentSlot slot;
        public EquippedItem item;
    }

    public readonly SyncList<EquipmentSlotItem> equippedItems = new SyncList<EquipmentSlotItem>();

    public event System.Action<EquipmentSlot, EquipmentData> OnItemEquipped;
    public event System.Action<EquipmentSlot> OnItemUnequipped;

    // REMOVIDO: Não precisa mais arrastar no Inspector
    // [SerializeField] private Transform rightHandHolder;
    // ...

    // Dicionário interno de holders (encontrados automaticamente)
    private Dictionary<EquipmentSlot, Transform> holders = new Dictionary<EquipmentSlot, Transform>();

    // Guarda os GameObjects instanciados no personagem
    private Dictionary<EquipmentSlot, GameObject> equippedVisuals = new Dictionary<EquipmentSlot, GameObject>();

    void Awake()
    {
        FindHoldersAutomatically();
    }

    void FindHoldersAutomatically()
    {
        // Busca TODOS os Transforms filhos (incluindo bones)
        var allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            string name = t.name.ToLower();

            // Weapon → mão direita
            if (name.Contains("r hand") || name.Contains("righthand") || name.Contains("hand_r") || name == "bip01 r hand")
                holders[EquipmentSlot.Weapon] = t;

            // Shield → mão esquerda
            else if (name.Contains("l hand") || name.Contains("lefthand") || name.Contains("hand_l") || name == "bip01 l hand")
                holders[EquipmentSlot.Shield] = t;

            // Helmet → cabeça
            else if (name.Contains("head") && !name.Contains("ahead"))
                holders[EquipmentSlot.Helmet] = t;

            // Armor → coluna/peito
            else if (name.Contains("spine1") || name.Contains("chest") || name.Contains("spine_1"))
                holders[EquipmentSlot.Armor] = t;

            // Gloves → mãos (usa o spine se não achar específico)
            else if (name.Contains("forearm") || name.Contains("clavicle"))
            {
                if (!holders.ContainsKey(EquipmentSlot.Gloves))
                    holders[EquipmentSlot.Gloves] = t;
            }

            // Boots → pés
            else if (name.Contains("foot") || name.Contains("ankle"))
            {
                if (!holders.ContainsKey(EquipmentSlot.Boots))
                    holders[EquipmentSlot.Boots] = t;
            }

            // Cape → costas/spine
            else if (name.Contains("spine") && !holders.ContainsKey(EquipmentSlot.Cape))
                holders[EquipmentSlot.Cape] = t;

            // Belt → quadril/pelvis
            else if (name.Contains("pelvis") || name.Contains("hips"))
                holders[EquipmentSlot.Belt] = t;
        }

        // Log para debug — veja no Console se achou tudo
        Debug.Log($"[PlayerEquipment] Holders encontrados:");
        foreach (var kvp in holders)
            Debug.Log($"  {kvp.Key} → {kvp.Value.name}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (FindSlotIndex(slot) == -1)
                equippedItems.Add(new EquipmentSlotItem { slot = slot, item = EquippedItem.Empty });
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        equippedItems.Callback += OnEquipmentUpdated;

        // Spawnar visuais que já estavam equipados antes de entrar
        if (equippedItems.Count > 0)
        {
            for (int i = 0; i < equippedItems.Count; i++)
            {
                var entry = equippedItems[i];
                if (!entry.item.IsEmpty)
                {
                    var data = ItemDatabase.Instance?.GetEquipment(entry.item.itemId);
                    SpawnVisual(entry.slot, data);
                    OnItemEquipped?.Invoke(entry.slot, data);
                }
            }
        }
    }

    void OnDestroy()
    {
        equippedItems.Callback -= OnEquipmentUpdated;
    }

    void OnEquipmentUpdated(SyncList<EquipmentSlotItem>.Operation op, int index, EquipmentSlotItem oldItem, EquipmentSlotItem newItem)
    {
        switch (op)
        {
            case SyncList<EquipmentSlotItem>.Operation.OP_ADD:
            case SyncList<EquipmentSlotItem>.Operation.OP_SET:
            case SyncList<EquipmentSlotItem>.Operation.OP_INSERT:
                if (newItem.item.IsEmpty)
                {
                    RemoveVisual(newItem.slot);
                    OnItemUnequipped?.Invoke(newItem.slot);
                }
                else
                {
                    var data = ItemDatabase.Instance?.GetEquipment(newItem.item.itemId);
                    SpawnVisual(newItem.slot, data);
                    OnItemEquipped?.Invoke(newItem.slot, data);
                }
                break;

            case SyncList<EquipmentSlotItem>.Operation.OP_REMOVEAT:
                RemoveVisual(oldItem.slot);
                OnItemUnequipped?.Invoke(oldItem.slot);
                break;

            case SyncList<EquipmentSlotItem>.Operation.OP_CLEAR:
                foreach (var slot in new List<EquipmentSlot>(equippedVisuals.Keys))
                {
                    RemoveVisual(slot);
                    OnItemUnequipped?.Invoke(slot);
                }
                break;
        }
    }

    int FindSlotIndex(EquipmentSlot slot)
    {
        for (int i = 0; i < equippedItems.Count; i++)
            if (equippedItems[i].slot == slot) return i;
        return -1;
    }

    [Server]
    public void EquipItem(EquipmentData item, int durability, int refineLevel)
    {
        if (item == null) return;
        
        int index = FindSlotIndex(item.slot);
        var equipped = new EquippedItem
        {
            itemId = item.itemId,
            durability = durability > 0 ? durability : item.maxDurability,
            refineLevel = refineLevel,
            gemSlots = new int[3]
        };

        if (index >= 0)
        {
            var entry = equippedItems[index];
            entry.item = equipped;
            equippedItems[index] = entry;
        }
        else
        {
            equippedItems.Add(new EquipmentSlotItem { slot = item.slot, item = equipped });
        }

        ApplyEquipmentStats(item, true);
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        int index = FindSlotIndex(slot);
        if (index >= 0)
        {
            var entry = equippedItems[index];
            
            if (!entry.item.IsEmpty)
            {
                var data = ItemDatabase.Instance?.GetEquipment(entry.item.itemId);
                if (data != null) ApplyEquipmentStats(data, false);
            }

            entry.item = EquippedItem.Empty;
            equippedItems[index] = entry;
        }
    }

    [Server]
    public bool CanEquip(EquipmentData item)
    {
        if (item == null) return false;
        return true;
    }

    public EquipmentData GetEquippedItem(EquipmentSlot slot)
    {
        int index = FindSlotIndex(slot);
        if (index >= 0 && !equippedItems[index].item.IsEmpty)
            return ItemDatabase.Instance?.GetEquipment(equippedItems[index].item.itemId);
        return null;
    }

    public EquippedItem GetEquippedItemStruct(EquipmentSlot slot)
    {
        int index = FindSlotIndex(slot);
        if (index >= 0)
            return equippedItems[index].item;
        return EquippedItem.Empty;
    }

    #region Visual (Aparecer no Personagem)

    [Client]
    void SpawnVisual(EquipmentSlot slot, EquipmentData equipData)
    {
        if (equipData == null || equipData.equipPrefab == null) return;

        RemoveVisual(slot);

        if (!holders.TryGetValue(slot, out Transform holder) || holder == null)
        {
            Debug.LogWarning($"[PlayerEquipment] Holder não encontrado para {slot}");
            return;
        }

        var visual = Instantiate(equipData.equipPrefab, holder);
        visual.transform.localPosition = equipData.equipPositionOffset;
        visual.transform.localRotation = Quaternion.Euler(equipData.equipRotationOffset);
        visual.transform.localScale = equipData.equipScale;

        equippedVisuals[slot] = visual;
        Debug.Log($"[PlayerEquipment] ✅ Visual spawnado: {equipData.itemName} em {slot} ({holder.name})");
    }

    [Client]
    void RemoveVisual(EquipmentSlot slot)
    {
        if (equippedVisuals.TryGetValue(slot, out var visual) && visual != null)
        {
            Destroy(visual);
            equippedVisuals.Remove(slot);
            Debug.Log($"[PlayerEquipment] Visual removido: {slot}");
        }
    }

    #endregion

    #region Stats

    void ApplyEquipmentStats(EquipmentData equipData, bool add)
    {
        if (equipData == null) return;
        int multiplier = add ? 1 : -1;
        Debug.Log($"[PlayerEquipment] Stats {(add ? "aplicados" : "removidos")}: {equipData.itemName} | ATK {equipData.bonusAttack * multiplier}");
    }

    #endregion
}