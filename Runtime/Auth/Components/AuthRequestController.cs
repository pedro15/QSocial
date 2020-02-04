using UnityEngine;

namespace QSocial.Auth.Components
{
    [AddComponentMenu("QSocial/Auth Request") , DefaultExecutionOrder(+50)]
    public class AuthRequestController : MonoBehaviour
    {
        private void Start()
        {
            AuthManager.Instance.auth.StateChanged += Auth_StateChanged;
            DoRequest();
        }


        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
                AuthManager.Instance.auth.StateChanged -= Auth_StateChanged;
        }

        private void Auth_StateChanged(object sender, System.EventArgs e)
        {
            DoRequest();
        }

        private void DoRequest()
        {
            AuthManager.Instance.RequestLogIn();
        }
    }
}