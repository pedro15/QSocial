using UnityEngine;

namespace QSocial.Auth.Methods
{
    [System.Serializable]
    public class GuestMethod : AuthMethod
    {
        public override string Id => "Guest-Login";

        private AuthResult tres = AuthResult.None;

        private System.Exception ex;

        private string uid = default;

        public override string ResultUserId => uid;

        public override System.Exception GetException() => ex;

        public override void OnEnter()
        {
            Debug.Log("GuestMethod!!");
            tres = AuthResult.Running;
            AuthManager.Instance.auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Auth Anonymous Failure!");
                    ex = task.Exception;
                    tres = AuthResult.Failure;
                    return;
                }

                Debug.Log("Auth Anonymous Completed !");
                uid = task.Result.UserId;
                tres = AuthResult.Completed;

                return;
            });
        }

        public override AuthResult GetResult()
        {
            return tres;
        }
    }
}