#region References
/// @cond hide_from_doxygen
using UnityEngine;

using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using MiniJSON.Teak;
using System.Collections.Generic;
/// @endcond
#endregion

/// <summary>
/// A MonoBehaviour which can be attached to a Unity GameObject to
/// provide access to Teak functionality.
/// </summary>
public partial class Teak : MonoBehaviour {
    /// <summary>
    /// Gets the <see cref="Teak"/> singleton.
    /// </summary>
    /// <value> The <see cref="Teak"/> singleton.</value>
    public static Teak Instance {
        get {
            return Teak.Init();
        }
    }

    /// <summary>
    /// Manually initialize Teak.
    /// </summary>
    /// <remarks>
    /// Under normal circumstances it is not necessassary to call this, and you can
    /// simply use Teak.Instance (which calls this method).
    /// </remarks>
    public static Teak Init() {
        if (mInstance == null) {
            mInstance = FindObjectOfType(typeof(Teak)) as Teak;

            if (mInstance == null) {
                GameObject teakGameObject = GameObject.Find("TeakGameObject");
                if (teakGameObject == null) {
                    teakGameObject = new GameObject("TeakGameObject");
                    teakGameObject.AddComponent<Teak>();
                    teakGameObject.hideFlags = HideFlags.DontSave;
                }
                mInstance = teakGameObject.GetComponent<Teak>();
            }
        }
        return mInstance;
    }

    /// <summary>Teak SDK version.</summary>
    public static string Version {
        get {
            return TeakVersion.Version;
        }
    }

    /// <summary>Teak App Id.</summary>
    public static string AppId {
        get;
        set;
    }

    /// <summary>Teak API Key.</summary>
    public static string APIKey {
        get;
        set;
    }

