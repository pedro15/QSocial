using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    public class AuthController : MonoBehaviour
    {
        #region Singleton 

        private static AuthController m_authController = null;

        public static AuthController Instance
        {
            get
            {
                if (!m_authController) m_authController = FindObjectOfType<AuthController>();

                return m_authController;
            }
        }

        private void RegisterSingleton()
        {
            if (m_authController != null && m_authController != this)
            {
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            m_authController = this;
        }

        #endregion

        private enum AuthUI : int
        {
            None = 0,
            MethodSelection = 1,
            UpgradeUserName = 2,
            CustomUI = 3
        }

        [Header("General Settings")]
        [SerializeField]
        private Button Button_Close = default;
        [SerializeField]
        private GameObject Container_Social = default;
        [SerializeField]
        private GameObject Container_MethodSelection = default;
        [SerializeField]
        private GameObject Container_UpgradeUsername = default;
        [Header("Profile")]
        [SerializeField]
        private Button Button_UpgradeUsername = default;
        [SerializeField]
        private TMP_InputField Input_Username = default;

        [Header("Auth Methods")]
        [SerializeField]
        private EmailAuth emailAuth = default;
        [SerializeField]
        private AnonymousAuth anonymousAuth = default;

        private Dictionary<string, AuthMethod> Methodsdb = new Dictionary<string, AuthMethod>();

        private AuthMethod currentMethod = null;

        private void Start()
        {
            RegisterSingleton();

            Button_UpgradeUsername.onClick.AddListener(() =>
           {
               AuthManager.Instance.UpdateProfile(Input_Username.text, string.Empty, 
                   () =>
                   {
                       // Success
                       Debug.Log("Profile Updated!");
                       RequestLogin();
                   }, (string msg) =>
                   {
                       // Failure
                       Debug.LogError("Profile Failed to update!");
                   });
           });

            Button_Close.onClick.AddListener(() => UpdateUI(AuthUI.None));

            emailAuth.Initialize(this);
            anonymousAuth.Initialize(this);
        }

        public void RequestLogin(bool AnonymousConversion = false)
        {
            if (AuthManager.Instance.IsLoggedIn())
            {
                string username = AuthManager.Instance.CurrentUser.DisplayName;
                if (string.IsNullOrEmpty(username))
                {
                    Button_Close.gameObject.SetActive(false);
                    UpdateUI(AuthUI.UpgradeUserName);
                }
                else
                {
                    if (AnonymousConversion && AuthMethod.IsAnonymousUser)
                    {
                        Button_Close.gameObject.SetActive(true);
                        UpdateUI(AuthUI.MethodSelection);
                    }else
                    {
                        UpdateUI(AuthUI.None);
                    }
                }
            }
            else
            {
                Button_Close.gameObject.SetActive(false);
                UpdateUI(AuthUI.MethodSelection);
            }
        }

        public void RegisterMethod(AuthMethod method, string MethodId)
        {
            if (!Methodsdb.ContainsKey(MethodId))
            {
                Methodsdb.Add(MethodId, method);
            }
        }

        public void ExecuteAuthMethod(string MethodId)
        {
            if (Methodsdb.TryGetValue(MethodId, out currentMethod))
            {
                IAuthCustomUI customUI = currentMethod as IAuthCustomUI;
                if (customUI != null) UpdateUI(AuthUI.CustomUI);

                currentMethod.OnSelect();

                StartCoroutine(IExecuteMethod());
            }
        }

        private void UpdateUI(AuthUI ui)
        {
            Container_Social.SetActive(ui != AuthUI.None);
            Container_MethodSelection.SetActive(ui == AuthUI.MethodSelection);

            if (Container_MethodSelection.activeInHierarchy)
            {
                bool GuestRequest = AuthManager.Instance.IsLoggedIn() && AuthManager.Instance.CurrentUser.IsAnonymous;

                anonymousAuth.SetActive(!GuestRequest);
            }

            Container_UpgradeUsername.SetActive(ui == AuthUI.UpgradeUserName);
        }

        private IEnumerator IExecuteMethod()
        {
            float m_time = 0;
            while (currentMethod != null)
            {
                AuthResult tres = currentMethod.OnExecute();

                bool GotInput = currentMethod.GoBackInput();

                bool InputValid = false;

                if (GotInput && (Time.time - m_time) > 0.35f)
                {
                    IAuthCustomUI customUI = currentMethod as IAuthCustomUI;
                    InputValid = (customUI != null) && customUI.GoBack();
                    m_time = Time.time;
                }

                if (tres == AuthResult.Success || tres == AuthResult.Failure || (tres == AuthResult.None && InputValid))
                {
                    if (tres == AuthResult.Success)
                    {
                       IAuthCustomUI customUI = currentMethod as IAuthCustomUI;
                       customUI?.HideUI();
                    }

                    currentMethod.OnReset();
                    currentMethod = null;

                    if (tres == AuthResult.None)
                        UpdateUI(AuthUI.MethodSelection);
                    else if (tres == AuthResult.Success)
                        RequestLogin();

                    break;
                }

                yield return new WaitForEndOfFrame();
            }
            StopAllCoroutines();
        }
    }
}