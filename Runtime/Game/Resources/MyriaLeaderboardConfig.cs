using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
#endif
using UnityEngine;
using MyriaLeaderboard.MyriaLeaderboardEnums;

namespace MyriaLeaderboard.MyriaLeaderboardEnums
{
    public enum MyriaEnvironment { Prod, PreProd, Staging, Dev };
}

namespace MyriaLeaderboard
{
    public class MyriaLeaderboardConfig : ScriptableObject
    {

        private static MyriaLeaderboardConfig settingsInstance;

        public virtual string SettingName { get { return "MyriaLeaderboardConfig"; } }

        public static MyriaLeaderboardConfig Get()
        {
            if (settingsInstance != null)
            {
                settingsInstance.ConstructUrls();
#if MYRIA_LEADERBOARD_COMMANDLINE_SETTINGS
                settingsInstance.CheckForSettingOverrides();
#endif
                return settingsInstance;
            }

            //Try to load it
            settingsInstance = Resources.Load<MyriaLeaderboardConfig>("Config/MyriaLeaderboardConfig");

#if UNITY_EDITOR
            // Could not be loaded, create it
            if (settingsInstance == null)
            {
                // Create a new Config
                MyriaLeaderboardConfig newConfig = ScriptableObject.CreateInstance<MyriaLeaderboardConfig>();

                // Folder needs to exist for Unity to be able to create an asset in it
                string dir = Application.dataPath+ "/MyriaLeaderboardSDK/Resources/Config";

                // If directory does not exist, create it
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Create config asset
                string configAssetPath = "Assets/MyriaLeaderboardSDK/Resources/Config/MyriaLeaderboardConfig.asset";
                AssetDatabase.CreateAsset(newConfig, configAssetPath);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
                AssetDatabase.Refresh();
                settingsInstance = newConfig;
            }

#else
            if (settingsInstance == null)
            {
                throw new ArgumentException("MyriaLeaderboard config does not exist. To fix this, play once in the Unity Editor before making a build.");
            }
#endif
            settingsInstance.ConstructUrls();
#if MYRIA_LEADERBOA_COMMANDLINE_SETTINGS
            settingsInstance.CheckForSettingOverrides();
#endif
            return settingsInstance;
        }

