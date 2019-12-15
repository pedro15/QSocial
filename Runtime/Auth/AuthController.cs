using Firebase.Auth;
using QSocial.Utility;
using UnityEngine;

namespace QSocial.Auth
{
    public class AuthController : MonoBehaviour
    {
        private static AuthController _instance = null;

        public static AuthController Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<AuthController>();

                return _instance;
            }
        }

        public delegate void _onStateChanged();

        public static event _onStateChanged OnStateChanged;

        public delegate void _onAuthComplete();

        public static event _onAuthComplete OnAuthComplete;

        public delegate void _onAuthFail(System.Exception ex);

        public static event _onAuthFail OnAuthFail;

        public delegate void _onLinkAccount();

        public static event _onLinkAccount OnLinkAccount;

        public delegate void _onLinkAccountFail(System.Exception ex);

        public static event _onLinkAccountFail OnLinkAccountFail;

        public delegate void _onUserProfileUpdated();
        public static event _onUserProfileUpdated OnUserProfileUpdated;

        public delegate void _onUserProfileUpdateFailed(System.Exception ex);
        public static event _onUserProfileUpdateFailed OnUserProfileUpdateFailed;

        private FirebaseAuth auth = null;

        private MainThreadExecutor executor;

        public bool IsSignedIn
        {
            get => auth.CurrentUser != null;
        }

        public FirebaseUser CurrentUser
        {
            get => auth?.CurrentUser;
        }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            auth = FirebaseAuth.DefaultInstance;
            executor = MainThreadExecutor.Instance;

            auth.StateChanged += Auth_StateChanged;
        }

        private void OnDestroy()
        {
            auth.StateChanged -= Auth_StateChanged;
        }

        private void Auth_StateChanged(object sender, System.EventArgs e)
        {
            executor.Enquene(() => OnStateChanged?.Invoke());
        }

        private void HandleSingIn(System.Threading.Tasks.Task<FirebaseUser> task, System.Action OnComplete = null,
            System.Action<System.Exception> OnFailure = null)
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                System.Exception ex = task.Exception;
                executor.Enquene(() =>
                {
                    OnFailure?.Invoke(ex);
                    OnAuthFail?.Invoke(ex);
                });

                return;
            }

            executor.Enquene(() =>
           {
               OnAuthComplete?.Invoke();
               OnComplete?.Invoke();
           });
        }

        public void SignOut()
        {
            auth.SignOut();
        }

        public void SignInWithEmail(string email, string password, System.Action OnComplete = null, System.Action<System.Exception> OnFailure = null)
        {
            auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
           {
               HandleSingIn(task, OnComplete, OnFailure);
           });
        }

        public void SignInWithCredential(Credential credential, System.Action OnComplete = null, System.Action<System.Exception> OnFailure = null)
        {
            auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
           {
               HandleSingIn(task, OnComplete, OnFailure);
           });
        }

        public void SignInAnonymous(System.Action OnComplete = null, System.Action<System.Exception> OnFailure = null)
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task =>
           {
               HandleSingIn(task, OnComplete, OnFailure);
           });
        }

        public void LinkAccount(Credential credential, System.Action OnComplete = null, System.Action<System.Exception>
            OnFailure = null)
        {
            if (IsSignedIn)
            {
                CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        System.Exception ex = task.Exception;

                        executor.Enquene(() =>
                        {
                            OnFailure?.Invoke(ex);
                            OnLinkAccountFail?.Invoke(ex);
                        });
                        return;
                    }

                    executor.Enquene(() =>
                   {
                       OnComplete?.Invoke();
                       OnLinkAccount?.Invoke();
                   });

                });
            }
            else
            {
                executor.Enquene(() =>
               {
                   var ex = new System.Exception("User is not autenticated!");
                   OnFailure?.Invoke(ex);
                   OnLinkAccountFail?.Invoke(ex);
               });
            }
        }

        public void UpdateProfile(string username, string PhotoUrl , System.Action OnComplete = null ,
            System.Action<System.Exception> OnFailure = null)
        {
            bool goodUrl = !string.IsNullOrEmpty(PhotoUrl) &&
                System.Uri.IsWellFormedUriString(PhotoUrl, System.UriKind.Absolute);

            UserProfile profile = (goodUrl) ? new UserProfile()
            {
                PhotoUrl = new System.Uri(PhotoUrl),
                DisplayName = username
            } : new UserProfile() { DisplayName = username };

            auth.CurrentUser.UpdateUserProfileAsync(profile).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    System.Exception ex = task.Exception;
                    executor.Enquene(() =>
                    {
                        OnFailure?.Invoke(ex);
                        OnUserProfileUpdateFailed?.Invoke(ex);
                    });
                    return;
                }

                executor.Enquene(() =>
                {
                    OnComplete?.Invoke();
                    OnUserProfileUpdated?.Invoke();
                });
            });
        }
    }
}