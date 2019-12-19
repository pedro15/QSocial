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
        [SerializeField]
        private Button[] MenuButtons = default;

        private bool isFinished = false;

        public override void OnInit(AuthManager manager)
        {
            foreach (Button btn in MenuButtons) btn?.onClick.AddListener(() => isFinished = true);
        }

        public override void Execute(AuthManager manager)
        {
            LayoutContainer.SetActive(true);
            manager.DisplayLayout(true);
        }

        public override bool IsCompleted()
        {
            return isFinished;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return user == null || (GuestRequest && user.IsAnonymous);
        }

        public override void OnFinish(AuthManager manager, bool Interrupted)
        {
            if (Interrupted)
            {
                manager.DisplayLayout(false);
            }
            
            LayoutContainer.SetActive(false);
            isFinished = false;
        }

        public override bool IsInterruptible()
        {
            return true;
        }
    }
}