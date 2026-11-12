using System;
using UnityEngine;

namespace FishNet.Transport.EOSNative.Logging
{
    /// <summary>
    /// Log level for the EOS SDK's native logging system (LoggingInterface).
    /// Mirrors Epic.OnlineServices.Logging.LogLevel for safe serialization.
    /// </summary>
    public enum EOSSDKLogLevel
    {
        Off = 0,
        Fatal = 1,
        Error = 2,
        Warning = 3,
        Info = 4,
        Verbose = 5,
        VeryVerbose = 6
    }

    /// <summary>
    /// A per-category log level override for the EOS SDK's native logging.
    /// </summary>
    [Serializable]
    public struct EOSSDKCategoryOverride
    {
        [Tooltip("SDK log category name (e.g. P2P, Lobby, RTC).")]
        public string CategoryName;

        [Tooltip("Log level for this category.")]
        public EOSSDKLogLevel Level;

        [Tooltip("Whether this override is active.")]
        public bool Enabled;
    }

    /// <summary>
    /// Flags enum representing all debug categories in the EOS Native transport.
    /// Each flag corresponds to a specific system that can be individually toggled for logging.
    /// </summary>
    [Flags]
    public enum DebugCategory
    {
        None = 0,

        // Core (6)
        EOSManager = 1 << 0,
        Transport = 1 << 1,
        Server = 1 << 2,
        Client = 1 << 3,
        ClientHost = 1 << 4,
        PacketFragmenter = 1 << 5,

        // Lobby (2)
        LobbyManager = 1 << 6,
        LobbyChatManager = 1 << 7,

        // Voice (3)
        VoiceManager = 1 << 8,
        VoicePlayer = 1 << 9,
        FishNetVoicePlayer = 1 << 10,

        // Migration (3)
        HostMigrationManager = 1 << 11,
        HostMigratable = 1 << 12,
        HostMigrationPlayerSpawner = 1 << 13,

        // Social (4)
        Friends = 1 << 14,
        Presence = 1 << 15,
        UserInfo = 1 << 16,
        CustomInvites = 1 << 17,

        // Stats (3)
        Stats = 1 << 18,
        Leaderboards = 1 << 19,
        Achievements = 1 << 20,

        // Storage (2)
        PlayerDataStorage = 1 << 21,
        TitleStorage = 1 << 22,

        // Moderation (3)
        Reports = 1 << 23,
        Sanctions = 1 << 24,
        Metrics = 1 << 25,

        // Demo (4)
        NetworkPhysicsObject = 1 << 26,
        PlayerBall = 1 << 27,
        PhysicsNetworkTransform = 1 << 28,
        SimpleCamera = 1 << 29,

        // Core (continued)
        PlayerRegistry = 1 << 30,

        // Group shortcuts
        AllCore = EOSManager | Transport | Server | Client | ClientHost | PacketFragmenter | PlayerRegistry,
        AllLobby = LobbyManager | LobbyChatManager,
        AllVoice = VoiceManager | VoicePlayer | FishNetVoicePlayer,
        AllMigration = HostMigrationManager | HostMigratable | HostMigrationPlayerSpawner,
        AllSocial = Friends | Presence | UserInfo | CustomInvites,
        AllStats = Stats | Leaderboards | Achievements,
        AllStorage = PlayerDataStorage | TitleStorage,
        AllModeration = Reports | Sanctions | Metrics,
        AllDemo = NetworkPhysicsObject | PlayerBall | PhysicsNetworkTransform | SimpleCamera,

        All = ~0
    }

    /// <summary>
    /// ScriptableObject storing debug settings for the EOS Native transport.
    /// Controls which systems log messages to the console.
    /// </summary>
    [CreateAssetMenu(fileName = "EOSDebugSettings", menuName = "FishNet/EOS Native/Debug Settings")]
    public class EOSDebugSettings : ScriptableObject
    {
        private static EOSDebugSettings _instance;

        /// <summary>
        /// Singleton accessor for the debug settings.
        /// Loads from Resources/EOSDebugSettings if available.
        /// </summary>
        public static EOSDebugSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EOSDebugSettings>("EOSDebugSettings");

