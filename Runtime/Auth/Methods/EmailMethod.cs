using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

namespace QSocial.Auth.Methods
{
    [HasAnonymousConversion,System.Serializable]
    public class EmailMethod : AuthMethod,IAuthCustomUI,IAuthCustomNavigation
    {
        [Header("Layout")]
        [SerializeField]
        private GameObject SinginForm = default;
        [SerializeField]
        private Button RegisterFormButton = default;
        [SerializeField]
        private Button BackButton = default;
        [SerializeField]
        private GameObject RegisterForm = default;
        [Header("Action Buttons")]
        [SerializeField]
        private Button RegisterButton = default;
        [SerializeField]
        private Button SingInButton = default;
        [Header("Input")]
        [SerializeField]
        private TMP_InputField Email_Register = default;
        [SerializeField]
        private TMP_InputField Password_Register = default;
        [SerializeField]
        private TMP_InputField Password_Register_c = default;
        [SerializeField]
        private TMP_InputField Email_SingIn = default;
        [SerializeField]
        private TMP_InputField Password_SingIn = default;

        private AuthResult result = AuthResult.None;

        private int navigationIndex = 0;

        private float lastbackInputTime = 0f;

        private bool ShouldGoBack = false;

        private System.Exception ex;

        public override string Id => "Auth-Email";

        public override void OnEnter()
        {
            ShouldGoBack = false;
            result = AuthResult.None;
        }

        public override System.Exception GetException() => ex;

        protected override void OnInit(AuthManager manager)
        {
            RegisterFormButton.onClick.AddListener(() =>
           {
               navigationIndex = 1;
               UpdateLayout(navigationIndex);
           });

            BackButton?.onClick.AddListener(() =>
            {
                if (navigationIndex == 1)
                {
                    navigationIndex = 0;
                    UpdateLayout(navigationIndex);
                }
                else if (navigationIndex == 0)
                {
                    navigationIndex = -1;
                    UpdateLayout(navigationIndex);
                    ShouldGoBack = true;
                }
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
                         Debug.LogError("Sing in with email failed " + task.Exception?.Message);
                         ex = task.Exception;
                         result = AuthResult.Failure;
                         return;
                     }


                     Debug.Log("Sing in with email completed!");
                     result = AuthResult.Completed;

                 });
            });
        }

        private void UpdateLayout(int index)
        {
            RegisterForm.SetActive(index == 1);
            SinginForm.SetActive(index == 0);
            BackButton?.gameObject.SetActive(index >= 0);
        }

        public override AuthResult GetResult()
        {
            return result;
        }

        public void DisplayUI(bool IsAnonymous)
        {
            navigationIndex = IsAnonymous ? 1 : 0;
            UpdateLayout(navigationIndex);
        }

        public void HideUI()
        {
            SinginForm.SetActive(false);
            RegisterForm.SetActive(false);
        }

        public bool GoBack()
        {
            if (navigationIndex == 1 )
            {
                if (Input.GetKey(KeyCode.Escape) && 
                    AuthManager.Instance.IsAuthenticated && AuthManager.Instance.auth.CurrentUser.IsAnonymous)
                {
                    navigationIndex = -1;
                    UpdateLayout(navigationIndex);
                    return true;
                }else
                {
                    if (Input.GetKey(KeyCode.Escape) && Time.time - lastbackInputTime > 0.2f)
                    {
                        navigationIndex = 0;
                        UpdateLayout(navigationIndex);
                        lastbackInputTime = Time.time;
                        return false;
                    }
                }
            }else if (navigationIndex == 0 && !ShouldGoBack)
            {
                if (Input.GetKey(KeyCode.Escape) && Time.time - lastbackInputTime > 0.2f)
                {
                    navigationIndex = -1;
                    UpdateLayout(navigationIndex);
                    lastbackInputTime = Time.time;
                    return true;
                }
            }else if (ShouldGoBack)
            {
                navigationIndex = -1;
                UpdateLayout(navigationIndex);
                return true;
            }
            return false;
        }
    }
}
