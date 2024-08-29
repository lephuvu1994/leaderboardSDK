using System.Collections.Generic;
using UnityEngine;
using System;
using MyriaLeaderboard.MyriaLeaderboardEnums;
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#else
using LLlibs.ZeroDepJson;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

//this is common between user and admin
namespace MyriaLeaderboard
{

    public static class MyriaLeaderboardJsonSettings
    {
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
        public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            Formatting = Formatting.None
        };
#else
        public static readonly JsonOptions Default = new JsonOptions(JsonSerializationOptions.Default & ~JsonSerializationOptions.SkipGetOnly & ~JsonSerializationOptions.ConvertToSnakeCase);
#endif
    }

    public static class MyriaLeaderboardJson
    {
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj, MyriaLeaderboardJsonSettings.Default);
        }

        public static string SerializeObject(object obj, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(obj, settings ?? MyriaLeaderboardJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json)
        {
            return DeserializeObject<T>(json, MyriaLeaderboardJsonSettings.Default);
        }


        public static T DeserializeObject<T>(string json, JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<T>(json, settings ?? MyriaLeaderboardJsonSettings.Default);
        }
#else //MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj, MyriaLeaderboardJsonSettings.Default);
        }

        public static string SerializeObject(object obj, JsonOptions options)
        {
            return Json.Serialize(obj, options ?? MyriaLeaderboardJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json)
        {
            return DeserializeObject<T>(json, MyriaLeaderboardJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json, JsonOptions options)
        {
            return Json.Deserialize<T>(json, options ?? MyriaLeaderboardJsonSettings.Default);
        }
#endif //MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
    }

    [Serializable]
    public enum MyriaLeaderboardHTTPMethod
    {
        GET = 0,
        POST = 1,
        DELETE = 2,
        PUT = 3,
        HEAD = 4,
        CREATE = 5,
        OPTIONS = 6,
        PATCH = 7,
        UPLOAD_FILE = 8,
        UPDATE_FILE = 9
    }

    public class MyriaLeaderboardErrorData
    {
        public MyriaLeaderboardErrorData(int httpStatusCode, string errorMessage)
        {
            code = $"HTTP{httpStatusCode}";
            doc_url = $"https://developer.mozilla.org/docs/Web/HTTP/Status/{httpStatusCode}";
            message = errorMessage;
        }

        public MyriaLeaderboardErrorData() { }

        /// <summary>
        /// A descriptive code identifying the error.
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// A link to further documentation on the error.
        /// </summary>
        public string doc_url { get; set; }

        /// <summary>
        /// A unique identifier of the request to use in contact with support.
        /// </summary>
        public string request_id { get; set; }

        /// <summary>
        /// A unique identifier for tracing the request through MyriaLeaderboard systems, use this in contact with support.
        /// </summary>
        public string trace_id { get; set; }

        /// <summary>
        /// If the request was not a success this property will hold any error messages
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// An easy way of debugging MyriaLeaderboardErrorData class, example: Debug.Log(onComplete.errorData);
        /// </summary>
        /// <returns>string used to debug errors</returns>
        public override string ToString()
        {
            // Empty error, make sure we print something
            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(trace_id) && string.IsNullOrEmpty(request_id))
            {
                return $"An unexpected MyriaLeaderboard error without error data occurred. Please try again later.\n If the issue persists, please contact MyriaLeaderboard support.";
            }

            //Prinost important info first
            string prettyError = $"MyriaLeaderboard Error: \"{message}\"";

            // Look for intermittent, non user errors
            if (!string.IsNullOrEmpty(code) && code.StartsWith("HTTP5"))
            {
                prettyError +=
                    $"\nTry again later. If the issue persists, please contact MyriaLeaderboard support and provide the following error details:\n trace ID - \"{trace_id}\",\n request ID - \"{request_id}\",\n message - \"{message}\".";
                if (!string.IsNullOrEmpty(doc_url))
                {
                    prettyError += $"\nFor more information, see {doc_url} (error code was \"{code}\").";
                }
            }
            // Print user errors
            else
            {
                prettyError +=
                    $"\nThere was a problem with your request. The error message provides information on the problem and will help you fix it.";
                if (!string.IsNullOrEmpty(doc_url))
                {
                    prettyError += $"\nFor more information, see {doc_url} (error code was \"{code}\").";
                }

                prettyError +=
                    $"\nIf you are unable to fix the issue, contact MyriaLeaderboard support and provide the following error details:";
                if (!string.IsNullOrEmpty(trace_id))
                {
                    prettyError += $"\n     trace ID - \"{trace_id}\"";
                }
                if (!string.IsNullOrEmpty(request_id))
                {
                    prettyError += $"\n     request ID - \"{request_id}\"";
                }

                prettyError += $"\n     message - \"{message}\".";
            }
            return prettyError;
        }
    }

    /// <summary>
    /// All ServerAPI.SendRequest responses will invoke the callback using an instance of this class for easier handling in client code.
    /// </summary>
    public class MyriaLeaderboardResponse
    {
        /// <summary>
        /// HTTP Status Code
        /// </summary>
        public int statusCode { get; set; }

        /// <summary>
        /// Whether this request was a success
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Raw text/http body from the server response
        /// </summary>
        public string text { get; set; }

        /// <summary>
        /// If this request was not a success, this structure holds all the information needed to identify the problem
        /// </summary>
        public MyriaLeaderboardErrorData errorData { get; set; }

        /// <summary>
        /// inheritdoc added this because unity main thread executing style cut the calling stack and make the event orphan see also calling multiple events 
        /// of the same type makes use unable to identify each one
        /// </summary>
        public string EventId { get; set; }

        public static void Deserialize<T>(Action<T> onComplete, MyriaLeaderboardResponse serverResponse,
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
            JsonSerializerSettings options = null
#else //MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
            JsonOptions options = null
#endif
            )
            where T : MyriaLeaderboardResponse, new()
        {
            onComplete?.Invoke(Deserialize<T>(serverResponse, options));
        }

        public static T Deserialize<T>(MyriaLeaderboardResponse serverResponse,
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
            JsonSerializerSettings options = null
#else //MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
            JsonOptions options = null
#endif
            )
            where T : MyriaLeaderboardResponse, new()
        {
            if (serverResponse == null)
            {
                return MyriaLeaderboardResponseFactory.ClientError<T>("Unknown error, please check your internet connection.");
            }
            else if (serverResponse.errorData != null)
            {
                return new T() { success = false, errorData = serverResponse.errorData, statusCode = serverResponse.statusCode, text = serverResponse.text };
            }

            var response = MyriaLeaderboardJson.DeserializeObject<T>(serverResponse.text, options ?? MyriaLeaderboardJsonSettings.Default) ?? new T();
            response.text = serverResponse.text;
            response.success = serverResponse.success;
            response.errorData = serverResponse.errorData;
            response.statusCode = serverResponse.statusCode;

            return response;
        }
    }

    public class PaginationMetaResponse
    {
        /// <summary>
        /// HTTP Status Code
        /// </summary>
        public int totalItems { get; set; }

        /// <summary>
        /// Whether this request was a success
        /// </summary>
        public int itemCount { get; set; }

        /// <summary>
        /// Raw text/http body from the server response
        /// </summary>
        public int itemsPerPage { get; set; }

        /// <summary>
        /// Raw text/http body from the server response
        /// </summary>
        public int totalPages { get; set; }

        /// <summary>
        /// Raw text/http body from the server response
        /// </summary>
        public int currentPage { get; set; }
    }

        public class MyriaLeaderboardPaginationResponse<TKey>
    {
        /// <summary>
        /// The total available items in this list
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// The cursor that points to the next item in the list. Use this in subsequent requests to get additional items from the list.
        /// </summary>
        public TKey next_cursor { get; set; }
        /// <summary>
        /// The cursor that points to the first item in this batch of items.
        /// </summary>
        public TKey previous_cursor { get; set; }
    }

    /// <summary>
    /// Convenience factory class for creating some responses that we use often.
    /// </summary>
    public class MyriaLeaderboardResponseFactory
    {
        /// <summary>
        /// Construct an error response from a network request to send to the client.
        /// </summary>
        public static T NetworkError<T>(string errorMessage, int httpStatusCode) where T : MyriaLeaderboardResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = httpStatusCode,
                errorData = new MyriaLeaderboardErrorData(httpStatusCode, errorMessage)
            };
        }

        /// <summary>
        /// Construct an error response from a client side error to send to the client.
        /// </summary>
        public static T ClientError<T>(string errorMessage) where T : MyriaLeaderboardResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = 0,
                errorData = new MyriaLeaderboardErrorData
                {
                    message = errorMessage,
                }
            };
        }

        /// <summary>
        /// Construct an error response for token expiration.
        /// </summary>
        public static T TokenExpiredError<T>() where T : MyriaLeaderboardResponse, new()
        {
            return NetworkError<T>("Token Expired", 401);
        }

        /// <summary>
        /// Construct an error response specifically when the SDK has not been initialized.
        /// </summary>
        public static T SDKNotInitializedError<T>() where T : MyriaLeaderboardResponse, new()
        {
            return ClientError<T>("The MyriaLeaderboard SDK has not been initialized, please start a session to call this method");
        }

        /// <summary>
        /// Construct an error response because an unserializable input has been given
        /// </summary>
        public static T InputUnserializableError<T>() where T : MyriaLeaderboardResponse, new()
        {
            return ClientError<T>("Method parameter could not be serialized");
        }

        /// <summary>
        /// Construct an error response because the rate limit has been hit
        /// </summary>
        public static T RateLimitExceeded<T>(string method, int secondsLeftOfRateLimit) where T : MyriaLeaderboardResponse, new()
        {
            return ClientError<T>($"Your request to {method} was not sent. You are sending too many requests and are being rate limited for {secondsLeftOfRateLimit} seconds");
        }
    }



    #region Rate Limiting Support

    public class RateLimiter
    {
        /* -- Configurable constants -- */
        // Tripwire settings, allow for a max total of n requests per x seconds
        protected const int TripWireTimeFrameSeconds = 60;
        protected const int MaxRequestsPerTripWireTimeFrame = 280;
        protected const int SecondsPerBucket = 5; // Needs to evenly divide the time frame

        // Moving average settings, allow for a max average of n requests per x seconds
        protected const float AllowXPercentOfTripWireMaxForMovingAverage = 0.8f; // Moving average threshold (the average number of requests per bucket) is set slightly lower to stop constant abusive call behaviour just under the tripwire limit
        protected const int CountMovingAverageAcrossNTripWireTimeFrames = 3; // Count Moving average across a longer time period

        /* -- Calculated constants -- */
        protected const int BucketsPerTimeFrame = TripWireTimeFrameSeconds / SecondsPerBucket;
        protected const int RateLimitMovingAverageBucketCount = CountMovingAverageAcrossNTripWireTimeFrames * BucketsPerTimeFrame;
        private const int MaxRequestsPerBucketOnMovingAverage = (int)((MaxRequestsPerTripWireTimeFrame * AllowXPercentOfTripWireMaxForMovingAverage) / (BucketsPerTimeFrame));


        /* -- Functionality -- */
        protected readonly int[] buckets = new int[RateLimitMovingAverageBucketCount];

        protected int lastBucket = -1;
        private DateTime _lastBucketChangeTime = DateTime.MinValue;
        private int _totalRequestsInBuckets;
        private int _totalRequestsInBucketsInTripWireTimeFrame;

        protected bool isRateLimited = false;
        private DateTime _rateLimitResolvesAt = DateTime.MinValue;

        protected virtual DateTime GetTimeNow()
        {
            return DateTime.Now;
        }

        public int GetSecondsLeftOfRateLimit()
        {
            if (!isRateLimited)
            {
                return 0;
            }
            return (int)Math.Ceiling((_rateLimitResolvesAt - GetTimeNow()).TotalSeconds);
        }
        private int MoveCurrentBucket(DateTime now)
        {
            int moveOverXBuckets = _lastBucketChangeTime == DateTime.MinValue ? 1 : (int)Math.Floor((now - _lastBucketChangeTime).TotalSeconds / SecondsPerBucket);
            if (moveOverXBuckets == 0)
            {
                return lastBucket;
            }

            for (int stepIndex = 1; stepIndex <= moveOverXBuckets; stepIndex++)
            {
                int bucketIndex = (lastBucket + stepIndex) % buckets.Length;
                if (bucketIndex == lastBucket)
                {
                    continue;
                }
                int bucketMovingOutOfTripWireTimeFrame = (bucketIndex - BucketsPerTimeFrame) < 0 ? buckets.Length + (bucketIndex - BucketsPerTimeFrame) : bucketIndex - BucketsPerTimeFrame;
                _totalRequestsInBucketsInTripWireTimeFrame -= buckets[bucketMovingOutOfTripWireTimeFrame]; // Remove the request count from the bucket that is moving out of the time frame from trip wire count
                _totalRequestsInBuckets -= buckets[bucketIndex]; // Remove the count from the bucket we're moving into from the total before emptying it
                buckets[bucketIndex] = 0;
            }

            return (lastBucket + moveOverXBuckets) % buckets.Length; // Step to next bucket and wrap around if necessary;
        }

        public virtual bool AddRequestAndCheckIfRateLimitHit()
        {
            DateTime now = GetTimeNow();
            var currentBucket = MoveCurrentBucket(now);

            if (isRateLimited)
            {
                if (_totalRequestsInBuckets <= 0)
                {
                    isRateLimited = false;
                    _rateLimitResolvesAt = DateTime.MinValue;
                }
            }
            else
            {
                buckets[currentBucket]++; // Increment the current bucket
                _totalRequestsInBuckets++; // Increment the total request count
                _totalRequestsInBucketsInTripWireTimeFrame++; // Increment the request count for the current time frame

                isRateLimited |= _totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame; // If the request count for the time frame is greater than the max requests per time frame, set isRateLimited to true
                isRateLimited |= _totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage; // If the average number of requests per bucket is greater than the max requests on moving average, set isRateLimited to true
#if UNITY_EDITOR
                if (_totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame) MyriaLeaderboardLogger.GetForLogLevel()("Rate Limit Hit due to Trip Wire, count = " + _totalRequestsInBucketsInTripWireTimeFrame + " out of allowed " + MaxRequestsPerTripWireTimeFrame);
                if (_totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage) MyriaLeaderboardLogger.GetForLogLevel()("Rate Limit Hit due to Moving Average, count = " + _totalRequestsInBuckets / RateLimitMovingAverageBucketCount + " out of allowed " + MaxRequestsPerBucketOnMovingAverage);
#endif
                if (isRateLimited)
                {
                    _rateLimitResolvesAt = (now - TimeSpan.FromSeconds(now.Second % SecondsPerBucket)) + TimeSpan.FromSeconds(buckets.Length * SecondsPerBucket);
                }
            }
            if (currentBucket != lastBucket)
            {
                _lastBucketChangeTime = now;
                lastBucket = currentBucket;
            }
            return isRateLimited;
        }

        protected int GetMaxRequestsInSingleBucket()
        {
            int maxRequests = 0;
            foreach (var t in buckets)
            {
                maxRequests = Math.Max(maxRequests, t);
            }

            return maxRequests;
        }

        private static RateLimiter _rateLimiter = null;

        public static RateLimiter Get()
        {
            if (_rateLimiter == null)
            {
                Reset();
            }
            return _rateLimiter;
        }

        public static void Reset()
        {
            _rateLimiter = new RateLimiter();
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)("Reset RateLimiter due to entering play mode");
            Reset();
        }
