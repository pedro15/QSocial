using QSocial.Utility;
using System;

namespace QSocial.Auth.Methods
{
    [System.Serializable]
    public class AnonymousMethod : AuthMethod
    {
        public override string Id => "SingIn-Anonymous";

        private string uid = default;

        private ProcessResult result = ProcessResult.None;

        private System.Exception ex;

        public override string ResultUserId => uid;

        public override ProcessResult GetResult() => result;

        public override Exception GetException() => ex;

        public override void OnEnter()
        {
            ex = null;
            AuthManager.BeginProcess();

            result = ProcessResult.Running;

            AuthManager.Instance.logger.Log("Anonymous SingIn", this, true);

            AuthManager.Instance.auth.SignInAnonymouslyAsync().ContinueWith(task =>
           {
               if (task.IsFaulted || task.IsCanceled)
               {
                   QEventExecutor.ExecuteInUpdate(() =>
                   {
                       ex = AuthManager.GetFirebaseException(ex);
                       AuthManager.Instance.logger.LogError("Anonymous SingIn Failure " + ex, this);
                       AuthManager.FinishProcess();
                       result = ProcessResult.Failure;
                   });
                   return;
               }

               QEventExecutor.ExecuteInUpdate(() =>
               {
                   AuthManager.Instance.logger.Log("Anonymous SingIn Completed!", this, true);
                   AuthManager.FinishProcess();
                   result = ProcessResult.Completed;
               });
           });
        }
    }
}