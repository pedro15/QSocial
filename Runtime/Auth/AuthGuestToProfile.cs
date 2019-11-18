using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    [AddComponentMenu("QAuth/Guest to Profile"), RequireComponent(typeof(Button))]
    public class AuthGuestToProfile : MonoBehaviour
    {
        private Button btn = default;

        private void Start()
        {
            AuthManager.OnLinkAccount += AuthManager_OnLinkAccount;
            AuthManager.OnStateChanged += AuthManager_OnStateChanged;

            btn = GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AuthController.Instance.RequestLogin(true);
            });

            RefreshState();
        }

        private void AuthManager_OnStateChanged()
        {
            RefreshState();
        }

        private void RefreshState()
        {
            btn.gameObject.SetActive(AuthMethod.IsAnonymousUser);
        }

        private void AuthManager_OnLinkAccount(AuthResult result, string message)
        {
            Debug.Log("OnLinkAccount!");
            RefreshState();
        }

        private void OnDestroy()
        {
            AuthManager.OnLinkAccount -= AuthManager_OnLinkAccount;
            AuthManager.OnStateChanged -= AuthManager_OnStateChanged;
        }
    }
}