using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using Firebase;
using Firebase.Auth;
using QSocial.Auth.Methods;
using QSocial.Auth.Modules;
using QSocial.Data;
using QSocial.Data.Users;

namespace QSocial.Auth
{
    [System.Serializable]
    public class AuthManager : MonoBehaviour
    {
        public delegate void _onAuthCompleted();

        public static event _onAuthCompleted OnAuthCompleted;

        public delegate void _onAuthCancelled();

        public static event _onAuthCancelled OnAuthCancelled;

        public delegate void _onAuthFail(System.Exception ex);

        public static event _onAuthFail OnAuthFail;

        public delegate void _onProfileCompleted();

        public static event _onProfileCompleted OnProfileCompleted;

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
        [SerializeField]
        private int MaximunErrorRetrys = 5;

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

        private Dictionary<string, AuthMethod> methodsdb = new Dictionary<string, AuthMethod>();

        private Dictionary<string, AuthModule> modulesdb = new Dictionary<string, AuthModule>();

        private AuthMethod selectedMethod = null;

        private AuthModule[] AuthModules = default;

        public bool AuthRunning { get; private set; } = false;

        private bool ModulesRunning = false;

        private bool WasrequestedbyGuest = false;
        private bool IsGuestRequest = false;
        private bool ExitRequest = false;
        private bool WantsCheckLogin = false;
        private float ExitTime = 0f;

        internal void CompleteProfile()
        {
            Debug.Log("Profile Completed!!!");
            OnProfileCompleted?.Invoke();
        }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            auth = FirebaseAuth.DefaultInstance;
            
            ExitBackground.onClick.AddListener(() => RequestExit());

            emailMethod.Init(this);
            anonymousMethod.Init(this);

            SelectionMenu.OnInit(this);
            SetupProfile.OnInit(this);

            AuthModules = new AuthModule[] { SelectionMenu , SetupProfile };
        }

        private void Update()
        {
            if (AuthRunning && selectedMethod != null)
            {
                selectedMethod.OnUpdate();
            }

            if (WantsCheckLogin)
            {
                if (!ModulesRunning)
                {
                    StartCoroutine(I_CheckLogin(IsGuestRequest));
                    WantsCheckLogin = false;
                }
            }
        }

        private void RequestExit()
        {
            if (ModulesRunning)
            {
                ExitRequest = true;
                ExitTime = Time.time;
            }
        }

        internal void DisplayLayout(bool display)
        {
            BaseLayout.SetActive(display);
        }

        public void RequestLogin(bool Guestvalid = false)
        {
            IsGuestRequest = Guestvalid;
            WantsCheckLogin = true;
        }

