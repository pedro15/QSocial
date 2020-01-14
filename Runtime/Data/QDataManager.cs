using System.Collections.Generic;
using UnityEngine;
using Firebase.Unity.Editor;
using Firebase.Database;
using QSocial.Utility;
using QSocial.Utility.SimpleJSON;
using QSocial.Data.Users;
using QSocial.Auth;

namespace QSocial.Data
{
    public class QDataManager : MonoBehaviour
    {
        private const string UsersNodePath = "users";

        private static QDataManager _instance = null; 

        public static QDataManager Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<QDataManager>();

                return _instance;
            }
        }

        [SerializeField]
        private string DatabaseUrl = default;
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            Firebase.FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(DatabaseUrl);
        }

        public void GetPlayerFromId(string uid , System.Action<UserPlayer> OnComplete = null , 
            System.Action<System.Exception> OnFailure = null)
        {
            UserPlayer user = null;
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid);
            _ref.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        Debug.LogError("Error executing query! " + task.Exception);
                        OnFailure?.Invoke(task.Exception);
                    });

                    return;
                }

                QEventExecutor.ExecuteInUpdate(() =>
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        Debug.Log("Recived JSON: " + json);

                        var root = JSON.Parse(json);

                        JSONNode friends_Node = root["friends"];
                        string[] friends = new string[friends_Node.Count];

                        for (int i = 0; i < friends.Length; i++)
                        {
                            friends[i] = friends_Node[i].Value;
                        }

                        user = new UserPlayer(uid,friends);

                        OnComplete?.Invoke(user);
                    }
                    catch
                    {
                        OnFailure?.Invoke(new System.Exception("Invalid JSON from user request"));
                    }
                });
            });
        }

        public void RegisterPlayerToDatabase(UserPlayer player , System.Action OnComplete = null , 
            System.Action<System.Exception> OnFalure = null)
        {
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(player.userid);

            _ref.SetRawJsonValueAsync(JsonUtility.ToJson(player)).ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        Debug.LogError("Error to push data " + task.Exception);
                        OnFalure?.Invoke(task.Exception);
                    });

                    return;
                }

                QEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log("Push complete!!");
                    OnComplete?.Invoke();
                });
            });
        }

        public void NicknameValid(string nickname , System.Action<bool> Result , 
            System.Action<System.Exception> OnFailure = null)
        {
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath);

            _ref.OrderByChild("username").EqualTo(nickname).GetValueAsync().ContinueWith( task =>
           {
               if (task.IsFaulted || task.IsCanceled)
               {
                   QEventExecutor.ExecuteInUpdate(() =>
                   {
                       Debug.LogError("Error fetching usernames");
                       OnFailure?.Invoke(task.Exception);
                   });
                   return;
               }
               Result.Invoke(task.Result.ChildrenCount == 0);
           });
        }

        public void RegisterNickname(string nickname,string uid , System.Action OnComplete = null , 
            System.Action<System.Exception> OnFailure = null)
        {
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid);
            Dictionary<string, object> m_update = new Dictionary<string, object>();
            m_update.Add("username", nickname);

            _ref.UpdateChildrenAsync(m_update).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        Debug.LogError("Update fail " + task.Exception);
                        OnFailure?.Invoke(task.Exception);
                    });
                    return;
                }

                QEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log("Username registered!");
                    OnComplete?.Invoke();
                });
            });
        }

        public void GetCurrentUserData(System.Action<string> Response, System.Action<System.Exception> OnFailure = null)
        {
            if (AuthManager.Instance.IsAuthenticated)
            {
                string uid = AuthManager.Instance.auth.CurrentUser.UserId;
                DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid)
                    .Child("userdata");

                _ref.GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted|| task.IsCanceled)
                    {
                        QEventExecutor.ExecuteInUpdate(() => OnFailure?.Invoke(task.Exception));
                        return;
                    }
                    QEventExecutor.ExecuteInUpdate( () => Response.Invoke(task.Result.GetRawJsonValue()));
                });
            }else
            {
                Debug.LogError("[QDataManager:: GetCurrentUserData] User is not autenticated!");
            }
        }

        public void SetCurrentUserData(string RawJSON , System.Action OnComplete = null, 
            System.Action<System.Exception> OnFailure = null )
        {
            if (AuthManager.Instance.IsAuthenticated)
            {
                string uid = AuthManager.Instance.auth.CurrentUser.UserId;
                DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid)
                    .Child("userdata");

                _ref.SetRawJsonValueAsync(RawJSON).ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        QEventExecutor.ExecuteInUpdate(() => OnFailure?.Invoke(task.Exception));
                        return;
                    }
                    QEventExecutor.ExecuteInUpdate(() => OnComplete?.Invoke());
                });
           
            }else
            {
                Debug.LogError("[QDataManager:: SetCurrentUserData] User is not autenticated!");
            }
        }
    }
}