#endif
    }
    #endregion

    /// <summary>
    /// Construct a request to send to the server.
    /// </summary>
    [Serializable]
    public struct MyriaLeaderboardServerRequest
    {
        public string endpoint { get; set; }
        public MyriaLeaderboardHTTPMethod httpMethod { get; set; }
        public Dictionary<string, object> payload { get; set; }
        public string jsonPayload { get; set; }
        public byte[] upload { get; set; }
        public string uploadName { get; set; }
        public string uploadType { get; set; }
        public MyriaLeaderboardCallerRole callerRole { get; set; }
        public WWWForm form { get; set; }

        /// <summary>
        /// Leave this null if you don't need custom headers
        /// </summary>
        public Dictionary<string, string> extraHeaders;

        /// <summary>
        /// Query parameters to append to the end of the request URI
        /// Example: If you include a dictionary with a key of "page" and a value of "42" (as a string) then the url would become "https://mydomain.com/endpoint?page=42"
        /// </summary>
        public Dictionary<string, string> queryParams;

        public int retryCount { get; set; }

        #region Make ServerRequest and call send (3 functions)

        public static void CallAPI(string endPoint, MyriaLeaderboardHTTPMethod httpMethod, string body = null, Action<MyriaLeaderboardResponse> onComplete = null, bool useAuthToken = true, MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole callerRole = MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            Debug.Log("json" + body );
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.RateLimitExceeded<MyriaLeaderboardResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }

#if UNITY_EDITOR
            MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Verbose)("Caller Type: " + callerRole);
