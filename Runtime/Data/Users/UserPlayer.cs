using StringBuilder = System.Text.StringBuilder;

namespace QSocial.Data.Users
{
    [System.Serializable]
    public class UserPlayer
    {
        public string nickname;
        public string userid;
        public string[] friends;

        public UserPlayer(string uid, string[] friendlistsIds)
        {
            userid = uid;
            friends = friendlistsIds;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("UserId: " + userid);
            builder.AppendLine();
            builder.Append("FriendsList:");
            builder.AppendLine();
            for (int i = 0; i < friends.Length; i++)
            {
                builder.Append(friends[i]);
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}