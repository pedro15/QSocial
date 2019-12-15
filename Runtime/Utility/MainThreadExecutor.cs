using System;
using System.Collections.Generic;
using UnityEngine;

namespace QSocial.Utility
{
    internal class MainThreadExecutor : MonoBehaviour
    {
        private static MainThreadExecutor _Instance = null;

        public static MainThreadExecutor Instance
        {
            get
            {
                if (!_Instance) _Instance = FindObjectOfType<MainThreadExecutor>();

                if (!_Instance)
                {
                    GameObject obj = new GameObject("Q-Social_MainThreadExecutor");
                    _Instance = obj.AddComponent<MainThreadExecutor>();
                }

                return _Instance;
            }
        }

        private Queue<Action> runActions = null;

        private bool QueneEmpty = true;

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            _Instance = this;
        }

        private void Update()
        {
            if (QueneEmpty) return;

            List<Action> actions = new List<Action>();

            lock (runActions)
            {
                for (int i = 0; i < runActions.Count; i++)
                    actions.Add(runActions.Dequeue());

                QueneEmpty = true;
            }

            for (int i = 0; i < actions.Count; i++)
                actions[i].Invoke();
        }

        public void Enquene(Action action)
        {
            lock (runActions)
            {
                runActions.Enqueue(action);
                QueneEmpty = false;
            }
        }
    }
}