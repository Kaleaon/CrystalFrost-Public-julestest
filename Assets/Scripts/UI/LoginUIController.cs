using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrystalFrost.Client.Credentials;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Handles all UI interactions for the login form, including form validation and UI state management
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loginUI;
        [SerializeField] private GameObject loggedInUI;
        [SerializeField] private GameObject consoleUI;
        [SerializeField] private TMP_InputField firstName;
        [SerializeField] private TMP_InputField lastName;
        [SerializeField] private TMP_InputField password;
        [SerializeField] private TMP_InputField gridURL;
        [SerializeField] private TMP_Text console;

        private LoginCredential _currentCredential;

        public System.Action<string, string, string, string> OnLoginRequested;

        public void Initialize(LoginCredential credential, GameObject loginUI, GameObject loggedInUI, 
            GameObject consoleUI, TMP_InputField firstName, TMP_InputField lastName, 
            TMP_InputField password, TMP_InputField gridURL, TMP_Text console)
        {
            _currentCredential = credential;
            
            // Set UI references
            this.loginUI = loginUI;
            this.loggedInUI = loggedInUI;
            this.consoleUI = consoleUI;
            this.firstName = firstName;
            this.lastName = lastName;
            this.password = password;
            this.gridURL = gridURL;
            this.console = console;
            
            LoadCredentialsToForm();
        }

        private void LoadCredentialsToForm()
        {
            if (_currentCredential != null)
            {
                firstName.text = _currentCredential.FirstName;
                lastName.text = _currentCredential.LastName;
                // Don't load password for security reasons
            }
        }

        public void ShowLoginUI()
        {
            loginUI.SetActive(true);
            loggedInUI.SetActive(false);
        }

        public void ShowLoggedInUI()
        {
            loginUI.SetActive(false);
            loggedInUI.SetActive(true);
        }

        public void OnLoginButtonClicked()
        {
            if (ValidateLoginForm())
            {
                OnLoginRequested?.Invoke(
                    firstName.text,
                    lastName.text, 
                    password.text,
                    gridURL.text
                );
            }
        }

        private bool ValidateLoginForm()
        {
            if (string.IsNullOrWhiteSpace(firstName.text))
            {
                ShowConsoleMessage("First name is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastName.text))
            {
                ShowConsoleMessage("Last name is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password.text))
            {
                ShowConsoleMessage("Password is required");
                return false;
            }

            return true;
        }

        public void ShowConsoleMessage(string message)
        {
            if (console != null)
            {
                console.text = message;
            }
            Debug.Log($"Login UI: {message}");
        }

        public void UpdateCredentials()
        {
            if (_currentCredential != null)
            {
                _currentCredential.FirstName = firstName.text;
                _currentCredential.LastName = lastName.text;
                _currentCredential.Password = password.text;
            }
        }

        public string GetGridURL()
        {
            return gridURL.text;
        }
    }
}