using Facebook.Unity;
using UnityEngine;

namespace QSocial.Auth.fb
{
    public class FacebookAuthController : MonoBehaviour
    {
        [SerializeField]
        private FacebookAuthMethod facebookauth = default;

        private void OnEnable()
        {
            FB.Init(() =>
            {
                facebookauth.Init(AuthManager.Instance);
                Debug.Log("[Facebook Auth] Init completed!");
            });
        }
    }
}