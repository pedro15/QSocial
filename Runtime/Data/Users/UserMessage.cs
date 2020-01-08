using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QSocial.Data.Users
{
    [System.Serializable]
    public class UserMessage
    {
        public string FromId;
        public MessageType messageType;
        public string MessageData;
    }
}