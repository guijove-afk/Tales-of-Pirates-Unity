using UnityEngine;
using TMPro;
using TOP.Gameplay;

namespace TOP.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player HUD")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private UnityEngine.UI.Slider hpBar;
        [SerializeField] private UnityEngine.UI.Slider mpBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;

        [Header("Messages")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float messageDuration = 3f;

        private PlayerController localPlayer;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetupLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            playerNameText.text = player.CharacterName;
            UpdateStats();
        }

        void Update()
        {
            if (localPlayer != null)
            {
                UpdateStats();
            }
        }

        void UpdateStats()
        {
            if (localPlayer.Stats == null) return;

            levelText.text = $"Lv. {localPlayer.Level}";

            hpBar.maxValue = localPlayer.Stats.MaxHp;
            hpBar.value = localPlayer.Stats.CurrentHp;
            hpText.text = $"{localPlayer.Stats.CurrentHp} / {localPlayer.Stats.MaxHp}";

            mpBar.maxValue = localPlayer.Stats.MaxMp;
            mpBar.value = localPlayer.Stats.CurrentMp;
            mpText.text = $"{localPlayer.Stats.CurrentMp} / {localPlayer.Stats.MaxMp}";
        }

        public void ShowMessage(string message, PlayerMessageType type)
        {
            messageText.text = message;
            messagePanel.SetActive(true);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }

        void HideMessage()
        {
            messagePanel.SetActive(false);
        }
    }
}
