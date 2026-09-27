using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Tabs")]
    [SerializeField] private Button loginTabButton;
    [SerializeField] private Button registerTabButton;
    [SerializeField] private Image loginTabBackground;
    [SerializeField] private Image registerTabBackground;
    [SerializeField] private TMP_Text loginTabLabel;
    [SerializeField] private TMP_Text registerTabLabel;

    [Header("Login")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button showLoginPasswordButton;
    [SerializeField] private TMP_Text showLoginPasswordLabel;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToRegisterButton;
    [SerializeField] private TMP_Text loginMessageText;

    [Header("Register")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button showRegisterPasswordButton;
    [SerializeField] private TMP_Text showRegisterPasswordLabel;
    [SerializeField] private Button showConfirmPasswordButton;
    [SerializeField] private TMP_Text showConfirmPasswordLabel;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button goToLoginButton;
    [SerializeField] private TMP_Text registerMessageText;

    private readonly Color activeTabColor = new(0.08f, 0.69f, 0.92f, 1f);
    private readonly Color inactiveTabColor = new(0.12f, 0.17f, 0.29f, 1f);
    private readonly Color normalTextColor = new(0.72f, 0.78f, 0.89f, 1f);
    private readonly Color successColor = new(0.29f, 0.91f, 0.61f, 1f);
    private readonly Color errorColor = new(1f, 0.39f, 0.46f, 1f);

    private bool loginPasswordVisible;
    private bool registerPasswordVisible;
    private bool confirmPasswordVisible;

    private void Awake()
    {
        loginTabButton.onClick.AddListener(ShowLoginPanel);
        registerTabButton.onClick.AddListener(ShowRegisterPanel);
        goToLoginButton.onClick.AddListener(ShowLoginPanel);
        goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

        loginButton.onClick.AddListener(SubmitLogin);
        registerButton.onClick.AddListener(SubmitRegister);

        showLoginPasswordButton.onClick.AddListener(ToggleLoginPassword);
        showRegisterPasswordButton.onClick.AddListener(ToggleRegisterPassword);
        showConfirmPasswordButton.onClick.AddListener(ToggleConfirmPassword);

        loginPasswordInput.onSubmit.AddListener(_ => SubmitLogin());
        confirmPasswordInput.onSubmit.AddListener(_ => SubmitRegister());

        ShowLoginPanel();
    }

    private void Start()
    {
        loginUsernameInput.Select();
        loginUsernameInput.ActivateInputField();
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        SetTabState(true);
        ClearMessages();
    }

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        SetTabState(false);
        ClearMessages();
        registerUsernameInput.Select();
        registerUsernameInput.ActivateInputField();
    }

    private void SetTabState(bool loginIsActive)
    {
        loginTabBackground.color = loginIsActive
            ? activeTabColor
            : inactiveTabColor;
        registerTabBackground.color = loginIsActive
            ? inactiveTabColor
            : activeTabColor;

        loginTabLabel.color = loginIsActive ? Color.white : normalTextColor;
        registerTabLabel.color = loginIsActive ? normalTextColor : Color.white;
    }

    private void SubmitLogin()
    {
        ClearMessages();

        bool success = LocalAccountService.Login(
            loginUsernameInput.text,
            loginPasswordInput.text,
            out string message
        );

        if (!success)
        {
            ShowMessage(loginMessageText, message, false);
            return;
        }

        loginPasswordInput.text = string.Empty;
        ShowMessage(loginMessageText, "Đăng nhập thành công!", true);
        TryOpenMainMenu();
    }

    private void SubmitRegister()
    {
        ClearMessages();

        if (registerPasswordInput.text != confirmPasswordInput.text)
        {
            ShowMessage(
                registerMessageText,
                "Mật khẩu nhập lại chưa khớp.",
                false
            );
            return;
        }

        bool success = LocalAccountService.Register(
            registerUsernameInput.text,
            registerPasswordInput.text,
            displayNameInput.text,
            out string message
        );

        if (!success)
        {
            ShowMessage(registerMessageText, message, false);
            return;
        }

        registerPasswordInput.text = string.Empty;
        confirmPasswordInput.text = string.Empty;
        ShowMessage(
            registerMessageText,
            "Tạo tài khoản thành công!",
            true
        );
        TryOpenMainMenu();
    }

    private void TryOpenMainMenu()
    {
        if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            SceneManager.LoadScene(MainMenuSceneName);
            return;
        }

        Debug.Log(
            "Đăng nhập thành công. MainMenu chưa tồn tại nên vẫn ở Login."
        );
    }

    private void ToggleLoginPassword()
    {
        loginPasswordVisible = !loginPasswordVisible;
        SetPasswordVisibility(
            loginPasswordInput,
            showLoginPasswordLabel,
            loginPasswordVisible
        );
    }

    private void ToggleRegisterPassword()
    {
        registerPasswordVisible = !registerPasswordVisible;
        SetPasswordVisibility(
            registerPasswordInput,
            showRegisterPasswordLabel,
            registerPasswordVisible
        );
    }

    private void ToggleConfirmPassword()
    {
        confirmPasswordVisible = !confirmPasswordVisible;
        SetPasswordVisibility(
            confirmPasswordInput,
            showConfirmPasswordLabel,
            confirmPasswordVisible
        );
    }

    private static void SetPasswordVisibility(
        TMP_InputField input,
        TMP_Text buttonLabel,
        bool isVisible)
    {
        input.contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        input.ForceLabelUpdate();
        buttonLabel.text = isVisible ? "ẨN" : "HIỆN";
        input.ActivateInputField();
        input.caretPosition = input.text.Length;
    }

    private void ClearMessages()
    {
        loginMessageText.text = string.Empty;
        registerMessageText.text = string.Empty;
    }

    private void ShowMessage(
        TMP_Text messageText,
        string message,
        bool success)
    {
        messageText.text = message;
        messageText.color = success ? successColor : errorColor;
    }
}
