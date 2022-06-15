#region References
/// @cond hide_from_doxygen
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MiniJSON.Teak;

#if UNITY_EDITOR
using System.IO;
using System.Net;
using System.Text;
#endif
/// @endcond
#endregion

/// <summary>
/// Interface for manipulating notifications from Teak.
/// </summary>
public partial class TeakNotification {
    public bool Incentivized { get; set; }
    public string ScheduleName { get; set; }
    public ulong ScheduleId { get; set; }
    public string CreativeName { get; set; }
    public ulong CreativeId { get; set; }
    public string ChannelName { get; set; }
    public string RewardId { get; set; }
    public string DeepLink { get; set; }

    public TeakNotification(Dictionary<string, object> json) {
        this.ScheduleName = json["teakScheduleName"] as string;
        this.CreativeName = json["teakCreativeName"] as string;
        this.ChannelName = json.ContainsKey("teakChannelName") ? json["teakChannelName"] as string : null;
        this.RewardId = json.ContainsKey("teakRewardId") ? json["teakRewardId"] as string : null;
        this.DeepLink = json.ContainsKey("teakDeepLink") ? json["teakDeepLink"] as string : null;
        this.Incentivized = (this.RewardId != null);

        ulong temp = 0;
        if (json.ContainsKey("teakScheduleId")) {
            UInt64.TryParse(json["teakScheduleId"] as string, out temp);
            this.ScheduleId = temp;
        }
        if (json.ContainsKey("teakCreativeId")) {
            temp = 0;
            UInt64.TryParse(json["teakCreativeId"] as string, out temp);
            this.CreativeId = temp;
        }
    }

    public override string ToString() {
        string formatString = "{{ Incentivized = '{0}', ScheduleName = '{1}', ScheduleId = '{2}', CreativeName = '{3}', CreativeId = '{4}', ChannelName = '{5}', RewardId = '{6}', DeepLink = '{7}' }}";
        return string.Format(formatString,
                             this.Incentivized,
                             this.ScheduleName,
                             this.ScheduleId,
                             this.CreativeName,
                             this.CreativeId,
                             this.ChannelName,
                             this.RewardId,
                             this.DeepLink);
    }

    public partial class Reply {
        public enum ReplyStatus {
            /// <summary>
            /// The call was successful, and the notification has been scheduled for delivery.
            /// </summary>
            Ok,

            /// <summary>
            /// The call could not be completed because Teak is unable to send a notification to the device.
            /// </summary>
            /// <remarks>
            /// This can either be that the user has not granted push permissions on iOS, or that the
            /// Teak Dashboard does not have sending credentials suitable for the current device
            /// (i.e. Teak has not been provided with an FCM Sender ID/API Key, APNS certificate,
            /// or ADM Client ID/Client Secret).
            /// </remarks>
            UnconfiguredKey,

            /// <summary>
            /// The call could not be completed because Teak is not aware of the device scheduling
            /// the notification.
            /// </summary>
            /// <remarks>
            /// This can happen if Teak was completely unable to get a push token for the device,
            /// which can occur due to intermittent failures in APNS/FCM/ADM,
            /// intermittent networking failures between the device and those services,
            /// or system modifications made on rooted devices.
            /// </remarks>
            InvalidDevice,

            /// <summary>
            /// An unknown error occured, and the call should be retried.
            /// </summary>
            InternalError
        }

        public struct Notification {
            public string ScheduleId;
            public string CreativeId;

            public override string ToString() {
                string formatString = "{{ ScheduleId = '{0}', CreativeId = '{1}' }}";
                return string.Format(formatString,
                                     this.ScheduleId,
                                     this.CreativeId);
            }
        }

        public ReplyStatus Status { get; set; }
        public List<Notification> Notifications { get; set; }
    }

