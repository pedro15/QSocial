using System.Collections.Generic;
using UnityEngine;
using Firebase.Unity.Editor;
using Firebase.Database;
using QSocial.Utility;
using QSocial.Utility.SimpleJSON;
using QSocial.Data.Users;

namespace QSocial.Data
{
    public class QDataManager : MonoBehaviour
    {
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
        [SerializeField]
        private string UsersNodePath = "users";

        public delegate void onUserPlayerRecivedData(UserPlayer userPlayer);

        public static event onUserPlayerRecivedData OnUserPlayerRecivedData;

        public delegate void onPushUserData();

        public static event onPushUserData OnPushUserData;

        Queue<UserPlayer> _userPlayers = new Queue<UserPlayer>();

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

            OnUserPlayerRecivedData += (UserPlayer p) =>
            {
                Debug.Log(p);
            };
        }

        public void RequestPlayerFromId(string uid)
        {
            UserPlayer user = null;
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid);
            _ref.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        Debug.LogError("Error executing query!" + task.Exception?.Message);
                    });

                    return;
                }

                QEventExecutor.ExecuteInUpdate(() =>
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();

                        var root = JSON.Parse(json);

                        string friends = root["friends"];

                        Debug.Log("JSON: " + json);

                        user = new UserPlayer(uid, friends.Split(','));

                        _userPlayers.Enqueue(user);
                    }
                    catch
                    {
                        Debug.LogWarning("Invalid JSON from user request");
                        _userPlayers.Enqueue(null);
                    }
                });
            });
        }

        public void RegisterPlayerToDatabase(UserPlayer player)
        {
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(player.userid);

            _ref.SetRawJsonValueAsync(JsonUtility.ToJson(player)).ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    QEventExecutor.ExecuteInUpdate(() =>
                    {
                        Debug.LogError("Error to push data " + task.Exception);
                    });

                    return;
                }

                QEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log("Push complete!!");
                    OnPushUserData?.Invoke();
                });
            });
        }

        public void RegisterNickname(string nickname,string uid)
        {
            DatabaseReference _ref = FirebaseDatabase.DefaultInstance.GetReference(UsersNodePath).Child(uid);
            Dictionary<string, object> m_update = new Dictionary<string, object>();
            m_update.Add("username", nickname);

            _ref.UpdateChildrenAsync(m_update).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    QEventExecutor.ExecuteInUpdate(() => Debug.LogError("Update fail " + task.Exception));
                    return;
                }
                QEventExecutor.ExecuteInUpdate(() => Debug.Log("Username registered!"));
            });
        }

        private void Update()
        {
            if (_userPlayers.Count > 0)
            {
                UserPlayer user = _userPlayers.Dequeue();
                OnUserPlayerRecivedData?.Invoke(user);
            }
        }

    }
}