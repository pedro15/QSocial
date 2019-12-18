using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Firebase.Auth;
using QSocial.Auth.Modules;
using QSocial.Auth.Methods;

namespace QSocial.Auth
{
    [System.Serializable]
    public class AuthManager : MonoBehaviour
    {
        private static AuthManager _instance = null;

        public static AuthManager Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<AuthManager>();

                return _instance;
            }
        }

        [Header("General Settings")]
        [SerializeField]
        private GameObject BaseLayout = default;
        
        [Header("Modules")]
        [SerializeField]
        private MenuModule SelectionMenu = default;
        [SerializeField]
        private SetupProfileModule SetupProfile = default;
        [Header("Methods")]
        [SerializeField]
        private GuestMethod anonymousMethod = default;
        [SerializeField]
        private EmailMethod emailMethod = default;

        public FirebaseAuth auth { get; private set; }

        public bool IsAuthenticated { get { return auth.CurrentUser != null; } }

        private Dictionary<string, AuthMethod> methodsdb;

        private AuthModule[] Modules = default;

        private bool ModulesRunning = false;
        private bool AuthRunning = false;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            auth = FirebaseAuth.DefaultInstance;
            methodsdb = new Dictionary<string, AuthMethod>();

            SelectionMenu.OnInit(this);
            SetupProfile.OnInit(this);

            Modules = new AuthModule[] { SelectionMenu, SetupProfile };

            emailMethod.Init(this);
            anonymousMethod.Init(this);
        }

        internal void DisplayLayout(bool display)
        {
            BaseLayout.SetActive(display);
        }

        public void RequestLogin(bool Guestvalid = false)
        {
            if (!ModulesRunning)
                StartCoroutine(I_CheckLogin(Guestvalid));
        }

        private IEnumerator I_CheckLogin(bool GuestRequest = false)
        {
            ModulesRunning = true;
            ValidateMethods();
            for (int i = 0; i < Modules.Length; i++)
            {
                AuthModule module = Modules[i];
                if (module.IsValid(GuestRequest, auth.CurrentUser))
                {
                    module.Execute(this);
                    yield return new WaitUntil(() => module.IsCompleted());
                    module.OnFinish(this);
                }
            }
            ModulesRunning = false;
            yield return 0;
        }

        private void ValidateMethods()
        {
            string[] keys = new string[methodsdb.Keys.Count];

            methodsdb.Keys.CopyTo(keys,0);

            for (int i = 0; i < keys.Length; i++)
            {
                if (methodsdb.TryGetValue(keys[i] , out AuthMethod method))
                {
                    if (auth.CurrentUser != null && auth.CurrentUser.IsAnonymous)
                    {
                        if (method.GetType().GetCustomAttribute(typeof(HasAnonymousConversionAttribute)) != null)
                        {
                            method.SetEnabled(true);
                        }else
                        {
                            method.SetEnabled(false);
                        }
                    }else
                    {
                        method.SetEnabled(true);
                    }
                }
            }
        }

        public void RegisterAuthMethod(AuthMethod method)
        {
            if (!methodsdb.ContainsKey(method.Id))
            {
                methodsdb.Add(method.Id, method);
            }
        }

        public void ExecuteAuthMethod(string methodid)
        {
            if (!AuthRunning)
                StartCoroutine(I_ExecuteMethod(methodid));
        }

        private IEnumerator I_ExecuteMethod(string methodid)
        {
            AuthMethod method = null;

            if (methodsdb.TryGetValue(methodid, out method))
            {
                AuthRunning = true;
                IAuthCustomUI customUI = method as IAuthCustomUI;
                customUI?.DisplayUI(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous);
                
                method.OnEnter();

                IAuthCustomNavigation customNavigation = method as IAuthCustomNavigation;

                while (true)
                {
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    AuthResult tres = method.GetResult();

                    if (tres == AuthResult.Completed || (tres == AuthResult.None && goback))
                    {
                        Debug.Log("END!");

                        if (tres == AuthResult.Completed)
                        {
                            method.OnFinish();
                        }

                        customUI?.HideUI();

                        RequestLogin();

                        break;
                    }
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                Debug.LogError("[AuthManager] Method not found: " + methodid);
            }

            AuthRunning = false;
            yield return 0;
        }

    }
}
