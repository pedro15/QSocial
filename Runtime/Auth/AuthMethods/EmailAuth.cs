using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QSocial.Auth
{
    [System.Serializable]
    public class EmailAuth : AuthMethod,IAuthCustomUI
    {
        [SerializeField]
        private GameObject LoginFormContainer = default;
        [SerializeField]
        private TMP_InputField text_email = default;
        [SerializeField]
        private TMP_InputField text_password = default;
        [SerializeField]
        private Button LoginButton = default;
        [SerializeField]
        private Button RegisterButton = default;

        private AuthResult _result = AuthResult.None;

        
        public override string Id => "Auth-email";

        public override AuthResult GetResult()
        {
            return _result;
        }

        protected override void OnInit()
        {
            LoginButton.onClick.AddListener(() => Login());
            RegisterButton.onClick.AddListener(() => Register());
        }

        public override void OnReset()
        {
            _result = AuthResult.None;
        }

        public void HideUI()
        {
            LoginFormContainer.SetActive(false);
        }

        public override void OnSelect()
        {
            LoginFormContainer.SetActive(true);
        }

        private void Login()
        {
            _result = AuthResult.Running;
            Debug.Log("Login in... (email)");
            AuthManager.Instance.LoginWithEmail(text_email.text, text_password.text,
                () =>
                {
                    _result = AuthResult.Success;
                    Debug.Log("Login with email Success!");
                }, (string msg) =>
                 {
                     Debug.LogError("Login with email Failure!");
                     _result = AuthResult.Failure;
                 });
        }

        private void Register()
        {
            _result = AuthResult.Running;
            Debug.Log("Register... (email)");
            AuthManager.Instance.CreateUserWithEmail(text_email.text, text_password.text,
                () =>
                {
                    _result = AuthResult.Success;
                    Debug.Log("Register with email Success!");
                }, (string msg) =>
                {
                    Debug.LogError("Register with email Failure!");
                    _result = AuthResult.Failure;
                });
        }
    }
}