using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QSocial.Data;
using QSocial.Utility;
using Logger = QSocial.Utility.QSocialLogger;

namespace QSocial.Auth.Modules
{
    [System.Serializable]
    public class SetupProfileModule : AuthModule, IAsyncModule
    {
        [SerializeField]
        private GameObject FormContainer = default;
        [SerializeField]
        private TMP_InputField textUsername = default;
        [SerializeField]
        private Button SetupButton = default;
        [SerializeField]
        private int MinimunUsernameLenght = 6;

        ProcessResult result = ProcessResult.None;

        ProcessResult checkresult = ProcessResult.Running;

        private bool ProfileReady = false;

        private System.Exception m_ex = null;

        public override void OnInit(AuthManager manager)
        {
            SetupButton.onClick.AddListener(() => SetupNickname());
        }

        public override void Execute(AuthManager manager)
        {
            FormContainer.SetActive(true);   
        }

        public override void OnFinish(AuthManager manager, bool Interrumpted)
        {
            Debug.Log("OnFinish!");
            FormContainer.SetActive(false);
        }

        public override void OnEnter()
        {
            m_ex = null;
            result = ProcessResult.None;
            checkresult = ProcessResult.None;
            CheckConfigured();
        }

        private void CheckConfigured()
        {
            if (AuthManager.Instance.IsAuthenticated)
            {
                checkresult = ProcessResult.Running;
                QDataManager.Instance.UsernameConfigured(AuthManager.Instance.auth.CurrentUser.UserId, (bool exists) =>
               {
                   ProfileReady = exists;
                   checkresult = ProcessResult.Completed;
               }, (System.Exception ex) =>
               {
                   m_ex = ex;
                   checkresult = ProcessResult.Failure;
               });
            }else
            {
                checkresult = ProcessResult.Failure;
            }
        }

        private void SetupNickname()
        {
            FirebaseUser usr = AuthManager.Instance.auth.CurrentUser;

            string username = textUsername.text.ToLower();

            if (username.Length < MinimunUsernameLenght)
            {
                Logger.LogWarning("Nickname selected is too short", this);
                result = ProcessResult.None;
                AuthManager.FinishProcess(true, new QAuthException(QAuthErrorCode.SHORT_USERNAME));
                return;
            }

            if (QWordFilter.IsValidString(username))
            {
                result = ProcessResult.Running;
                AuthManager.BeginProcess();

                QDataManager.Instance.NicknameValid(username, (bool m_result) =>
                {
                    if (m_result)
                    {
                        QDataManager.Instance.RegisterNickname(username, usr.UserId, () =>
                        {
                            usr?.UpdateUserProfileAsync
                            (new UserProfile() { DisplayName = username }).ContinueWith(task =>
                            {
                                if (task.IsCanceled || task.IsFaulted)
                                {
                                    QEventExecutor.ExecuteInUpdate(() =>
                                    {
                                        m_ex = AuthManager.GetFirebaseException(m_ex);
                                        Logger.LogError("Setup Profile Failure! " + m_ex, this);
                                        result = ProcessResult.Failure;
                                        AuthManager.FinishProcess();
                                    });
                                    return;
                                }

                                QEventExecutor.ExecuteInUpdate(() =>
                                {
                                    Logger.Log("Setup Profile Completed!", this , true);
                                    result = ProcessResult.Completed;
                                    textUsername.text = string.Empty;
                                    AuthManager.FinishProcess();
                                    AuthManager.CompleteProfile();
                                });
                            });
                        },
                        (System.Exception ex) =>
                        {
                            m_ex = ex;
                            Logger.LogError("An error ocurrer at register nickname " + ex , this);
                            result = ProcessResult.Failure;
                            AuthManager.FinishProcess();
                        });
                    }
                    else
                    {
                        Logger.LogWarning("Nickname already in the database", this);
                        result = ProcessResult.Failure;
                        AuthManager.FinishProcess(true , new QAuthException(QAuthErrorCode.USERNAME_EXISTS));
                    }
                }, (System.Exception ex) =>
                {
                    m_ex = ex;
                    Logger.LogError("Error at checking nickname" + ex , this);
                    result = ProcessResult.Failure;
                    AuthManager.FinishProcess();
                });
            }
            else
            {
                Logger.LogWarning("Nickname selected is not valid", this);
                result = ProcessResult.None;
                AuthManager.FinishProcess(true ,new QAuthException(QAuthErrorCode.INVALID_USERNAME));
            }
        }

        public override System.Exception GetException() => m_ex;

        public override ProcessResult GetResult()
        {
            return result;
        }

        public override bool IsValid(bool GuestRequest, FirebaseUser user)
        {
            return !ProfileReady && AuthManager.Instance.IsAuthenticated;
        }

        public ProcessResult AsyncResult()
        {
            return checkresult;
        }
    }
}