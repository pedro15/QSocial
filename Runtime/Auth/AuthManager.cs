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
    [System.Serializable,DefaultExecutionOrder(-50)]
    public class AuthManager : MonoBehaviour
    {
        
        public delegate void _OnProcessBegin();

        public static event _OnProcessBegin OnProcessBegin;

        public delegate void _OnProcessFinish(System.Exception ex);

        public static event _OnProcessFinish OnProcessFinish;

        public delegate void _onAuthBegin();

        public static event _onAuthBegin OnAuthBegin;

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

        private AuthMethod selectedMethod = null;

        private AuthModule[] AuthModules = default;

        public bool AuthRunning { get; private set; } = false;

        internal bool UserChecking { get; private set; } = false;

        private bool ModulesRunning = false;
        private bool dbcheck = false;

        private bool KeepRequest = false;
        private bool WasrequestedbyGuest = false;
        private bool IsGuestRequest = false;
        private bool ExitRequest = false;
        private bool WantsCheckLogin = false;
        private bool processStarted = false;

        private float ExitTime = 0f;

        // UNITY EVENTS ====

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

                if (!KeepRequest) WantsCheckLogin = false;
            }

            Debug.Log(WantsCheckLogin);
        }

        // INTERNAL API ========

        internal void CompleteProfile()
        {
            OnProfileCompleted?.Invoke();
        }

        internal void DisplayLayout(bool display)
        {
            BaseLayout.SetActive(display);
        }

        // PRIVATE API ========

        private void StartProcess()
        {
            if (processStarted) return;

            if (OnProcessBegin != null)
                OnProcessBegin.Invoke();

            processStarted = true;
        }

        private void FinishProcess(System.Exception ex)
        {
            if (OnProcessFinish != null)
                OnProcessFinish.Invoke(ex);

            processStarted = false;
        }
        
        private void SetDefaultAuthModules()
        {
            AuthModules = new AuthModule[] { SelectionMenu, SetupProfile };
        }

        private void RequestExit()
        {
            if (ModulesRunning)
            {
                ExitRequest = true;
                ExitTime = Time.time;
            }
        }

        public void RequestLogin(bool Guestvalid = false, bool checkuserindb = true, bool KeepRequestUntilAviable = false)
        {
            IsGuestRequest = Guestvalid;
            dbcheck = checkuserindb;
            KeepRequest = KeepRequestUntilAviable;
            WantsCheckLogin = true;
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

        private void CheckUserDB(string uid)
        {
            if (!UserChecking)
                StartCoroutine(I_CheckUserInDatabase(uid));
        }

        // PUBLIC API ========

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

        public static FirebaseException GetFirebaseException(System.Exception ex)
        {
            return ex?.InnerException.GetBaseException() as FirebaseException;
        }

        // COROUTINES ========

        private IEnumerator I_CheckUserInDatabase(string uid)
        {
            int retrys = 0;
        checkuserdb:

            UserChecking = true;
            int state = -1;

            // State = 0 --> Failed  | State = 1 --> Completed

            System.Exception eex = null;
            Debug.Log("USER ID: " + uid);
            
            QDataManager.Instance.UserExists(uid, (bool exists) =>
            {
                Debug.Log("[AuthManager] User check complete, user exists: " + exists);

                if (!exists)
                {
                    Debug.Log("[AuthManager] Uploading user to database");
                    QDataManager.Instance.RegisterPlayerToDatabase(new UserPlayer(uid, new string[0]), () =>
                    {
                        Debug.Log("[AuthManager] Upload user to database finished");
                        state = 1;
                    }, (System.Exception ex) =>
                    {
                        Debug.LogWarning("[AuthManager] Upload user to database got error " + ex);
                        state = 0;
                    });
                }else
                {
                    Debug.Log("[AuthManager] user already in database, continue normally!");
                    state = 1;
                }
            }, (System.Exception ex) =>
            {
                Debug.LogError("[AuthManager] Failed to check username " + ex);
                state = 0;
                eex = ex;
            });

            yield return new WaitUntil(() => state == 0 || state == 1);

            if (state == 0 && retrys < MaximunErrorRetrys)
            {
                retrys++;
                Debug.LogError("An error ocurred, retrying... " + retrys);
                yield return new WaitForSeconds(0.5f);
                goto checkuserdb;
            }

            UserChecking = false;
        }

        // Check User Login
        private IEnumerator I_CheckLogin(bool GuestRequest = false)
        {
            SetDefaultAuthModules();

            ModulesRunning = true;
            bool hasvalidated = false;
            WasrequestedbyGuest = GuestRequest;
            ValidateMethods();
            float time_enter = Time.time;

            if (auth.CurrentUser != null && dbcheck)
            {
                StartProcess();

                CheckUserDB(auth.CurrentUser.UserId);

                Debug.Log("[AuthManager] Check user on database (login)");

                yield return new WaitUntil(() => !UserChecking);

                FinishProcess(null);
            }

            mainmenu:
            bool gomainmenu = false;
            for (int i = 0; i < AuthModules.Length; i++)
            {
                AuthModule module = AuthModules[i];
                int retrys = 0;

            moduleprocess:
                module.OnEnter();

                IAsyncModule asyncModule = module as IAsyncModule;

                if (asyncModule != null)
                {
                    //StartProcess();
                    
                    yield return new WaitUntil(() => !asyncModule.IsLoading(GuestRequest, auth.CurrentUser));

                    System.Exception a_exception = asyncModule.GetException();

                    //FinishProcess((retrys < MaximunErrorRetrys) ? null : a_exception);
                    
                    if (a_exception != null)
                    {
                        Debug.LogWarning("[AuthManager] Execution of AsyncModule got error " + a_exception);

                        retrys++;
                        if (retrys < MaximunErrorRetrys)
                        {
                            Debug.LogWarning("[AuthManager] Retrying... " + retrys);
                            yield return new WaitForSeconds(0.5f);
                            goto moduleprocess;
                        }
                        else
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
                                gomainmenu = true;
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

                if (i == AuthModules.Length - 1)
                {
                    if (!hasvalidated)
                        DisplayLayout(false);
                }

            }

            if (gomainmenu)
                goto mainmenu;

            Debug.Log("Check login finished");
            ModulesRunning = false;
            yield return 0;
        }

        // Execute auth method
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
                if (OnAuthBegin != null) OnAuthBegin.Invoke();
                bool processstarted = false;

                do
                {
                    tres = selectedMethod.GetResult();
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    if (customUI != null && tres == AuthResult.None && goback)
                    {
                        customUI?.HideUI();
                        OnAuthCancelled?.Invoke();
                        RequestLogin(WasrequestedbyGuest , true);
                        break;
                    }

                    Debug.Log(tres);

                    if (tres == AuthResult.Running && !processstarted)
                    {
                       // StartProcess();
                        processstarted = true;
                    }

                    yield return new WaitForEndOfFrame();

                } while (tres == AuthResult.None || tres == AuthResult.Running);


                if (tres == AuthResult.Completed)
                {
                    if (tres == AuthResult.Completed)
                    {
                        selectedMethod.OnFinish();
                    }

                    Debug.Log("[AuthManager] Check user in database");


                    CheckUserDB(selectedMethod.ResultUserId);

                    yield return new WaitUntil(() => !UserChecking);



                    customUI?.HideUI();

                    OnAuthCompleted?.Invoke();

                    RequestLogin(WasrequestedbyGuest, false , true);
                }
                else if (tres == AuthResult.Failure)
                {
                    OnAuthFail?.Invoke(selectedMethod.GetException());
                    Debug.LogError("[AuthManager] Auth Fail! " + selectedMethod.GetException());
                    FinishProcess(selectedMethod.GetException());
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
            FinishProcess(null);
            AuthRunning = false;
            selectedMethod = null;
            yield return 0;
        }
    }
}
