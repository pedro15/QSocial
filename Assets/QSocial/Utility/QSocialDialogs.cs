using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QSocial.Auth;
using UDialogs;

namespace QSocial.Essentials
{
    public class QSocialDialogs : MonoBehaviour
    {
        private const string LOADING_KEY = "QSocial-Loading";
        private const string ALERT_KEY = "QSocial-Alert";

        private void Start()
        {
            AuthManager.OnProcessBegin += AuthManager_OnProcessBegin;
            AuthManager.OnProcessFinish += AuthManager_OnProcessFinish;

        }

        private void OnDestroy()
        {
            AuthManager.OnProcessBegin += AuthManager_OnProcessBegin;
            AuthManager.OnProcessFinish += AuthManager_OnProcessFinish;
        }

        private void AuthManager_OnProcessFinish(bool wantsuser , System.Exception ex)
        {
            UDialogManager.HideDialogWindow(LOADING_KEY);
            if (wantsuser)
            {
                UDialogManager.DisplayDialogWindow(ALERT_KEY, ex.Message, DialogType.Warning, DialogButtons.Ok
                    , new ButtonOptions("Accept", (UDialogMessage msg) => msg.Hide()));
            }
        }

        private void AuthManager_OnProcessBegin()
        {
            UDialogManager.DisplayDialogWindow(LOADING_KEY, "Loading...", DialogType.Loading, DialogButtons.None);
        }
    }
}