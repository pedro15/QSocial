using UnityEngine;

namespace QSocial.Data.Users
{
    [System.Serializable]
    public class UserPlayer
    {
        public string username;
        public string userid;
        public string[] friends;

        public UserPlayer(string Userid, string[] Friends)
        {
            userid = Userid;
            friends = Friends;
        }

        public UserPlayer (string Username, string Userid, string[] Friends)
        {
            username = Username;
            userid = Userid;
            friends = Friends;
        }

        public override string ToString()
        {
            return "UserPlayer: " + JsonUtility.ToJson(this, true);
        }
    }
}