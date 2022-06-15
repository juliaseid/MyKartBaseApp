using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

using System.IO;
using System.Linq;

using System.Xml;
using System.Xml.Linq;

public class TeakAndroidAssetLibBuilder : IPreprocessBuildWithReport {
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report) {
        if (TeakSettings.JustShutUpIKnowWhatImDoing) { return; }
        if (report.summary.platformGroup != BuildTargetGroup.Android) { return; }

        if (string.IsNullOrEmpty(TeakSettings.AppId)) {
            Debug.LogError("Teak App Id needs to be assigned in the Edit/Teak menu.");
        }

        if (string.IsNullOrEmpty(TeakSettings.APIKey)) {
            Debug.LogError("Teak API Key needs to be assigned in the Edit/Teak menu.");
        }

        string androidLibPath = "Plugins/Android/teak-values.androidlib";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, androidLibPath, "res/values"));

        // res/values/teak.xml
        XDocument doc = new XDocument(
            new XElement("resources",
                         new XElement("string", TeakSettings.AppId, new XAttribute("name", "io_teak_app_id")),
                         new XElement("string", TeakSettings.APIKey, new XAttribute("name", "io_teak_api_key"))
                        )
        );
        doc.Save(Path.Combine(Application.dataPath, androidLibPath, "res/values/teak.xml"));

        // project.properties
        File.WriteAllText(Path.Combine(Application.dataPath, androidLibPath, "project.properties"), "android.library=true\n");

        // AndroidManifest.xml
        string[] lines = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"io.teak.sdk.values\">",
            "    <application>",
            "    </application>",
            "</manifest>"
        };
        File.WriteAllLines(Path.Combine(Application.dataPath, androidLibPath, "AndroidManifest.xml"), lines);

        // Update AssetDatabase
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
        AssetDatabase.SaveAssets();
    }
}
