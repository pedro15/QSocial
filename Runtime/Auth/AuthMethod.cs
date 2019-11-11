using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    [System.Serializable]
    public abstract class AuthMethod
    {
        [SerializeField]
        private bool Enabled = true;

        [SerializeField]
        private Button button = default;

        protected AuthController controller { get; private set; }

        public abstract AuthResult GetResult();

        public abstract string Id { get; }

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

        public abstract void Execute();
    }
}