#endif

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();
                if (callerRole == MyriaLeaderboardCallerRole.Admin)
                {
#if UNITY_EDITOR
                    if (!string.IsNullOrEmpty(MyriaLeaderboardConfig.current.adminToken))
                    {
                        headers.Add("x-auth-token", MyriaLeaderboardConfig.current.adminToken);
                    }
#endif
                }
                else if (!string.IsNullOrEmpty(MyriaLeaderboardConfig.current.token))
                {
                    headers.Add("x-session-token", MyriaLeaderboardConfig.current.token);
                }
            }

            if (MyriaLeaderboardConfig.current != null)
                headers.Add("x-api-developer-key", MyriaLeaderboardConfig.current.xDeveloperApiKey);
                headers.Add(MyriaLeaderboardConfig.current.dateVersion.key, MyriaLeaderboardConfig.current.dateVersion.value);

            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
            }

            new MyriaLeaderboardServerRequest(endPoint, httpMethod, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }

//        public static void CallDomainAuthAPI(string endPoint, MyriaLeaderboardHTTPMethod httpMethod, string body = null, Action<MyriaLeaderboardResponse> onComplete = null)
//        {
//            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
//            {
//                onComplete?.Invoke(MyriaLeaderboardResponseFactory.RateLimitExceeded<MyriaLeaderboardResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
//                return;
//            }

