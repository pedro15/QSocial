using UnityEngine;

namespace QSocial.Auth
{
    [System.Serializable]
    public class AnonymousAuth : AuthMethod
    {
        public override string Id => "Auth-Anonymous";

        private AuthResult Result = AuthResult.None;

        public override void OnReset()
        {
            Result = AuthResult.None;
        }

        public override AuthResult OnExecute()
        {
            return Result;
        }

        public override void OnSelect()
        {
            Result = AuthResult.Running;
            Debug.Log("Auth Anonymous!");
            AuthManager.Instance.SingInAnonymous(() =>
            {
                Debug.Log("Auth Anonymous completed!");
                Result = AuthResult.Success;
            }, (string msg) =>
            {
                Debug.LogError($"Auth Anonymous Failure! {msg}");
                Result = AuthResult.Failure;
            });
        }

    }
}