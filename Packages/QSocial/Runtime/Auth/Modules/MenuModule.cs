using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class MenuModule : AuthModule
    {
        private ProcessResult result = ProcessResult.None;

        [SerializeField]
        private GameObject MenuLayout = default;
        [SerializeField]
        private Button[] MenuButtons = default;
        [SerializeField]
        private Button SkipButton = default;
        [SerializeField]
        private Button LoginButton = default;
        [SerializeField]
        private Button BackButton = default;

        public override void OnInit(AuthManager manager)
        {
            foreach (Button btn in MenuButtons)
            {
                btn.onClick.AddListener(() => result = ProcessResult.Completed);
            }
            
            SkipButton.onClick.AddListener(() => result = ProcessResult.Completed);

            
            LoginButton.onClick.AddListener(() =>
            {
                foreach (Button btn in MenuButtons)
                    btn.gameObject.SetActive(true);

                BackButton.gameObject.SetActive(true);
                LoginButton.gameObject.SetActive(false);
                SkipButton.gameObject.SetActive(false);
            });

            BackButton.onClick.AddListener(() =>
            {
                foreach (Button btn in MenuButtons)
                    btn.gameObject.SetActive(false);

                BackButton.gameObject.SetActive(false);
                LoginButton.gameObject.SetActive(true);
                SkipButton.gameObject.SetActive(true);
            });
        }

        public override void OnEnter()
        {
            bool m_enabled = AuthManager.Instance.AuthMethodEnabled("No-Login");
           
            if (m_enabled)
            {
                foreach (Button btn in MenuButtons)
                    btn.gameObject.SetActive(false);

                BackButton.gameObject.SetActive(false);
                LoginButton.gameObject.SetActive(true);
                SkipButton.gameObject.SetActive(true);
            }else
            {
                foreach (Button btn in MenuButtons)
                    btn.gameObject.SetActive(true);

                SkipButton.gameObject.SetActive(false);
                LoginButton.gameObject.SetActive(false);
                BackButton.gameObject.SetActive(false);
            }
        }

        public override void Execute(AuthManager manager)
        {
            MenuLayout.SetActive(true);
            result = ProcessResult.Running;
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