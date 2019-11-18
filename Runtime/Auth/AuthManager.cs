using Firebase.Auth;
using System.Collections.Generic;
using UnityEngine;
using UniToolkit.Utility;
using System;

namespace QSocial.Auth
{
    [AddComponentMenu("QSocial/Auth Manager"),DefaultExecutionOrder(-50)]
    public class AuthManager : MonoSingleton<AuthManager>
    {
        public delegate void e_OnStateChanged();

        public static event e_OnStateChanged OnStateChanged;

        public delegate void e_BaseEvent(AuthResult result, string message);

        public static event e_BaseEvent OnEmailSingIn;

        public static event e_BaseEvent OnAnonymousSingIn;

        public static event e_BaseEvent OnCreateUserEmail;

        public static event e_BaseEvent OnUpdateProfile;

        public static event e_BaseEvent OnLinkAccount;

        private readonly Queue<Action> _ExecutionQuene = new Queue<Action>();
        private bool _QueneEmpty = true;

        protected override bool Persistent => true;
        public FirebaseUser CurrentUser => auth.CurrentUser;

        public bool IsLoggedIn()
        {
            return CurrentUser != null;
        }

        private FirebaseAuth auth;

        private void Start()
        {
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += Auth_StateChanged;   
        }

        private void OnDestroy()
        {
            auth.StateChanged -= Auth_StateChanged;
        }

        private void Auth_StateChanged(object sender, EventArgs e)
        {
            OnStateChanged?.Invoke();
        }

        private void Update()
        {
            if (_QueneEmpty) return;

            List<Action> actions = new List<Action>();

            lock (_ExecutionQuene)
            {
                for (int i = 0; i < _ExecutionQuene.Count; i++)
                {
                    actions.Add(_ExecutionQuene.Dequeue());
                }
                _QueneEmpty = true;
            }

            foreach (Action action in actions) action.Invoke();
        }

        private void Enquene(Action action)
        {
            lock (_ExecutionQuene)
            {
                _ExecutionQuene.Enqueue(action);
                _QueneEmpty = false;
            }
        }

        public void SingInWithEmail(string email, string password, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  string msg = (task.Exception != null) ? task.Exception.Message : "Canceled";

                  if (OnFailure != null) Enquene( () => OnFailure.Invoke(msg));

                  if (OnEmailSingIn != null) Enquene(() => OnEmailSingIn.Invoke(AuthResult.Failure , msg));

                  return;
              }
              if (OnSuccess != null) Enquene(OnSuccess);

              if (OnEmailSingIn != null) Enquene(() => OnEmailSingIn.Invoke(AuthResult.Success, string.Empty));
          });
        }

        public void SingInAnonymous(Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string msg = (task.Exception != null) ? task.Exception.Message : "Canceled";

                    if (OnFailure != null) Enquene(() => OnFailure.Invoke(msg));

                    if (OnAnonymousSingIn != null) Enquene(() => OnAnonymousSingIn.Invoke(AuthResult.Failure,
                        msg));

                    return;
                }
                if (OnSuccess != null) Enquene(OnSuccess);
                if (OnAnonymousSingIn != null) OnAnonymousSingIn.Invoke(AuthResult.Success, string.Empty);
            });
        }

        public void CreateUserWithEmail(string email, string password, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   string msg = (task.Exception != null) ? task.Exception.Message : "Canceled";

                   if (OnFailure != null) Enquene(() => OnFailure.Invoke(msg));

                   if (OnCreateUserEmail != null) Enquene(() => OnCreateUserEmail.Invoke(AuthResult.Failure, msg));

                   return;
               }

               Enquene(() =>
              {
                  if (OnSuccess != null) OnSuccess.Invoke();

                  if (OnCreateUserEmail != null) OnCreateUserEmail.Invoke(AuthResult.Success, string.Empty);
              });
           });
        }

        public void LinkAccount(Credential credential, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            if (CurrentUser.IsAnonymous)
            {
                auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
               {
                   if (task.IsCanceled || task.IsFaulted)
                   {
                       string msg = (task.Exception != null) ? task.Exception.Message : "Canceled";

                       Enquene(() =>
                       {
                           if (OnFailure != null) OnFailure.Invoke(msg);

                           if (OnLinkAccount != null) OnLinkAccount.Invoke(AuthResult.Failure, msg);
                       });

                       return;
                   }

                   Enquene(() =>
                   {
                       if (OnSuccess != null) OnSuccess.Invoke();

                       if (OnLinkAccount != null) OnLinkAccount.Invoke(AuthResult.Success, string.Empty);
                   });

               });
            }
        }

        public void UpdateProfile( string newUsername, string AvatarUrl , Action OnSuccess  = null , Action<string> OnFailure = null )
        {
            bool urlValid = !string.IsNullOrEmpty(AvatarUrl) && Uri.IsWellFormedUriString(AvatarUrl, UriKind.Absolute);

            UserProfile profile = urlValid ? new UserProfile()
            {
                DisplayName = newUsername,
                PhotoUrl = new Uri(AvatarUrl)
            } : new UserProfile()
            {
                DisplayName = newUsername
            };

            auth.CurrentUser.UpdateUserProfileAsync(profile).ContinueWith(task =>
            {
                if(task.IsCanceled || task.IsFaulted)
                {
                    string msg = (task.Exception != null) ? task.Exception.Message : "Canceled";

                    if (OnFailure != null) Enquene(() => OnFailure.Invoke(msg));

                    if (OnUpdateProfile != null) Enquene(() => OnUpdateProfile.Invoke(AuthResult.Failure, msg));
                    
                    return;
                }

                if (OnSuccess != null) Enquene(OnSuccess);

                if (OnUpdateProfile != null) Enquene(() => OnUpdateProfile.Invoke(AuthResult.Success, string.Empty));
            });
        }

        public void SignOut()
        {
            if (IsLoggedIn()) auth.SignOut();
        }
    }
}