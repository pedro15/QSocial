using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QSocial.Auth
{
    [AddComponentMenu("QAuth/Guest to Profile"),RequireComponent(typeof(Button))]
    public class AuthGuestToProfile : MonoBehaviour
    {
        private Button btn = default;

        private void Start()
        {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if(AuthManager.Instance.IsLoggedIn() && AuthManager.Instance.CurrentUser.IsAnonymous)
                {

                }
            });
        }
    }
}