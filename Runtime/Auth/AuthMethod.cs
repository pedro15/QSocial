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

        public void Initialize(AuthController controller)
        {
            if (Enabled)
            {
                button.onClick.AddListener(() => GetAction());
                if (!button.gameObject.activeInHierarchy)
                    button.gameObject.SetActive(true);
            }
            else if (button.gameObject.activeInHierarchy)
            {
                button.gameObject.SetActive(false);
            }
        }

        protected abstract void GetAction();
    }
}