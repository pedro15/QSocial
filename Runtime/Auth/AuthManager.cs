﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using Firebase.Auth;
using QSocial.Auth.Modules;
using QSocial.Auth.Methods;

namespace QSocial.Auth
{
    [System.Serializable]
    public class AuthManager : MonoBehaviour
    {
        public delegate void _onAuthCompleted();

        public static event _onAuthCompleted OnAuthCompleted;

        public delegate void _onAuthFail(System.Exception ex);

        public static event _onAuthFail OnAuthFail;


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
        [SerializeField]
        private Button ExitBackground = default;

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
        private bool WasrequestedbyGuest = false;
        private bool ExitRequest = false;
        private float ExitTime = 0f;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            auth = FirebaseAuth.DefaultInstance;
            methodsdb = new Dictionary<string, AuthMethod>();

            ExitBackground.onClick.AddListener(() => RequestExit());

            SelectionMenu.OnInit(this);
            SetupProfile.OnInit(this);

            Modules = new AuthModule[] { SelectionMenu, SetupProfile };

            emailMethod.Init(this);
            anonymousMethod.Init(this);
        }

        private void RequestExit()
        {
            ExitRequest = true;
            ExitTime = Time.time;
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
            bool hasvalidated = false;
            WasrequestedbyGuest = GuestRequest;
            ValidateMethods();
            float time_enter = Time.time;
            for (int i = 0; i < Modules.Length; i++)
            {
                AuthModule module = Modules[i];
                if (module.IsValid(GuestRequest, auth.CurrentUser))
                {
                    hasvalidated = true;
                    module.Execute(this);
                    yield return new WaitUntil(() =>
                    {
                        if (Input.GetKey(KeyCode.Escape) && Time.time - time_enter >= 0.15f)
                        {
                            RequestExit();
                        }
                        
                        float diff = Time.time - ExitTime;
                        if (module.IsInterruptible() && ExitRequest && diff > 0.2f && diff < 0.5f )
                        {
                            if (IsAuthenticated)
                            {
                                return true;
                            }else
                            {
                                ExitRequest = false;
                                return false;
                            }
                        }

                        if (diff >= 0.5f)
                        {
                            ExitTime = 0f;
                        }

                        return module.IsCompleted();
                    });
                    module.OnFinish(this , ExitRequest);
                    ExitRequest = false;

                }

                if (!hasvalidated && i == Modules.Length -1)
                {
                    DisplayLayout(false);
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
                    if (!method.Enabled)
                    {
                        method.SetEnabled(false);
                        continue;
                    }

                    if (auth.CurrentUser != null && auth.CurrentUser.IsAnonymous)
                    {
                        var attr = method.GetType().GetCustomAttribute<HasAnonymousConversionAttribute>();

                        if (attr != null)
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
                DisplayLayout(true);
                AuthRunning = true;
                IAuthCustomUI customUI = method as IAuthCustomUI;
                customUI?.DisplayUI(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous);

                method.OnEnter();

                IAuthCustomNavigation customNavigation = method as IAuthCustomNavigation;
                
                AuthResult tres = AuthResult.None;

            process:
                do
                {
                    tres = method.GetResult();
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);
                    
                    if (customUI != null && tres == AuthResult.None && goback)
                    {
                        customUI?.HideUI();
                        RequestLogin(WasrequestedbyGuest);
                        break;
                    }

                    yield return new WaitForEndOfFrame();

                } while (tres == AuthResult.None || tres == AuthResult.Running);
                
                if (tres == AuthResult.Completed)
                {
                    if (tres == AuthResult.Completed)
                    {
                        method.OnFinish();
                    }

                    customUI?.HideUI();

                    OnAuthCompleted?.Invoke();

                    RequestLogin(WasrequestedbyGuest);
                }
                else if (tres == AuthResult.Failure)
                {
                    OnAuthFail?.Invoke(method.GetException());
                    tres = AuthResult.None;
                    goto process;
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
