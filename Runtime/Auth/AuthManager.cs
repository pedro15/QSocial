using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
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

        private AuthController controller;

        private Dictionary<string, AuthMethod> methodsdb;

        private AuthModule[] Modules;

        private bool ModulesRunning = false;
        private bool AuthRunning = false;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            controller = AuthController.Instance;
            methodsdb = new Dictionary<string, AuthMethod>();
        }

        public void RequestLogin(bool Guestvalid = false)
        {
            if (!ModulesRunning)
                StartCoroutine(I_CheckLogin(Guestvalid));
        }

        private IEnumerator I_CheckLogin(bool GuestRequest = false)
        {
            ModulesRunning = true;
            for (int i = 0; i < Modules.Length; i++)
            {
                AuthModule module = Modules[i];

                if (module.IsValid(GuestRequest, controller.CurrentUser))
                {
                    module.Execute(this);
                    yield return new WaitUntil(() => module.IsCompleted());
                    module.OnFinish(this);
                }
            }
            ModulesRunning = false;
            yield return 0;
        }

        public void RegisterAuthMethod(AuthMethod method)
        {
            if (!methodsdb.ContainsKey(method.Id))
            {
                methodsdb.Add(method.Id , method);
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

            if (methodsdb.TryGetValue(methodid , out method))
            {
                IAuthCustomUI customUI = method as IAuthCustomUI;
                customUI?.DisplayUI();

                IAuthCustomNavigation customNavigation = method as IAuthCustomNavigation;

                while(true)
                {
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    AuthResult tres = method.GetResult();

                    if (tres == AuthResult.Completed || (tres == AuthResult.None && goback) )
                    {
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
            }else
            {
                Debug.LogError("[AuthManager] Method not found: " + methodid);
            }

            yield return 0;
        }

    }
}
