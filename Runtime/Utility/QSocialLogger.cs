using UnityEngine;

namespace QSocial.Utility
{
    public class QSocialLogger : MonoBehaviour
    {
        private static QSocialLogger _instance = null;

        public static QSocialLogger Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<QSocialLogger>();

                return _instance;
            }
        }

        [SerializeField]
        private LogMode logMode = LogMode.Full;

        private void Awake()
        {
            if (_instance != null || _instance != this)
            {
                Destroy(_instance);
                return;
            }

            _instance = this;
        }

        public void LogError(object message, object context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(FormatMessage(message, context), context as Object);
#endif
        }

        public void Log(object message, object context, bool IsExtraInfo = false)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!IsExtraInfo && (logMode == LogMode.Info || logMode == LogMode.Full) ||
                IsExtraInfo && logMode == LogMode.Full)
                Debug.Log(FormatMessage(message, context), context as Object);
#endif
        }

        public void LogWarning(object message, object context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logMode == LogMode.Full)
                Debug.LogWarning(FormatMessage(message, context), context as Object);
#endif
        }

        private string FormatMessage(object message, object context)
        {
            return $"[ { context.GetType().Name } ] { message }";
        }
    }
}