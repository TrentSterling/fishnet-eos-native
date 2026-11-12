using System.Diagnostics;
using Epic.OnlineServices.Logging;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FishNet.Transport.EOSNative.Logging
{
    /// <summary>
    /// Static utility class for centralized debug logging.
    /// Respects EOSDebugSettings for per-category log filtering.
    /// Logs are stripped from release builds via Conditional attributes.
    /// </summary>
    public static class EOSDebugLogger
    {
        /// <summary>
        /// Log a debug message if the category is enabled.
        /// Stripped from release builds.
        /// </summary>
        /// <param name="category">The debug category.</param>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("EOS_DEBUG")]
        public static void Log(DebugCategory category, string className, string message)
        {
            if (EOSDebugSettings.Instance.IsCategoryEnabled(category))
            {
                Debug.Log($"[{className}] {message}");
            }
        }

        /// <summary>
        /// Log a debug message with an associated object if the category is enabled.
        /// Stripped from release builds.
        /// </summary>
        /// <param name="category">The debug category.</param>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object context for the log message.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("EOS_DEBUG")]
        public static void Log(DebugCategory category, string className, string message, UnityEngine.Object context)
        {
            if (EOSDebugSettings.Instance.IsCategoryEnabled(category))
            {
                Debug.Log($"[{className}] {message}", context);
            }
        }

        /// <summary>
        /// Log a warning message if the category is enabled.
        /// Stripped from release builds.
        /// </summary>
        /// <param name="category">The debug category.</param>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("EOS_DEBUG")]
        public static void LogWarning(DebugCategory category, string className, string message)
        {
            if (EOSDebugSettings.Instance.IsCategoryEnabled(category))
            {
                Debug.LogWarning($"[{className}] {message}");
            }
        }

        /// <summary>
        /// Log a warning message with an associated object if the category is enabled.
        /// Stripped from release builds.
        /// </summary>
        /// <param name="category">The debug category.</param>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object context for the log message.</param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("EOS_DEBUG")]
        public static void LogWarning(DebugCategory category, string className, string message, UnityEngine.Object context)
        {
            if (EOSDebugSettings.Instance.IsCategoryEnabled(category))
            {
                Debug.LogWarning($"[{className}] {message}", context);
            }
        }

        /// <summary>
        /// Log an error message. Errors are always logged regardless of settings.
        /// Not stripped from release builds - errors should always be visible.
        /// </summary>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        public static void LogError(string className, string message)
        {
            Debug.LogError($"[{className}] {message}");
        }

        /// <summary>
        /// Log an error message with an associated object.
        /// Errors are always logged regardless of settings.
        /// </summary>
        /// <param name="className">The name of the class logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object context for the log message.</param>
        public static void LogError(string className, string message, UnityEngine.Object context)
        {
            Debug.LogError($"[{className}] {message}", context);
        }

        /// <summary>
        /// Check if a category is enabled without logging.
        /// Useful for expensive logging operations that should be skipped entirely.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if the category is enabled.</returns>
        public static bool IsEnabled(DebugCategory category)
        {
            return EOSDebugSettings.Instance.IsCategoryEnabled(category);
        }

        #region EOS SDK Native Logging

        /// <summary>
        /// Applies the SDK log level settings from EOSDebugSettings to the EOS LoggingInterface.
        /// Call this after PlatformInterface.Initialize() and before CreatePlatformInterface().
        /// </summary>
        public static void ApplyEOSSDKLogLevels()
        {
            var settings = EOSDebugSettings.Instance;
            if (!settings.EOSSDKLoggingEnabled)
            {
                // Disable all SDK logging
                LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Off);
                return;
            }

            // Set global default level
            LogLevel defaultLevel = MapLogLevel(settings.EOSSDKDefaultLogLevel);
            LoggingInterface.SetLogLevel(LogCategory.AllCategories, defaultLevel);

            // Apply per-category overrides
            var overrides = settings.EOSSDKCategoryOverrides;
            if (overrides != null)
            {
                for (int i = 0; i < overrides.Length; i++)
                {
                    if (!overrides[i].Enabled || string.IsNullOrEmpty(overrides[i].CategoryName))
                        continue;

                    if (TryParseSDKCategory(overrides[i].CategoryName, out LogCategory category))
                    {
                        LoggingInterface.SetLogLevel(category, MapLogLevel(overrides[i].Level));
                    }
                }
            }

            // Register the callback to route SDK logs to Unity console
            if (settings.EOSSDKRouteToUnityConsole)
            {
                LoggingInterface.SetCallback(OnEOSSDKLogMessage);
            }
        }

        /// <summary>
        /// Callback from the EOS SDK's LoggingInterface. Routes SDK log messages to the Unity console.
        /// </summary>
        private static void OnEOSSDKLogMessage(LogMessage message)
        {
            string formatted = $"[EOS:{message.Category}] {message.Message}";

            switch (message.Level)
            {
                case LogLevel.Fatal:
                case LogLevel.Error:
                    Debug.LogError(formatted);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                default:
                    Debug.Log(formatted);
                    break;
            }
        }

        /// <summary>
        /// Maps our serializable enum to the SDK's LogLevel enum.
        /// </summary>
        private static LogLevel MapLogLevel(EOSSDKLogLevel level)
        {
            switch (level)
            {
                case EOSSDKLogLevel.Off:         return LogLevel.Off;
                case EOSSDKLogLevel.Fatal:       return LogLevel.Fatal;
                case EOSSDKLogLevel.Error:       return LogLevel.Error;
                case EOSSDKLogLevel.Warning:     return LogLevel.Warning;
                case EOSSDKLogLevel.Info:        return LogLevel.Info;
                case EOSSDKLogLevel.Verbose:     return LogLevel.Verbose;
                case EOSSDKLogLevel.VeryVerbose: return LogLevel.VeryVerbose;
                default:                         return LogLevel.Warning;
            }
        }

        /// <summary>
        /// Parses an SDK category name string into a LogCategory enum value.
        /// </summary>
        private static bool TryParseSDKCategory(string name, out LogCategory category)
        {
            switch (name.ToLowerInvariant())
            {
                case "core":               category = LogCategory.Core; return true;
                case "auth":               category = LogCategory.Auth; return true;
                case "friends":            category = LogCategory.Friends; return true;
                case "presence":           category = LogCategory.Presence; return true;
                case "userinfo":           category = LogCategory.UserInfo; return true;
                case "httpserialization":   category = LogCategory.HttpSerialization; return true;
                case "ecom":               category = LogCategory.Ecom; return true;
                case "p2p":                category = LogCategory.P2P; return true;
                case "sessions":           category = LogCategory.Sessions; return true;
                case "ratelimiter":        category = LogCategory.RateLimiter; return true;
                case "playerdatastorage":  category = LogCategory.PlayerDataStorage; return true;
                case "analytics":          category = LogCategory.Analytics; return true;
                case "messaging":          category = LogCategory.Messaging; return true;
                case "connect":            category = LogCategory.Connect; return true;
                case "overlay":            category = LogCategory.Overlay; return true;
                case "ui":                 category = LogCategory.UI; return true;
                case "lobby":              category = LogCategory.Lobby; return true;
                case "leaderboards":       category = LogCategory.Leaderboards; return true;
                case "keychain":           category = LogCategory.Keychain; return true;
                case "integratedplatform": category = LogCategory.IntegratedPlatform; return true;
                case "titlestorage":       category = LogCategory.TitleStorage; return true;
                case "mods":               category = LogCategory.Mods; return true;
                case "anticheat":          category = LogCategory.AntiCheat; return true;
                case "stats":              category = LogCategory.Stats; return true;
                case "achievements":       category = LogCategory.Achievements; return true;
                case "reports":            category = LogCategory.Reports; return true;
                case "sanctions":          category = LogCategory.Sanctions; return true;
                case "progressionsnapshots": category = LogCategory.ProgressionSnapshots; return true;
                case "kws":                category = LogCategory.KWS; return true;
                case "rtc":                category = LogCategory.RTC; return true;
                case "rtcadmin":           category = LogCategory.RTCAdmin; return true;
                case "custominvites":      category = LogCategory.CustomInvites; return true;
                case "matchmaking":        category = LogCategory.Matchmaking; return true;
                default:
                    category = LogCategory.Core;
                    return false;
            }
        }

        #endregion
    }
}
