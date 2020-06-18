using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace QSocial.Utility
{
    public class QSocialNetworkUtility : MonoBehaviour
    {
        private static QSocialNetworkUtility _instance;

        private static QSocialNetworkUtility Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType<QSocialNetworkUtility>();

                return _instance;
            }
        }

        [SerializeField]
        private bool ExecuteOnAwake = false;
        [SerializeField]
        private string PingIP = "1.1.1.1";
        [SerializeField]
        private float Timeout = 15f;
        [Space(8)]
        public UnityEvent OnInternet;
        public UnityEvent OnNoInternet;

        public static bool LastResponse { get; private set; } = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            if (ExecuteOnAwake)
                CheckInternet(null);
        }

        private bool CheckRunning = false;

        public static void CheckInternet(System.Action<bool> result)
        {
            if (!Instance.CheckRunning)
            {
                Instance.StartCoroutine(Instance.I_CheckInternet(result));
            }
        }

        private IEnumerator I_CheckInternet(System.Action<bool> res)
        {
            CheckRunning = true;
            Ping ping = new Ping(PingIP);
            float t = 0f;
            
            while (!ping.isDone)
            {
                t += Time.deltaTime;
                if (t >= Timeout)
                    break;

                yield return new WaitForEndOfFrame();
            }

            bool response = ping.time > 0;

            if(res != null ) res.Invoke(response);

            if (response)
                OnInternet.Invoke();
            else OnNoInternet.Invoke();

            LastResponse = response;

            ping.DestroyPing();

            CheckRunning = false;
            
            yield return null;
        }
    }
}