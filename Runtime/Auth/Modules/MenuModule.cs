using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    internal class MenuModule : AuthModule
    {
        [SerializeField]
        private GameObject LayoutContainer = default;

        private bool isFinished = false;

        public override void Execute(AuthManager manager)
        {
            LayoutContainer.SetActive(true);
            manager.DisplayLayout(true);
        }

        public void Finish()
        {
            isFinished = true;
        }

        public override bool IsCompleted()
        {
            return isFinished;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return user == null || (GuestRequest && user.IsAnonymous);
        }

        public override void OnFinish(AuthManager manager)
        {
            LayoutContainer.SetActive(false);
            isFinished = false;
        }
    }
}