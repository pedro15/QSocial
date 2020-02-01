using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Firebase.Auth;
using CommandStateMachine;
using QSocial.Data;
using QSocial.Data.Users;
using QSocial.Utility;
using QSocial.Auth.Modules;

namespace QSocial.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public delegate void _OnAuthCancelled();

        public static event _OnAuthCancelled OnAuthCancelled;

        public delegate void _OnAuthCompleted();

        public static event _OnAuthCompleted OnAuthCompleted;



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
            SetupProfile = 8
        }

        [System.Flags]
        private enum AuthCheckCommand : int
        {
            GoBack = 1,
            Next = 2
        }

        private QSocialLogger logger = default;

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

        public FirebaseAuth auth { get; private set; }
        public bool IsAuthenticated { get { return auth.CurrentUser != null; } }

        private bool WasRequestedByGuest = false;
        private bool AuthRunning = false;
        private bool ModuleRunning = false;
        private bool UserChecking = false;

        private bool ExitRequest = false;
        private bool WantsCheckdb = false;
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

            logger = QSocialLogger.Instance;

            ExitBackground.onClick.AddListener(() => RequestExit());

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

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.Next, () => AuthCheckState.SetupProfile);

            fsm.AddTransition(AuthCheckState.AuthMethod, AuthCheckCommand.GoBack, () => AuthCheckState.MainMenu);

            fsm.AddTransition(AuthCheckState.MainMenu, AuthCheckCommand.GoBack, () => AuthCheckState.None);

            fsm.AddTransition(AuthCheckState.SetupProfile, AuthCheckCommand.Next, () => AuthCheckState.None);

            fsm.OnStateChanged = (AuthCheckState state) =>
           {
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

                   case AuthCheckState.SetupProfile:

                       module = profileModule;

                       break;

                   case AuthCheckState.None:

                       DisplayLayout(false);

                       break;
               }

               if (state != AuthCheckState.None && module != null)
               {
                   StartCoroutine(I_RunModule(module, WantsCheckdb));
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

        private void CheckUserInDatabase(string userid)
        {
            if (!UserChecking)
                StartCoroutine(I_CheckUserDatabase(userid));
        }

        public void RequestLogIn(bool GuestRequest , bool Checkdb)
        {
            WasRequestedByGuest = GuestRequest;
            WantsCheckdb = Checkdb;
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
                logger.LogError("Auth Method not found for id: " + Id, this);
                return;
            }

            fsm.MoveNext(AuthCheckCommand.Next);
        }

        private IEnumerator I_CheckUserDatabase(string uid)
        {
            int retrys = 0;
            bool shouldretry = false;
            UserChecking = true;
            WantsCheckdb = false;

            ProcessResult tres = ProcessResult.None;

            while(true)
            {
                if (tres == ProcessResult.None || (tres == ProcessResult.Failure && shouldretry) )
                {
                    QDataManager.Instance.UserExists(uid, (bool result) =>
                    {
                        retrys = 0;

                        if (!result)
                        {
                            logger.Log("User not found in database, uploading new registry", this, true);

                            QDataManager.Instance.RegisterPlayerToDatabase(new UserPlayer(uid, new string[0]),
                                () =>
                                {
                                    logger.Log("Upload user to database completed!", this, true);
                                    tres = ProcessResult.Completed;
                                }, (System.Exception ex) =>
                               {
                                   logger.LogWarning("Got Error during process of upload user on database " + ex, this);

                                   if (retrys < MaximunErrorRetrys)
                                   {
                                       logger.LogWarning("Retrying upload... " + retrys, this);
                                       retrys++;
                                       shouldretry = true;
                                   }
                                   else
                                   {
                                       logger.LogError("Maximun error retrys reached", this);
                                   }
                                   tres = ProcessResult.Failure;
                               });
                        }else
                        {
                            logger.Log("User already found in database, continue without changes" , this , true);
                            tres = ProcessResult.Completed;
                        }
                    }, (System.Exception ex) =>
                    {
                        logger.LogWarning("Got Error during process of check user on database " + ex, this);

                        if (retrys < MaximunErrorRetrys)
                        {
                            logger.LogWarning("Retrying check... " + retrys, this);
                            retrys++;
                            shouldretry = true;
                        }else
                        {
                            logger.LogError("Maximun error retrys reached", this);
                        }
                        tres = ProcessResult.Failure;
                    });

                    shouldretry = false;
                    tres = ProcessResult.Running;
                }

                if (tres == ProcessResult.Completed || tres == ProcessResult.Failure)
                {
                    yield break;
                }

                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator I_RunModule(AuthModule module, bool checkuserdb)
        {
            ModuleRunning = true;
            int retrys = 0;

            if (auth.CurrentUser != null && checkuserdb)
            {
                CheckUserInDatabase(auth.CurrentUser.UserId);

                yield return new WaitUntil(() => !UserChecking);

            }

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
                    logger.LogWarning("Got Error during process of AsyncModule " + module.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        goto startrunning;
                    }
                }
            }

            retrys = 0;

            if (module.IsValid(WasRequestedByGuest , auth.CurrentUser))
            {
                runmodule:
                module.Execute(this);

                ProcessResult tres = ProcessResult.None;
                float time_enter = Time.time;

                do
                {
                   tres = module.GetResult();

                    if (Input.GetKey(KeyCode.Escape) && Time.time - time_enter >= 0.15f)
                    {
                        RequestExit();
                    }

                    // Interrumpt the module when you're about to upgrade the acocount 
                    float diff = Time.time - ExitTime;
                    if (IsAuthenticated && (module.IsInterruptible() && ExitRequest && diff > 0.2f && diff < 0.5f))
                    {
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
                    logger.LogWarning("Got Error during process of Module " + module.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        goto runmodule;
                    }
                }

                module.OnFinish(this,ExitRequest);
                ExitRequest = false;
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
                        customUI?.HideUI();
                        //OnAuthCancelled?.Invoke();
                        
                        fsm.MoveNext(AuthCheckCommand.GoBack);
                        break;
                    }

                    logger.Log("Auth Running: " + tres , this , true);
                    yield return new WaitForEndOfFrame();
                } while (tres == ProcessResult.None || tres == ProcessResult.Running);

                if (tres == ProcessResult.Failure)
                {
                    logger.LogWarning("Got Error during process of Module " + SelectedMethod.GetException(), this);

                    if (retrys < MaximunErrorRetrys)
                    {
                        logger.LogWarning("Retrying... " + retrys, this);
                        retrys++;
                        goto Execution;
                    }

                }else if (tres == ProcessResult.Completed)
                {
                    SelectedMethod.OnFinish();

                    fsm.MoveNext(AuthCheckCommand.Next);
                }
            }

            AuthRunning = false;
            SelectedMethod = null;
            logger.Log("Auth Execution Finished", this , true);
            yield return 0;
        }

    }
}