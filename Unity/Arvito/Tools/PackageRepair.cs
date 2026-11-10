// Copied into a disposable EMPTY Unity project's Assets/Editor by Repair-Packages.ps1.
// Keeping this outside the game's Assets avoids its existing package compilation errors.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ArvitoPackageRepair
{
    [Serializable] public class Recipe { public string[] requests; public string successFile; }
    [InitializeOnLoad]
    public static class Installer
    {
        const string Active = "Arvito.PackageRepair.Active", Deadline = "Arvito.PackageRepair.Deadline";
        static Recipe recipe;
        static ListRequest list;
        static AddAndRemoveRequest change;
        static Installer()
        {
            // Package changes can reload assemblies. Continue from the registered graph.
            if (SessionState.GetBool(Active, false)) EditorApplication.delayCall += BeginList;
        }
        public static void Install()
        {
            SessionState.SetBool(Active, true);
            SessionState.SetFloat(Deadline, (float)(EditorApplication.timeSinceStartup + 600));
            BeginList();
        }
        static void BeginList()
        {
            try
            {
                var path = Environment.GetEnvironmentVariable("ARVITO_PACKAGE_REPAIR_RECIPE");
                recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(path));
                if (recipe.requests == null || recipe.requests.Length == 0 || string.IsNullOrEmpty(recipe.successFile)) throw new InvalidDataException("Missing repair recipe.");
                Debug.Log("[Arvito Packages] Inspecting the temporary project's package graph.");
                list = Client.List(offlineMode: true, includeIndirectDependencies: false);
                EditorApplication.update -= Poll; EditorApplication.update += Poll;
            }
            catch (Exception e) { Fail(e.Message); }
        }
        static bool Matches(IEnumerable<PackageInfo> installed)
        {
            var actual = installed.Where(p => p.isDirectDependency).ToDictionary(p => p.name, p => p.version);
            var expected = recipe.requests.Select(Split).ToDictionary(p => p[0], p => p[1]);
            return actual.Count == expected.Count && expected.All(p => actual.TryGetValue(p.Key, out var version) && version == p.Value);
        }
        static string[] Split(string request) => request.Split(new[] { '@' }, 2);
        static void Poll()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0)) { Fail("UPM resolution timed out after 10 minutes."); return; }
                if (list != null)
                {
                    if (!list.IsCompleted) return;
                    if (list.Status != StatusCode.Success) { Fail(list.Error?.message); return; }
                    if (Matches(list.Result)) { Complete(); return; }
                    var wanted = new HashSet<string>(recipe.requests.Select(r => Split(r)[0]));
                    var remove = list.Result.Where(p => p.isDirectDependency && !wanted.Contains(p.name)).Select(p => p.name).ToArray();
                    Debug.Log("[Arvito Packages] Resolving: " + string.Join(", ", recipe.requests));
                    Debug.Log("[Arvito Packages] Removing temporary-project extras: " + string.Join(", ", remove));
                    change = Client.AddAndRemove(recipe.requests, remove);
                    list = null; return;
                }
                if (change == null || !change.IsCompleted) return;
                if (change.Status != StatusCode.Success) { Fail(change.Error?.message); return; }
                // Read the registered graph after resolution rather than assuming what
                // subset a particular Editor returns in AddAndRemoveRequest.Result.
                change = null;
                list = Client.List(offlineMode: true, includeIndirectDependencies: false);
            }
            catch (Exception e) { Fail(e.Message); }
        }
        static void Complete()
        {
            if (!File.Exists("Packages/manifest.json") || !File.Exists("Packages/packages-lock.json")) { Fail("UPM did not produce its manifest and lock."); return; }
            File.WriteAllText(recipe.successFile, "UPM resolution succeeded");
            Debug.Log("[Arvito Packages] Unity Client API resolution succeeded.");
            Stop(); EditorApplication.Exit(0);
        }
        static void Fail(string message)
        {
            Debug.LogError("[Arvito Packages] " + message);
            Stop(); EditorApplication.Exit(1);
        }
        static void Stop() { EditorApplication.update -= Poll; SessionState.SetBool(Active, false); }
    }
}
#endif
