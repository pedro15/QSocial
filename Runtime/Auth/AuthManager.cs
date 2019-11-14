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

        private void Auth_StateChanged(object sender, System.EventArgs e)
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

        public void LoginWithEmail(string email, string password, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  if (OnFailure != null) Enquene( () => OnFailure.Invoke((task.Exception != null) ?  
                      task.Exception.Message : "Canceled"));
                  return;
              }
              if (OnSuccess != null) Enquene(OnSuccess);
          });
        }

        public void LoginAnonymous(Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    if (OnFailure != null) Enquene(() => OnFailure.Invoke((task.Exception != null) ?
                     task.Exception.Message : "Canceled"));
                    return;
                }
                if (OnSuccess != null) Enquene(OnSuccess);
            });
        }

        public void CreateUserWithEmail(string email, string password, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   if (OnFailure != null) Enquene(() => OnFailure.Invoke((task.Exception != null) ?
                     task.Exception.Message : "Canceled"));
                   return;
               }
               if (OnSuccess != null) Enquene(OnSuccess);
           });
        }

        public void CreateUserFromAnonymousCredential(Credential credential, Action OnSuccess = null, Action<string> OnFailure = null)
        {
            if (CurrentUser.IsAnonymous)
            {
                auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
               {
                   if (task.IsCanceled || task.IsFaulted)
                   {
                       if (OnFailure != null) Enquene(() => OnFailure.Invoke((task.Exception != null) ?
                     task.Exception.Message : "Canceled"));
                       return;
                   }

                   if (OnSuccess != null) Enquene(OnSuccess);
               });
            }
        }

        public void UpdateUserName( string newUsername , Action OnSuccess  = null , Action<string> OnFailure = null )
        {
            auth.CurrentUser.UpdateUserProfileAsync(new UserProfile() { DisplayName = newUsername }).ContinueWith(task =>
            {
                if(task.IsCanceled || task.IsFaulted)
                {
                    if (OnFailure != null) Enquene(() => OnFailure.Invoke((task.Exception != null) ?
                     task.Exception.Message : "Canceled"));
                    return;
                }

                if (OnSuccess != null) Enquene(OnSuccess);
            });
        }

        public void SignOut()
        {
            if (IsLoggedIn()) auth.SignOut();
        }
    }
}