using UnityEngine;
using TMPro;
using TOP.Gameplay;
using UnityEngine.UI;
using TOP.Player;
using System;
using TOP.Inventory;

namespace TOP.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player HUD")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider mpBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private TextMeshProUGUI spText;

        [Header("Messages")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float messageDuration = 3f;

        [Header("Hotbar")]
        [SerializeField] private ItemSlotUI[] hotbarSlots;

        private PlayerController localPlayer;
        private float _lastStatsUpdate;

        void Awake()
        {
            if (Instance != null) 
            { 
                Destroy(gameObject); 
                return; 
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetupLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            if (playerNameText != null)
                playerNameText.text = player.CharacterName;
            
            if (hotbarSlots != null)
            {
                for (int i = 0; i < hotbarSlots.Length; i++)
                    hotbarSlots[i].Initialize(i, InventoryUI.Instance);
            }

            UpdateStats();
            Debug.Log($"[UIManager] HUD configurado para {player.CharacterName}");
        }

        // ÚNICO MÉTODO UPDATE
        void Update()
        {
            // --- 1. Lógica da Hotbar (Teclas 1 a 9) ---
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectHotbarSlot(i);
                }
            }

            // Tecla 0 separada (Slot index 9)
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                SelectHotbarSlot(9);
            }

            // --- 2. Atualização Otimizada de Stats ---
            if (localPlayer != null && Time.time - _lastStatsUpdate > 0.1f)
            {
                UpdateStats();
                _lastStatsUpdate = Time.time;
            }
        }

        void UpdateStats()
        {
            if (localPlayer?.Stats == null) return;

            if (levelText != null)
                levelText.text = $"Lv. {localPlayer.Level}";

            if (hpBar != null)
            {
                hpBar.maxValue = localPlayer.Stats.MaxHp;
                hpBar.value = localPlayer.Stats.CurrentHp;
            }
            if (hpText != null)
                hpText.text = $"{localPlayer.Stats.CurrentHp}/{localPlayer.Stats.MaxHp}";

            if (mpBar != null)
            {
                mpBar.maxValue = localPlayer.Stats.MaxMp;
                mpBar.value = localPlayer.Stats.CurrentMp;
            }
            if (mpText != null)
                mpText.text = $"{localPlayer.Stats.CurrentMp}/{localPlayer.Stats.MaxMp}";

            if (spText != null)
                spText.text = $"{localPlayer.Stats.CurrentSp}/{localPlayer.Stats.MaxSp}";
        }

        public void ShowMessage(string message, PlayerController.PlayerMessageType type)
        {
            if (messageText == null || messagePanel == null) return;

            string color = type switch
            {
                PlayerController.PlayerMessageType.Info => "#FFFFFF",
                PlayerController.PlayerMessageType.Warning => "#FFFF00",
                PlayerController.PlayerMessageType.Error => "#FF0000",
                PlayerController.PlayerMessageType.Success => "#00FF00",
                _ => "#FFFFFF"
            };

            messageText.text = $"<color={color}>{message}</color>";
            messagePanel.SetActive(true);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }

        void HideMessage()
        {
            if (messagePanel != null)
                messagePanel.SetActive(false);
        }

        void SelectHotbarSlot(int index)
        {
            if (hotbarSlots != null && index < hotbarSlots.Length)
            {
                InventoryUI.Instance?.OnSlotSelected(index);
                Debug.Log($"[UIManager] Hotbar slot {index} selecionado");
            }
        }

        public void OnPlayerStatsChanged()
        {
            UpdateStats();
        }
    }
}
