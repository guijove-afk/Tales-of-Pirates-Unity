using UnityEngine;
using UnityEngine.UI;

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