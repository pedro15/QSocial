using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QSocial.Data;
using QSocial.Utility;

namespace QSocial.Auth.Modules
{
    public class SetupProfileModule : AuthModule, IAsyncModule
    {
        [SerializeField]
        private GameObject FormContainer = default;
        [SerializeField]
        private TMP_InputField textUsername = default;
        [SerializeField]
        private Button SetupButton = default;

        ProcessResult result = ProcessResult.None;

        ProcessResult checkresult = ProcessResult.Running;

        public override void Execute(AuthManager manager)
        {
            
        }

        public override ProcessResult GetResult()
        {
            return result;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            throw new System.NotImplementedException();
        }

        public ProcessResult AsyncResult()
        {
            return checkresult;
        }
    }
}