    /// <summary>UNIX Timestamp.</summary>
    public static long UNIXNow {
        get {
            return (long)(DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }

    /// <summary>The user identifier for the current user.</summary>
    public string UserId {
        get;
        private set;
    }

    /// <summary>
    /// Possible push notification states.
    /// </summary>
    /// <remarks>
    /// Some states are specific to iOS versions.
    /// </remarks>
    public enum NotificationState : int {
        /// <summary>Unable to determine the notification state.</summary>
        UnableToDetermine   = -1,
        /// <summary>Notifications are enabled, your app can send push notifications.</summary>
        Enabled             = 0,
        /// <summary>Notifications are disabled, your app cannot send push notifications.</summary>
        Disabled            = 1,
        /// <summary>
        /// Provisional notifications are enabled, your app can send notifications but
        /// they will only display in the Notification Center (iOS 12+ only).
        /// </summary>
        Provisional         = 2,
        /// <summary>The user has not been asked to authorize push notifications (iOS only).</summary>
        NotRequested        = 3
    }

    /// <summary>
    /// State of push notifications.
    /// </summary>
    public NotificationState PushNotificationState {
        get {
#if UNITY_EDITOR
            return NotificationState.UnableToDetermine;
#elif UNITY_WEBGL
            return NotificationState.Enabled;
#elif UNITY_ANDROID
            AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
            return (NotificationState) teak.CallStatic<int>("getNotificationStatus");
#elif UNITY_IPHONE
            return (NotificationState) TeakGetNotificationState();
#else
            return NotificationState.UnableToDetermine;
#endif
        }
    }

    /// <summary>
    /// Get Teak's configuration data about the current app.
    /// </summary>
    /// <returns>A dictionary containing app info, or null if it's not ready</returns>
    public Dictionary<string, object> AppConfiguration {
        get {
            if (mAppConfiguration == null) {
#if UNITY_EDITOR || UNITY_WEBGL
                string configuration = "{}";
#elif UNITY_ANDROID
                AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
                string configuration = teak.CallStatic<string>("getAppConfiguration");
#elif UNITY_IPHONE
                string configuration = Marshal.PtrToStringAnsi(TeakGetAppConfiguration());
#else
                string configuration = "{}";
#endif
                if (!string.IsNullOrEmpty(configuration)) {
                    mAppConfiguration = Json.TryDeserialize(configuration) as Dictionary<string,object>;
                }
            }
            return mAppConfiguration;
        }
    }

    /// <summary>
    /// Teak will log all Unity method calls to the Unity log if true.
    ///
    /// This defaults to the setting for the native SDK, but can be assigned at runtime as well.
    /// </summary>
    public bool Trace {
        get;
        set;
    }

    /// <summary>
    /// Value provided to IdentifyUser to opt out of collecting an IDFA for this specific user.
    /// </summary>
    /// <remarks>
    /// If you prevent Teak from collecting the Identifier For Advertisers (IDFA),
    /// Teak will no longer be able to add this user to Facebook Ad Audiences.
    /// </remarks>
    public const string OptOutIdfa = "opt_out_idfa";

    /// <summary>
    /// Value provided to IdentifyUser to opt out of collecting a Push Key for this specific user.
    /// </summary>
    /// <remarks>
    /// If you prevent Teak from collecting the Push Key, Teak will no longer be able
    /// to send Local Notifications or Push Notifications for this user.
    /// </remarks>
    public const string OptOutPushKey = "opt_out_push_key";

    /// <summary>
    /// Value provided to IdentifyUser to opt out of collecting a Facebook Access Token for this specific user.
    /// </summary>
    /// <remarks>
    /// If you prevent Teak from collecting the Facebook Access Token,
    /// Teak will no longer be able to correlate this user across multiple devices.
    /// </remarks>

    public const string OptOutFacebook = "opt_out_facebook";

    /// <summary>
    /// Tell Teak how it should identify the current user.
    /// </summary>
    /// <remarks>
    /// This should be the same way you identify the user in your backend.
    /// </remarks>
    /// <param name="userIdentifier">An identifier which is unique for the current user.</param>
    /// <param name="email">The email address for the current user.</param>
    // [Obsolete]
    // public void IdentifyUser(string userIdentifier, String email) {
    //     this.IdentifyUser(userIdentifier, null, email);
    // }

    /// <summary>
    /// Tell Teak how it should identify the current user.
    /// </summary>
    /// <remarks>
    /// This should be the same way you identify the user in your backend.
    /// </remarks>
    /// <param name="userIdentifier">An identifier which is unique for the current user.</param>
    /// <param name="optOut">A list containing zero or more of: OptOutIdfa, OptOutPushKey, OptOutFacebook</param>
    /// <param name="email">The email address for the current user.</param>
    // [Obsolete]
    // public void IdentifyUser(string userIdentifier, List<string> optOut = null, String email = null) {
    //     if (optOut == null) { optOut = new List<string>(); }

    //     UserConfiguration userConfiguration = new UserConfiguration {
    //         Email = email,
    //         OptOutFacebook = optOut.Contains(OptOutFacebook),
    //         OptOutPushKey = optOut.Contains(OptOutPushKey),
    //         OptOutIdfa = optOut.Contains(OptOutIdfa)
    //     };

    //     this.IdentifyUser(userIdentifier, userConfiguration);
    // }

    /// <summary>
    /// Configuration options for identifying a user.
    /// </summary>
    public class UserConfiguration {
        /// Email address
        public string Email { get; set; }
        public string FacebookId { get; set; }
        public bool OptOutFacebook { get; set; }
        public bool OptOutIdfa { get; set; }
        public bool OptOutPushKey { get; set; }

#if UNITY_ANDROID
        public AndroidJavaObject ToAndroidJavaObject() {
            return new AndroidJavaObject("io.teak.sdk.Teak$UserConfiguration",
                                         this.Email, this.FacebookId, this.OptOutFacebook, this.OptOutIdfa, this.OptOutPushKey);
        }
#endif

        public Dictionary<string, object> ToDictionary() {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["email"] = this.Email;
            dict["facebook_id"] = this.FacebookId;
            dict["opt_out_facebook"] = this.OptOutFacebook;
            dict["opt_out_idfa"] = this.OptOutIdfa;
            dict["opt_out_push_key"] = this.OptOutPushKey;
            return dict;
        }
    }

    /// <summary>
    /// Tell Teak how it should identify the current user.
    /// </summary>
    /// <remarks>
    /// This should be the same way you identify the user in your backend.
    /// </remarks>
    /// <param name="userIdentifier">An identifier which is unique for the current user.</param>
    /// <param name="userConfiguration">Additional configuration for the current user.</param>
    public void IdentifyUser(string userIdentifier, UserConfiguration userConfiguration) {
        this.UserId = userIdentifier;
        if (userConfiguration == null) { userConfiguration = new UserConfiguration(); }

        if (this.Trace) {
            Debug.Log("[Teak] IdentifyUser(): " + userIdentifier);
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        using (AndroidJavaObject javaConfig = userConfiguration.ToAndroidJavaObject()) {
            teak.CallStatic("identifyUser", userIdentifier, javaConfig);
        }
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakIdentifyUser(userIdentifier, Json.Serialize(userConfiguration.ToDictionary()));
#   if UNITY_WEBGL
        TeakUnityReadyForDeepLinks();
#   endif
#endif
    }

    /// <summary>
    /// Logout the current user.
    /// </summary>
    public void Logout() {
        if (this.Trace) {
            Debug.Log("[Teak] Logout()");
        }
#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("logout");
#elif UNITY_IPHONE
        TeakLogout();
#endif
    }

    /// <summary>
    /// On iOS, if 'TeakDoNotRefreshPushToken' is set to 'true' then this method
    /// will tell Teak that the push token is ready, and that the user has authorized
    /// push notifications. If the user has not authorized push notifications, this will
    /// have no effect.
    /// </summary>
    public void RefreshPushTokenIfAuthorized() {
#if UNITY_EDITOR
#elif UNITY_IPHONE
        TeakRefreshPushTokenIfAuthorized();
#endif
    }

    /// <summary>
    /// Track an arbitrary event in Teak.
    /// </summary>
    /// <param name="actionId">The identifier for the action, e.g. 'complete'.</param>
    /// <param name="objectTypeId">The type of object that is being posted, e.g. 'quest'.</param>
    /// <param name="objectInstanceId">The specific instance of the object, e.g. 'gather-quest-1'</param>
    public void TrackEvent(string actionId, string objectTypeId, string objectInstanceId) {
        if (this.Trace) {
            Debug.Log("[Teak] TrackEvent(): " + actionId + " - " + objectTypeId + " - " + objectInstanceId);
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("trackEvent", actionId, objectTypeId, objectInstanceId);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakTrackEvent(actionId, objectTypeId, objectInstanceId);
#endif
    }

    /// <summary>
    /// Increment an arbitrary event in Teak.
    /// </summary>
    /// <param name="actionId">The identifier for the action, e.g. 'complete'.</param>
    /// <param name="objectTypeId">The type of object that is being posted, e.g. 'quest'.</param>
    /// <param name="objectInstanceId">The specific instance of the object, e.g. 'gather-quest-1'</param>
    /// <param name="count">The amount by which to increment</param>
    public void IncrementEvent(string actionId, string objectTypeId, string objectInstanceId, long count) {
        if (this.Trace) {
            Debug.Log("[Teak] IncrementEvent(): " + actionId + " - " + objectTypeId + " - " + objectInstanceId + " - " + count);
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        long longCountForJava = (long) count;
        teak.CallStatic("incrementEvent", actionId, objectTypeId, objectInstanceId, longCountForJava);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakIncrementEvent(actionId, objectTypeId, objectInstanceId, count);
#endif
    }

    /// <summary>
    /// An event which gets fired when the app is launched via a push notification.
    /// </summary>
    public event System.Action<TeakNotification> OnLaunchedFromNotification;

    /// <summary>
    /// An event which gets fired when a Teak Reward has been processed (successfully or unsuccessfully).
    /// </summary>
    public event System.Action<TeakReward> OnReward;

    /// <summary>
    /// An event which gets fired when a push notification is received while the app is in the foreground.
    /// </summary>
    public event System.Action<TeakNotification> OnForegroundNotification;

    /// <summary>
    /// An event which is dispatched for each log event from the Teak SDK
    /// </summary>
    public event System.Action<Dictionary<string, object>> OnLogEvent;

    /// <summary>
    /// An event which is dispatched when additional data is available for the current user.
    /// </summary>
    public event System.Action<Dictionary<string, object>> OnAdditionalData;

    /// <summary>
    /// An event which is dispatched when the app is launched from a link created by the Teak dashboard.
    /// </summary>
    public event System.Action<Dictionary<string, object>> OnLaunchedFromLink;

    /// <summary>
    /// An event which is dispatched each time the app is launched, with info about the launch.
    /// </summary>
    public event System.Action<TeakPostLaunchSummary> OnPostLaunchSummary;

    /// <summary>
    /// An event which is dispatched when your code, executed via deep link callback, throws an exception.
    /// </summary>
    public event System.Action<string, Exception, Dictionary<string, object>> OnCallbackError;

    /// <summary>
    /// Method used to register a deep link route.
    /// </summary>
    /// <param name="route">The route for this deep link.</param>
    /// <param name="name">The name of this deep link, used in the Teak dashboard.</param>
    /// <param name="description">A description for what this deep link does, used in the Teak dashboard.</param>
    /// <param name="action">A function, or lambda to execute when this deep link is invoked via a notification or web link.</param>
    public void RegisterRoute(string route, string name, string description, Action<Dictionary<string, object>> action) {
        mDeepLinkRoutes[route] = action;

        if (this.Trace) {
            Debug.Log("[Teak] RegisterRoute(): " + route + " - " + name + " - " + description);
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.wrapper.unity.TeakUnity");
        teakUnity.CallStatic("registerRoute", route, name, description);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakUnityRegisterRoute(route, name, description);
#endif
    }

    /// <summary>
    /// Method to set the number displayed on the icon of the app on the home screen.
    /// </summary>
    /// <param name="count">The number to display on the icon of the app on the home screen, or 0 to clear.</param>
    /// <returns>True if Teak was able to set the badge count, false otherwise.</returns>
    public bool SetBadgeCount(int count) {
        if (this.Trace) {
            Debug.Log("[Teak] SetBadgeCount(" + count + ")");
        }

#if UNITY_EDITOR
        return true;
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        return teak.CallStatic<bool>("setApplicationBadgeNumber", count);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakSetBadgeCount(count);
        return true;
#else
        return true;
#endif
    }

    /// <summary>
    /// Open the settings for your app.
    /// </summary>
    /// <returns>false if Teak was unable to open the settings for your app, true otherwise.</returns>
    public bool OpenSettingsAppToThisAppsSettings() {
        if (this.Trace) {
            Debug.Log("[Teak] OpenSettingsAppToThisAppsSettings()");
        }
 
#if UNITY_EDITOR || UNITY_WEBGL
        return false;
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        return !teak.CallStatic<bool>("openSettingsAppToThisAppsSettings");
#elif UNITY_IPHONE
        return TeakOpenSettingsAppToThisAppsSettings();
#else
        return false;
#endif
    }

    /// <summary>
    /// Assign a numeric value to a user profile attribute
    /// </summary>
    /// <param name="key">The name of the numeric attribute.</param>
    /// <param name="value">The value for the numeric attribute.</param>
    public void SetNumericAttribute(string key, double value) {
        if (this.Trace) {
            Debug.Log("[Teak] SetNumericAttribute(" + key + ", " + value + ")");
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("setNumericAttribute", key, value);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakSetNumericAttribute(key, value);
#endif
    }

    /// <summary>
    /// Assign a string value to a user profile attribute
    /// </summary>
    /// <param name="key">The name of the string attribute.</param>
    /// <param name="value">The value for the string attribute.</param>
    public void SetStringAttribute(string key, string value) {
        if (this.Trace) {
            Debug.Log("[Teak] SetStringAttribute(" + key + ", " + value + ")");
        }

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("setStringAttribute", key, value);
#elif UNITY_IPHONE || UNITY_WEBGL
        TeakSetStringAttribute(key, value);
#endif
    }

    /// <summary>
    /// Get Teak's configuration data about the current device.
    /// </summary>
    /// <returns>A dictionary containing device info, or null if it's not ready</returns>
    public Dictionary<string, object> GetDeviceConfiguration() {
#if UNITY_EDITOR || UNITY_WEBGL
        return new Dictionary<string, object>();
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        return Json.TryDeserialize(teak.CallStatic<string>("getDeviceConfiguration")) as Dictionary<string,object>;
#elif UNITY_IPHONE
        string configuration = Marshal.PtrToStringAnsi(TeakGetDeviceConfiguration());
        return Json.TryDeserialize(configuration) as Dictionary<string,object>;
#else
        return new Dictionary<string, object>();
#endif
    }

    /// <summary>
    /// Register for Provisional Push Notifications.
    /// </summary>
    /// <remarks>
    /// This method only has any effect on iOS devices running iOS 12 or higher.
    /// </remarks>
    /// <returns>true if the device was an iOS 12+ device</returns>
    public bool RegisterForProvisionalNotifications() {
#if !UNITY_EDITOR && UNITY_IPHONE
        return TeakRequestPushAuthorization(true);
#else
        return false;
#endif
    }

    /// <summary>
    /// Register for Push Notifications.
    /// </summary>
    /// <remarks>
    /// This method only has any effect on iOS devices, and is safe to use on iOS 8+
    /// </remarks>
    public void RegisterForNotifications() {
#if !UNITY_EDITOR && UNITY_IPHONE
        TeakRequestPushAuthorization(false);
#endif
    }

    /// <summary>
    /// Indicate that your app is ready for deep links.
    /// </summary>
    /// <remarks>
    /// Deep links will not be processed sooner than the earliest of:
    /// - <see cref="IdentifyUser"/> is called
    /// - This method is called
    /// </remarks>
    public void ProcessDeepLinks() {
#if UNITY_EDITOR || UNITY_WEBGL
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("processDeepLinks");
#elif UNITY_IPHONE
        TeakProcessDeepLinks();
#endif
    }

#if UNITY_WEBGL
    /// <summary>
    /// When using Facebook Payments, call this method from your callback
    /// for <code>FB.Canvas.Pay</code> or <code>FB.Canvas.PayWithProductId</code>.
    /// </summary>
    /// <param name="rawResult">The contents of IPayResult.RawResult</param>
    public void ReportCanvasPurchase(string rawResult) {
        try {
            TeakUnityReportCanvasPurchase(rawResult);
        } catch (Exception) {
        }
    }
#endif

    /// @cond hide_from_doxygen
    private static Teak mInstance;
    private Dictionary<string, Action<Dictionary<string, object>>> mDeepLinkRoutes = new Dictionary<string, Action<Dictionary<string, object>>>();
    private Dictionary<string, object> mAppConfiguration = null;

#if UNITY_IPHONE || UNITY_WEBGL
    [DllImport ("__Internal")]
    private static extern void TeakIdentifyUser(string userId, string userConfigurationJson);

    [DllImport ("__Internal")]
    private static extern void TeakTrackEvent(string actionId, string objectTypeId, string objectInstanceId);

    [DllImport ("__Internal")]
    private static extern void TeakIncrementEvent(string actionId, string objectTypeId, string objectInstanceId, long count);

    [DllImport ("__Internal")]
    private static extern void TeakUnityRegisterRoute(string route, string name, string description);

    [DllImport ("__Internal")]
    private static extern void TeakSetBadgeCount(int count);

    [DllImport ("__Internal")]
    private static extern void TeakSetNumericAttribute(string key, double value);

    [DllImport ("__Internal")]
    private static extern void TeakSetStringAttribute(string key, string value);
#endif

#if UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern int TeakGetNotificationState();

    [DllImport ("__Internal")]
    private static extern bool TeakOpenSettingsAppToThisAppsSettings();

    [DllImport ("__Internal")]
    private static extern bool TeakRequestPushAuthorization(bool includeProvisional);

    [DllImport ("__Internal")]
    private static extern void TeakProcessDeepLinks();

    [DllImport ("__Internal")]
    private static extern void TeakLogout();
#endif

#if UNITY_WEBGL
    [DllImport ("__Internal")]
    private static extern string TeakInitWebGL(string appId, string apiKey);

    [DllImport ("__Internal")]
    private static extern void TeakUnityReadyForDeepLinks();

    [DllImport ("__Internal")]
    private static extern void TeakUnityReportCanvasPurchase(string payload);
#elif UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern IntPtr TeakGetAppConfiguration();

    [DllImport ("__Internal")]
    private static extern IntPtr TeakGetDeviceConfiguration();

    [DllImport ("__Internal")]
    private static extern void TeakRefreshPushTokenIfAuthorized();
#endif
    /// @endcond

    #region UnitySendMessage
    /// @cond hide_from_doxygen
    void NotificationLaunch(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnLaunchedFromNotification != null) {
            OnLaunchedFromNotification(new TeakNotification(json));
        }
    }

    void RewardClaimAttempt(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnReward != null) {
            OnReward(new TeakReward(json));
        }
    }

    void PostLaunchSummary(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnPostLaunchSummary != null) {
            OnPostLaunchSummary(new TeakPostLaunchSummary(json));
        }
    }

    void DeepLink(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        string route = json["route"] as string;
        Dictionary<string, object> parameters = json["parameters"] as Dictionary<string, object>;
        if (mDeepLinkRoutes.ContainsKey(route)) {
            try {
                mDeepLinkRoutes[route](parameters);
            } catch (Exception e) {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data["route"] = route;
                data["parameters"] = parameters;
                OnCallbackError("deep_link", e, data);
            }
        } else {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["route"] = route;
            data["parameters"] = parameters;
            OnCallbackError("deep_link", new ArgumentException("No action for route: " + route), data);
        }
    }

    public void LogEvent(string jsonString) {
        if (OnLogEvent != null) {
            Dictionary<string, object> json = null;

            try {
                json = Json.Deserialize(jsonString) as Dictionary<string, object>;
            } catch (Exception ex) {
                Dictionary<string, object> eventData = CreateLogEventDataFromException(ex);
                // This can occur due to a bug in how .NET 4.0+ handles strings with UTF-16 characters from
                // supplementary planes (e.g. emoji) when the string comes in through the JNI on Android
                // 5.1.1 or earlier. Because Amazon Kindle devices are based on a fork of Android 5.1 this
                // also impacts all Kindle devices.
                //
                // The most likely log message to contain emoji is the notification.received event, which
                // includes the full contents of the notification message.
                //
                // We do not provide the string that caused parsing to fail in this event because in our testing
                // the string is "tainted" -- any operations executed using it fail. For example, it cannot be
                // printed with Debug.Log. Because the error only impacts the C# layer, Teak can identfy the
                // string which triggered the error given the current game assigned player id and the timestamp
                // from this error message, if remote logging is enabled by Teak for that player.
                if (ex is OverflowException) {
                    eventData["error_type"] = "overflow";
                    eventData["error_description"] = "I encountered an OverflowException attempting to parse the log message. This most likely means that I am on Android < 6 running .NET 4.0 and the log message contained an emoji or other special character.";
                } else {
                    // We've only ever seen OverflowException occur when trying to parse log messages, but now
                    // that we know errors _can_ happen, we should assume they _will_ happen. If you see this
                    // message, then it means you've encountered a log parsing error that we had not heard of
                    // at the time your version of the SDK was released. Please let us know what error you
                    // saw, so that we can properly handle it in future SDK versions!
                    eventData["error_type"] = "unknown";
                    eventData["error_description"] = "I encountered an unknown error attempting to parse the log message. Please contact team@teak.io with the details of the exception in the 'exception' key so that my humans can help me handle this better in the future!";
                }

                json = CreateInternalErrorLogEvent("error.loghandler", eventData);
            }

            if (json == null) {
                return;
            }

            OnLogEvent(json);
        }
    }

    Dictionary<string, object> CreateInternalErrorLogEvent(string eventType, Dictionary<string, object> eventData) {
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["event_type"] = eventType;
        json["log_level"] = "ERROR";
        json["timestamp"] = Teak.UNIXNow;
        json["run_id"] = 0L;
        json["event_id"] = 0L;

        if (eventData != null) {
            json["event_data"] = eventData;
        }

        return json;
    }

    Dictionary<string, object> CreateLogEventDataFromException(Exception exception) {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData["exception"] = exception.ToString();
        return eventData;
    }

    void ForegroundNotification(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnForegroundNotification != null) {
            OnForegroundNotification(new TeakNotification(json));
        }
    }

    void AdditionalData(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnAdditionalData != null) {
            OnAdditionalData(json);
        }
    }

    void LaunchedFromLink(string jsonString) {
        Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
        if (json == null) {
            return;
        }

        if (OnLaunchedFromLink != null) {
            OnLaunchedFromLink(json);
        }
    }

#if UNITY_WEBGL
    void NotificationCallback(string jsonString) {
        try {
            Dictionary<string, object> json = Json.TryDeserialize(jsonString) as Dictionary<string, object>;
            if (json == null) {
                return;
            }

            string callbackId = json["callbackId"] as string;
            string status = json["status"] as string;
            string creativeId = json.ContainsKey("creativeId") ? json["creativeId"] as string : null;
            string data = json.ContainsKey("data") ? (json["data"] is string ? json["data"] as string : Json.Serialize(json["data"])) : null;
            TeakNotification.WebGLCallback(callbackId, status, data, creativeId);
        } catch (Exception e) {
            Debug.LogError("[Teak] Error executing callback for notification data: " + jsonString + "\n" + e.ToString());
        }
    }
#endif
    /// @endcond
    #endregion

    #region Internal Callbacks
    /// @cond hide_from_doxygen
    void InternalOnCallbackError(string callback, Exception exception, Dictionary<string, object> data) {
        Debug.LogError("[Teak] Callback error (" + callback + "): " + exception.ToString());
    }

    public void ReportCallbackError(string callback, Exception exception, Dictionary<string, object> data) {
        OnCallbackError(callback, exception, data);
    }
    /// @endcond
    #endregion

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Awake() {
        Debug.Log("[Teak] Unity SDK Version: " + Teak.Version);
        DontDestroyOnLoad(this);

        string appId = null;
        string apiKey = null;
#if UNITY_EDITOR
#elif UNITY_WEBGL
        appId = (string.IsNullOrEmpty(Teak.AppId) ? TeakSettings.AppId : Teak.AppId);
        apiKey = (string.IsNullOrEmpty(Teak.APIKey) ? TeakSettings.APIKey : Teak.APIKey);

        if (string.IsNullOrEmpty(appId)) {
            throw new ArgumentNullException("Teak.AppId cannot be null or empty.");
        } else if (string.IsNullOrEmpty(apiKey)) {
            throw new ArgumentNullException("Teak.APIKey cannot be null or empty.");
        }

        TeakInitWebGL(appId, apiKey);
#else
        if (this.AppConfiguration != null) {
            appId = this.AppConfiguration["appId"] as string;
            apiKey = this.AppConfiguration["apiKey"] as string;
        }
#endif
        if (appId != null) { Teak.AppId = appId; }
        if (apiKey != null) { Teak.APIKey = apiKey; }

        // Register our internal callback error handler
        OnCallbackError += InternalOnCallbackError;
    }

    void Start() {
#if UNITY_EDITOR
        // Editor mode default to trace on
        this.Trace = true;
#endif

        // Trace log default from app config
        object trace = null;
        if (this.AppConfiguration != null && this.AppConfiguration.TryGetValue("traceLog", out trace)) {
            this.Trace = (bool) trace;
        }
    }

    void OnApplicationQuit() {
        Destroy(this.gameObject);
    }
    /// @endcond
    #endregion
}
