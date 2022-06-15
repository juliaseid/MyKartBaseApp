using System;
using System.Collections.Generic;

public class TeakPostLaunchSummary {
    public string LaunchLink { get; private set; }
    public string ScheduleName { get; private set; }
    public string ScheduleId { get; private set; }
    public string CreativeName { get; private set; }
    public string CreativeId { get; private set; }
    public string RewardId { get; private set; }
    public string ChannelName { get; private set; }
    public string DeepLink { get; private set; }
    public string SourceSendId { get; private set; }

    public TeakPostLaunchSummary(Dictionary<string, object> json) {
        this.LaunchLink = json.ContainsKey("launch_link") ? json["launch_link"] as string : null;
        this.ScheduleName = json.ContainsKey("teakScheduleName") ? json["teakScheduleName"] as string : null;
        this.ScheduleId = json.ContainsKey("teakScheduleId") ? json["teakScheduleId"] as string : null;
        this.CreativeName = json.ContainsKey("teakCreativeName") ? json["teakCreativeName"] as string : null;
        this.CreativeId = json.ContainsKey("teakCreativeId") ? json["teakCreativeId"] as string : null;
        this.RewardId = json.ContainsKey("teakRewardId") ? json["teakRewardId"] as string : null;
        this.ChannelName = json.ContainsKey("teakChannelName") ? json["teakChannelName"] as string : null;
        this.DeepLink = json.ContainsKey("teakDeepLink") ? json["teakDeepLink"] as string : null;
        this.SourceSendId = json.ContainsKey("teakNotifId") ? json["teakNotifId"] as string : null;
    }

    public override string ToString() {
        string formatString = "{{ LaunchLink = '{0}', ScheduleName = '{1}', ScheduleId = '{2}', CreativeName = '{3}', CreativeId = '{4}', RewardId = '{5}', ChannelName = '{6}', DeepLink = '{7}', SourceSendId = '{8}' }}";
        return string.Format(formatString,
                             this.LaunchLink,
                             this.ScheduleName,
                             this.ScheduleId,
                             this.CreativeName,
                             this.CreativeId,
                             this.RewardId,
                             this.ChannelName,
                             this.DeepLink,
                             this.SourceSendId
                            );
    }
}
