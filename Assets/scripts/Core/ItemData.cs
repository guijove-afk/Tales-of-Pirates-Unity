using UnityEngine;

namespace TOP.Core
{

    [CreateAssetMenu(fileName = "NewItem", menuName = "Tales of Pirates/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Informações Básicas")]
        public int itemId;
        public string itemName = "Item";
        public string description = "";
        public Sprite icon;
        public int maxStack = 1;
        public int buyPrice;
        public int sellPrice;

        [Header("Tipo")]
        public ItemType itemType = ItemType.Other;

        [Header("Drop no Mundo (WorldItem)")]
        public GameObject worldModelPrefab;
        public Vector3 dropRotation = Vector3.zero;
        public Vector3 dropScale = Vector3.one;

        public bool IsStackable => maxStack > 1;
    }
}
