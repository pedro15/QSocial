using Facebook.Unity;
using Firebase.Auth;
using QSocial.Auth;
using QSocial.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace QSocial.Auth.fb
{
    [HasAnonymousConversion,System.Serializable]
    public class FacebookAuthMethod : AuthMethod
    {
        public override string Id => "LogIn-Facebook";

        private System.Exception ex = null;

        private string uid = default;

        public override string ResultUserId => uid;

        private ProcessResult tres = ProcessResult.None;

        public override void OnEnter()
        {
            tres = ProcessResult.Running;
            uid = string.Empty;
            Debug.Log("[FacebookAuth] Auth called");
            FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, HandleResult);
        }

        public override void OnFinish()
        {
            tres = ProcessResult.None;
            ex = null;
        }

        public override System.Exception GetException() => ex;

        private void HandleResult(IResult result)
        {
            Debug.Log("[FacebookAuth] Result: " + result);

            if (result == null)
            {
                ex = new System.Exception("Error at login!");
                tres = ProcessResult.Failure;
                return;
            }

            // Some platforms return the empty string instead of null.
            if (!string.IsNullOrEmpty(result.Error))
            {
                ex = new System.Exception(result.Error);
                tres = ProcessResult.Failure;
            }
            else if (result.Cancelled)
            {
                ex = new System.Exception("LogIn cancelled");
                tres = ProcessResult.Failure;
            }
            else if (!string.IsNullOrEmpty(result.RawResult))
            {
                Debug.Log("[Facebook Auth] login OK!... Access token:: " + AccessToken.CurrentAccessToken.TokenString);

                Credential fbcredential = FacebookAuthProvider.GetCredential(AccessToken.CurrentAccessToken.TokenString);

                if (AuthManager.Instance.IsAuthenticated)
                {
                    if (AuthManager.Instance.auth.CurrentUser.IsAnonymous)
                    {
                        AuthManager.Instance.auth.CurrentUser.LinkWithCredentialAsync(fbcredential).ContinueWith(task =>
                        {
                            if (task.IsCanceled || task.IsFaulted)
                            {
                                QEventExecutor.ExecuteInUpdate(() =>
                                {
                                    ex = AuthManager.GetFirebaseException(task.Exception);
                                    Debug.LogError("[Facebok auth] An Error ocurred " + ex);
                                    tres = ProcessResult.Failure;
                                });
                                return;
                            }

                            if (task.IsCompleted)
                            {
                                QEventExecutor.ExecuteInUpdate(() =>
                                {
                                    Debug.Log("[Facebook Auth] Auth completed!");
                                    uid = task.Result.UserId;
                                    tres = ProcessResult.Completed;
                                });
                            }
                        });
                    }else
                    {
                        ex = new System.ArgumentException("User is not anonymous");
                        Debug.LogError("[FacebookAuth] User is not Anonymous!");
                        tres = ProcessResult.Failure;
                    }
                }
                else
                {
                    AuthManager.Instance.auth.SignInWithCredentialAsync(fbcredential).ContinueWith(task =>
                    {
                        if (task.IsCanceled || task.IsFaulted)
                        {
                            QEventExecutor.ExecuteInUpdate(() =>
                            {
                                ex = AuthManager.GetFirebaseException(task.Exception);
                                Debug.LogError("[Facebok auth] An Error ocurred " + ex);
                                tres = ProcessResult.Failure;
                            });
                            return;
                        }

                        if (task.IsCompleted)
                        {
                            QEventExecutor.ExecuteInUpdate(() =>
                            {
                                Debug.Log("[Facebook Auth] Register completed!");
                                uid = task.Result.UserId;
                                tres = ProcessResult.Completed;
                            });
                        }
                    });
                }

            }
            else
            {
                ex = new System.Exception("Empty Response");
                tres = ProcessResult.Failure;
            }

            Debug.Log("[Facebook AUth] result: " + result.ToString());
        }

        public override ProcessResult GetResult()
        {
            return tres;
        }
    }
}