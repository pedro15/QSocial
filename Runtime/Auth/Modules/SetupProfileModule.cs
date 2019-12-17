using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class SetupProfileModule : AuthModule
    {
        [SerializeField]
        private GameObject FormContainer = default;
        [SerializeField]
        private TMP_InputField textUsername = default;
        [SerializeField]
        private Button SetupButton = default;

        private bool isFinished = false;

        public override void OnInit(AuthManager manager)
        {
            SetupButton.onClick.AddListener(() =>
            {
                AuthController.Instance.UpdateProfile(textUsername.text, string.Empty,
                    () =>
                    {
                        Debug.Log("Update profile completed!");
                        isFinished = true;
                    }, (System.Exception ex) =>
                   {
                       Debug.LogError("Error updating profile " + ex?.Message);
                   });
            });
        }

        public override void OnFinish(AuthManager manager)
        {
            FormContainer.SetActive(false);
        }

        public override void Execute(AuthManager manager)
        {
            FormContainer.SetActive(true);
        }

        public override bool IsCompleted()
        {
            return isFinished;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return (user != null && string.IsNullOrEmpty(user.DisplayName));
        }
    }
}