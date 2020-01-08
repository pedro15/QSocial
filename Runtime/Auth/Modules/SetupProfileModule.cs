using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QSocial.Data.Users;
using QSocial.Data;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class SetupProfileModule : AuthModule
    {
        [SerializeField]
        private GameObject FormContainer = default;
        [SerializeField]
        private TMP_InputField textUsername = default;
        [SerializeField]
        private Button SetupButton = default;

        private bool isFinished = false;

        public override void OnInit(AuthManager manager)
        {
            SetupButton.onClick.AddListener(() =>
            {
                SetupNickname();
            });
            QDataManager.OnUserPlayerRecivedData += QDataManager_OnUserPlayerRecivedData;

        }

        private void QDataManager_OnUserPlayerRecivedData(UserPlayer userPlayer)
        {
            if (userPlayer != null)
            {
                SetupNickname();
            }
        }

        private void SetupNickname()
        {
            AuthManager.Instance.auth.CurrentUser?.UpdateUserProfileAsync
                 (new UserProfile() { DisplayName = textUsername.text }).ContinueWith(task =>
                 {
                     if (task.IsCanceled || task.IsFaulted)
                     {
                         Debug.Log("Setup Profile failure!!");
                         isFinished = false;
                         return;
                     }

                     QDataManager.Instance.RegisterNickname(textUsername.text,
                         AuthManager.Instance.auth.CurrentUser.UserId);

                     Debug.Log("Setup profile completed !!");
                     isFinished = true;
                 });
        }

        public override void OnFinish(AuthManager manager , bool interrupted)
        {
            FormContainer.SetActive(false);
            manager.DisplayLayout(false);
        }

        public override void Execute(AuthManager manager)
        {
            isFinished = false;
            FormContainer.SetActive(true);
            manager.DisplayLayout(true);
        }

        public override bool IsCompleted()
        {
            return isFinished;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {

            return (user != null && string.IsNullOrEmpty(user.DisplayName));
        }
    }
}