//            if (MyriaLeaderboardConfig.current.domainKey.ToString().Length == 0)
//            {
//#if UNITY_EDITOR
//                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)("MyriaLeaderboard domain key must be set in settings");
//#endif
//                onComplete?.Invoke(MyriaLeaderboardResponseFactory.ClientError<MyriaLeaderboardResponse>("MyriaLeaderboard domain key must be set in settings"));

//                return;
//            }

//            Dictionary<string, string> headers = new Dictionary<string, string>();
//            headers.Add("domain-key", MyriaLeaderboardConfig.current.domainKey);

//            new MyriaLeaderboardServerRequest(endPoint, httpMethod, body, headers, callerRole: MyriaLeaderboardCallerRole.Base).Send((response) => { onComplete?.Invoke(response); });
//        }

        #endregion

        #region ServerRequest constructor

        public MyriaLeaderboardServerRequest(string endpoint, MyriaLeaderboardHTTPMethod httpMethod = MyriaLeaderboardHTTPMethod.GET, byte[] upload = null, string uploadName = null, string uploadType = null, Dictionary<string, string> body = null,
            Dictionary<string, string> extraHeaders = null, bool useAuthToken = true, MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole callerRole = MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.User, bool isFileUpload = true)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = null;
            this.upload = upload;
            this.uploadName = uploadName;
            this.uploadType = uploadType;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = null;
            this.callerRole = callerRole;
            this.form = new WWWForm();

            foreach (var kvp in body)
            {
                this.form.AddField(kvp.Key, kvp.Value);
            }

            this.form.AddBinaryData("file", upload, uploadName);

            bool isNonPayloadMethod = (this.httpMethod == MyriaLeaderboardHTTPMethod.GET || this.httpMethod == MyriaLeaderboardHTTPMethod.HEAD || this.httpMethod == MyriaLeaderboardHTTPMethod.OPTIONS);

            if (this.payload != null && isNonPayloadMethod)
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        public MyriaLeaderboardServerRequest(string endpoint, MyriaLeaderboardHTTPMethod httpMethod = MyriaLeaderboardHTTPMethod.GET, Dictionary<string, object> payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null,
            bool useAuthToken = true, MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole callerRole = MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = payload != null && payload.Count == 0 ? null : payload; //Force payload to null if an empty dictionary was supplied
            this.upload = null;
            this.uploadName = null;
            this.uploadType = null;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.callerRole = callerRole;
            bool isNonPayloadMethod = (this.httpMethod == MyriaLeaderboardHTTPMethod.GET || this.httpMethod == MyriaLeaderboardHTTPMethod.HEAD || this.httpMethod == MyriaLeaderboardHTTPMethod.OPTIONS);
            this.form = null;
            if (this.payload != null && isNonPayloadMethod)
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        public MyriaLeaderboardServerRequest(string endpoint, MyriaLeaderboardHTTPMethod httpMethod = MyriaLeaderboardHTTPMethod.GET, string payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true,
            MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole callerRole = MyriaLeaderboard.MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.jsonPayload = payload;
            this.upload = null;
            this.uploadName = null;
            this.uploadType = null;
            this.payload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.callerRole = callerRole;
            bool isNonPayloadMethod = (this.httpMethod == MyriaLeaderboardHTTPMethod.GET || this.httpMethod == MyriaLeaderboardHTTPMethod.HEAD || this.httpMethod == MyriaLeaderboardHTTPMethod.OPTIONS);
            this.form = null;
            if (!string.IsNullOrEmpty(jsonPayload) && isNonPayloadMethod)
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        #endregion

        /// <summary>
        /// just debug and call ServerAPI.SendRequest which takes the current ServerRequest and pass this response
        /// </summary>
        public void Send(System.Action<MyriaLeaderboardResponse> OnServerResponse)
        {
            MyriaLeaderboardServerApi.SendRequest(this, (response) => { OnServerResponse?.Invoke(response); });
        }
    }
}