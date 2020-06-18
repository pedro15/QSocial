using Firebase.Auth;
using NetworkUtility = QSocial.Utility.QSocialNetworkUtility;

namespace QSocial.Auth.Modules
{
    public class InternetCheckModule : AuthModule, ICustomCommand,IAsyncModule
    {
        private ProcessResult result = ProcessResult.Running;

        private AuthCheckCommand cmd = AuthCheckCommand.Next;

        public override void OnEnter()
        {
            AuthManager.BeginProcess();
            result = ProcessResult.Running;
            cmd = AuthCheckCommand.Next;
            NetworkUtility.CheckInternet((bool internet) =>
            {
                cmd = internet ? AuthCheckCommand.Next : AuthCheckCommand.Exit;
                AuthManager.FinishProcess(!internet, !internet ? new System.Exception("No internet connection") : null);
                result = ProcessResult.Completed;
            });
        }

        public override void Execute(AuthManager manager) { }

        public AuthCheckCommand GetNextCommand()
        {
            return cmd;
        }

        public override ProcessResult GetResult()
        {
            return ProcessResult.Completed;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return true;
        }

        public ProcessResult AsyncResult()
        {
            return result;
        }
    }
}
