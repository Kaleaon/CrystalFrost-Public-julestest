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
        private IUIStateTracker _uiStateTracker;

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

            // Get UI state tracker
            try
            {
                _uiStateTracker = Services.GetService<IUIStateTracker>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not get UIStateTracker: {ex.Message}");
            }
            
            LoadCredentialsToForm();

            // Track initial UI state
            if (_uiStateTracker != null)
            {
                if (loginUI != null)
                    _uiStateTracker.TrackComponentCreated(loginUI, "LoginWindow");
                if (loggedInUI != null)
                    _uiStateTracker.TrackComponentCreated(loggedInUI, "LoggedInWindow");
                if (consoleUI != null)
                    _uiStateTracker.TrackComponentCreated(consoleUI, "ConsoleWindow");
            }
        }

        private void LoadCredentialsToForm()
        {
            if (_currentCredential != null)
            {
                firstName.text = _currentCredential.FirstName;
                lastName.text = _currentCredential.LastName;
                // Don't load password for security reasons

                // Track content changes
                if (_uiStateTracker != null)
                {
                    _uiStateTracker.TrackContentChanged(firstName.gameObject, "InputField", 
                        new { FieldName = "FirstName", Value = _currentCredential.FirstName });
                    _uiStateTracker.TrackContentChanged(lastName.gameObject, "InputField", 
                        new { FieldName = "LastName", Value = _currentCredential.LastName });
                }
            }
        }

        public void ShowLoginUI()
        {
            loginUI.SetActive(true);
            loggedInUI.SetActive(false);

            // Track UI state changes
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackVisibilityChanged(loginUI, "LoginWindow", true);
                _uiStateTracker.TrackVisibilityChanged(loggedInUI, "LoggedInWindow", false);
            }
        }

        public void ShowLoggedInUI()
        {
            loginUI.SetActive(false);
            loggedInUI.SetActive(true);

            // Track UI state changes
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackVisibilityChanged(loginUI, "LoginWindow", false);
                _uiStateTracker.TrackVisibilityChanged(loggedInUI, "LoggedInWindow", true);
            }
        }

        public void OnLoginButtonClicked()
        {
            // Track the interaction
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackInteraction(gameObject, "LoginUIController", "LoginButtonClicked", 
                    new { FirstName = firstName.text, LastName = lastName.text, GridURL = gridURL.text });
            }

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
                
                // Track console content change
                if (_uiStateTracker != null)
                {
                    _uiStateTracker.TrackContentChanged(console.gameObject, "ConsoleText", 
                        new { Message = message });
                }
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

                // Track credential update
                if (_uiStateTracker != null)
                {
                    _uiStateTracker.TrackInteraction(gameObject, "LoginUIController", "CredentialsUpdated", 
                        new { FirstName = firstName.text, LastName = lastName.text });
                }
            }
        }

        public string GetGridURL()
        {
            return gridURL.text;
        }

        private void OnDestroy()
        {
            // Track UI destruction
            if (_uiStateTracker != null)
            {
                if (loginUI != null)
                {
                    string componentId = $"{GetHierarchyPath(loginUI)}_{loginUI.GetInstanceID()}";
                    _uiStateTracker.TrackComponentDestroyed(componentId, "LoginWindow");
                }
                if (loggedInUI != null)
                {
                    string componentId = $"{GetHierarchyPath(loggedInUI)}_{loggedInUI.GetInstanceID()}";
                    _uiStateTracker.TrackComponentDestroyed(componentId, "LoggedInWindow");
                }
                if (consoleUI != null)
                {
                    string componentId = $"{GetHierarchyPath(consoleUI)}_{consoleUI.GetInstanceID()}";
                    _uiStateTracker.TrackComponentDestroyed(componentId, "ConsoleWindow");
                }
            }
        }

        private string GetHierarchyPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform current = gameObject.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}