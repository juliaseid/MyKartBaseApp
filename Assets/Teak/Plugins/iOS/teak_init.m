/* Teak -- Copyright (C) 2016-2019 Teak.io, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// From TeakHooks.m
extern void Teak_Plant(Class appDelegateClass, NSString* appId, NSString* appSecret);

// From Teak.m
extern NSString* const TeakNotificationAppLaunch;
extern NSString* const TeakOnReward;
extern NSString* const TeakForegroundNotification;
extern NSString* const TeakAdditionalData;
extern NSString* const TeakLaunchedFromLink;
extern NSString* const TeakPostLaunchSummary;

extern NSDictionary* TeakWrapperSDK;
extern NSDictionary* TeakXcodeVersion;

typedef void (^TeakLinkBlock)(NSDictionary* _Nonnull parameters);
extern void TeakRegisterRoute(const char* route, const char* name, const char* description, TeakLinkBlock block);

typedef void (^TeakLogListener)(NSString* _Nonnull event,
                                NSString* _Nonnull level,
                                NSDictionary* _Nullable eventData);

extern void TeakSetLogListener(TeakLogListener listener);

// TeakNotification
extern NSObject* TeakNotificationSchedule(const char* creativeId, const char* message, uint64_t delay);
extern NSObject* TeakNotificationScheduleLongDistance(const char* creativeId, int64_t delay, const char* inUserIds[], int inUserIdCount);
extern NSObject* TeakNotificationCancel(const char* scheduleId);
extern NSObject* TeakNotificationCancelAll();

// Unity
extern void UnitySendMessage(const char*, const char*, const char*);

extern NSString* TeakUnitySDKVersion;

void TeakRelease(id ptr)
{
#if __has_feature(objc_arc)
   void *retainedThing = (__bridge void *)ptr;
   id unretainedThing = (__bridge_transfer id)retainedThing;
   unretainedThing = nil;
#else
   [ptr release];
#endif
}

void* TeakNotificationSchedule_Retained(const char* creativeId, const char* message, uint64_t delay)
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationSchedule(creativeId, message, delay);
   return notif;
#else
   return [TeakNotificationSchedule(creativeId, message, delay) retain];
#endif
}

void* TeakNotificationScheduleLongDistance_Retained(const char* creativeId, int64_t delay, const char* userIds[], int userIdCount)
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationScheduleLongDistance(creativeId, delay, userIds, userIdCount);
   return notif;
#else
   return [TeakNotificationScheduleLongDistance(creativeId, delay, userIds, userIdCount) retain];
#endif
}

void* TeakNotificationCancel_Retained(const char* scheduleId)
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationCancel(scheduleId);
   return notif;
#else
   return [TeakNotificationCancel(scheduleId) retain];
#endif
}

void* TeakNotificationCancelAll_Retained()
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationCancelAll();
   return notif;
#else
   return [TeakNotificationCancelAll() retain];
#endif
}

void teakOnJsonEvent(NSDictionary* userInfo, const char* eventName, bool sendEmptyOnError)
{
   NSError* error = nil;

   NSData* jsonData = [NSJSONSerialization dataWithJSONObject:userInfo
                                                      options:0
                                                        error:&error];

   if (error != nil) {
      NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
      if (sendEmptyOnError) {
        UnitySendMessage("TeakGameObject", eventName, "{}");
      }
   } else {
      NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
      UnitySendMessage("TeakGameObject", eventName, [jsonString UTF8String]);
   }
}

void TeakUnityRegisterRoute(const char* route, const char* name, const char* description)
{
   NSString* nsRoute = [NSString stringWithUTF8String:route];
   TeakRegisterRoute(route, name, description, ^(NSDictionary * _Nonnull parameters) {
      NSError* error = nil;
      NSData* jsonData = [NSJSONSerialization dataWithJSONObject:@{@"route" : nsRoute, @"parameters" : parameters}
                                                         options:0
                                                           error:&error];

      if (error != nil) {
         NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
      } else {
         NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
         UnitySendMessage("TeakGameObject", "DeepLink", [jsonString UTF8String]);
      }
   });
}

__attribute__((constructor))
static void teak_init()
{
   TeakWrapperSDK = @{@"unity" : TeakUnitySDKVersion};
   TeakXcodeVersion = @{@"product" : [NSNumber numberWithInt:__apple_build_version__]};

   NSString* appId = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakAppId"];
   NSString* apiKey = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakApiKey"];
   Teak_Plant(NSClassFromString(@"UnityAppController"), appId, apiKey);

   TeakSetLogListener(^(NSString* _Nonnull event,
                        NSString* _Nonnull level,
                        NSDictionary* _Nullable eventData) {
      NSError* error = nil;
      NSData* jsonData = [NSJSONSerialization dataWithJSONObject:eventData
                                                         options:0
                                                           error:&error];
      if (error != nil) {
         NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
      } else {
         NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
         UnitySendMessage("TeakGameObject", "LogEvent", [jsonString UTF8String]);
      }
   });

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakNotificationAppLaunch
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "NotificationLaunch", true);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakOnReward
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "RewardClaimAttempt", false);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakForegroundNotification
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "ForegroundNotification", true);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakAdditionalData
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "AdditionalData", false);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakLaunchedFromLink
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "LaunchedFromLink", false);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakPostLaunchSummary
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnJsonEvent(notification.userInfo, "PostLaunchSummary", false);
                                                 }];
}
