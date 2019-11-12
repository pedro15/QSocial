using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
               AuthManager.Instance.UpdateUserName(Input_Username.text,
                   () =>
                   {
                       // Success
                       Debug.Log("Username Updated!");
                       RequestLogin();
                   }, () =>
                   {
                       // Failure
                       Debug.LogError("Username Failed to update!");
                   });
           });

            emailAuth.Initialize(this);
            anonymousAuth.Initialize(this);
        }

        public void RequestLogin()
        {
            if(AuthManager.Instance.IsLoggedIn())
            {
                string username = AuthManager.Instance.CurrentUser.DisplayName;
                if(string.IsNullOrEmpty(username))
                {
                    UpdateUI(AuthUI.UpgradeUserName);
                }else
                {
                    UpdateUI(AuthUI.None);
                }
            }else
            {
                UpdateUI(AuthUI.MethodSelection);
            }
        }

        public void RegisterMethod(AuthMethod method , string MethodId)
        {
            if(!Methodsdb.ContainsKey(MethodId))
            {
                Methodsdb.Add(MethodId, method);
            }
        }

        public void ExecuteAuthMethod(string MethodId)
        {
            if (Methodsdb.TryGetValue(MethodId , out currentMethod))
            {
                IAuthCustomUI customUI = currentMethod as IAuthCustomUI;
                if (customUI != null) UpdateUI(AuthUI.CustomUI);

                currentMethod.Execute();

                StartCoroutine(IExecuteMethod());
            }
        }

        private void UpdateUI(AuthUI ui)
        {
            Container_Social.SetActive(ui != AuthUI.None);
            Container_MethodSelection.SetActive(ui == AuthUI.MethodSelection);
            Container_UpgradeUsername.SetActive(ui == AuthUI.UpgradeUserName);
        }

        private IEnumerator IExecuteMethod()
        {
            while(currentMethod != null)
            {
                AuthResult tres = currentMethod.GetResult();

                Debug.Log($"Current result: { tres } Input Recived: { Input.GetKey(KeyCode.Escape) }");

                if(tres == AuthResult.Success || tres == AuthResult.Failure || 
                    (tres == AuthResult.None && Input.GetKey(KeyCode.Escape)) )
                {
                    IAuthCustomUI customUI = currentMethod as IAuthCustomUI;
                    customUI?.HideUI();

                    currentMethod.OnReset();
                    currentMethod = null;

                    if (tres == AuthResult.None)
                        UpdateUI(AuthUI.MethodSelection);
                    else if (tres == AuthResult.Success)
                        UpdateUI(AuthUI.None);

                    break;
                }

                yield return new WaitForEndOfFrame();
            }
            StopAllCoroutines();
        }
    }
}