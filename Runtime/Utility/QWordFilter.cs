using QSocial.Utility.SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSocial.Utility
{
    public class QWordFilter : MonoBehaviour
    {
        [SerializeField]
        private TextAsset WordFilter = default;

        private string[] BadWords = null;

        private static QWordFilter _instance = null;

        private static QWordFilter Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<QWordFilter>();

                if (!_instance)
                {
                    GameObject go = new GameObject(typeof(QWordFilter).Name);
                    _instance = go.AddComponent<QWordFilter>();
                }

                return _instance;
            }
        }

        public static bool IsValidString(string s)
        {
            return !string.IsNullOrEmpty(s) && !Instance.BadWords.Any((string word) => s.Contains(word));
        }

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
            List<string> Inputtxt = WordFilter.text.Split('\n').ToList();
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < Inputtxt.Count; i++)
            {
                if (Inputtxt[i].StartsWith("//")) continue;

                stringBuilder.Append(Inputtxt[i]);
                stringBuilder.AppendLine();
            }

            JSONNode bwordsnode = JSON.Parse(stringBuilder.ToString());
            BadWords = new string[bwordsnode.Count];

            for (int i = 0; i < BadWords.Length; i++)
            {
                BadWords[i] = bwordsnode[i].Value;
                
            }
        }
    }
}