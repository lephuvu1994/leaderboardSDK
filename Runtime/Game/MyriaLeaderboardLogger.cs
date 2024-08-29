using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyriaLeaderboard
{
    public class MyriaLeaderboardLogger
    {
        public enum LogLevel
        {
            Verbose
            , Info
            , Warning
            , Error
        }

        /// <summary>
        /// Get logger for the specified level. Use like: GetForLevel(LogLevel::Info)(message);
        /// </summary>
        /// <param name="logLevel">What level should this be logged as</param>
        public static Action<string> GetForLogLevel(LogLevel logLevel = LogLevel.Info)
        {
            if (!ShouldLog(logLevel))
            {
                return ignored => { };
            }

            AdjustLogLevelToSettings(ref logLevel);

            switch (logLevel)
            {
                case LogLevel.Error:
                    return Debug.LogError;
                case LogLevel.Warning:
                    return Debug.LogWarning;
                case LogLevel.Verbose:
                case LogLevel.Info:
                default:
                    return Debug.Log;
            }
        }

        private static bool ShouldLog(LogLevel logLevel)
        {
#if UNITY_EDITOR
            switch (logLevel)
            {
                case LogLevel.Error:
                {
                    if (MyriaLeaderboardConfig.current == null ||
                        (new List<MyriaLeaderboardConfig.DebugLevel>
                        {
                            MyriaLeaderboardConfig.DebugLevel.All, 
                            MyriaLeaderboardConfig.DebugLevel.AllAsNormal,
                            MyriaLeaderboardConfig.DebugLevel.ErrorOnly
                        }).Contains(MyriaLeaderboardConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Warning:
                {
                    if (MyriaLeaderboardConfig.current == null ||
                        (new List<MyriaLeaderboardConfig.DebugLevel>
                        {
                            MyriaLeaderboardConfig.DebugLevel.All,
                            MyriaLeaderboardConfig.DebugLevel.AllAsNormal
                        })
                        .Contains(MyriaLeaderboardConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Verbose:
                {
                    if (MyriaLeaderboardConfig.current == null ||
                        (new List<MyriaLeaderboardConfig.DebugLevel>
                        {
                            MyriaLeaderboardConfig.DebugLevel.All, 
                            MyriaLeaderboardConfig.DebugLevel.AllAsNormal
                        })
                        .Contains(MyriaLeaderboardConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Info:
                default:
                {
                    if (MyriaLeaderboardConfig.current == null ||
                        (new List<MyriaLeaderboardConfig.DebugLevel>
                        {
                            MyriaLeaderboardConfig.DebugLevel.All, 
                            MyriaLeaderboardConfig.DebugLevel.AllAsNormal,
                            MyriaLeaderboardConfig.DebugLevel.NormalOnly
                        }).Contains(MyriaLeaderboardConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
            }
#endif

            return false;
        }

        private static void AdjustLogLevelToSettings(ref LogLevel logLevel)
        {
            if (MyriaLeaderboardConfig.current != null && MyriaLeaderboardConfig.DebugLevel.AllAsNormal == MyriaLeaderboardConfig.current.currentDebugLevel)
            {
                logLevel = LogLevel.Info;
            }
        }
    }
}