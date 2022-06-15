#if UNITY_IOS

#region References
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using UnityEditor.iOS.Xcode.Extensions;
using UnityEditor.iOS.Xcode;

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
#endregion

public class TeakXcodeProjectMutator : IPostprocessBuildWithReport {
    public int callbackOrder { get { return 100; } }

    public void OnPostprocessBuild(BuildReport report) {
        if (TeakSettings.JustShutUpIKnowWhatImDoing) { return; }
        if (report.summary.platformGroup != BuildTargetGroup.iOS) { return; }

        string projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
        string unityTarget = project.GetUnityMainTargetGuid();
#else
        string unityTarget = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

        /////
        // Add Frameworks to Unity target
        string[] teakRequiredFrameworks = new string[] {
            "AdSupport",
            "AVFoundation",
            "CoreServices",
            "StoreKit",
            "UserNotifications",
            "ImageIO"
        };
        project.AddFrameworksToTarget(unityTarget, teakRequiredFrameworks);

        /////
        // Modify plist
        string plistPath = report.summary.outputPath + "/Info.plist";
        File.WriteAllText(plistPath, AddTeakEntriesToPlist(File.ReadAllText(plistPath)));

        /////
        // Add Teak app extensions
        string[] teakExtensionCommonFrameworks = new string[] {"AdSupport", "AVFoundation", "CoreGraphics", "ImageIO", "CoreServices", "StoreKit", "SystemConfiguration", "UIKit", "UserNotifications"};

        AddTeakExtensionToProjectTarget("TeakNotificationService", "TeakNotificationService",
                                        teakExtensionCommonFrameworks,
                                        project, unityTarget, projectPath);

        AddTeakExtensionToProjectTarget("TeakNotificationContent", "TeakNotificationContent",
                                        new string[] {"UserNotificationsUI"}.Concat(teakExtensionCommonFrameworks).ToArray(),
                                        project, unityTarget, projectPath);

        /////
        // Write out modified project
        project.WriteToFile(projectPath);

        /////
        // Add/modify entitlements
        string unityTargetName = "Unity-iPhone";
        string entitlementsFileName = unityTargetName + ".entitlements";
        ProjectCapabilityManager capabilityManager = new ProjectCapabilityManager(projectPath, entitlementsFileName, unityTargetName);
        capabilityManager.AddPushNotifications(UnityEngine.Debug.isDebugBuild);
        capabilityManager.AddAssociatedDomains(new string[] {"applinks:" + TeakSettings.ShortlinkDomain});
        capabilityManager.WriteToFile();
    }

    private static string AddTeakEntriesToPlist(string inputPlist) {
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(inputPlist);

        // Teak credentials
        plist.root.SetString("TeakAppId", TeakSettings.AppId);
        plist.root.SetString("TeakApiKey", TeakSettings.APIKey);

        // Teak URL Scheme
        AddURLSchemeToPlist(plist, "teak" + TeakSettings.AppId);

        // Add remote notifications background mode
        AddElementToArrayIfMissing(plist, "UIBackgroundModes", "remote-notification");

        return plist.WriteToString();
    }

    public static void AddURLSchemeToPlist(PlistDocument plist, string urlSchemeToAdd) {
        // Get/create array of URL types
        PlistElementArray urlTypesArray = null;
        if (!plist.root.values.ContainsKey("CFBundleURLTypes")) {
            urlTypesArray = plist.root.CreateArray("CFBundleURLTypes");
        } else {
            urlTypesArray = plist.root.values["CFBundleURLTypes"].AsArray();
        }
        if (urlTypesArray == null) {
            urlTypesArray = plist.root.CreateArray("CFBundleURLTypes");
        }

        // Get/create an entry in the array
        PlistElementDict urlTypesItems = null;
        if (urlTypesArray.values.Count == 0) {
            urlTypesItems = urlTypesArray.AddDict();
        } else {
            urlTypesItems = urlTypesArray.values[0].AsDict();

            if (urlTypesItems == null) {
                urlTypesItems = urlTypesArray.AddDict();
            }
        }

        // Get/create array of URL schemes
        PlistElementArray urlSchemesArray = null;
        if (!urlTypesItems.values.ContainsKey("CFBundleURLSchemes")) {
            urlSchemesArray = urlTypesItems.CreateArray("CFBundleURLSchemes");
        } else {
            urlSchemesArray = urlTypesItems.values["CFBundleURLSchemes"].AsArray();

            if (urlSchemesArray == null) {
                urlSchemesArray = urlTypesItems.CreateArray("CFBundleURLSchemes");
            }
        }

        // Add URL scheme
        if (!urlSchemesArray.ContainsElement(urlSchemeToAdd)) {
            urlSchemesArray.Add(urlSchemeToAdd);
        }
    }