    // Returns an id that can be used to cancel a scheduled notification
    public static IEnumerator ScheduleNotification(string scheduleName, string defaultMessage, long delayInSeconds, System.Action<Reply> callback) {
        if (Teak.Instance.Trace) {
            Debug.Log("[TeakNotification] ScheduleNotification(" + scheduleName + ", " + defaultMessage + ", " + delayInSeconds + ")");
        }

#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        string data = null;
        string status = null;
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("scheduleNotification", scheduleName, defaultMessage, delayInSeconds);
        if (future != null) {
            while (!future.Call<bool>("isDone")) { yield return null; }

            try {
                Dictionary<string, object> json = Json.TryDeserialize(future.Call<string>("get")) as Dictionary<string, object>;
                data = json["data"] as string;
                status = json["status"] as string;
            } catch {
                status = "error.internal";
                data = null;
            }
        }

        SafePerformCallback("schedule_notification.local", callback, data, status, scheduleName);
#elif UNITY_IPHONE
        string data = null;
        string status = null;
        IntPtr notif = TeakNotificationSchedule_Retained(scheduleName, defaultMessage, delayInSeconds);
        if (notif != IntPtr.Zero) {
            while (!TeakNotificationIsCompleted(notif)) { yield return null; }
            data = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
            status = Marshal.PtrToStringAnsi(TeakNotificationGetStatus(notif));
            TeakRelease(notif);
        }

        SafePerformCallback("schedule_notification.local", callback, data, status, scheduleName);
#elif UNITY_WEBGL
        string callbackId = DateTime.Now.Ticks.ToString();
        webGlCallbackMap.Add(callbackId, callback);
        TeakNotificationSchedule(callbackId, scheduleName, defaultMessage, delayInSeconds);
        yield return null;
#else
        yield return null;
#endif
    }

    public static IEnumerator ScheduleNotification(string scheduleName, long delayInSeconds, string[] userIds, System.Action<Reply> callback) {
        if (Teak.Instance.Trace) {
            Debug.Log("[TeakNotification] ScheduleNotification(" + scheduleName + ", " + delayInSeconds + ", " + userIds + ")");
        }

#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        string data = null;
        string status = null;
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("scheduleNotification", scheduleName, delayInSeconds, userIds);
        if (future != null) {
            while (!future.Call<bool>("isDone")) { yield return null; }

            try {
                Dictionary<string, object> json = Json.TryDeserialize(future.Call<string>("get")) as Dictionary<string, object>;
                data = json["data"] as string;
                status = json["status"] as string;
            } catch {
                status = "error.internal";
                data = null;
            }
        }

        SafePerformCallback("schedule_notification.long_distance", callback, data, status, scheduleName);
#elif UNITY_IPHONE
        string data = null;
        string status = null;
        IntPtr notif = TeakNotificationScheduleLongDistance_Retained(scheduleName, delayInSeconds, userIds, userIds.Length);
        if (notif != IntPtr.Zero) {
            while (!TeakNotificationIsCompleted(notif)) { yield return null; }
            data = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
            status = Marshal.PtrToStringAnsi(TeakNotificationGetStatus(notif));
            TeakRelease(notif);
        }

        SafePerformCallback("schedule_notification.long_distance", callback, data, status, scheduleName);
#elif UNITY_WEBGL
        string callbackId = DateTime.Now.Ticks.ToString();
        webGlCallbackMap.Add(callbackId, callback);
        TeakNotificationScheduleLongDistance(callbackId, scheduleName, Json.Serialize(userIds), delayInSeconds);
        yield return null;
#else
        yield return null;
#endif
    }

    // Cancel an existing notification
    public static IEnumerator CancelScheduledNotification(string scheduleId, System.Action<Reply> callback) {
        if (Teak.Instance.Trace) {
            Debug.Log("[TeakNotification] CancelScheduledNotification(" + scheduleId + ")");
        }

#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        string data = null;
        string status = null;
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("cancelNotification", scheduleId);
        if (future != null) {
            while (!future.Call<bool>("isDone")) { yield return null; }
            try {
                Dictionary<string, object> json = Json.TryDeserialize(future.Call<string>("get")) as Dictionary<string, object>;
                data = json["data"] as string;
                status = json["status"] as string;
            } catch {
                status = "error.internal";
                data = null;
            }
        }

        SafePerformCallback("cancel_notification", callback, data, status, null);
#elif UNITY_IPHONE
        string data = null;
        string status = null;
        IntPtr notif = TeakNotificationCancel_Retained(scheduleId);
        if (notif != IntPtr.Zero) {
            while (!TeakNotificationIsCompleted(notif)) { yield return null; }
            data = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
            status = Marshal.PtrToStringAnsi(TeakNotificationGetStatus(notif));
            TeakRelease(notif);
        }

        SafePerformCallback("cancel_notification", callback, data, status, null);
#elif UNITY_WEBGL
        string callbackId = DateTime.Now.Ticks.ToString();
        webGlCallbackMap.Add(callbackId, callback);
        TeakNotificationCancel(callbackId, scheduleId);
        yield return null;
#else
        yield return null;
#endif
    }