        private void CheckForSettingOverrides()
        {
#if MYRIA_LEADERBOARD_COMMANDLINE_SETTINGS
            string[] args = System.Environment.GetCommandLineArgs();
            string _xDeveloperApiKey = null;
            string _baseUrl = null;
            string _domainKey = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-xDeveloperApiKey")
                {
                    _xDeveloperApiKey = args[i + 1];
                }
                else if (args[i] == "-domainkey")
                {
                    _domainKey = args[i + 1];
                }
                else if (args[i] == "-baseURL")
                {
                    _baseURL = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(_xDeveloperApiKey) || string.IsNullOrEmpty(_domainKey) || string.IsNullOrEmpty(_baseURL))
            {
                return;
            }
            xDeveloperApiKey = _xDeveloperApiKey;
            domainKey = _domainKey;
            baseURL = _baseURL;
#endif
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void CreateConfigFile()
        {

            // Get the path to the project directory
            string projectPath = Application.dataPath;

            // Use the Directory class to get the creation time of the project directory
            DateTime creationTime = Directory.GetCreationTime(projectPath);
            string configFileEditorPref = "configFileCreated" + creationTime.GetHashCode().ToString();

            if (EditorPrefs.GetBool(configFileEditorPref) == false)
            {
                // Create config file instantly when SDK has been installed
                Get();
                EditorPrefs.SetBool(configFileEditorPref, true);
            }
        }

        protected static ListRequest ListInstalledPackagesRequest;

        [InitializeOnLoadMethod]
        static void StoreSDKVersion()
        {
            if ((!string.IsNullOrEmpty(MyriaLeaderboardConfig.current.sdk_version) &&
                 !MyriaLeaderboardConfig.current.sdk_version.Equals("N/A")) || ListInstalledPackagesRequest != null)
            {
                return;
            }
            ListInstalledPackagesRequest = Client.List();
            EditorApplication.update += ListInstalledPackagesRequestProgress;
        }

        [Serializable]
        private class LLPackageDescription
        {
            public string name { get; set; }
            public string version { get; set; }
        }

        static void ListInstalledPackagesRequestProgress()
        {
            if (ListInstalledPackagesRequest.IsCompleted)
            {
                EditorApplication.update -= ListInstalledPackagesRequestProgress;
                foreach (var package in ListInstalledPackagesRequest.Result)
                {
                    if (package.name.Equals("com.lootlocker.myrialeaderboardsdk"))
                    {
                        MyriaLeaderboardConfig.current.sdk_version = package.version;
                        return;
                    }
                }

                if (File.Exists("Assets/MyriaLeaderboardSDK/package.json"))
                {
                    MyriaLeaderboardConfig.current.sdk_version = MyriaLeaderboardJson.DeserializeObject<LLPackageDescription>(File.ReadAllText("Assets/MyriaLeaderboardSDK/package.json")).version;
                    return;
                }


                foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
                {
                    if (assetPath.EndsWith("package.json"))
                    {
                        var packageDescription = MyriaLeaderboardJson.DeserializeObject<LLPackageDescription>(File.ReadAllText(assetPath));
                        if (!string.IsNullOrEmpty(packageDescription.name) && packageDescription.name.Equals("com.lootlocker.myrialeaderboardsdk"))
                        {
                            MyriaLeaderboardConfig.current.sdk_version = packageDescription.version;
                            return;
                        }
                    }
                }

                MyriaLeaderboardConfig.current.sdk_version = "N/A";
            }
        }
#endif
        public static bool CreateNewSettings(string xDeveloperApiKey, MyriaEnvironment env, string gameVersion, string domainKey, MyriaLeaderboardConfig.DebugLevel debugLevel = DebugLevel.All, string baseURLParam = null, bool allowTokenRefresh = false)
        {
            _current = Get();

            _current.xDeveloperApiKey = xDeveloperApiKey;
            //_current.baseUrl = string.IsNullOrEmpty(baseURLParam) ? _current.baseUrl : baseURLParam;
            _current.env = env;
            _current.game_version = gameVersion;
            _current.currentDebugLevel = debugLevel;
            _current.allowTokenRefresh = allowTokenRefresh;
            _current.domainKey = domainKey;
            _current.ConstructUrls();
            return true;
        }

        private void ConstructUrls()
        {
            string startOfUrl = "";
            switch(env)
            {
                case MyriaEnvironment.Prod:
                    startOfUrl = "https://prod.myriaverse-leaderboard-api.nonprod-myria.com";
                    break;
                case MyriaEnvironment.PreProd:
                    startOfUrl = "https://preprod.myriaverse-leaderboard-api.nonprod-myria.com";
                    break;
                case MyriaEnvironment.Staging:
                    startOfUrl = "https://staging.myriaverse-leaderboard-api.nonprod-myria.com";
                    break;
                case MyriaEnvironment.Dev:
                    startOfUrl = "https://staging.myriaverse-leaderboard-api.nonprod-myria.com";
                    break;
                default:
                    startOfUrl = "https://staging.myriaverse-leaderboard-api.nonprod-myria.com";
                    break;
            }
            if (!string.IsNullOrEmpty(domainKey))
            {
                startOfUrl += domainKey + ".";
            }
            adminUrl = startOfUrl + AdminUrlAppendage;
            playerUrl = startOfUrl + PlayerUrlAppendage;
            userUrl = startOfUrl + UserUrlAppendage;
            url = startOfUrl;
        }

        private static MyriaLeaderboardConfig _current;

        public static MyriaLeaderboardConfig current
        {
            get
            {
                if (_current == null)
                {
                    _current = Get();
                }

                return _current;
            }
        }

        public (string key, string value) dateVersion = ("LL-Version", "2021-03-01");
        public string xDeveloperApiKey;
        public MyriaEnvironment env;
        [HideInInspector]
        public string token;
#if UNITY_EDITOR
        [HideInInspector]
        public string adminToken;
#endif
        [HideInInspector]
        public string refreshToken;
        [HideInInspector]
        public string domainKey;
        [HideInInspector]
        public int gameID;
        public string game_version = "1.0.0.0";
        [HideInInspector] 
        public string sdk_version = "";
        [HideInInspector]
        public string deviceID = "defaultPlayerId";

        [HideInInspector] private static readonly string UrlCoreOverride =
#if MYRIA_LEADERBOARD_TARGET_STAGE_ENV
           "api.stage.internal.dev.lootlocker.cloud";
#else
            null;
#endif
        private static string GetUrlCore() { return string.IsNullOrEmpty(UrlCoreOverride) ? baseUrl : UrlCoreOverride; }


        [HideInInspector] private static readonly string UrlAppendage = "/v1";
        [HideInInspector] private static readonly string AdminUrlAppendage = "/admin";
        [HideInInspector] private static readonly string PlayerUrlAppendage = "/player";
        [HideInInspector] private static readonly string UserUrlAppendage = "/game";

        [HideInInspector] public static string baseUrl = "";

        [HideInInspector] public string url = baseUrl + UrlAppendage;
        [HideInInspector] public string adminUrl = baseUrl + AdminUrlAppendage;
        [HideInInspector] public string playerUrl = baseUrl + PlayerUrlAppendage;
        [HideInInspector] public string userUrl = baseUrl + UserUrlAppendage;
        [HideInInspector] public float clientSideRequestTimeOut = 180f;
        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off , AllAsNormal}
        public DebugLevel currentDebugLevel = DebugLevel.All;
        public bool allowTokenRefresh = true;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            _current = null;
        }
#endif
    }
}