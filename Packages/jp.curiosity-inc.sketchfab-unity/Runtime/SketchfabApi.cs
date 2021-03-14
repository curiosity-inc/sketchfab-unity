using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Text;

namespace Curiosity.Sketchfab
{
    public class SketchfabApi : Curiosity.Sketchfab.Singleton<SketchfabApi>
    {
        private string LOGIN_URL = "https://sketchfab.com/oauth2/authorize/?state=&response_type=token&client_id={0}";
        private string MODEL_UPLOAD_API_URL = "https://api.sketchfab.com/v3/models";
        private string MY_PROFILE_API_URL = "https://api.sketchfab.com/v3/me";
        private const string MODEL_DOWNLOAD_URL = "https://api.sketchfab.com/v3/models/{0}/download";
        private const string SEARCH_MODEL_URL = "https://api.sketchfab.com/v3/search?type=models";
        private const string SEARCH_MY_MODEL_URL = "https://api.sketchfab.com/v3/me/search?type=models";
        private SketchfabApiConfig _config;
        private string _accessToken;
        public const string PREF_SKETCHFAB_ACCESS_TOKEN = "sketchfab_token";
        public UnityEvent OnLogin = new UnityEvent();

        public class ModelSearchOption
        {
            public string query;
            public string[] tags;

            public bool? downloadable;
            public bool? rigged;

