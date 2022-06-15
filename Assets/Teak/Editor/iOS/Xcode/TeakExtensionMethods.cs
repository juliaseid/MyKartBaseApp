#if UNITY_IOS

#region License
/* Teak -- Copyright (C) 2018-2019 Teak.io,Inc.
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
#endregion

#region References
using System;
using System.IO;

using UnityEditor.iOS.Xcode;
#endregion

namespace UnityEditor.iOS.Xcode {
    public static class TeakExtensions {
        /////
        // PlistElement
        public static bool Equals(this PlistElement element, object test) {
            if (test is string) {
                return ((string) test).Equals(element.AsString());
            } else if (test is int) {
                return ((int) test).Equals(element.AsInteger());
            } else if (test is bool) {
                return ((bool) test).Equals(element.AsBoolean());
            } else if (test is float) {
                return ((float) test) == element.AsReal();
            } else if (test is DateTime) {
                return ((DateTime) test).Equals(element.AsDate());
            } else {
                throw new NotImplementedException("Not implemented for type: " + test.GetType());
            }
        }

        /////
        // PlistElementArray
        public static void Add(this PlistElementArray array, object val) {
            if (val is string) {
                array.AddString(val as string);
            } else if (val is int) {
                array.AddInteger((int) val);
            } else if (val is bool) {
                array.AddBoolean((bool) val);
            } else if (val is float) {
                array.AddReal((float) val);
            } else if (val is DateTime) {
                array.AddDate((DateTime) val);
            } else {
                throw new NotImplementedException("Not implemented for type: " + val.GetType());
            }
        }

        public static bool ContainsElement(this PlistElementArray array, object element) {
            foreach (PlistElement e in array.values) {
                if (e.Equals(element)) {
                    return true;
                }
            }
            return false;
        }

        /////
        // PBXProject
        public static void AddFrameworksToTarget(this PBXProject project, string target, string[] frameworks, bool weak = false) {
            foreach (string f in frameworks) {
                string framework = f;
                if (!framework.EndsWith(".framework")) framework = framework + ".framework";
                if (!project.ContainsFramework(target, framework)) {
                    project.AddFrameworkToProject(target, framework, weak);
                }
            }
        }

        // From: https://stackoverflow.com/a/23697173
        public static string GetRelativePathFrom(this FileSystemInfo to, FileSystemInfo from) {
            return from.GetRelativePathTo(to);
        }

        public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to) {
            Func<FileSystemInfo, string> getPath = fsi => {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd('\\') + "\\";
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}

#endif // UNITY_IOS
