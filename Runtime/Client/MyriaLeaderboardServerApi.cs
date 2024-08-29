using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using MyriaLeaderboard.MyriaLeaderboardEnums;
using UnityEditor;
using MyriaLeaderboard.Requests;

namespace MyriaLeaderboard.MyriaLeaderboardEnums
{
    public enum MyriaLeaderboardCallerRole { User, Admin, Player, Base };
}

namespace MyriaLeaderboard
{
    public class MyriaLeaderboardServerApi : MonoBehaviour
    {
        private static MyriaLeaderboardServerApi _instance;
        private const int MaxRetries = 3;
        private int _tries;

        public static void Instantiate()
        {
            if (_instance == null)
            {
                _instance = new GameObject("MyriaLeaderboardServerApi").AddComponent<MyriaLeaderboardServerApi>();

                if (Application.isPlaying)
                    DontDestroyOnLoad(_instance.gameObject);
            }
        }

        public static void ResetInstance()
        {
            if (_instance == null) return;
#if UNITY_EDITOR
            DestroyImmediate(_instance.gameObject);
#else
            Destroy(_instance.gameObject);
#endif
            _instance = null;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            ResetInstance();
        }
#endif

        public static void SendRequest(MyriaLeaderboardServerRequest request, Action<MyriaLeaderboardResponse> OnServerResponse = null)
        {
            if (_instance == null)
            {
                Instantiate();
            }

            _instance._SendRequest(request, OnServerResponse);
        }