    // Cancel all scheduled notifications
    public static IEnumerator CancelAllScheduledNotifications(System.Action<Reply> callback) {
        if (Teak.Instance.Trace) {
            Debug.Log("[TeakNotification] CancelAllScheduledNotifications()");
        }

#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        string data = null;
        string status = null;
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("cancelAll");
        if (future != null) {
            while (!future.Call<bool>("isDone")) { yield return null; }
            try {
                Dictionary<string, object> json = Json.TryDeserialize(future.Call<string>("get")) as Dictionary<string, object>;
                data = Json.Serialize(json["data"]);
                status = json["status"] as string;
            } catch {
                status = "error.internal";
                data = null;
            }
        }

        SafePerformCallback("cancel_all_notifications", callback, data, status, null);
#elif UNITY_IPHONE
        string data = null;
        string status = null;
        IntPtr notif = TeakNotificationCancelAll_Retained();
        if (notif != IntPtr.Zero) {
            while (!TeakNotificationIsCompleted(notif)) { yield return null; }
            data = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
            status = Marshal.PtrToStringAnsi(TeakNotificationGetStatus(notif));
            TeakRelease(notif);
        }

        SafePerformCallback("cancel_all_notifications", callback, data, status, null);
#elif UNITY_WEBGL
        string callbackId = DateTime.Now.Ticks.ToString();
        webGlCallbackMap.Add(callbackId, callback);
        TeakNotificationCancelAll(callbackId);
        yield return null;
#else
        yield return null;
#endif
    }

    /// @cond hide_from_doxygen
    private static void SafePerformCallback(string method, System.Action<Reply> callback, string data, string status, string scheduleName) {
        try {
            callback(new Reply(status, data, scheduleName));
        } catch (Exception e) {
            Dictionary<string, object> extras = new Dictionary<string, object>();
            extras["data"] = data;
            extras["status"] = status;
            if (scheduleName != null) {
                extras["creative_id"] = scheduleName;
            }
            Teak.Instance.ReportCallbackError(method, e, extras);
        }
    }

#if UNITY_IOS
    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationSchedule_Retained(string scheduleName, string message, long delay);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationScheduleLongDistance_Retained(string scheduleName, long delayInSeconds, string[] userIds, int lenUserIds);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationCancel_Retained(string scheduleId);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationCancelAll_Retained();

    [DllImport ("__Internal")]
    private static extern void TeakRelease(IntPtr obj);

    [DllImport ("__Internal")]
    private static extern bool TeakNotificationIsCompleted(IntPtr notif);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationGetTeakNotifId(IntPtr notif);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationGetStatus(IntPtr notif);
#elif UNITY_WEBGL
    [DllImport ("__Internal")]
    private static extern void TeakNotificationSchedule(string callbackId, string scheduleName, string message, long delay);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationScheduleLongDistance(string callbackId, string scheduleName, string jsonUserIds, long delay);

    [DllImport ("__Internal")]
    private static extern void TeakNotificationCancel(string callbackId, string scheduleId);

    [DllImport ("__Internal")]
    private static extern void TeakNotificationCancelAll(string callbackId);

    private static Dictionary<string, System.Action<Reply>> webGlCallbackMap = new Dictionary<string, System.Action<Reply>>();
    public static void WebGLCallback(string callbackId, string status, string data, string scheduleName) {
        if (webGlCallbackMap.ContainsKey(callbackId)) {
            System.Action<Reply> callback = webGlCallbackMap[callbackId];
            webGlCallbackMap.Remove(callbackId);
            callback(new Reply(status, data, scheduleName));
        }
    }
#endif
    /// @endcond

    /// @cond hide_from_doxygen
    public partial class Reply {
        public Reply(string status, string data, string creativeId = null) {
            this.Status = ReplyStatus.InternalError;
            switch (status) {
                case "ok":
                    this.Status = ReplyStatus.Ok;
                    break;
                case "unconfigured_key":
                    this.Status = ReplyStatus.UnconfiguredKey;
                    break;
                case "invalid_device":
                    this.Status = ReplyStatus.InvalidDevice;
                    break;
            }

            if (this.Status == ReplyStatus.Ok) {
                List<object> replyList = Json.TryDeserialize(data) as List<object>;
                if (replyList != null) {
                    // Data contains array of pairs
                    this.Notifications = new List<Notification>();
                    foreach (object e in replyList) {
                        Dictionary<string, object> entry = e as Dictionary<string, object>;
                        if (entry != null) {
                            this.Notifications.Add(new Notification { ScheduleId = entry["schedule_id"].ToString(), CreativeId = entry["creative_id"] as string });
                        } else {
                            this.Notifications.Add(new Notification { ScheduleId = e as string, CreativeId = creativeId });
                        }
                    }
                } else {
                    this.Notifications = new List<Notification> {
                        new Notification { ScheduleId = data, CreativeId = creativeId }
                    };
                }
            }
        }
    }
    /// @endcond
}
