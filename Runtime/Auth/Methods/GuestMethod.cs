using UnityEngine;

namespace QSocial.Auth.Methods
{
    [System.Serializable]
    public class GuestMethod : AuthMethod
    {
        public override string Id => "Guest-Login";

        private AuthResult tres = AuthResult.None;

        public override void OnEnter()
        {
            Debug.Log("GuestMethod!!");
            tres = AuthResult.Running;
            AuthManager.Instance.auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Auth Anonymous Failure!");
                    tres = AuthResult.Failure;
                    return;
                }

                //QEventExecutor.Instance.Enquene(() =>
                //{
                //    Debug.Log("Auth Anonymous Completed !");
                //    tres = AuthResult.Completed;
                //});

                Debug.Log("Auth Anonymous Completed !");
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