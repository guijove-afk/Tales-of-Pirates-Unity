using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TOP.Network;
using TOP.Services;

namespace TOP.UI
{
    public class LoginUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;

        [Header("Login Inputs")]
        [SerializeField] private TMP_InputField loginUsername;
        [SerializeField] private TMP_InputField loginPassword;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button gotoRegisterButton;

        [Header("Register Inputs")]
        [SerializeField] private TMP_InputField registerUsername;
        [SerializeField] private TMP_InputField registerPassword;
        [SerializeField] private TMP_InputField registerConfirmPassword;
        [SerializeField] private TMP_InputField registerEmail;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backToLoginButton;

        [Header("Error")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button errorOkButton;

        [Header("Server")]
        [SerializeField] private TMP_InputField serverAddress;
        [SerializeField] private TMP_InputField serverPort;

        private NetworkManager networkManager;

        void Start()
        {
            networkManager = NetworkManager.singleton;

            loginButton.onClick.AddListener(OnLoginClick);
            gotoRegisterButton.onClick.AddListener(() => ShowPanel(registerPanel));
            registerButton.onClick.AddListener(OnRegisterClick);
            backToLoginButton.onClick.AddListener(() => ShowPanel(loginPanel));
            errorOkButton.onClick.AddListener(() => errorPanel.SetActive(false));

            NetworkClient.RegisterHandler<LoginResponse>(OnLoginResponse);
            NetworkClient.RegisterHandler<ServerMessage>(OnServerMessage);

            ShowPanel(loginPanel);

            if (PlayerPrefs.HasKey("TOP_LastUser"))
                loginUsername.text = PlayerPrefs.GetString("TOP_LastUser");

            if (PlayerPrefs.HasKey("TOP_Server"))
                serverAddress.text = PlayerPrefs.GetString("TOP_Server");
            else
                serverAddress.text = "localhost";

            if (PlayerPrefs.HasKey("TOP_Port"))
                serverPort.text = PlayerPrefs.GetString("TOP_Port");
            else
                serverPort.text = "7777";
        }

        void OnDestroy()
        {
            loginButton.onClick.RemoveAllListeners();
            gotoRegisterButton.onClick.RemoveAllListeners();
            registerButton.onClick.RemoveAllListeners();
            backToLoginButton.onClick.RemoveAllListeners();
            errorOkButton.onClick.RemoveAllListeners();
        }

        void ShowPanel(GameObject panel)
        {
            loginPanel.SetActive(panel == loginPanel);
            registerPanel.SetActive(panel == registerPanel);
            loadingPanel.SetActive(panel == loadingPanel);
        }

        void ShowError(string message)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }

        void OnLoginClick()
        {
            if (string.IsNullOrWhiteSpace(loginUsername.text))
            {
                ShowError("Digite um nome de usuario!");
                return;
            }

            if (string.IsNullOrWhiteSpace(loginPassword.text))
            {
                ShowError("Digite uma senha!");
                return;
            }

            ShowPanel(loadingPanel);

            networkManager.networkAddress = serverAddress.text;

            PlayerPrefs.SetString("TOP_LastUser", loginUsername.text);
            PlayerPrefs.SetString("TOP_Server", serverAddress.text);
            PlayerPrefs.SetString("TOP_Port", serverPort.text);
            PlayerPrefs.Save();

            networkManager.StartClient();

            StartCoroutine(SendLoginAfterConnect());
        }

        System.Collections.IEnumerator SendLoginAfterConnect()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkClient.isConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!NetworkClient.isConnected)
            {
                ShowError("Nao foi possivel conectar ao servidor!");
                yield break;
            }

            NetworkClient.Send(new LoginRequest
            {
                Username = loginUsername.text,
                Password = loginPassword.text
            });
        }

        void OnLoginResponse(LoginResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($"Login falhou: {msg.ErrorCode}");
                NetworkClient.Disconnect();
                return;
            }

            Debug.Log($"Login OK! Token: {msg.SessionToken}");

            UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelectScene");
        }

        void OnRegisterClick()
        {
            if (string.IsNullOrWhiteSpace(registerUsername.text) || registerUsername.text.Length < 3)
            {
                ShowError("Nome de usuario muito curto (min 3 chars)");
                return;
            }

            if (registerPassword.text != registerConfirmPassword.text)
            {
                ShowError("As senhas nao coincidem!");
                return;
            }

            if (string.IsNullOrWhiteSpace(registerEmail.text))
            {
                ShowError("Digite um email!");
                return;
            }

            ShowPanel(loadingPanel);

            _ = RegisterAsync(registerUsername.text, registerPassword.text, registerEmail.text);
        }

        async System.Threading.Tasks.Task RegisterAsync(string user, string pass, string email)
        {
            var success = await DatabaseService.Instance.CreateAccountAsync(user, pass, email);

            if (success)
            {
                ShowError("Conta criada com sucesso! Faca login.");
                ShowPanel(loginPanel);
                loginUsername.text = user;
            }
            else
            {
                ShowError("Falha ao criar conta. Tente outro nome.");
                ShowPanel(registerPanel);
            }
        }

        void OnServerMessage(ServerMessage msg)
        {
            ShowError(msg.Text);
        }
    }
}
