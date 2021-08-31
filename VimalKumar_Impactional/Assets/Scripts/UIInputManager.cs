using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Manages all the text and button inputs
// Also acts like the main manager script for the game.
public class UIInputManager : MonoBehaviour
{
   public static string CachePath;

   public Button signupButton;
   public Button loginButton;
   public Button startButton;
   public Button logoutButton;
   public Button confirmButton;
    public InputField confirmCodeField;
    public InputField emailFieldLogin;
   public InputField passwordFieldLogin;
   public InputField usernameField;
   public InputField emailField;
   public InputField passwordField;

   private AuthenticationManager _authenticationManager;
   private GameObject _unauthInterface;
   private GameObject _authInterface;
   private GameObject _loading;
   private GameObject _welcome;
   private GameObject _confirmEmail;
   public GameObject _signupContainer;

   private List<Selectable> _fields;
   private int _selectedFieldIndex = -1;

   private void displayComponentsFromAuthStatus(bool authStatus)
   {
      if (authStatus)
      {
         // Debug.Log("User authenticated, show welcome screen with options");
         _loading.SetActive(false);
         _unauthInterface.SetActive(false);
         _authInterface.SetActive(true);
         _welcome.SetActive(true);
      }
      else
      {
         // Debug.Log("User not authenticated, activate/stay on login scene");
         _loading.SetActive(false);
         _unauthInterface.SetActive(true);
         _authInterface.SetActive(false);
      }

      // clear out passwords
      passwordFieldLogin.text = "";
      passwordField.text = "";

      // set focus to email field on login form
      _selectedFieldIndex = -1;
   }

   private async void onLoginClicked()
   {
      _unauthInterface.SetActive(false);
      _loading.SetActive(true);
      // Debug.Log("onLoginClicked: " + emailFieldLogin.text + ", " + passwordFieldLogin.text);
      bool successfulLogin = await _authenticationManager.Login(emailFieldLogin.text, passwordFieldLogin.text);
      displayComponentsFromAuthStatus(successfulLogin);
   }

   private async void onSignupClicked()
   {
      _unauthInterface.SetActive(false);
      _loading.SetActive(true);

      // Debug.Log("onSignupClicked: " + usernameField.text + ", " + emailField.text + ", " + passwordField.text);
      bool successfulSignup = await _authenticationManager.Signup(usernameField.text, emailField.text, passwordField.text);

      if (successfulSignup)
      {
         // here we re-enable the whole auth container but hide the sign up panel
         _signupContainer.SetActive(false);

         _confirmEmail.SetActive(true);

         // copy over the new credentials to make the process smoother
         emailFieldLogin.text = emailField.text;
         passwordFieldLogin.text = passwordField.text;

         // set focus to email field on login form
         _selectedFieldIndex = 0;
      }
      else
      {
         _confirmEmail.SetActive(false);
         // set focus to email field on signup form
         _selectedFieldIndex = 3;
      }

      _loading.SetActive(false);
      _unauthInterface.SetActive(true);
   }

    public async void onConfirmClick()
    {


        // Debug.Log("onSignupClicked: " + usernameField.text + ", " + emailField.text + ", " + passwordField.text);
        bool successfullConfirm = await _authenticationManager.Confirm(usernameField.text, emailField.text, confirmCodeField.text);

        if (successfullConfirm)
        {
            // here we re-enable the whole auth container but hide the sign up panel
            Debug.Log("Conirmed User");
        }
        else
        {
            Debug.Log("Error Conirmed User");
        }


    }

    private void onLogoutClick()
   {
      _authenticationManager.SignOut();
      displayComponentsFromAuthStatus(false);
   }

   private void onStartClick()
   {
      SceneManager.LoadScene("GameScene");
      //Debug.Log("Changed to GameScene");
   }

   private async void RefreshToken()
   {
      bool successfulRefresh = await _authenticationManager.RefreshSession();
      displayComponentsFromAuthStatus(successfulRefresh);
   }
 

   void Start()
   {
      Debug.Log("UIInputManager: Start");

      RefreshToken();
      confirmButton.onClick.AddListener(onConfirmClick);
      signupButton.onClick.AddListener(onSignupClicked);
      loginButton.onClick.AddListener(onLoginClicked);
      startButton.onClick.AddListener(onStartClick);
      logoutButton.onClick.AddListener(onLogoutClick);
   }

   void Update()
   {
      HandleInputTabbing();
   }

   // Handles tabbing between inputs and buttons
   private void HandleInputTabbing()
   {
      if (Input.GetKeyDown(KeyCode.Tab))
      {
         CheckForAndSetManuallyChangedIndex();

         // update index to where we need to tab to
         _selectedFieldIndex++;

         if (_selectedFieldIndex >= _fields.Count)
         {
            // reset back to first input
            _selectedFieldIndex = 0;
         }
         _fields[_selectedFieldIndex].Select();
      }
   }


   private void CheckForAndSetManuallyChangedIndex()
   {
      for (var i = 0; i < _fields.Count; i++)
      {
         if (_fields[i] is InputField && ((InputField)_fields[i]).isFocused && _selectedFieldIndex != i)
         {
            // Debug.Log("_selectedFieldIndex is : " + _selectedFieldIndex + ", Reset _selectedFieldIndex to: " + i);
            _selectedFieldIndex = i;
            break;
         }
      }
   }

   void Awake()
   {
      CachePath = Application.persistentDataPath;

      _unauthInterface = GameObject.Find("UnauthInterface");
      _authInterface = GameObject.Find("AuthInterface");
      _loading = GameObject.Find("Loading");
      _welcome = GameObject.Find("Welcome");
      _confirmEmail = GameObject.Find("ConfirmEmail");

      _unauthInterface.SetActive(false); 
      _authInterface.SetActive(false);
      _welcome.SetActive(false);
      _confirmEmail.SetActive(false);
   
      _authenticationManager = FindObjectOfType<AuthenticationManager>();
      _fields = new List<Selectable> { emailFieldLogin, passwordFieldLogin, confirmButton,loginButton, emailField, usernameField, passwordField, signupButton };
   }
}
