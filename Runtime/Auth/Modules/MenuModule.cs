using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;

namespace QSocial.Auth.Modules
{
    public class MenuModule : AuthModule
    {
        private ProcessResult result = ProcessResult.None;

        [SerializeField]
        private GameObject MenuLayout = default;
        [SerializeField]
        private Button[] MenuButtons = default;

        public override void OnInit(AuthManager manager)
        {
            foreach (Button btn in MenuButtons)
                btn.onClick.AddListener(() => result = ProcessResult.Completed);
        }

        public override void Execute(AuthManager manager)
        {
            MenuLayout.SetActive(true);
        }

        public override void OnFinish(AuthManager manager , bool Interrumpted)
        {
            MenuLayout.SetActive(false);
        }

        public override ProcessResult GetResult()
        {
            return result;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return (user == null || (GuestRequest && user.IsAnonymous));
        }

        public override bool IsInterruptible()
        {
            return true;
        }
    }
}