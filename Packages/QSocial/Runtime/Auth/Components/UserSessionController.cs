using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth.Components
{
    [AddComponentMenu("QSocial/User session controller"), DefaultExecutionOrder(+50)]
    public class UserSessionController : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI DisplayUsername = default;
        [SerializeField]
        private Button SignOutBtn = default;

        private void Start()
        {
            AuthManager.OnProfileCompleted += AuthManager_OnProfileCompleted;
            AuthManager.Instance.auth.StateChanged += Auth_StateChanged;
            
            if (SignOutBtn != null)
            {
                SignOutBtn.onClick.AddListener(() => AuthManager.Instance.auth.SignOut());
            }

            Refresh();
        }

        private void AuthManager_OnProfileCompleted()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            AuthManager.OnProfileCompleted -= AuthManager_OnProfileCompleted;
            if (AuthManager.Instance != null)
                AuthManager.Instance.auth.StateChanged -= Auth_StateChanged;
        }

        private void Auth_StateChanged(object sender, System.EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (SignOutBtn != null)
            {
                SignOutBtn.gameObject.SetActive(AuthManager.Instance.IsAuthenticated);
            }

            if (DisplayUsername != null && AuthManager.Instance.IsAuthenticated)
            {
                DisplayUsername.text = AuthManager.Instance.auth.CurrentUser.DisplayName;
            }

            if (DisplayUsername != null)
            {
                DisplayUsername.gameObject.SetActive(AuthManager.Instance.IsAuthenticated);
            }
        }
    }
}