                    // If no settings exist, create runtime defaults (logging disabled by default)
                    if (_instance == null)
                    {
                        _instance = CreateInstance<EOSDebugSettings>();
                        _instance._globalEnabled = false;
                        _instance._enabledCategories = DebugCategory.None;
                    }
                }
                return _instance;
            }
        }

        [Header("Global Settings")]
        [Tooltip("Master toggle - when OFF, no debug logs are output regardless of category settings.")]
        [SerializeField]
        private bool _globalEnabled = false;

        [Header("Category Settings")]
        [Tooltip("Which debug categories are enabled for logging.")]
        [SerializeField]
        private DebugCategory _enabledCategories = DebugCategory.None;

        [Header("Group Muting")]
        [Tooltip("Which category groups are muted (temporarily disabled without changing selections).")]
        [SerializeField]
        private DebugCategory _mutedGroups = DebugCategory.None;

        /// <summary>
        /// Master toggle for all debug logging.
        /// When false, no logs are output regardless of category settings.
        /// </summary>
        public bool GlobalEnabled
        {
            get => _globalEnabled;
            set => _globalEnabled = value;
        }

        /// <summary>
        /// The currently enabled debug categories.
        /// </summary>
        public DebugCategory EnabledCategories
        {
            get => _enabledCategories;
            set => _enabledCategories = value;
        }

        /// <summary>
        /// The currently muted category groups (temporarily disabled without changing selections).
        /// </summary>
        public DebugCategory MutedGroups
        {
            get => _mutedGroups;
            set => _mutedGroups = value;
        }

        /// <summary>
        /// Check if a specific category is enabled for logging.
        /// A category logs if: global enabled AND category enabled AND not muted.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if the category should log.</returns>
        public bool IsCategoryEnabled(DebugCategory category)
        {
            return _globalEnabled && (_enabledCategories & category) != 0 && (_mutedGroups & category) == 0;
        }

        /// <summary>
        /// Check if a group is muted.
        /// </summary>
        public bool IsGroupMuted(DebugCategory groupFlag)
        {
            return (_mutedGroups & groupFlag) == groupFlag;
        }

        /// <summary>
        /// Mute a category group (temporarily disable without changing selections).
        /// </summary>
        public void MuteGroup(DebugCategory groupFlag)
        {
            _mutedGroups |= groupFlag;
        }

        /// <summary>
        /// Unmute a category group.
        /// </summary>
        public void UnmuteGroup(DebugCategory groupFlag)
        {
            _mutedGroups &= ~groupFlag;
        }

        /// <summary>
        /// Toggle mute state for a category group.
        /// </summary>
        public void ToggleGroupMute(DebugCategory groupFlag)
        {
            if (IsGroupMuted(groupFlag))
                UnmuteGroup(groupFlag);
            else
                MuteGroup(groupFlag);
        }

        /// <summary>
        /// Enable a specific category.
        /// </summary>
        public void EnableCategory(DebugCategory category)
        {
            _enabledCategories |= category;
        }

        /// <summary>
        /// Disable a specific category.
        /// </summary>
        public void DisableCategory(DebugCategory category)
        {
            _enabledCategories &= ~category;
        }

        /// <summary>
        /// Toggle a specific category.
        /// </summary>
        public void ToggleCategory(DebugCategory category)
        {
            _enabledCategories ^= category;
        }

        /// <summary>
        /// Enable all categories.
        /// </summary>
        public void EnableAllCategories()
        {
            _enabledCategories = DebugCategory.All;
        }

        /// <summary>
        /// Disable all categories.
        /// </summary>
        public void DisableAllCategories()
        {
            _enabledCategories = DebugCategory.None;
        }

        // Mask of only the 31 individual categories (bits 0-30), excluding group shortcuts
        private const DebugCategory IndividualCategoriesMask =
            DebugCategory.EOSManager | DebugCategory.Transport | DebugCategory.Server |
            DebugCategory.Client | DebugCategory.ClientHost | DebugCategory.PacketFragmenter |
            DebugCategory.LobbyManager | DebugCategory.LobbyChatManager |
            DebugCategory.VoiceManager | DebugCategory.VoicePlayer | DebugCategory.FishNetVoicePlayer |
            DebugCategory.HostMigrationManager | DebugCategory.HostMigratable | DebugCategory.HostMigrationPlayerSpawner |
            DebugCategory.Friends | DebugCategory.Presence | DebugCategory.UserInfo | DebugCategory.CustomInvites |
            DebugCategory.Stats | DebugCategory.Leaderboards | DebugCategory.Achievements |
            DebugCategory.PlayerDataStorage | DebugCategory.TitleStorage |
            DebugCategory.Reports | DebugCategory.Sanctions | DebugCategory.Metrics |
            DebugCategory.NetworkPhysicsObject | DebugCategory.PlayerBall | DebugCategory.PhysicsNetworkTransform |
            DebugCategory.SimpleCamera | DebugCategory.PlayerRegistry;

        /// <summary>
        /// Get the number of enabled categories (only counts the 31 individual categories).
        /// </summary>
        public int GetEnabledCategoryCount()
        {
            int count = 0;
            // Mask to only individual categories, then count bits
            uint value = (uint)(_enabledCategories & IndividualCategoriesMask);
            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        #region EOS SDK Native Logging

        [Header("EOS SDK Native Logging")]
        [Tooltip("Enable routing EOS SDK internal logs through LoggingInterface.SetLogLevel/SetCallback.")]
        [SerializeField]
        private bool _eosSDKLoggingEnabled = false;

        [Tooltip("Default log level applied to all SDK categories.")]
        [SerializeField]
        private EOSSDKLogLevel _eosSDKDefaultLogLevel = EOSSDKLogLevel.Warning;

        [Tooltip("Per-category log level overrides for SDK categories.")]
        [SerializeField]
        private EOSSDKCategoryOverride[] _eosSDKCategoryOverrides = new EOSSDKCategoryOverride[0];

        [Tooltip("Route SDK log messages to the Unity console (Debug.Log/LogWarning/LogError).")]
        [SerializeField]
        private bool _eosSDKRouteToUnityConsole = true;

        /// <summary>
        /// Whether EOS SDK native logging is enabled.
        /// </summary>
        public bool EOSSDKLoggingEnabled
        {
            get => _eosSDKLoggingEnabled;
            set => _eosSDKLoggingEnabled = value;
        }

        /// <summary>
        /// Default log level for all SDK categories.
        /// </summary>
        public EOSSDKLogLevel EOSSDKDefaultLogLevel
        {
            get => _eosSDKDefaultLogLevel;
            set => _eosSDKDefaultLogLevel = value;
        }

        /// <summary>
        /// Per-category log level overrides.
        /// </summary>
        public EOSSDKCategoryOverride[] EOSSDKCategoryOverrides
        {
            get => _eosSDKCategoryOverrides;
            set => _eosSDKCategoryOverrides = value;
        }

        /// <summary>
        /// Whether to route SDK log messages to the Unity console.
        /// </summary>
        public bool EOSSDKRouteToUnityConsole
        {
            get => _eosSDKRouteToUnityConsole;
            set => _eosSDKRouteToUnityConsole = value;
        }

        /// <summary>
        /// Gets the effective SDK log level for a given category name.
        /// Returns the override level if one exists and is enabled, otherwise the default level.
        /// </summary>
        public EOSSDKLogLevel GetSDKLogLevel(string categoryName)
        {
            if (_eosSDKCategoryOverrides != null)
            {
                for (int i = 0; i < _eosSDKCategoryOverrides.Length; i++)
                {
                    if (_eosSDKCategoryOverrides[i].Enabled &&
                        string.Equals(_eosSDKCategoryOverrides[i].CategoryName, categoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        return _eosSDKCategoryOverrides[i].Level;
                    }
                }
            }
            return _eosSDKDefaultLogLevel;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// For editor use - clears the cached instance to force reload.
        /// </summary>
        public static void ClearCache()
        {
            _instance = null;
        }

        /// <summary>
        /// For editor use - sets the instance directly.
        /// </summary>
        public static void SetInstance(EOSDebugSettings settings)
        {
            _instance = settings;
        }
#endif
    }
}