            public string ToQueryString()
            {
                var sb = new StringBuilder();
                if (query != null)
                {
                    sb.Append($"&q={query}");
                }
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        sb.Append($"&tags={tag}");
                    }
                }
                if (downloadable.HasValue)
                {
                    sb.Append($"&downloadable={downloadable.Value}");
                }
                if (rigged.HasValue)
                {
                    sb.Append($"&rigged={rigged.Value}");
                }
                return sb.ToString();
            }
        }

        private void Awake()
        {
            _config = Resources.Load<SketchfabApiConfig>("SketchfabApiConfig");
            if (string.IsNullOrEmpty(_config.ClientId))
            {
                Debug.LogWarning("ClientId is empty. Please set.");
            }
            _accessToken = PlayerPrefs.GetString(PREF_SKETCHFAB_ACCESS_TOKEN, "");
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            PlayerPrefs.SetString(PREF_SKETCHFAB_ACCESS_TOKEN, accessToken);
        }

        public void Login()
        {
            Application.OpenURL(string.Format(LOGIN_URL, _config.ClientId));
        }

        public void SearchModels(ModelSearchOption option, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress)
        {
            StartCoroutine(SearchModelsCoroutine(option, onSuccess, onError, onProgress));
        }
        public IEnumerator SearchModelsCoroutine(ModelSearchOption option, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress)
        {
            var url = SEARCH_MODEL_URL;
            if (option != null)
            {
                url += option.ToQueryString();
            }
            yield return SearchModelsWithUrlCoroutine(url, onSuccess, onError, onProgress, false);
        }

        public void SearchMyModels(ModelSearchOption option, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress)
        {
            StartCoroutine(SearchMyModelsCoroutine(option, onSuccess, onError, onProgress));
        }
        public IEnumerator SearchMyModelsCoroutine(ModelSearchOption option, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress)
        {
            var url = SEARCH_MY_MODEL_URL;
            if (option != null)
            {
                url += option.ToQueryString();
            }
            yield return SearchModelsWithUrlCoroutine(url, onSuccess, onError, onProgress, true);
        }

        public void SearchModelsWithUrl(string url, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress, bool needAuth = false)
        {
            StartCoroutine(SearchModelsWithUrlCoroutine(url, onSuccess, onError, onProgress, needAuth));
        }

        public IEnumerator SearchModelsWithUrlCoroutine(string url, UnityAction<ModelsApiResult> onSuccess, UnityAction onError, UnityAction<float> onProgress, bool needAuth = false)
        {
            var req = UnityWebRequest.Get(url);
            req.timeout = 5;
            if (needAuth)
            {
                var authorization = $"Bearer {_accessToken}";
                req.SetRequestHeader("Authorization", authorization);
            }
            var reqOperation = req.SendWebRequest();
            while (!reqOperation.isDone)
            {
                onProgress?.Invoke(req.downloadProgress);
                yield return null;
            }
            if (req.isHttpError || req.isNetworkError)
            {
                var error = req.downloadHandler.text ?? req.error;
                Debug.LogWarning(error);
                onError?.Invoke();
                yield break;
            }
            var bodyStr = req.downloadHandler.text;
            var results = JsonUtility.FromJson<ModelsApiResult>(bodyStr);
            Debug.Log(results.ToString());
            onSuccess?.Invoke(results);
        }

        public void CheckAccess(UnityAction<User> onSuccess, UnityAction onError)
        {
            StartCoroutine(CheckAccessCoroutine(onSuccess, onError));
        }
        public IEnumerator CheckAccessCoroutine(UnityAction<User> onSuccess, UnityAction onError)
        {
            Debug.Log("sketchfab getProfile");
            if (string.IsNullOrEmpty(_accessToken))
            {
                onError?.Invoke();
                yield break;
            }
            var authorization = $"Bearer {_accessToken}";
            UnityWebRequest www = UnityWebRequest.Get(MY_PROFILE_API_URL);
            www.timeout = 5;
            www.SetRequestHeader("Authorization", authorization);
            www.SendWebRequest();
            while (!www.isDone)
            {
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                Debug.Log(www.downloadHandler.text);
                //Loading.Instance.DismissLoading();
                onError?.Invoke();
                yield break;
            }

            var bodyStr = www.downloadHandler.text;
            var body = JsonUtility.FromJson<User>(bodyStr);
            onSuccess?.Invoke(body);
        }

        public IEnumerator UploadModel(string path, UnityAction<string> onSuccess, UnityAction<string> onError, UnityAction<float> onProgress)
        {
            Debug.Log("sketchfab uploadModel");
            var authorization = $"Bearer {_accessToken}";

            // TODO:WWWFormより効率のいい方法を見つけたい、、、
            var formData = new WWWForm();
            var fileName = Path.GetFileName(path);
            formData.AddField("name", $"{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")} exported by Rememory");
            formData.AddField("isPublished", "false");
            formData.AddField("tags", "LiDAR");
            formData.AddField("tags", "Rememory");
            formData.AddBinaryData("modelFile", File.ReadAllBytes(path), fileName);

            UnityWebRequest www = UnityWebRequest.Post(MODEL_UPLOAD_API_URL, formData);
            //www.timeout = 10;
            www.SetRequestHeader("Authorization", authorization);
            www.SendWebRequest();
            while (!www.isDone)
            {
                onProgress?.Invoke(www.uploadHandler.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                var error = string.IsNullOrEmpty(www.downloadHandler.text) ? www.error : www.downloadHandler.text;
                Debug.Log($"{error}");
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log("Successfully uploaded");
            var bodyStr = www.downloadHandler.text;
            var body = JsonUtility.FromJson<ModelUploadResponse>(bodyStr);

            UnityWebRequest detailReq = UnityWebRequest.Get(body.uri);
            detailReq.SendWebRequest();
            while (!detailReq.isDone)
            {
                yield return null;
            }

            var detailBodyStr = detailReq.downloadHandler.text;
            var detailBody = JsonUtility.FromJson<ModelDetailResponse>(detailBodyStr);

            onSuccess?.Invoke(detailBody.viewerUrl);
        }

        public IEnumerator GetModelDownloadInfo(Model model, UnityAction<DownloadModelResult> onSuccess, UnityAction<string> onError, UnityAction<float> onProgress)
        {
            var url = string.Format(MODEL_DOWNLOAD_URL, model.uid);
            var req = UnityWebRequest.Get(url);
            req.timeout = 5;
            req.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
            var reqOperation = req.SendWebRequest();
            while (!reqOperation.isDone)
            {
                yield return null;
            }
            if (req.isHttpError || req.isNetworkError)
            {
                var error = req.downloadHandler.text ?? req.error;
                Debug.LogWarning(error);
                onError?.Invoke(error);
                yield break;
            }
            var bodyStr = req.downloadHandler.text;
            Debug.Log(bodyStr);
            var downloads = JsonUtility.FromJson<DownloadModelResult>(bodyStr);
            onSuccess?.Invoke(downloads);
        }

        public static IEnumerator DownloadModel(string downloadUrl, UnityAction<string> onSuccess, UnityAction<string> onError, UnityAction<float> onProgress)
        {
            var req = UnityWebRequest.Get(downloadUrl);
            req.timeout = 5;
            var reqOperation = req.SendWebRequest();
            while (!reqOperation.isDone)
            {
                onProgress?.Invoke(req.uploadHandler.progress);
                yield return null;
            }
            if (req.isHttpError || req.isNetworkError)
            {
                var error = req.downloadHandler.text ?? req.error;
                Debug.LogWarning(error);
                onError?.Invoke(error);
                yield break;
            }
            // TODO
            //File.WriteAllBytes(compressedFilePath, s3req.downloadHandler.data);
            onSuccess?.Invoke(null);
        }

        [Serializable]
        private class ModelUploadResponse
        {
            public string uid;
            public string uri;
        }

        [Serializable]
        private class ModelDetailResponse
        {
            public string uid;
            public string uri;
            public string name;
            public string viewerUrl;
        }

        public void OnDeepLinkActivated(string urlString)
        {
            // Debug.Log($"onDeepLinkActivated:{urlString}");
            if (urlString.StartsWith(_config.CallbackUrl))
            {
                var url = new Uri(urlString);
                var pathStr = url.Fragment;
                var fragment = pathStr.Substring(pathStr.IndexOf('#') + 1);
                var parameters = HttpUtils.ParseQueryString(fragment);
                var accessToken = parameters.Get("access_token");
                SketchfabApi.Instance.SetAccessToken(accessToken);
                OnLogin?.Invoke();
            }
        }
    }
}
