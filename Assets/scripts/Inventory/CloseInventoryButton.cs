using UnityEngine;
using UnityEngine.UI;

namespace TOP.Inventory
{

    [RequireComponent(typeof(Button))]
    public class CloseInventoryButton : MonoBehaviour
    {
        void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => {
                InventoryUI.Instance?.CloseInventory();
            });
        }
    }
}
