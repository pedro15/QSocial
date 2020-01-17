using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase;

using QSocial.Data;
using QSocial.Data.Users;
using QSocial.Utility;

namespace QSocial.Auth.Methods
{
    [HasAnonymousConversion,System.Serializable]
    public class EmailMethod : AuthMethod,IAuthCustomUI,IAuthCustomNavigation
    {
        enum emailNavigation
        {
            None = -1,
            LoginForm = 0,
            RegisterForm = 1,
            ForgotPassword = 2,
            ForgotPasswordFinish = 3
        }

        [Header("SingIn-Form")]
        [SerializeField]
        private GameObject form_SingIn = default;
        [SerializeField]
        private Button RegisterFormButton = default;
        [SerializeField]
        private TMP_InputField Email_SingIn = default;
        [SerializeField]
        private TMP_InputField Password_SingIn = default;
        [SerializeField]
        private Button ForgotPasswordButton = default;
        [SerializeField]
        private Button SingInButton = default;
        [Header("Register-Form")]
        [SerializeField]
        private GameObject form_Register = default;
        [SerializeField]
        private Button RegisterButton = default;
        [SerializeField]
        private TMP_InputField Email_Register = default;
        [SerializeField]
        private TMP_InputField Password_Register = default;
        [SerializeField]
        private TMP_InputField Password_Register_c = default;
        [Header("ForgotPassword-Form")]
        [SerializeField]
        private GameObject form_ForgotPassword = default;
        [SerializeField]
        private TMP_InputField Email_Recoverpassword = default;
        [SerializeField]
        private Button RecoverPasswordButton = default;
        [Header("ForgotPassword-Completed")]
        [SerializeField]
        private GameObject form_ForgotPwFinish = default;
        [SerializeField]
        private Button BackToSingInButton = default;

        private AuthResult result = AuthResult.None;

        private emailNavigation nav = emailNavigation.LoginForm;

        private bool BackResult = false;

        private string uid;

        private System.Exception ex;

        public override string Id => "Auth-Email";

        public override string ResultUserId => uid; 

        public override void OnEnter()
        {
            result = AuthResult.None;
        }

        public override System.Exception GetException() => ex;

        protected override void OnInit(AuthManager manager)
        {
            RegisterFormButton.onClick.AddListener(() =>
           {
               nav = emailNavigation.RegisterForm;
               UpdateLayout();
           });

            BackToSingInButton.onClick.AddListener(() =>
           {
               nav = emailNavigation.LoginForm;
               UpdateLayout();
           });

            ForgotPasswordButton.onClick.AddListener(() =>
            {
                nav = emailNavigation.ForgotPassword;
                UpdateLayout();
            });

            RecoverPasswordButton.onClick.AddListener(() =>
            {
                result = AuthResult.Running;
                Debug.Log("Recover email password");

                if (!string.IsNullOrEmpty(Email_SingIn.text))
                    Email_Recoverpassword.text = Email_SingIn.text;

                AuthManager.Instance.auth.SendPasswordResetEmailAsync(Email_Recoverpassword.text).ContinueWith
                (task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.Log("Fail to recover email!");
                        ex = task.Exception;
                        result = AuthResult.Failure;
                        return;
                    }

                    Debug.Log("Recover password sent!");

                    QEventExecutor.ExecuteInUpdate(() =>
                   {
                       nav = emailNavigation.ForgotPasswordFinish;
                       UpdateLayout();
                   });

                    result = AuthResult.Running;
                });
            });

