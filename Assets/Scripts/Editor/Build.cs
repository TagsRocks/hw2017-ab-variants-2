﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build.AssetBundle;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using System.Linq;

[InitializeOnLoad]
public class MyBuildProcess
{
    static MyBuildProcess()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerDelegate);
    }

    public static void BuildPlayerDelegate(BuildPlayerOptions options)
    {
        BuildPipeline.BuildPlayer(options);
        BuildAssetBundles();
    }

    public static string bundleBuildPath
    {
        get
        {
            // [project folder]/AssetBundles
            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "AssetBundles");

            // e.g. [project folder]/AssetBundles/Android
            return Path.Combine(path, EditorUserBuildSettings.activeBuildTarget.ToString());
        }
    }

    public static string streamingAssetsBundlePath
    {
        get
        {
            var path = Path.Combine(Application.streamingAssetsPath, "bundles");
            return Path.Combine(path, EditorUserBuildSettings.activeBuildTarget.ToString());
        }
    }

    [MenuItem("AssetBundles/Build Bundles")]
    public static void BuildAssetBundles()
    {
        var outputPath = bundleBuildPath;
        
        if(Directory.Exists(outputPath))
            Directory.Delete(bundleBuildPath, true);

        Directory.CreateDirectory(outputPath);

        var results = CompileScripts();
       
        var settings = new BuildSettings();
        settings.target = EditorUserBuildSettings.activeBuildTarget;
        settings.group = EditorUserBuildSettings.selectedBuildTargetGroup;
        settings.typeDB = results.typeDB;
        settings.outputFolder = outputPath;

        var input = BuildInterface.GenerateBuildInput();

        if(input.definitions.Length == 0)
        {
            Debug.Log("No asset bundles to build.");
            return;
        }

        BuildCommandSet commands;
        if(AssetBundleBuildPipeline.GenerateCommandSet(settings, input, out commands))
        {
            BuildOutput output;
            if(AssetBundleBuildPipeline.ExecuteCommandSet(settings, commands, out output))
            {
                var bundlesToCopy = new List<string>(output.results.Select(x => x.assetBundleName));

                CopyBundlesToStreamingAssets(bundlesToCopy);
            }
        }
    }
    
    public static ScriptCompilationResult CompileScripts()
    {
        ScriptCompilationSettings input = new ScriptCompilationSettings();
        input.target = EditorUserBuildSettings.activeBuildTarget;
        input.targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        input.options = ScriptCompilationOptions.None;
        input.outputFolder = "Library/ScriptAssemblies";
        return PlayerBuildInterface.CompilePlayerScripts(input);
    }

    static void CopyBundlesToStreamingAssets(List<string> bundlesToCopy)
    {
        // First clean out the existing bundles from streaming assets
        if(Directory.Exists(streamingAssetsBundlePath))
            Directory.Delete(streamingAssetsBundlePath, true);

        Directory.CreateDirectory(streamingAssetsBundlePath);

        foreach(var bundleName in bundlesToCopy)
        {
            var copyFromPath = Path.Combine(bundleBuildPath, bundleName);
            var copyToPath = Path.Combine(streamingAssetsBundlePath, bundleName);
            Debug.LogFormat("Copying asset bundle: {0} => {1}", copyFromPath, copyToPath);

            var parentDir = Path.GetDirectoryName(copyToPath);
            Directory.CreateDirectory(parentDir);

            File.Copy(copyFromPath, copyToPath);
        }
    }

//    static ContentCatalog GenerateContentCatalog()
//    {
//    }
//
//    static BuildInput GenerateBuildInputFromContentCatalog(ContentCatalog catalog)
//    {
//    }

}
