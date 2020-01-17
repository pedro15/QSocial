using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    [System.Serializable]
    public abstract class AuthMethod
    {
        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private Button SelectionButton = default;
        
        public bool Enabled { get => enabled; }

        public abstract string Id { get; }

        public abstract string ResultUserId { get; }

        private bool Initialized = false;

        public void Init(AuthManager manager)
        {
            if (!Initialized)
            {
                SelectionButton.onClick.AddListener(() => manager.ExecuteAuthMethod(Id));

                manager.RegisterAuthMethod(this);
                Initialized = true;
                OnInit(manager);
            }
        }

        public void SetEnabled(bool enabled)
        {
            SelectionButton.gameObject.SetActive(enabled);
        }

        public abstract AuthResult GetResult();

        protected virtual void OnInit(AuthManager manager) { }

        public virtual void OnUpdate() { }

        public virtual void OnEnter() { }

        public virtual void OnFinish() { }

        public virtual System.Exception GetException() => null;
    }
}