        private void _SendRequest(MyriaLeaderboardServerRequest request, Action<MyriaLeaderboardResponse> OnServerResponse = null)
        {
            StartCoroutine(coroutine());
            IEnumerator coroutine()
            {
                //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
                yield return null;

                //Build the URL that we will hit based on the specified endpoint, query params, etc
                string url = BuildUrl(request.endpoint, request.queryParams, request.callerRole);
#if UNITY_EDITOR
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)("ServerRequest " + request.httpMethod + " URL: " + url);
#endif
                using (UnityWebRequest webRequest = CreateWebRequest(url, request))
                {
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    float startTime = Time.time;
                    bool timedOut = false;

                    UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = webRequest.SendWebRequest();
                    yield return new WaitUntil(() =>
                    {
                        if (unityWebRequestAsyncOperation == null)
                        {
                            return true;
                        }

                        timedOut = !unityWebRequestAsyncOperation.isDone && Time.time - startTime >= MyriaLeaderboardConfig.current.clientSideRequestTimeOut;

                        return timedOut || unityWebRequestAsyncOperation.isDone;

                    });

                    if (!webRequest.isDone && timedOut)
                    {
                        MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Warning)("Exceeded maxTimeOut waiting for a response from " + request.httpMethod + " " + url);
                        OnServerResponse?.Invoke(MyriaLeaderboardResponseFactory.ClientError<MyriaLeaderboardResponse>(request.endpoint + " timed out."));
                        yield break;
                    }

                    LogResponse(request, webRequest.responseCode, webRequest.downloadHandler.text, startTime);

                    if (WebRequestSucceeded(webRequest))
                    {
                        OnServerResponse?.Invoke(new MyriaLeaderboardResponse
                        {
                            statusCode = (int)webRequest.responseCode,
                            success = true,
                            text = webRequest.downloadHandler.text,
                            errorData = null
                        });
                        yield break;
                    }


                    //_tries = 0;
                    MyriaLeaderboardResponse response = new MyriaLeaderboardResponse
                    {
                        statusCode = (int)webRequest.responseCode,
                        success = false,
                        text = webRequest.downloadHandler.text
                    };

                    try
                    {
                        response.errorData = MyriaLeaderboardJson.DeserializeObject<MyriaLeaderboardErrorData>(webRequest.downloadHandler.text);
                    }
                    catch (Exception)
                    {
                        if (webRequest.downloadHandler.text.StartsWith("<"))
                        {
                            MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Warning)("JSON Starts with <, info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text);
                        }
                        response.errorData = null;
                    }
                    // Error data was not parseable, populate with what we know
                    if (response.errorData == null)
                    {
                        response.errorData = new MyriaLeaderboardErrorData((int)webRequest.responseCode, webRequest.downloadHandler.text);
                    }

                    MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)(response.errorData.ToString());
                    OnServerResponse?.Invoke(response);
                }
            }
        }


         #region Private Methods
         private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403) && MyriaLeaderboardConfig.current.allowTokenRefresh && timesRetried < MaxRetries;
            //return (statusCode == 401 || statusCode == 403) && MyriaLeaderboardConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static void LogResponse(MyriaLeaderboardServerRequest request, long statusCode, string responseBody, float startTime)
        {
            try
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)("Server Response: " +
                    statusCode + " " +
                    request.endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    MyriaLeaderboardObfuscator
                        .ObfuscateJsonStringForLogging(responseBody));
            }
            catch
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)(request.httpMethod.ToString());
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)(request.endpoint);
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)(
                    MyriaLeaderboardObfuscator.ObfuscateJsonStringForLogging(responseBody));
            }
        }

        private static string GetUrl(MyriaLeaderboardCallerRole callerRole)
        {
            switch (callerRole)
            {
                case MyriaLeaderboardCallerRole.Admin:
                    return MyriaLeaderboardConfig.current.adminUrl;
                case MyriaLeaderboardCallerRole.User:
                    return MyriaLeaderboardConfig.current.userUrl;
                case MyriaLeaderboardCallerRole.Player:
                    return MyriaLeaderboardConfig.current.playerUrl;
                case MyriaLeaderboardCallerRole.Base:
                    return MyriaLeaderboardConfig.current.url;
                default:
                    return MyriaLeaderboardConfig.current.url;
            }
        }

        private bool WebRequestSucceeded(UnityWebRequest webRequest)
        {
            return !
#if UNITY_2020_1_OR_NEWER
            (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(webRequest.error));
#else
            (webRequest.isHttpError || webRequest.isNetworkError || !string.IsNullOrEmpty(webRequest.error));
#endif
        }

        private static readonly Dictionary<string, string> BaseHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json; charset=UTF-8" },
            { "Content-Type", "application/json; charset=UTF-8" },
            { "Access-Control-Allow-Credentials", "true" },
            { "Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time" },
            { "Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD" },
            { "Access-Control-Allow-Origin", "*" },
            { "LL-Instance-Identifier", System.Guid.NewGuid().ToString() }
        };

        private static bool ShouldRefreshUsingRefreshToken(MyriaLeaderboardServerRequest cachedRequest)
        {
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            return (string.IsNullOrEmpty(cachedRequest.jsonPayload) || !cachedRequest.jsonPayload.Contains("refresh_token")) && !string.IsNullOrEmpty(MyriaLeaderboardConfig.current.refreshToken);
        }

        private void CompleteCall(MyriaLeaderboardServerRequest cachedRequest, Action<MyriaLeaderboardResponse> onComplete)
        {

            if (cachedRequest.retryCount >= 4)
            {
                MyriaLeaderboardLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.TokenExpiredError<MyriaLeaderboardResponse>());
                return;
            }

            cachedRequest.extraHeaders["x-session-token"] = MyriaLeaderboardConfig.current.token;
            _SendRequest(cachedRequest, onComplete);
            cachedRequest.retryCount++;
        }

        private UnityWebRequest CreateWebRequest(string url, MyriaLeaderboardServerRequest request)
        {
            UnityWebRequest webRequest;
            switch (request.httpMethod)
            {
                case MyriaLeaderboardHTTPMethod.UPLOAD_FILE:
                    webRequest = UnityWebRequest.Post(url, request.form);
                    break;
                case MyriaLeaderboardHTTPMethod.UPDATE_FILE:
                    // Workaround for UnityWebRequest with PUT HTTP verb not having form fields
                    webRequest = UnityWebRequest.Post(url, request.form);
                    webRequest.method = UnityWebRequest.kHttpVerbPUT;
                    break;
                case MyriaLeaderboardHTTPMethod.POST:
                case MyriaLeaderboardHTTPMethod.PATCH:
                // Defaults are fine for PUT
                case MyriaLeaderboardHTTPMethod.PUT:

                    if (request.payload == null && request.upload != null)
                    {
                        List<IMultipartFormSection> form = new List<IMultipartFormSection>
                        {
                            new MultipartFormFileSection(request.uploadName, request.upload, System.DateTime.Now.ToString(), request.uploadType)
                        };

                        // generate a boundary then convert the form to byte[]
                        byte[] boundary = UnityWebRequest.GenerateBoundary();
                        byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
                        // Set the content type - NO QUOTES around the boundary
                        string contentType = String.Concat("multipart/form-data; boundary=--", Encoding.UTF8.GetString(boundary));

                        // Make my request object and add the raw text. Set anything else you need here
                        webRequest = new UnityWebRequest();
                        webRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=--");
                        webRequest.uri = new Uri(url);
                        MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)(url);//the url is wrong in some cases
                        webRequest.uploadHandler = new UploadHandlerRaw(formSections);
                        webRequest.uploadHandler.contentType = contentType;
                        webRequest.useHttpContinue = false;

                        // webRequest.method = "POST";
                        webRequest.method = UnityWebRequest.kHttpVerbPOST;
                    }
                    else
                    {
                        string json = (request.payload != null && request.payload.Count > 0) ? MyriaLeaderboardJson.SerializeObject(request.payload) : request.jsonPayload;
#if UNITY_EDITOR
                        MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)("REQUEST BODY = " + MyriaLeaderboardObfuscator.ObfuscateJsonStringForLogging(json));
#endif
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json);
                        webRequest = UnityWebRequest.Put(url, bytes);
                        webRequest.method = request.httpMethod.ToString();
                    }

                    break;

                case MyriaLeaderboardHTTPMethod.OPTIONS:
                case MyriaLeaderboardHTTPMethod.HEAD:
                case MyriaLeaderboardHTTPMethod.GET:
                    // Defaults are fine for GET
                    webRequest = UnityWebRequest.Get(url);
                    webRequest.method = request.httpMethod.ToString();
                    break;

                case MyriaLeaderboardHTTPMethod.DELETE:
                    // Defaults are fine for DELETE
                    webRequest = UnityWebRequest.Delete(url);
                    break;
                default:
                    throw new System.Exception("Invalid HTTP Method");
            }

            if (BaseHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in BaseHeaders)
                {
                    if (pair.Key == "Content-Type" && request.upload != null) continue;

                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(MyriaLeaderboardConfig.current?.sdk_version))
            {
                webRequest.SetRequestHeader("LL-SDK-Version", MyriaLeaderboardConfig.current.sdk_version);
            }

            if (request.extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in request.extraHeaders)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            return webRequest;
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null, MyriaLeaderboardCallerRole callerRole = MyriaLeaderboardCallerRole.User)
        {
            string ep = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();

            return (GetUrl(callerRole) + ep + GetQueryStringFromDictionary(queryParams)).Trim();
        }

        private string GetQueryStringFromDictionary(Dictionary<string, string> queryDict)
        {
            if (queryDict == null || queryDict.Count == 0) return string.Empty;

            string query = "?";

            foreach (KeyValuePair<string, string> pair in queryDict)
            {
                if (query.Length > 1)
                    query += "&";

                query += pair.Key + "=" + pair.Value;
            }

            return query;
        }
        #endregion
    }
}
