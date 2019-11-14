using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    [System.Serializable]
    public abstract class AuthMethod
    {
        [SerializeField]
        private bool Enabled = true;

        public bool IsEnabled { get { return Enabled; } }

        [SerializeField]
        private Button button = default;

        protected AuthController controller { get; private set; }

        public abstract AuthResult GetResult();

        public abstract string Id { get; }

        public static bool IsAnonymousConversion
        {
            get
            {
                return AuthManager.Instance.IsLoggedIn() && AuthManager.Instance.CurrentUser.IsAnonymous;
            }
        }

        public void SetActive(bool active)
        {
            bool state = IsEnabled && active;
            if (button.gameObject.activeInHierarchy != state)
                button.gameObject.SetActive(state);
        }

        public void Initialize(AuthController controller)
        {
            this.controller = controller;
            if (Enabled)
            {
                controller.RegisterMethod(this, Id);
                button.onClick.AddListener(() => controller.ExecuteAuthMethod(Id));
                if (!button.gameObject.activeInHierarchy)
                    button.gameObject.SetActive(true);
                OnInit();
            }
            else if (button.gameObject.activeInHierarchy)
            {
                button.gameObject.SetActive(false);
            }

        }

        protected virtual void OnInit() { }

        public virtual void OnReset() { }

        public abstract void OnSelect();
    }
}