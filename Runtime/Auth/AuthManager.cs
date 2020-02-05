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

        public delegate void _OnProcessFinish();

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

        private void InitFSM()
        {
            fsm.AddTransition(AuthCheckState.None, AuthCheckCommand.Next, () => AuthCheckState.MainMenu);

            fsm.AddTransition(AuthCheckState.MainMenu, AuthCheckCommand.Next, () => AuthCheckState.AuthMethod);

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.Next, () => AuthCheckState.DatabaseCheck);

            fsm.AddTransition(AuthCheckState.DatabaseCheck, AuthCheckCommand.Next, () => AuthCheckState.SetupProfile);

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.GoBack, () => AuthCheckState.MainMenu);

            fsm.AddTransition(AuthCheckState.MainMenu, AuthCheckCommand.GoBack, () => AuthCheckState.None);

            fsm.AddTransition(AuthCheckState.SetupProfile, AuthCheckCommand.Next, () => AuthCheckState.None);

            fsm.AddTransition(AuthCheckState.DatabaseCheck, AuthCheckCommand.Exit, () => AuthCheckState.None);

            fsm.OnStateChanged = (AuthCheckState state) =>
           {

               Logger.Log("State changed " + state , this, true);

               AuthModule module = null;

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

                       break;
               }

               if (state != AuthCheckState.None && module != null)
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

        internal static void BeginProcess()
        {
            if (OnProcessBegin != null)
                OnProcessBegin.Invoke();
        }

        internal static void FinishProcess()
        {
            if (OnProcessFinish != null)
                OnProcessFinish.Invoke();
        }

        internal static void CompleteProfile()
        {
            if (OnProfileCompleted != null) OnProfileCompleted.Invoke();
        }

        public void RequestLogIn(bool GuestRequest = false , bool Checkdb = false)
        {
            if (AuthRunning || ModuleRunning) return;

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

        private IEnumerator I_RunModule(AuthModule module)
        {
            ModuleRunning = true;
            int retrys = 0;

            Logger.Log("Module ::: " + module, this, true);
            
            startrunning:
            module.OnEnter();

            IAsyncModule asyncModule = module as IAsyncModule;
            if (asyncModule != null )
            {
                ProcessResult ares = ProcessResult.None;
                do
                {
                    ares = asyncModule.AsyncResult();

                    yield return new WaitForEndOfFrame();

                } while (ares == ProcessResult.None || ares == ProcessResult.Running);

                if (ares == ProcessResult.Failure)
                {
                    Logger.LogWarning("Got Error during process of AsyncModule " + module.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        Logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        yield return new WaitForSeconds(0.5f);
                        goto startrunning;
                    }
                }
            }

            retrys = 0;

            if (module.IsValid(WasRequestedByGuest , auth.CurrentUser))
            {
                Debug.Log("Module valid");

                runmodule:
                module.Execute(this);

                ProcessResult tres = ProcessResult.None;
                float time_enter = Time.time;

                do
                {
                    tres = module.GetResult();

                    Debug.Log(module.GetType().Name + " :: " + tres);

                    if (Input.GetKey(KeyCode.Escape) && Time.time - time_enter >= 0.15f)
                    {
                        RequestExit();
                    }

                    //Interrumpt the module when you're about to upgrade the acocount 
                    float diff = Time.time - ExitTime;
                    if (IsAuthenticated && (module.IsInterruptible() && ExitRequest && diff > 0.2f && diff < 0.5f))
                    {

                        Debug.Log("Move next! -- go back");
                        fsm.MoveNext(AuthCheckCommand.GoBack);
                        ModuleRunning = false;
                        yield break;
                    }

                    if (diff >= 0.55f)
                    {
                        ExitTime = 0;
                    }

                    yield return new WaitForEndOfFrame();
                } while (tres == ProcessResult.None || tres == ProcessResult.Running);

                if (tres == ProcessResult.Failure)
                {
                    Logger.LogWarning("Got Error during process of Module " + module.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        Logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        goto runmodule;
                    }
                }

                if (tres == ProcessResult.Completed)
                {
                    Debug.Log("<color=blue>Module completed!</color>");
                    module.OnFinish(this,ExitRequest);
                    fsm.MoveNext(AuthCheckCommand.Next);
                    ExitRequest = false;
                }

            }else
            {
                fsm.MoveNext(AuthCheckCommand.Next);
                ModuleRunning = false;
                yield break;
            }

            ModuleRunning = false;
            yield return 0;
        }

        private IEnumerator I_ExecuteMethod()
        {
            if (SelectedMethod != null )
            {
                int retrys = 0;
                AuthRunning = true;
                DisplayLayout(true);
                IAuthCustomUI customUI = SelectedMethod as IAuthCustomUI;

                if (customUI != null) customUI.DisplayUI(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous);

                IAuthCustomNavigation customNavigation = SelectedMethod as IAuthCustomNavigation;

            Execution:
                SelectedMethod.OnEnter();

                ProcessResult tres = ProcessResult.None;
                do
                {
                    tres = SelectedMethod.GetResult();
                    bool goback = (customNavigation != null) ? customNavigation.GoBack() : Input.GetKey(KeyCode.Escape);

                    if (customUI != null && tres == ProcessResult.None && goback)
                    {
                        customUI.HideUI();

                        if (OnAuthCancelled != null) OnAuthCancelled.Invoke();

                        fsm.MoveNext(AuthCheckCommand.GoBack);

                        break;
                    }

                    Logger.Log("Auth Running: " + tres , this , true);
                    yield return new WaitForEndOfFrame();
                } while (tres == ProcessResult.None || tres == ProcessResult.Running);

                if (tres == ProcessResult.Failure)
                {
                    Logger.LogWarning("Got Error during process of Module " + SelectedMethod.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        Logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        goto Execution;
                    }

                }else if (tres == ProcessResult.Completed)
                {
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