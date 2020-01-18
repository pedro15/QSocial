using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QSocial.Data;
using QSocial.Utility;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class SetupProfileModule : AuthModule,IAsyncModule
    {
        [SerializeField]
        private GameObject FormContainer = default;
        [SerializeField]
        private TMP_InputField textUsername = default;
        [SerializeField]
        private Button SetupButton = default;

        private bool isFinished = false;
        private bool isLoading = false;
        private bool running = false;
        private bool profileReady = false;

        private System.Exception ex;

        public override void OnInit(AuthManager manager)
        {
            SetupButton.onClick.AddListener(() =>
            {
                SetupNickname();
            });

        }

        private void SetupNickname()
        {
            FirebaseUser usr = AuthManager.Instance.auth.CurrentUser;

            string username = textUsername.text;

            if (QWordFilter.IsValidString(username))
            {
                QDataManager.Instance.NicknameValid(username, (bool result) =>
                {
                    if (result)
                    {
                        QDataManager.Instance.RegisterNickname(username, usr.UserId, () => {
                            usr?.UpdateUserProfileAsync
                            (new UserProfile() { DisplayName = username }).ContinueWith(task =>
                            {
                                if (task.IsCanceled || task.IsFaulted)
                                {
                                    Debug.Log("Setup Profile failure!!");
                                    isFinished = false;
                                    return;
                                }

                                QEventExecutor.ExecuteInUpdate(() => AuthManager.Instance.CompleteProfile());
                                Debug.Log("Setup profile completed !!");
                                isFinished = true;
                            });
                        },
                        (System.Exception ex) => Debug.LogError("An error ocurrer at register nickname " + ex));
                    }
                    else
                    {
                        Debug.LogWarning("nickname exists");
                    }
                }, (System.Exception ex) => Debug.LogError("Error checking username " + ex));
            }else
            {
                Debug.LogWarning("Invalid Username!");
            }
        }

        public override void OnFinish(AuthManager manager , bool interrupted)
        {
            FormContainer.SetActive(false);
            manager.DisplayLayout(false);
            isFinished = false;
            isLoading = false;
        }

        public override void Execute(AuthManager manager)
        {
            isFinished = false;
            FormContainer.SetActive(true);
            manager.DisplayLayout(true);
        }

        public override void OnEnter()
        {
            isLoading = true;
            profileReady = false;
            ex = null;
        }

        public  bool IsLoading(bool Guest, FirebaseUser user)
        {
            Debug.Log("User: " + user);
            if (!running && user != null)
            {
                QDataManager.Instance.UsernameConfigured(user.UserId, (bool x) =>
                {
                    profileReady = x;
                    Debug.Log("Profile ready: " + profileReady);
                    running = false;
                    isLoading = false;
                }, (System.Exception e) =>
                {
                    ex = e;
                    Debug.LogError("Error at loading user data");
                    running = false;
                    isLoading = false;
                });

                running = true;
            }

            if (!AuthManager.Instance.AuthRunning && user == null)
            {
                profileReady = true;
                return false;
            }

            return isLoading;
        }

        public System.Exception GetException()
        {
            return ex;
        }

        public override bool IsCompleted()
        {
            return isFinished;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return !profileReady;
        }

    }
}