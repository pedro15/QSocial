using Firebase.Auth;
using QSocial.Data;
using QSocial.Data.Users;
using Logger = QSocial.Utility.QSocialLogger;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class DatabaseCheckModule : AuthModule
    {
        private ProcessResult result = ProcessResult.None;

        private System.Exception _ex = null;

        public override void OnEnter()
        {
            result = ProcessResult.None;
            _ex = null;
        }

        public override void Execute(AuthManager manager)
        {
            AuthManager.BeginProcess();
            Checkdb();
        }

        public override System.Exception GetException() => _ex;

        public override ProcessResult GetResult() => result;

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return user != null;
        }

        private void Checkdb()
        {
            string uid = AuthManager.Instance.auth.CurrentUser.UserId;

            Logger.Log("Check user in Database",this);
            result = ProcessResult.Running;
            QDataManager.Instance.UserExists(uid, (bool exists) =>
           {
               if (!exists)
               {
                   Logger.Log("User not found in database, Uploading...", this, true);
                   QDataManager.Instance.RegisterPlayerToDatabase(new UserPlayer(uid), () =>
                   {
                       Logger.Log("Upload user to database completed!", this, true);
                       AuthManager.FinishProcess();
                       result = ProcessResult.Completed;
                   }, (System.Exception ex) =>
                   {
                       _ex = ex;
                       AuthManager.FinishProcess();
                       result = ProcessResult.Failure;
                   });
               }else
               {
                   Logger.Log("User already exists in database, continue without changes", this, true);
                   AuthManager.FinishProcess();
                   result = ProcessResult.Completed;
               }
           } , (System.Exception ex) =>
           {
               AuthManager.FinishProcess();
               _ex = ex;
               result = ProcessResult.Failure;
           });
        }
    }
}
