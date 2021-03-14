using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Curiosity.Sketchfab
{
    public class ProcessDeepLinkManager : MonoBehaviour
    {
        public static ProcessDeepLinkManager Instance { get; private set; }
        [HideInInspector]
        public string deeplinkURL;
        public UnityEvent OnSketchfabCallback = new UnityEvent();
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Application.deepLinkActivated += OnDeepLinkActivated;
                if (!string.IsNullOrEmpty(Application.absoluteURL))
                {
                    // Cold start and Application.absoluteURL not null so process Deep Link.
                    OnDeepLinkActivated(Application.absoluteURL);
                }
                // Initialize DeepLink Manager global variable.
                else deeplinkURL = "[none]";
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnDeepLinkActivated(string urlString)
        {
            Debug.Log($"onDeepLinkActivated:{urlString}");

            // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
            deeplinkURL = urlString;

            // Decode the URL to determine action. 
            // In this example, the app expects a link formatted like this:
            // https://www.curiosity-inc.jp/ar-live-sketchfab-callback
            var url = new Uri(urlString);
            var segments = url.Segments;
            if (segments[1].Equals("ar-live-sketchfab-callback"))
            {
                var pathStr = url.Fragment;
                var fragment = pathStr.Substring(pathStr.IndexOf('#') + 1);
                var parameters = HttpUtils.ParseQueryString(fragment);
                var accessToken = parameters.Get("access_token");
                SketchfabApi.Instance.SetAccessToken(accessToken);
                OnSketchfabCallback?.Invoke();
            }
        }
    }
}
