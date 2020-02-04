using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Firebase.Auth;
using QSocial.Utility;

namespace QSocial.Auth.Methods
{
    [System.Serializable,HasAnonymousConversion]
    public class EmailMethod : AuthMethod, IAuthCustomUI, IAuthCustomNavigation
    {
        enum emailNavigation
        {
            None = -1,
            LoginForm = 0,
            RegisterForm = 1,
            ForgotPassword = 2,
            ForgotPasswordFinish = 3
        }

        public override string Id => "Email-SingIn";

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

        private emailNavigation nav = emailNavigation.LoginForm;

        private string uid = default;
        public override string ResultUserId => uid;

        private ProcessResult result = ProcessResult.None;
        public override ProcessResult GetResult() => result;
        
        private System.Exception ex = null;

        public override System.Exception GetException() => ex;

        private bool BackResult = false;

        public override void OnEnter()
        {
            result = ProcessResult.None;
        }

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
                result = ProcessResult.Running;

                if (!string.IsNullOrEmpty(Email_SingIn.text))
                    Email_Recoverpassword.text = Email_SingIn.text;

                AuthManager.BeginProcess();
                AuthManager.Instance.auth.SendPasswordResetEmailAsync(Email_Recoverpassword.text).ContinueWith
                (task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        QEventExecutor.ExecuteInUpdate(() =>
                        {
                            ex = AuthManager.GetFirebaseException(task.Exception);
                            AuthManager.Instance.logger.LogError("Error sending password " + ex, this);
                            AuthManager.FinishProcess();
                            result = ProcessResult.Failure;
                        });
                        return;
                    }

                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        AuthManager.Instance.logger.Log("Password sent correctly", this , true);
                        nav = emailNavigation.ForgotPasswordFinish;
                        UpdateLayout();
                        result = ProcessResult.Running;
                    });
                });
            });

            RegisterButton.onClick.AddListener(() =>
            {
                result = ProcessResult.Running;
                if (!string.IsNullOrEmpty(Password_Register.text) &&
                string.Equals(Password_Register.text, Password_Register_c.text))
                {
                    if (AuthManager.Instance.IsAuthenticated)
                    {
                        if (AuthManager.Instance.auth.CurrentUser.IsAnonymous)
                        {
                            AuthManager.BeginProcess();
                            Credential ecred = EmailAuthProvider.GetCredential(Email_Register.text,
                                Password_Register.text);

                            AuthManager.Instance.auth.CurrentUser.LinkWithCredentialAsync(ecred).ContinueWith
                            (task =>
                            {
                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    QEventExecutor.ExecuteInUpdate(() =>
                                    {
                                        AuthManager.FinishProcess();
                                        ex = AuthManager.GetFirebaseException(task.Exception);
                                        AuthManager.Instance.logger.LogError("Fail to link account " + ex, this);
                                        result = ProcessResult.Running;
                                    });
                                    return;
                                }

                                QEventExecutor.ExecuteInUpdate(() =>
                                {
                                    AuthManager.FinishProcess();
                                    AuthManager.Instance.logger.Log("Link Account completed!" , this , true);
                                    result = ProcessResult.Completed;
                                });
                            });
                        }
                        else
                        {
                            AuthManager.Instance.logger.LogWarning("User is not anonymous!", this);
                            result = ProcessResult.Failure;
                        }
                    }
                    else
                    {
                        AuthManager.BeginProcess();
                        AuthManager.Instance.auth.CreateUserWithEmailAndPasswordAsync(Email_Register.text,
                               Password_Register.text).ContinueWith(task =>
                               {
                                   if (task.IsFaulted || task.IsCanceled)
                                   {
                                       QEventExecutor.ExecuteInUpdate(() =>
                                       {
                                           AuthManager.FinishProcess();
                                           ex = AuthManager.GetFirebaseException(task.Exception);
                                           AuthManager.Instance.logger.LogError("Failed to create user with email " + ex, this);
                                           result = ProcessResult.Failure;
                                       });
                                       return;
                                   }

                                   QEventExecutor.ExecuteInUpdate(() =>
                                   {
                                       AuthManager.FinishProcess();
                                       AuthManager.Instance.logger.Log("Create user with email done", this, true);
                                       uid = task.Result.UserId;
                                       result = ProcessResult.Completed;
                                   });
                                   
                               });
                    }
                }
                else
                {
                    AuthManager.Instance.logger.LogWarning("Passwords must match", this);
                    result = ProcessResult.None;
                }
            });

            SingInButton.onClick.AddListener(() =>
            {
                AuthManager.BeginProcess();
                result = ProcessResult.Running;
                AuthManager.Instance.auth.SignInWithEmailAndPasswordAsync(Email_SingIn.text, Password_SingIn.text)
                 .ContinueWith(task =>
                 {
                     if (task.IsFaulted || task.IsCanceled)
                     {
                         QEventExecutor.ExecuteInUpdate(() =>
                         {
                             AuthManager.FinishProcess();
                             ex = AuthManager.GetFirebaseException(task.Exception);
                             result = ProcessResult.Failure;
                         });
                         return;
                     }

                     QEventExecutor.ExecuteInUpdate(() =>
                     {
                         AuthManager.FinishProcess();
                         result = ProcessResult.Completed;
                     });
                 });
            });
        }

        private void UpdateLayout()
        {
            form_Register.SetActive(nav == emailNavigation.RegisterForm);
            form_SingIn.SetActive(nav == emailNavigation.LoginForm);
            form_ForgotPassword.SetActive(nav == emailNavigation.ForgotPassword);
            form_ForgotPwFinish.SetActive(nav == emailNavigation.ForgotPasswordFinish);
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
            }
            else if (Input.GetKeyUp(KeyCode.Escape))
            {
                BackResult = false;
            }
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

        public bool GoBack() => BackResult;
    }
}