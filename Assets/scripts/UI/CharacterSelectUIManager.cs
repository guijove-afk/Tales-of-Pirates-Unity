using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TOP.Network;
using TOP.Services;
using System.Collections.Generic;

namespace TOP.UI
{
    public class CharacterSelectUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject characterListPanel;
        [SerializeField] private GameObject createCharacterPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;

        [Header("Character Slots")]
        [SerializeField] private Transform characterSlotsParent;
        [SerializeField] private GameObject characterCardPrefab;

        [Header("Create Character")]
        [SerializeField] private TMP_InputField charNameInput;
        [SerializeField] private TMP_Dropdown jobDropdown;
        [SerializeField] private TMP_Dropdown genderDropdown;
        [SerializeField] private Button createCharButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button createNewButton;

        [Header("Error")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button errorOkButton;

        [Header("Character Preview")]
        [SerializeField] private Transform previewSpawnPoint;
        [SerializeField] private GameObject[] classPreviewPrefabs;

        private List<CharacterPreviewData> currentCharacters = new List<CharacterPreviewData>();
        private GameObject currentPreview;
        private int selectedSlot = -1;

        void Start()
        {
            createCharButton.onClick.AddListener(OnCreateCharacterClick);
            backButton.onClick.AddListener(() => ShowPanel(characterListPanel));
            createNewButton.onClick.AddListener(() => ShowPanel(createCharacterPanel));
            errorOkButton.onClick.AddListener(() => errorPanel.SetActive(false));

            NetworkClient.RegisterHandler<CharacterListResponse>(OnCharacterListResponse);
            NetworkClient.RegisterHandler<CreateCharacterResponse>(OnCreateCharacterResponse);
            NetworkClient.RegisterHandler<SelectCharacterResponse>(OnSelectCharacterResponse);
            NetworkClient.RegisterHandler<DeleteCharacterResponse>(OnDeleteCharacterResponse);

            ShowPanel(loadingPanel);

            NetworkClient.Send(new CharacterListRequest());

            jobDropdown.ClearOptions();
            jobDropdown.AddOptions(new List<string> { "Swordsman", "Hunter", "Herbalist", "Explorer" });

            genderDropdown.ClearOptions();
            genderDropdown.AddOptions(new List<string> { "Male", "Female" });

            jobDropdown.onValueChanged.AddListener(OnJobChanged);
        }

        void OnDestroy()
        {
            createCharButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            createNewButton.onClick.RemoveAllListeners();
            errorOkButton.onClick.RemoveAllListeners();
            jobDropdown.onValueChanged.RemoveAllListeners();
        }

        void ShowPanel(GameObject panel)
        {
            characterListPanel.SetActive(panel == characterListPanel);
            createCharacterPanel.SetActive(panel == createCharacterPanel);
            loadingPanel.SetActive(panel == loadingPanel);
        }

        void ShowError(string message)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }

        void OnCharacterListResponse(CharacterListResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($"Erro: {msg.Error}");
                return;
            }

            currentCharacters = new List<CharacterPreviewData>(msg.Characters);
            RefreshCharacterList();
            ShowPanel(characterListPanel);
        }

        void RefreshCharacterList()
        {
            foreach (Transform child in characterSlotsParent)
                Destroy(child.gameObject);

            for (int i = 0; i < 3; i++)
            {
                GameObject card = Instantiate(characterCardPrefab, characterSlotsParent);
                CharacterCardUI cardUI = card.GetComponent<CharacterCardUI>();

                CharacterPreviewData character = currentCharacters.Find(c => c.SlotIndex == i);
                bool hasChar = character != null;

                int slot = i;
                long charId = character?.Id ?? 0;

                cardUI.Setup(i, hasChar, character,
                    onSelect: () => OnSelectCharacter(slot, charId),
                    onDelete: () => OnDeleteCharacter(charId),
                    onCreate: () => { selectedSlot = slot; ShowPanel(createCharacterPanel); });
            }
        }

        void OnSelectCharacter(int slot, long charId)
        {
            if (charId == 0) return;

            ShowPanel(loadingPanel);
            NetworkClient.Send(new SelectCharacterRequest { CharacterId = charId });
        }

        void OnSelectCharacterResponse(SelectCharacterResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($"Erro ao entrar: {msg.Error}");
                return;
            }

            Debug.Log($"Entrando no mundo: {msg.MapName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        void OnCreateCharacterClick()
        {
            if (string.IsNullOrWhiteSpace(charNameInput.text) || charNameInput.text.Length < 3)
            {
                ShowError("Nome muito curto (min 3 caracteres)");
                return;
            }

            ShowPanel(loadingPanel);

            NetworkClient.Send(new CreateCharacterRequest
            {
                SlotIndex = (byte)selectedSlot,
                Name = charNameInput.text,
                Gender = (byte)genderDropdown.value,
                Job = (byte)jobDropdown.value,
                HairStyle = 0,
                HairColor = 0
            });
        }

        void OnCreateCharacterResponse(CreateCharacterResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($"Falha: {msg.Error}");
                return;
            }

            NetworkClient.Send(new CharacterListRequest());
            ShowPanel(loadingPanel);
        }

        void OnDeleteCharacter(long charId)
        {
            NetworkClient.Send(new DeleteCharacterRequest
            {
                CharacterId = charId,
                Password = "" // TODO: Pedir senha
            });
        }

        void OnDeleteCharacterResponse(DeleteCharacterResponse msg)
        {
            if (msg.Success)
            {
                NetworkClient.Send(new CharacterListRequest());
                ShowPanel(loadingPanel);
            }
            else
            {
                ShowError("Falha ao deletar personagem");
            }
        }

        void OnJobChanged(int jobIndex)
        {
            if (currentPreview != null)
                Destroy(currentPreview);

            if (classPreviewPrefabs != null && jobIndex < classPreviewPrefabs.Length)
            {
                currentPreview = Instantiate(classPreviewPrefabs[jobIndex], previewSpawnPoint);
            }
        }
    }

    public class CharacterCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI jobText;
        [SerializeField] private Image charImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button createButton;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;

        public void Setup(int slot, bool hasCharacter, CharacterPreviewData data,
            System.Action onSelect, System.Action onDelete, System.Action onCreate)
        {
            if (hasCharacter && data != null)
            {
                emptyState.SetActive(false);
                filledState.SetActive(true);

                nameText.text = data.Name;
                levelText.text = $"Lv. {data.Level}";
                jobText.text = GetJobName(data.Job);

                selectButton.onClick.AddListener(() => onSelect?.Invoke());
                deleteButton.onClick.AddListener(() => onDelete?.Invoke());
            }
            else
            {
                emptyState.SetActive(true);
                filledState.SetActive(false);
                createButton.onClick.AddListener(() => onCreate?.Invoke());
            }
        }

        string GetJobName(byte job)
        {
            switch (job)
            {
                case 0: return "Swordsman";
                case 1: return "Hunter";
                case 2: return "Herbalist";
                case 3: return "Explorer";
                default: return "Unknown";
            }
        }
    }
}