    private static PlistElementArray AddElementToArrayIfMissing(PlistDocument plist, string key, object element) {
        PlistElementArray plistArray = null;
        if (!plist.root.values.ContainsKey(key)) {
            plistArray = plist.root.CreateArray(key);
        } else {
            plistArray = plist.root.values[key].AsArray();
        }

        if (!plistArray.ContainsElement(element)) {
            plistArray.Add(element);
        }

        return plistArray;
    }

    private static string AddTeakExtensionToProjectTarget(string name, string displayName, string[] frameworks, PBXProject project, string target, string projectPath) {
        string __FILE__ = new StackTrace(new StackFrame(true)).GetFrame(0).GetFileName();
        string teakEditorIosPath = Path.GetDirectoryName(__FILE__) + "/iOS";
        string extensionSrcPath = teakEditorIosPath + "/" + name;
        FileInfo projectPathInfo = new FileInfo(Path.GetDirectoryName(projectPath));

        /////
        // Create app extension target
        string extensionTarget = project.AddAppExtension(target, name,
                                 PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS) + "." + displayName,
                                 projectPathInfo.GetRelativePathTo(new FileInfo(extensionSrcPath + "/Info.plist")));
        string buildPhaseId = project.AddSourcesBuildPhase(extensionTarget);

        /////
        // Set TeamId
        project.SetTeamId(extensionTarget, PlayerSettings.iOS.appleDeveloperTeamID);

        /////
        // Add source files
        string[] extensionsIncluded = new string[] {".h", ".m", ".mm"};
        string[] fileEntries = Directory.GetFiles(extensionSrcPath);
        foreach (string fileName in fileEntries) {
            if (!extensionsIncluded.Contains(Path.GetExtension(fileName))) { continue; }

            string relativeFileName = projectPathInfo.GetRelativePathTo(new FileInfo(fileName));
            project.AddFileToBuildSection(extensionTarget, buildPhaseId,
                                          project.AddFile(relativeFileName, name + "/" + Path.GetFileName(fileName)));
        }

        /////
        // Add Frameworks
        project.AddFrameworksToTarget(extensionTarget, frameworks);

        /////
        // Add libTeak.a

        // If the 'Runtime' directory exists, this is coming from a UPM package
        string pathToCheck = Path.GetDirectoryName(Path.GetDirectoryName(__FILE__));
        string relativeTeakPath = new DirectoryInfo(Application.dataPath).GetRelativePathTo(new DirectoryInfo(pathToCheck));
        if (Directory.Exists(pathToCheck + "/Runtime")) {
            relativeTeakPath = "io.teak.unity.sdk/Runtime";
        }
        project.AddFileToBuild(extensionTarget, project.AddFile("libTeak.a", name + "/libTeak.a"));
        project.AddBuildProperty(extensionTarget, "LIBRARY_SEARCH_PATHS", "$(SRCROOT)/Libraries/" + relativeTeakPath + "/Plugins/iOS");
        project.AddBuildProperty(extensionTarget, "ALWAYS_SEARCH_USER_PATHS", "NO");

        /////
        // Build properties
        project.SetBuildProperty(extensionTarget, "IPHONEOS_DEPLOYMENT_TARGET", "10.0");
        project.AddBuildProperty(extensionTarget, "TARGETED_DEVICE_FAMILY", "1,2");

        // armv7 and armv7s do not support Notification Content Extensions
        project.AddBuildProperty(extensionTarget, "ARCHS", "arm64");

        return extensionTarget;
    }
}

#endif // UNITY_IOS
