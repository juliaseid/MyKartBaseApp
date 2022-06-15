using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

using System.IO;
using System.Linq;

using System.Xml;
using System.Xml.Linq;

class TeakAndroidManifestAdditions : IPreprocessBuildWithReport {
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report) {
        if (TeakSettings.JustShutUpIKnowWhatImDoing) { return; }
        if (report.summary.platformGroup != BuildTargetGroup.Android) { return; }

        string[] manifestFilesToTry = new string [] {
            "Assets/Plugins/Android/AndroidManifest.xml",
            // Add your custom manifest location here, in case the default is not correct
        };

        string manifestLocation = null;
        foreach (string location in manifestFilesToTry) {
            if (File.Exists(location)) {
                manifestLocation = location;
                break;
            }
        }
        if (manifestLocation == null) {
            Debug.LogWarning("[Teak] AndroidManifest.xml template not found. Could not add deep link <intent-filter>.");
            return;
        }

        bool didModifyAndroidManifest = false;

        // Find the launch activity
        XDocument androidManifest = XDocument.Load(manifestLocation);
        XElement mainActivity = androidManifest.Descendants()
                                .Where(e => e.Name.LocalName == "action")
                                .Where(e => e.Attribute("{http://schemas.android.com/apk/res/android}name").Value == "android.intent.action.MAIN")
                                .FirstOrDefault();
        if (mainActivity == null) {
            Debug.LogWarning("[Teak] Could not find 'android.intent.action.MAIN' in AndroidManifest.xml. Could not add deep link <intent-filter>.");
            return;
        }
        mainActivity = mainActivity.Parent.Parent;

        // See if there is already a universal link intent filter
        // <data android:scheme="http" android:host="{{teak_short_url_domain}}" />
        // <data android:scheme="https" android:host="{{teak_short_url_domain}}" />
        XElement universalLink = mainActivity.Descendants()
                                 .Where(e => e.Name.LocalName == "data")
                                 .Where(e => e.Attribute("{http://schemas.android.com/apk/res/android}host").Value == TeakSettings.ShortlinkDomain)
                                 .ToList()
                                 .FirstOrDefault();
        if (universalLink != null) {
            universalLink = universalLink.Parent;
        } else {
            // <action android:name="android.intent.action.VIEW" />
            // <category android:name="android.intent.category.DEFAULT" />
            // <category android:name="android.intent.category.BROWSABLE" />
            // <data android:scheme="http" android:host="{{teak_short_url_domain}}" />
            // <data android:scheme="https" android:host="{{teak_short_url_domain}}" />
            universalLink = new XElement("intent-filter",
                                         new XAttribute("{http://schemas.android.com/apk/res/android}autoVerify", true),
                                         new XElement("action", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.action.VIEW")),
                                         new XElement("category", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.category.DEFAULT")),
                                         new XElement("category", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.category.BROWSABLE")),
                                         new XElement("data",
                                                 new XAttribute("{http://schemas.android.com/apk/res/android}scheme", "http"),
                                                 new XAttribute("{http://schemas.android.com/apk/res/android}host", TeakSettings.ShortlinkDomain)
                                                     ),
                                         new XElement("data",
                                                 new XAttribute("{http://schemas.android.com/apk/res/android}scheme", "https"),
                                                 new XAttribute("{http://schemas.android.com/apk/res/android}host", TeakSettings.ShortlinkDomain)
                                                     )
                                        );
            mainActivity.Add(universalLink);
            didModifyAndroidManifest = true;
        }

        // See if there is already a url scheme intent filter
        // <data android:scheme="teak{{teak_app_id}}" android:host="*" />
        XElement urlScheme = mainActivity.Descendants()
                             .Where(e => e.Name.LocalName == "data")
                             .Where(e => e.Attribute("{http://schemas.android.com/apk/res/android}scheme").Value == "teak" + TeakSettings.AppId)
                             .ToList()
                             .FirstOrDefault();
        if (urlScheme != null) {
            urlScheme = urlScheme.Parent;
        } else {
            // <action android:name="android.intent.action.VIEW" />
            // <category android:name="android.intent.category.DEFAULT" />
            // <category android:name="android.intent.category.BROWSABLE" />
            // <data android:scheme="teak{{teak_app_id}}" android:host="*" />
            urlScheme = new XElement("intent-filter",
                                     new XElement("action", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.action.VIEW")),
                                     new XElement("category", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.category.DEFAULT")),
                                     new XElement("category", new XAttribute("{http://schemas.android.com/apk/res/android}name", "android.intent.category.BROWSABLE")),
                                     new XElement("data",
                                                  new XAttribute("{http://schemas.android.com/apk/res/android}scheme", "teak" + TeakSettings.AppId),
                                                  new XAttribute("{http://schemas.android.com/apk/res/android}host", "*")
                                                 )
                                    );
            mainActivity.Add(urlScheme);
            didModifyAndroidManifest = true;
        }

        if (didModifyAndroidManifest) {
            // Write it out
            androidManifest.Save(manifestLocation);
            Debug.Log("[Teak] Wrote modifications to Android Manifest at: " + manifestLocation);
        }
    }
}
