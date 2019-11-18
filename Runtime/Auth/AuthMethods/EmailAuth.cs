using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

namespace QSocial.Auth
{
    [System.Serializable]
    public class EmailAuth : AuthMethod,IAuthCustomUI
    {
        [SerializeField]
        private int MinimunPasswordLenght = 8;
        [SerializeField]
        private GameObject EmailFormContainer = default;
        [SerializeField]
        private GameObject LoginForm = default;
        [SerializeField]
        private GameObject RegisterForm = default;
        [SerializeField]
        private Button RegisterFormButton = default;
        [SerializeField]
        private TMP_InputField login_email = default;
        [SerializeField]
        private TMP_InputField login_password = default;
        [SerializeField]
        private Button LoginButton = default;
        [SerializeField]
        private TMP_InputField register_email = default;
        [SerializeField]
        private TMP_InputField register_password = default;
        [SerializeField]
        private TMP_InputField register_passwordConfirm = default;
        [SerializeField]
        private Button RegisterButton = default;

        public override string Id => "Auth-email";

        private AuthResult Result = AuthResult.None;


        protected override void OnInit()
        {
            LoginButton.onClick.AddListener(() => Login());
            RegisterButton.onClick.AddListener(() => Register());
            RegisterFormButton.onClick.AddListener(() => GoRegister());
        }

        private void GoRegister()
        {
            RegisterForm.SetActive(true);
            LoginForm.SetActive(false);
        }

        public void HideUI()
        {
            LoginForm.SetActive(false);
            RegisterForm.SetActive(false);
            EmailFormContainer.SetActive(false);
        }

        public bool GoBack()
        {
            Debug.Log("GoBack!");
            if (RegisterForm.activeInHierarchy)
            {
                LoginForm.SetActive(true);
                RegisterForm.SetActive(false);
                return false;
            }else
            {
                LoginForm.SetActive(false);
                return true;
            }
        }

        public override void OnSelect()
        {
            EmailFormContainer.SetActive(true);
            LoginForm.SetActive(!IsAnonymousUser);
            RegisterForm.SetActive(IsAnonymousUser);
        }

        public override void OnReset()
        {
            Result = AuthResult.None;
        }

        public override AuthResult OnExecute()
        {
            return Result;
        }

        private void Login()
        {
            Result = AuthResult.Running;
            Debug.Log("Login in... (email)");
            AuthManager.Instance.SingInWithEmail(login_email.text, login_password.text,
                () =>
                {
                    Result = AuthResult.Success;
                    Debug.Log("Login with email Success!");
                }, (string msg) =>
                {
                    Debug.LogError("Login with email Failure! " + msg);
                    Result = AuthResult.Failure;
                });
        }

        private void Register()
        {

            if (register_password.text.Length < 8)
            {
                Result = AuthResult.Failure;
                Debug.LogError($"Password must be at least {MinimunPasswordLenght} characters lenght");
                return;
            }

            if (!string.Equals(register_password.text, register_passwordConfirm.text))
            {
                Result = AuthResult.Failure;
                Debug.LogError("Passwords are not equals!");
                return;
            }

            Result = AuthResult.Running;

            if (IsAnonymousUser)
            {
                Debug.Log("Anonymous to user: email");
                AuthManager.Instance.LinkAccount(EmailAuthProvider.GetCredential(register_email.text,
                    register_password.text),
                    () =>
                    {
                        Debug.Log("Anonymous to user: completed");
                        Result = AuthResult.Success;
                    },
                    (string msg) =>
                    {
                        Debug.LogError("Anonymous to user: Failure => " + msg);
                        Result = AuthResult.Failure;
                    });
            }
            else
            {
                Debug.Log("Register... (email)");
                AuthManager.Instance.CreateUserWithEmail(register_email.text, register_password.text,
                    () =>
                    {
                        Debug.Log("Register with email Success!");
                        Result = AuthResult.Success;
                    }, (string msg) =>
                    {
                        Debug.LogError("Register with email Failure!");
                        Result = AuthResult.Failure;
                    });
            }
        }
    }
}