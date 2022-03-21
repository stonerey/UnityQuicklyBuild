using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

using System.IO;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.Text;

public class EasyBuild : EditorWindow
{
    string buildPath = "BuildPath";
 
    string versionTemplate = "";
    string buildPackgaeName = "";
    string buildCompanyName = "";
    string counter = "0";
    bool isCounter = true;
  
    [MenuItem("Tools/快速编译工具")]
    static void Init()
    {
        // Get Inspector type, so we can try to autodock beside it.
        Assembly editorAsm = typeof(Editor).Assembly;
        Type inspWndType = editorAsm.GetType("UnityEditor.InspectorWindow");

        EasyBuild window;
        if (inspWndType != null)
        {
            window = GetWindow<EasyBuild>(inspWndType);
        }
        else
        {
            window = GetWindow<EasyBuild>();
        }

        window.Show();
    }



   

    void OnGUI()
    {
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);

        GUILayout.Space(25);

        buildPath = EditorGUILayout.TextField("Build Path:", buildPath);
        if (GUILayout.Button("打开文件夹"))
        {
            SetPath();
        }
        GUILayout.Space(25);


        EditorGUILayout.BeginHorizontal();
   
        GUILayout.Label("Version:", EditorStyles.boldLabel);
        GUILayout.Label(PlayerSettings.bundleVersion, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        versionTemplate = EditorGUILayout.TextField("Template Version", "1.0.$BUILD"); 
        counter  = EditorGUILayout.TextField("Base Of Growth ", counter); 
        isCounter = EditorGUILayout.Toggle("Auto-Generate Version",isCounter);

       
      
        GUILayout.Space(15);
        buildPackgaeName = EditorGUILayout.TextField("Package name", PlayerSettings.productName);
        GUILayout.Space(15);
        buildCompanyName = EditorGUILayout.TextField("Company name", PlayerSettings.companyName);

        PlayerSettings.productName = buildPackgaeName;
        PlayerSettings.companyName = buildCompanyName;

        GUILayout.Space(35);

        if (GUILayout.Button("编 译", GUILayout.ExpandWidth(true), GUILayout.MinHeight(30)))
        {
            BuildWindows64Mono();
        }



    }
    private void SetPath(  )
    {
        FilePathAttribute filePathAttr = new FilePathAttribute();
        string directory;
        if (filePathAttr.folder)
            directory = EditorUtility.OpenFolderPanel(filePathAttr.message, filePathAttr.projectPath, filePathAttr.initialNameOrFilter);
        else
            directory = EditorUtility.OpenFilePanel(filePathAttr.message, filePathAttr.projectPath, filePathAttr.initialNameOrFilter);

        // Canceled.
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        // Normalize path separators.
        directory = Path.GetFullPath(directory);

       
        buildPath = EditorGUILayout.TextField("Text Field", directory);
        Debug.Log(directory);
       
    }
    public  void BuildWindows64Mono()
    {
        if (isCounter)
        {
            int num = Convert.ToInt32(counter);
            StringBuilder sb = new StringBuilder(versionTemplate);
            sb.Replace("$BUILD", (++num).ToString());
            counter = num.ToString();
            PlayerSettings.bundleVersion = sb.ToString();
        
        }


        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
        string path = "";
        string OutputDirectory = buildPath+"/"+ buildPackgaeName+"/" + target + "/" + PlayerSettings.bundleVersion;
        switch (target)
        {
            case BuildTarget.Android:
                path = $"{OutputDirectory}/{PlayerSettings.productName + "_" + PlayerSettings.bundleVersion}.apk";
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                path = $"{OutputDirectory}/{PlayerSettings.productName+"_"+ PlayerSettings.bundleVersion}.exe";
                break;
        }
      


        buildPlayerOptions.scenes = GetEnabledScenePaths();
        string locationPathName = path;
        buildPlayerOptions.locationPathName = locationPathName;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.None;


        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        ShowExplorerOnSuccess(report.summary, locationPathName);
        LogBuildSummary(report.summary);
    }

    public class FilePathAttribute : PropertyAttribute
    {
        public bool folder = false;
        public bool allowManualEdit = true;
        public string message = "";
        public string initialNameOrFilter = "";
        public string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;

        public FilePathAttribute(bool folder = true, bool allowManualEdit = true, string message = "", string initialFolderName = "")
        {
            this.folder = folder;
            this.allowManualEdit = allowManualEdit;
            this.message = message;
            this.initialNameOrFilter = initialFolderName;
        }
    }

    #region Helper

    /// <summary>
    /// Returns the paths of the enabled scenes from Build Settings
    /// </summary>
    /// <returns></returns>
    private static string[] GetEnabledScenePaths()
    {
        var enabledScenePaths = new List<string>();
        // Get all the scenes from Build Settings
        var scenesInBuildSettings = EditorBuildSettings.scenes;

        for (int i = 0; i < scenesInBuildSettings.Length; i++)
        {
            // Check if the scene is enabled in build settings 
            if (scenesInBuildSettings[i].enabled)
            {
                enabledScenePaths.Add(scenesInBuildSettings[i].path);
            }
        }

        return enabledScenePaths.ToArray();
    }

    /// <summary>
    /// Logs a given build summary
    /// </summary>
    /// <param name="buildSummary"></param>
    private static void LogBuildSummary(BuildSummary buildSummary)
    {
        if (buildSummary.result == BuildResult.Succeeded)
        {
            DebugColorText("Build succeeded: " + SizeSuffix(buildSummary.totalSize) + " in " + buildSummary.totalTime +
                      " time", new Color(0.2734f,0.6320f,0)); ;
        }

        if (buildSummary.result == BuildResult.Failed)
        {
            DebugColorText("Build failed",Color.red);
        }
    }

    private static readonly string[] sizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    static string SizeSuffix(ulong value, int decimalPlaces = 0)
    {
        if (value < 0)
        {
            throw new ArgumentException("Bytes should not be negative", "value");
        }

        var mag = (int)Math.Max(0, Math.Log(value, 1024));
        var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
        return String.Format("{0} {1}", adjustedSize, sizeSuffixes[mag]);
    }

    /// <summary>
    /// Shows Explorer/Finder when a build completed successfully
    /// </summary>
    /// <param name="buildSummary"></param>
    /// <param name="locationPathName"></param>
    private static void ShowExplorerOnSuccess(BuildSummary buildSummary, string locationPathName)
    {
        if (buildSummary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(locationPathName);
        }
    }


    private static void DebugColorText(string str, Color color)
    {
        string text = ColorUtility.ToHtmlStringRGB(color);
        MonoBehaviour.print(string.Concat(new string[]
        {
            "<color=#",
            text,
            "><b>",
            str,
            "</b></color>"
        }));
    }
    #endregion
}