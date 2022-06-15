#region References
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#endregion

[InitializeOnLoad]
[CustomEditor(typeof(TeakSettings))]
public class TeakSettingsEditor : Editor {
    static TeakSettingsEditor() {
        EditorApplication.update += EditorRunOnceOnLoad;
    }
    static void EditorRunOnceOnLoad() {
        EditorApplication.update -= EditorRunOnceOnLoad;
    }

    public override void OnInspectorGUI() {

        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakSettings.AppId = EditorGUILayout.TextField("Teak App Id", TeakSettings.AppId);
        TeakSettings.APIKey = EditorGUILayout.TextField("Teak API Key", TeakSettings.APIKey);
        TeakSettings.ShortlinkDomain = EditorGUILayout.TextField("Short Link Domain", TeakSettings.ShortlinkDomain);

        EditorGUILayout.Space();
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        GUIContent justShutUpIKnowWhatImDoingContent = new GUIContent("Build Post-Processing [?]",  "When enabled, Teak will post-proces the Unity build and add dependencies, generate plist, XML, etc.");
        TeakSettings.JustShutUpIKnowWhatImDoing = !EditorGUILayout.Toggle(justShutUpIKnowWhatImDoingContent, !TeakSettings.JustShutUpIKnowWhatImDoing);
    }
}
