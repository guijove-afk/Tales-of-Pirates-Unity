using System;
using Mirror;

/// <summary>
/// Representa um item no inventário de mochila (não equipado).
/// Usa int itemId para ser leve na rede.
/// </summary>
[Serializable]
public struct InventoryItem : NetworkMessage
{
    public int itemId;      // 0 = slot vazio. Mapeado no ItemDatabase
    public int quantity;    // Quantidade (1 para equipamentos)
    public int durability;  // Durabilidade atual (-1 = máxima)
    public int refineLevel; // Nível de refinamento

    public bool IsEmpty => itemId == 0;

    public static InventoryItem Empty => new InventoryItem { itemId = 0, quantity = 0, durability = -1, refineLevel = 0 };
}