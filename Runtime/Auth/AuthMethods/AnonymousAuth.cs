using UnityEngine;

namespace QSocial.Auth
{
    [System.Serializable]
    public class AnonymousAuth : AuthMethod
    {
        public override string Id => "Auth-Anonymous";

        private AuthResult _result = AuthResult.None;

        public override void OnReset()
        {
            _result = AuthResult.None;
        }

        public override void OnSelect()
        {
            _result = AuthResult.Running;
            Debug.Log("Auth Anonymous!");
            AuthManager.Instance.LoginAnonymous(() =>
            {
                Debug.Log("Auth Anonymous completed!");
                _result = AuthResult.Success;
            }, (string msg) =>
            {
                Debug.LogError($"Auth Anonymous Failure! {msg}");
                _result = AuthResult.Failure;
            });
        }

        public override AuthResult GetResult()
        {
            return _result;
        }
    }
}