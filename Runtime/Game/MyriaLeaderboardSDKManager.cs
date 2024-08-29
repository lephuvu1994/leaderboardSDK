using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using MyriaLeaderboard.MyriaLeaderboardEnums;
using System.Linq;
using System.Security.Cryptography;
#if MYRIA_LEADERBOARD_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using LLlibs.ZeroDepJson;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyriaLeaderboard.Requests
{
    public partial class MyriaLeaderboardSDKManager
    {
#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            initialized = false;
        }
#endif

        /// <summary>
        /// Stores which platform the player currently has a session for.
        /// </summary>
        public static string GetCurrentPlatform()
        {
            return CurrentPlatform.GetString();
        }

        #region Init
        static bool initialized;
        public static bool Init()
        {
            MyriaLeaderboardServerApi.Instantiate();
            return LoadConfig();
        }

        /// <summary>
        /// Manually initialize the SDK.
        /// </summary>
        /// <returns>True if initialized successfully, false otherwise</returns>
        public static bool Init(string xDeveloperApiKey, string gameVersion, string baseURL, MyriaEnvironment env, MyriaLeaderboardConfig.DebugLevel debugLevel)
        {
            MyriaLeaderboardServerApi.Instantiate();
            return MyriaLeaderboardConfig.CreateNewSettings(xDeveloperApiKey, env , gameVersion, baseURL, debugLevel);
        }

        static bool LoadConfig()
        {
            initialized = false;
            if (MyriaLeaderboardConfig.current == null)
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)("SDK could not find settings, please contact support \n You can also set config manually by calling Init(string apiKey, string gameVersion, bool onDevelopmentMode, string domainKey)");
                return false;
            }
            if (string.IsNullOrEmpty(MyriaLeaderboardConfig.current.xDeveloperApiKey))
            {
                MyriaLeaderboardLogger.GetForLogLevel(MyriaLeaderboardLogger.LogLevel.Error)("API Key has not been set, set it in project settings or manually calling Init(string apiKey, string gameVersion, bool onDevelopmentMode, string domainKey)");
                return false;
            }

            initialized = true;
            return initialized;
        }


        /// <summary>
        /// Checks if an active session exists.
        /// </summary>
        /// <returns>True if a token is found, false otherwise.</returns>
        private static bool CheckActiveSession()
        {
            //if (string.IsNullOrEmpty(MyriaLeaderboardConfig.current.token))
            //{
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// Utility function to check if the sdk has been initialized
        /// </summary>
        /// <returns>True if initialized, false otherwise.</returns>
        public static bool CheckInitialized(bool skipSessionCheck = false)
        {
            if (!initialized)
            {
                MyriaLeaderboardConfig.current.token = "";
                MyriaLeaderboardConfig.current.refreshToken = "";
                MyriaLeaderboardConfig.current.deviceID = "";
                if (!Init())
                {
                    return false;
                }
            }

            if (!skipSessionCheck && !CheckActiveSession())
            {
                return false;
            }

            return true;
        }

        #endregion

        #region ClearLocalSession
        /// <summary>
        /// Clears client session data. WARNING: This does not end the session in MyriaLeadeboard servers.
        /// </summary>
        public static void ClearLocalSession()
        {
            // Clear White Label Login credentials
            if (CurrentPlatform.Get() == Platforms.WhiteLabel)
            {
                PlayerPrefs.DeleteKey("MyriaLeadeboardWhiteLabelSessionToken");
                PlayerPrefs.DeleteKey("MyriaLeadeboardWhiteLabelSessionEmail");
            }

            CurrentPlatform.Reset();

            MyriaLeaderboardConfig.current.token = "";
            MyriaLeaderboardConfig.current.deviceID = "";
            MyriaLeaderboardConfig.current.refreshToken = "";
        }
        #endregion

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="after">How many after the last entry to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type MyriaLeaderboardGetScoreListResponse</param>
        public static void GetScoreList(string leaderboardId, int page, int limit, Action<MyriaLeaderboardGetScoreListResponse> onComplete, ESortingField? sortingField, EOrderBy? orderBy)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.SDKNotInitializedError<MyriaLeaderboardGetScoreListResponse>());
                return;
            }
            MyriaLeaderboardGetScoreListRequest request = new MyriaLeaderboardGetScoreListRequest();
            request.leaderboardKey = leaderboardId;
            request.limit = limit > 0 ? limit : 10;
            request.page = page > 0 ? page : 1;
            request.sortingField = sortingField.HasValue ? ESortingFieldExtensions.SortingToStringValue(sortingField.Value) : "createdAt";
            request.orderBy = orderBy.HasValue ? EOrderByExtensions.OderByToStringValue(orderBy.Value) : "DESC";
            Action<MyriaLeaderboardGetScoreListResponse> callback = (response) =>
            {
                MyriaLeaderboardGetScoreListResponse parsedData = MyriaLeaderboardGetScoreListResponse.Deserialize<MyriaLeaderboardGetScoreListResponse>(response);
                onComplete?.Invoke(parsedData);
            };
            MyriaLeaderboardAPIManager.GetScoreList(request, callback);
        }

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type MyriaLeaderboardGetScoreListResponse</param>
        public static void PostScore(string leaderboardId, MyriaLeaderboardPostScoreParams data, Action<MyriaLeaderboardPostScoreResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.SDKNotInitializedError<MyriaLeaderboardPostScoreResponse>());
                return;
            }


            MyriaLeaderboardPostScoreRequest request = new MyriaLeaderboardPostScoreRequest();
            request.leaderboardKey = leaderboardId;
            Action<MyriaLeaderboardPostScoreResponse> callback = (response) =>
            {
                onComplete?.Invoke(response);
            };
            Debug.Log("paramPostScore" + data.items[0].displayName);
            MyriaLeaderboardAPIManager.PostScores(request, data, callback);

            //MyriaLeaderboardServerRequest.CallAPI(MyriaLeaderboardEndPoints.MyriaEndPointPostScoreByLeaderboardId.endPoint, MyriaLeaderboardEndPoints.MyriaEndPointPostScoreByLeaderboardId.httpMethod, json, (serverResponse) => { MyriaLeaderboardResponse.Deserialize(onComplete, serverResponse); }, false, MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.Base);
        }

    }
}