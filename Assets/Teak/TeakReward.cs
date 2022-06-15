#region References
/// @cond hide_from_doxygen
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using MiniJSON.Teak;
/// @endcond
#endregion

public class TeakReward {
    public enum RewardStatus {
        GrantReward,
        SelfClick,
        AlreadyClicked,
        TooManyClicks,
        ExceedMaxClicksForDay,
        Expired,
        InvalidPost,
        InternalError
    }

    public RewardStatus Status { get; set; }
    public Dictionary<string, object> Reward { get; set; }
    public string ScheduleName { get; set; }
    public ulong ScheduleId { get; set; }
    public string CreativeName { get; set; }
    public ulong CreativeId { get; set; }
    public string ChannelName { get; set; }
    public bool Incentivized { get; set; }
    public string RewardId { get; set; }

    public TeakReward(Dictionary<string, object> json) {
        this.Status = RewardStatus.InternalError;
        this.Incentivized = true;
        this.RewardId = json["teakRewardId"] as string;

        // Optional
        if (json.ContainsKey("teakScheduleName")) { this.ScheduleName = json["teakScheduleName"] as string; }
        if (json.ContainsKey("teakScheduleId")) {
            ulong temp = 0;
            UInt64.TryParse(json["teakScheduleId"] as string, out temp);
            this.ScheduleId = temp;
        }
        if (json.ContainsKey("teakCreativeName")) { this.CreativeName = json["teakCreativeName"] as string; }
        if (json.ContainsKey("teakCreativeId")) {
            ulong temp = 0;
            UInt64.TryParse(json["teakCreativeId"] as string, out temp);
            this.CreativeId = temp;
        }
        if (json.ContainsKey("teakChannelName")) { this.ChannelName = json["teakChannelName"] as string; }

        switch (json["status"] as string) {
            case "grant_reward": {
                // The user has been issued this reward by Teak
                try {
                    this.Status = RewardStatus.GrantReward;
                    this.Reward = json["reward"] as Dictionary<string, object>;
                } catch {
                    this.Status = RewardStatus.InternalError;
                }
            }
            break;

            case "self_click": {
                // The user has attempted to claim a reward from their own social post
                this.Status = RewardStatus.SelfClick;
            }
            break;

            case "already_clicked": {
                // The user has already been issued this reward
                this.Status = RewardStatus.AlreadyClicked;
            }
            break;

            case "too_many_clicks": {
                // The reward has already been claimed its maximum number of times globally
                this.Status = RewardStatus.TooManyClicks;
            }
            break;

            case "exceed_max_clicks_for_day": {
                // The user has already claimed their maximum number of rewards of this type for the day
                this.Status = RewardStatus.ExceedMaxClicksForDay;
            }
            break;

            case "expired": {
                // This reward has expired and is no longer valid
                this.Status = RewardStatus.Expired;
            }
            break;

            case "invalid_post": {
                //Teak does not recognize this reward id
                this.Status = RewardStatus.InvalidPost;
            }
            break;
        }
    }

    public override string ToString() {
        string formatString = "{{ Status = '{0}', ScheduleId = '{1}', CreativeId = '{2}', ChannelName = '{3}', Incentivized = '{4}', RewardId = '{5}'{6} }}";
        return string.Format(formatString,
                             this.Status,
                             this.ScheduleId,
                             this.CreativeId,
                             this.ChannelName,
                             this.Incentivized,
                             this.RewardId,
                             this.Status == RewardStatus.GrantReward ? string.Format(", Reward = '{0}'", Json.Serialize(this.Reward)) : ""
                            );
    }
}
