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
            btn.onClick.AddListener(() => AuthManager.Instance.RequestLogin(true));
            AuthManager.Instance.auth.StateChanged += Auth_StateChanged;
        }

        private void Auth_StateChanged(object sender, System.EventArgs e)
        {
            btn.gameObject.SetActive(AuthManager.Instance.IsAuthenticated && AuthManager.Instance.auth.CurrentUser.IsAnonymous);
        }
    }
}