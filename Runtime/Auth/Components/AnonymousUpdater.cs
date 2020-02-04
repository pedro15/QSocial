using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth.Components
{
    [RequireComponent(typeof(Button)), DefaultExecutionOrder(25)]
    public class AnonymousUpdater : MonoBehaviour
    {
        private Button btn = null;
        private void Start()
        {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(() => AuthManager.Instance.RequestLogIn(true));
            AuthManager.Instance.auth.StateChanged += Auth_StateChanged;
            AuthManager.OnAuthCompleted += AuthManager_OnAuthCompleted;
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
                AuthManager.Instance.auth.StateChanged -= Auth_StateChanged;
            
            AuthManager.OnAuthCompleted -= AuthManager_OnAuthCompleted;
        }

        private void AuthManager_OnAuthCompleted()
        {
            RefreshAuthState();
        }

        private void Auth_StateChanged(object sender, System.EventArgs e)
        {
            RefreshAuthState();
        }

        private void RefreshAuthState()
        {
            btn.gameObject.SetActive(AuthManager.Instance.IsAuthenticated 
                && AuthManager.Instance.auth.CurrentUser.IsAnonymous);
        }

    }
}