using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Firebase;
using Firebase.Auth;
using CommandStateMachine;
using QSocial.Auth.Modules;
using QSocial.Auth.Methods;
using Logger = QSocial.Utility.QSocialLogger;
using System.Reflection;

namespace QSocial.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public delegate void _OnAuthCancelled();

        public static event _OnAuthCancelled OnAuthCancelled;

        public delegate void _OnAuthCompleted();

        public static event _OnAuthCompleted OnAuthCompleted;

        public delegate void _OnProcessBegin();

        public static event _OnProcessBegin OnProcessBegin;

        public delegate void _OnProcessFinish(bool WantsNotifyUser , System.Exception exception);

        public static event _OnProcessFinish OnProcessFinish;

        public delegate void _OnProfileCompleted();

        public static event _OnProfileCompleted OnProfileCompleted;

        private static AuthManager _instance = null;

        public static AuthManager Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<AuthManager>();

                return _instance;
            }
        }

        [System.Flags]
        private enum AuthCheckState : int
        {
            None = 1,
            MainMenu = 2,
            AuthMethod = 4,
            DatabaseCheck = 8,
            SetupProfile = 16
        }

        [System.Flags]
        private enum AuthCheckCommand : int
        {
            GoBack = 1,
            Next = 2,
            Exit = 4
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
        private MenuModule menuModule = default;
        [SerializeField]
        private SetupProfileModule profileModule = default;
        [SerializeField]
        private DatabaseCheckModule databaseCheck = default;
        [Header("Built-In Methods")]
        [SerializeField]
        private EmailMethod emailMethod = default;
        [SerializeField]
        private AnonymousMethod anonymousMethod = default;

        public FirebaseAuth auth { get; private set; }

        private static bool ProcessRunning = false;

        public bool IsAuthenticated { get { return auth.CurrentUser != null; } }

        private bool WasRequestedByGuest = false;
        private bool AuthRunning = false;
        private bool ModuleRunning = false;
        
        private bool ExitRequest = false;
        private float ExitTime = 0f;

        private AuthMethod SelectedMethod = null;


        private Dictionary<string, AuthMethod> Methodsdb = new Dictionary<string, AuthMethod>();
        private CommandFSM<AuthCheckState, AuthCheckCommand> fsm = new CommandFSM<AuthCheckState, AuthCheckCommand>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(_instance);
                return;
            }

            auth = FirebaseAuth.DefaultInstance;

            ExitBackground.onClick.AddListener(() => RequestExit());

            menuModule.OnInit(this);
            profileModule.OnInit(this);
            databaseCheck.OnInit(this);

            emailMethod.Init(this);
            anonymousMethod.Init(this);

            InitFSM();
        }

        private void Update()
        {
            if (SelectedMethod != null && AuthRunning)
            {
                SelectedMethod.OnUpdate();
            }
        }

        // PRIVATE API /-/-/-/-/-/-/-/-/-/-/-/

        private void InitFSM()
        {
            fsm.AddTransition(AuthCheckState.None, AuthCheckCommand.Next, () => AuthCheckState.MainMenu);

            fsm.AddTransition(AuthCheckState.MainMenu, AuthCheckCommand.Next, () => AuthCheckState.AuthMethod);

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.Next, () => AuthCheckState.DatabaseCheck);

            fsm.AddTransition(AuthCheckState.DatabaseCheck, AuthCheckCommand.Next, () => AuthCheckState.SetupProfile);

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.GoBack, () => AuthCheckState.MainMenu);

            fsm.AddTransition(AuthCheckState.MainMenu, AuthCheckCommand.GoBack, () => AuthCheckState.None);

            fsm.AddTransition(AuthCheckState.SetupProfile, AuthCheckCommand.Next, () => AuthCheckState.None);

           // fsm.AddTransition(AuthCheckState.DatabaseCheck, AuthCheckCommand.Exit, () => AuthCheckState.None);

            fsm.OnStateChanged = (AuthCheckState state) =>
           {
               AuthModule module = null;

               Logger.Log($"<color=blue>State changed:: { state } </color>", this, true);

               DisplayLayout(true);

                switch(state)
               {
                   case AuthCheckState.MainMenu:

                       module = menuModule;

                       break;

                   case AuthCheckState.AuthMethod:

                       if (SelectedMethod == null )
                       {
                           fsm.MoveNext(AuthCheckCommand.Next);
                       }else if (!AuthRunning)
                       {
                           StartCoroutine(I_ExecuteMethod());
                       }

                       break;

                   case AuthCheckState.DatabaseCheck:

                       if (auth.CurrentUser != null)
                       {
                            module = databaseCheck;
                       }else
                       {
                           fsm.MoveNext(AuthCheckCommand.Exit);
                       }

                       break;

                   case AuthCheckState.SetupProfile:

                       module = profileModule;

                       break;

                   case AuthCheckState.None:

                       DisplayLayout(false);
                       ModuleRunning = false;

                       break;
               }

               if (state != AuthCheckState.None && module != null )
               {
                   StartCoroutine(I_RunModule(module));
               }
           };
        }

        private void DisplayLayout(bool active)
        {
            BaseLayout.SetActive(active);
        }

        private void RequestExit()
        {
            if (ModuleRunning)
            {
                ExitRequest = true;
                ExitTime = Time.time;
            }
        }

        private void ValidateMethods()
        {
            string[] keys = new string[Methodsdb.Keys.Count];

            Methodsdb.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
            {
                if (Methodsdb.TryGetValue(keys[i], out AuthMethod method))
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

        // INTERNAL API  /-/-/-/-/-/-/-/-/-/-/-/-/-/

        internal static void BeginProcess()
        {

            if (OnProcessBegin != null && !ProcessRunning)
            {
                OnProcessBegin.Invoke();
                ProcessRunning = true;
            }
        }

        internal static void FinishProcess(bool NotifyUser = false, System.Exception exception = null)
        {
            if (OnProcessFinish != null)
            {
                OnProcessFinish.Invoke(NotifyUser, exception);
                ProcessRunning = false;
            }
        }

        internal static void CompleteProfile()
        {
            if (OnProfileCompleted != null) OnProfileCompleted.Invoke();
        }

        // PUBLIC API /-/-/-/-/-/-/-/-/-/-/-/-/-/

        public void RequestLogIn(bool GuestRequest = false)
        {
            if (AuthRunning || ModuleRunning) return;

            ValidateMethods();
            WasRequestedByGuest = GuestRequest;
            fsm.MoveNext(AuthCheckCommand.Next);
        }

        public void RegisterAuthMethod(AuthMethod amethod)
        {
            if (!Methodsdb.ContainsKey(amethod.Id))
            {
                Methodsdb.Add(amethod.Id, amethod);
            }
        }

        public void ExecuteAuthMethod(string Id)
        {
            if (AuthRunning) return;

            if (!Methodsdb.TryGetValue(Id , out SelectedMethod))
            {
                Logger.LogError("Auth Method not found for id: " + Id, this);
                return;
            }
        }

        public static System.Exception GetFirebaseException(System.Exception ex)
        {
            if (ex != null )
            {
                System.Exception a_ex =  ex.InnerException.GetBaseException();
                if (a_ex.GetType() == typeof(FirebaseException))
                {
                    return a_ex as FirebaseException;
                }
                return a_ex;
            }
            return null;
        }

        // Coroutines /-/-/-/-/-/-/-/-/-/-/-/-/

        // RUN AUTH MODULE
        private IEnumerator I_RunModule(AuthModule module)
        {

            ModuleRunning = true;
            int retrys = 0;
            bool module_retrying = false;

            module.OnEnter();

            IAsyncModule asyncModule = module as IAsyncModule;
            if (asyncModule != null )
            {
                ProcessResult ares = ProcessResult.None;
                do
                {
                    if (module_retrying)
                    {
                        module.OnEnter();
                        module_retrying = false;
                    }

                    ares = asyncModule.AsyncResult();

                    if (ares == ProcessResult.Failure)
                    {
                        Logger.LogWarning("Got Error during process of AsyncModule " + module.GetException(), this);

                        if (retrys < MaximunErrorRetrys)
                        {
                            yield return new WaitForSeconds(0.5f);
                            Logger.LogWarning("Retrying... " + retrys, this);
                            retrys++;
                            module_retrying = true;
                        }
                    }

                    yield return new WaitForEndOfFrame();
                } while (ares != ProcessResult.Completed);

                yield return new WaitUntil(() => ares == ProcessResult.Completed || retrys >= MaximunErrorRetrys );
            }

            retrys = 0;

            if (module.IsValid(WasRequestedByGuest, auth.CurrentUser))
            {
                module.Execute(this);

                ProcessResult tres = ProcessResult.None;
                float time_enter = Time.time;
                
                do
                {
                    if (module_retrying)
                    {
                        module.OnEnter();
                        module.Execute(this);
                        time_enter = Time.time;

                        module_retrying = false;
                    }

                    tres = module.GetResult();

                    Debug.Log("MODULE " + module.GetType().Name + " RESULT: " + tres);

                    if (Input.GetKey(KeyCode.Escape) && Time.time - time_enter >= 0.15f)
                    {
                        RequestExit();
                    }

                    //Interrumpt the module when you're about to upgrade the acocount 
                    float diff = Time.time - ExitTime;
                    if (IsAuthenticated && (module.IsInterruptible() && ExitRequest && diff > 0.2f && diff < 0.5f))
                    {
                        fsm.MoveNext(AuthCheckCommand.GoBack);
                        yield break;
                    }

                    if (diff >= 0.55f)
                    {
                        ExitTime = 0;
                    }

                    if (tres == ProcessResult.Failure)
                    {
                        Logger.LogWarning("Got Error during process of module " + module.GetException(), this);

                        if (retrys < MaximunErrorRetrys)
                        {
                            yield return new WaitForSeconds(0.5f);
                            Logger.LogWarning("Retrying... " + retrys, this);
                            retrys++;
                            module_retrying = true;
                        }
                    }

                    yield return new WaitForEndOfFrame();
                
                } while (tres != ProcessResult.Completed);
                
                if (tres == ProcessResult.Completed)
                {
                    Logger.Log("Module completed: " + module.GetType().Name, this, true);
                    module.OnFinish(this, ExitRequest);
                    fsm.MoveNext(AuthCheckCommand.Next);
                    ExitRequest = false;
                }
            }
            else
            {
                fsm.MoveNext(AuthCheckCommand.Next);
                yield break;
            }

            ModuleRunning = false;
            yield return 0;
        }

        // RUN AUTH METHOD ----
        private IEnumerator I_ExecuteMethod()
        {
            if (SelectedMethod != null )
            {
                bool Method_Retry = false;
                AuthRunning = true;
                DisplayLayout(true);
                IAuthCustomUI customUI = SelectedMethod as IAuthCustomUI;

                if (customUI != null) customUI.DisplayUI(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous);

                IAuthCustomNavigation customNavigation = SelectedMethod as IAuthCustomNavigation;

                SelectedMethod.OnEnter();

                ProcessResult tres = ProcessResult.None;
                do
                {
                    if (Method_Retry)
                    {
                        SelectedMethod.OnEnter();
                        Method_Retry = false;
                    }

                    tres = SelectedMethod.GetResult();
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    if (customUI != null && tres == ProcessResult.None && goback)
                    {
                        customUI.HideUI();

                        if (OnAuthCancelled != null) OnAuthCancelled.Invoke();

                        fsm.MoveNext(AuthCheckCommand.GoBack);

                        break;
                    }

                    if (tres == ProcessResult.Failure)
                    {
                        Logger.LogWarning("Got Error during process of Method " + SelectedMethod.GetException(), this);
                        yield return new WaitForSeconds(0.5f);
                        Logger.LogWarning("Retrying..." , this);
                        Method_Retry = true;
                    }

                    yield return new WaitForEndOfFrame();
                } while (tres != ProcessResult.Completed );

                if (tres == ProcessResult.Completed)
                {
                    if (customUI != null)
                        customUI.HideUI();

                    SelectedMethod.OnFinish();

                    fsm.MoveNext(AuthCheckCommand.Next);

                    if (OnAuthCompleted != null)
                        OnAuthCompleted.Invoke();
                }
            }

            AuthRunning = false;
            SelectedMethod = null;
            Logger.Log("Auth Execution Finished", this , true);
            yield return 0;
        }
    }
}