            RegisterButton.onClick.AddListener(() =>
            {
                result = AuthResult.Running;
                Debug.Log("Create user with email!");
                if (!string.IsNullOrEmpty(Password_Register.text) && 
                string.Equals(Password_Register.text , Password_Register_c.text))
                {

                    if (AuthManager.Instance.IsAuthenticated)
                    {
                        if (AuthManager.Instance.auth.CurrentUser.IsAnonymous)
                        {
                            Credential ecred = EmailAuthProvider.GetCredential(Email_Register.text,
                                Password_Register.text);

                            AuthManager.Instance.auth.CurrentUser.LinkWithCredentialAsync(ecred).ContinueWith
                            (task =>
                            {
                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Debug.LogError("Fail to link account! " + task.Exception?.ToString());
                                    ex = task.Exception;
                                    result = AuthResult.Failure;
                                    return;
                                }

                                Debug.Log("Link account completed !");
                                result = AuthResult.Completed;
                            });
                        }else
                        {
                            Debug.LogError("User is not Anonymouus!");
                            result = AuthResult.Failure;
                        }
                    }else
                    {
                        AuthManager.Instance.auth.CreateUserWithEmailAndPasswordAsync(Email_Register.text,
                               Password_Register.text).ContinueWith(task =>
                               {
                                   if (task.IsFaulted || task.IsCanceled)
                                   {
                                       Debug.LogError("Create user with email failed " + task.Exception?.Message);
                                       ex = task.Exception;
                                       result = AuthResult.Failure;
                                       return;
                                   }

                                   Debug.Log("Create user with email completed !");

                                   uid = task.Result.UserId;
                                   result = AuthResult.Completed;

                               });
                    }
                }else
                {
                    ex = new System.Exception("Passwords must match!");
                    result = AuthResult.Failure;
                    Debug.LogWarning("Passwords must match !");
                }
            });

            SingInButton.onClick.AddListener(() =>
            {
                Debug.Log("Sing in with email");
                result = AuthResult.Running;
                AuthManager.Instance.auth.SignInWithEmailAndPasswordAsync(Email_SingIn.text, Password_SingIn.text)
                 .ContinueWith(task =>
                 {
                     if (task.IsFaulted || task.IsCanceled)
                     {
                         ex = AuthManager.GetFirebaseException(task.Exception);

                         Debug.LogError("Sing in with email failed " + ex?.Message +  " error code:  " + ((FirebaseException)ex).ErrorCode);
                         result = AuthResult.Failure;
                         return;
                     }


                     Debug.Log("Sing in with email completed!");
                     result = AuthResult.Completed;

                 });
            });
        }

        private void UpdateLayout()
        {
            Debug.Log($"UpdateLayout :: { nav }");
            form_Register.SetActive(nav == emailNavigation.RegisterForm);
            form_SingIn.SetActive(nav == emailNavigation.LoginForm);
            form_ForgotPassword.SetActive(nav == emailNavigation.ForgotPassword);
            form_ForgotPwFinish.SetActive(nav == emailNavigation.ForgotPasswordFinish);
        }

        public override AuthResult GetResult()
        {
            return result;
        }

        public void DisplayUI(bool IsAnonymous)
        {
            nav = IsAnonymous ? emailNavigation.RegisterForm : emailNavigation.LoginForm;
            UpdateLayout();
        }

        public void HideUI()
        {
            nav = emailNavigation.None;
            BackResult = false;
            UpdateLayout();
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (AuthManager.Instance.IsAuthenticated && AuthManager.Instance.auth.CurrentUser.IsAnonymous)
                {
                    nav = emailNavigation.None;
                    UpdateLayout();
                    BackResult = true;
                }
                else
                {
                    switch (nav)
                    {
                        case emailNavigation.ForgotPassword:

                            nav = emailNavigation.LoginForm;

                            break;

                        case emailNavigation.ForgotPasswordFinish:

                            nav = emailNavigation.ForgotPassword;

                            break;

                        case emailNavigation.RegisterForm:

                            nav = emailNavigation.LoginForm;

                            break;

                        case emailNavigation.LoginForm:

                            nav = emailNavigation.None;

                            break;
                    }
                    UpdateLayout();
                    BackResult = (nav != emailNavigation.None) ? false : true;
                }
            }else if (Input.GetKeyUp(KeyCode.Escape))
            {
                BackResult = false;
            }
        }

        public bool GoBack() => BackResult;
    }
}
