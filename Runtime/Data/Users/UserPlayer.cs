using UnityEngine;

namespace QSocial.Data.Users
{
    [System.Serializable]
    public class UserPlayer
    {
        public string userid;
        public string[] friends;

        public UserPlayer(string Uid, string[] FriendlistsIds)
        {
            userid = Uid;
            friends = FriendlistsIds;
        }

        public override string ToString()
        {
            return "UserPlayer: " + JsonUtility.ToJson(this, true);
        }
    }
}