        private IEnumerator I_CheckLogin(bool GuestRequest = false)
        {
            ModulesRunning = true;
            bool hasvalidated = false;
            WasrequestedbyGuest = GuestRequest;
            ValidateMethods();
            float time_enter = Time.time;
            for (int i = 0; i < AuthModules.Length; i++)
            {
                Debug.Log("[AuthManager] Module (" + i + ") -> " + AuthModules[i].GetType());
                AuthModule module = AuthModules[i];
                int retrys = 0;
                
                moduleprocess:
                module.OnEnter();

                IAsyncModule asyncModule = module as IAsyncModule;

                if (asyncModule != null)
                {
                    yield return new WaitUntil(() => !asyncModule.IsLoading(GuestRequest , auth.CurrentUser));

                    System.Exception a_exception = asyncModule.GetException();

                    if (a_exception != null)
                    {
                        Debug.LogWarning("[AuthManager] Execution of AsyncModule got error " + a_exception);
                        retrys++;
                        if(retrys < MaximunErrorRetrys)
                        {
                            Debug.LogWarning("[AuthManager] Retrying... " + retrys);
                            yield return new WaitForSeconds(0.5f);
                            goto moduleprocess;
                        }else
                        {
                            Debug.LogError("Maximun retrys reached, continue to next module");
                            continue;
                        }
                    }
                }

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
                        if (module.IsInterruptible() && ExitRequest && diff > 0.2f && diff < 0.5f)
                        {
                            if (IsAuthenticated)
                            {
                                return true;
                            }
                            else
                            {
                                ExitRequest = false;
                                return false;
                            }
                        }

                        if (diff >= 0.55f)
                        {
                            ExitTime = 0;
                        }

                        return module.IsCompleted();
                    });
                    module.OnFinish(this, ExitRequest);
                    ExitRequest = false;
                }

                if (!hasvalidated && i == AuthModules.Length - 1)
                {
                    DisplayLayout(false);
                }

            }

            Debug.Log("Check login finished");
            ModulesRunning = false;
            yield return 0;
        }

        private void ValidateMethods()
        {
            string[] keys = new string[methodsdb.Keys.Count];

            methodsdb.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
            {
                if (methodsdb.TryGetValue(keys[i], out AuthMethod method))
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
                        }
                        else
                        {
                            method.SetEnabled(false);
                        }
                    }
                    else
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
            if (methodsdb.TryGetValue(methodid, out selectedMethod))
            {
                DisplayLayout(true);
                AuthRunning = true;
                IAuthCustomUI customUI = selectedMethod as IAuthCustomUI;
                customUI?.DisplayUI(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous);

                IAuthCustomNavigation customNavigation = selectedMethod as IAuthCustomNavigation;
                AuthResult tres = AuthResult.None;
                
            process:
                selectedMethod.OnEnter();

                do
                {
                    tres = selectedMethod.GetResult();
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    if (customUI != null && tres == AuthResult.None && goback)
                    {
                        customUI?.HideUI();
                        OnAuthCancelled?.Invoke();
                        RequestLogin(WasrequestedbyGuest);
                        break;
                    }

                    yield return new WaitForEndOfFrame();

                } while (tres == AuthResult.None || tres == AuthResult.Running);

                if (tres == AuthResult.Completed)
                {
                    if (tres == AuthResult.Completed)
                    {
                        selectedMethod.OnFinish();
                    }

                    int retrys = 0;

                checkusername:
                    bool failed = false;
                    bool checkusercompleted = false;

                    Debug.Log("USER ID: " + selectedMethod.ResultUserId);

                   QDataManager.Instance.UserExists(selectedMethod.ResultUserId, (bool exists) =>
                   {
                       Debug.Log("[AuthManager] User check complete, user exists: " + exists);
                       if (!exists)
                       {
                           Debug.Log("[AuthManager] user does not exists, uploading to database...");
                           QDataManager.Instance.RegisterPlayerToDatabase(new UserPlayer(
                               selectedMethod.ResultUserId, new string[0]) , () =>
                               {
                                   Debug.Log("[Auth Manager] user upload to database completed");
                                   checkusercompleted = true;  
                               } , (System.Exception ex) =>
                               {
                                   Debug.LogError("[AuthManager] Failed to register user to database " + ex);
                                   failed = true;
                               });
                       }else
                       {
                           Debug.Log("[AuthManager] user exists, continue without changes");
                           checkusercompleted = true;
                       }
                   }, (System.Exception ex) =>
                   {
                       Debug.LogError("[AuthManager] Failed to check username " + ex);
                       failed = true;
                   });

                    if (failed && retrys < MaximunErrorRetrys)
                    {
                        retrys++;
                        Debug.LogError("An error ocurred, retrying... " + retrys);
                        yield return new WaitForSeconds(1f);
                        goto checkusername;
                    }

                    yield return new WaitUntil(() => checkusercompleted);

                    customUI?.HideUI();

                    OnAuthCompleted?.Invoke();

                    RequestLogin(WasrequestedbyGuest);
                }
                else if (tres == AuthResult.Failure)
                {
                    OnAuthFail?.Invoke(selectedMethod.GetException());
                    Debug.LogError("[AuthManager] Auth Fail! " + selectedMethod.GetException());
                    selectedMethod.OnFinish();
                    tres = AuthResult.None;
                    goto process;
                }
            }
            else
            {
                Debug.LogError("[AuthManager] Method not found: " + methodid);
            }

            Debug.Log("Auth execution Finished!");
            AuthRunning = false;
            selectedMethod = null;
            yield return 0;
        }

        public static FirebaseException GetFirebaseException(System.Exception ex)
        {
            return ex?.InnerException.GetBaseException() as FirebaseException;
        }
    }
}
