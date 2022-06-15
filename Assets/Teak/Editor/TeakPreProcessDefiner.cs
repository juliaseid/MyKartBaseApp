using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

using System.Collections.Generic;

class TeakPreProcessDefiner : IPreprocessBuildWithReport {
    public int callbackOrder { get { return 0; } }
    public static readonly string[] TeakDefines = new string[] {
        "TEAK_2_0_OR_NEWER",
        "TEAK_2_1_OR_NEWER",
        "TEAK_2_2_OR_NEWER",
        "TEAK_2_3_OR_NEWER",
        "TEAK_3_0_OR_NEWER",
        "TEAK_3_1_OR_NEWER",
        "TEAK_3_2_OR_NEWER",
        "TEAK_3_3_OR_NEWER",
        "TEAK_3_4_OR_NEWER",
        "TEAK_4_0_OR_NEWER",
        "TEAK_4_1_OR_NEWER"
    };

    public void OnPreprocessBuild(BuildReport report) {
        BuildTargetGroup buildTargetGroup = report.summary.platformGroup;
        string[] existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');

        HashSet<string> updatedDefines = new HashSet<string>(existingDefines);
        updatedDefines.RemoveWhere(define => define.StartsWith("TEAK_") && define.EndsWith("_OR_NEWER"));
        updatedDefines.UnionWith(TeakDefines);

        string[] defines = new string[updatedDefines.Count];
        updatedDefines.CopyTo(defines);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
    }
}
