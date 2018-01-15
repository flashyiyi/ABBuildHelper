using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ABBuildHelper
{
    public class ABBuild : EditorWindow
    {
        [MenuItem("Window/AB BuildHelper/AB Build", false, 4)]
        static void Init()
        {
            ABBuild w = EditorWindow.GetWindow<ABBuild>(false, "AB Build", true);
            w.Show();
        }

        enum CompressOption
        {
            Uncompressed,
            StandardCompression,
            ChunkBasedCompression
        }

        BuildTarget buildTarget
        {
            get { return EditorPrefs.HasKey("ABBuild.buildTarget") ? (BuildTarget)EditorPrefs.GetInt("ABBuild.buildTarget") : BuildTarget.StandaloneWindows; }
            set { EditorPrefs.SetInt("ABBuild.buildTarget", (int)value); }
        }
        string outputPath
        {
            get { return EditorPrefs.HasKey("ABBuild.outputPath") ? EditorPrefs.GetString("ABBuild.outputPath") : "AssetBundles"; }
            set { EditorPrefs.SetString("ABBuild.outputPath", value); }
        }

        CompressOption compressOption
        {
            get { return EditorPrefs.HasKey("ABBuild.compressOption") ? (CompressOption)EditorPrefs.GetInt("ABBuild.compressOption") : CompressOption.ChunkBasedCompression; }
            set { EditorPrefs.SetInt("ABBuild.compressOption", (int)value); }
        }

        bool forceBuild
        {
            get { return EditorPrefs.GetBool("ABBuild.forceBuild"); }
            set { EditorPrefs.SetBool("ABBuild.forceBuild", value); }
        }

        private void OnGUI()
        {
            buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", buildTarget);

            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            if (Event.current.type == EventType.DragUpdated && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                if (DragAndDrop.objectReferences[0] is DefaultAsset)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.DragPerform && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                outputPath = GetAssetPath(DragAndDrop.paths[0]);
                GUI.FocusControl(null);
                Event.current.Use();
            }
            if (GUILayout.Button("Brower"))
            {
                string result = EditorUtility.OpenFolderPanel("", "选择目录", "");
                if (result != null)
                {
                    outputPath = GetAssetPath(result);
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            compressOption = (CompressOption)EditorGUILayout.EnumPopup("Compress Option", compressOption);

            forceBuild = EditorGUILayout.Toggle("Rebuild", forceBuild);

            if (GUILayout.Button("Build"))
            {
                BuildAssetBundleOptions options = 0;
                switch (compressOption)
                {
                    case CompressOption.Uncompressed: options = options | BuildAssetBundleOptions.UncompressedAssetBundle; break;
                    case CompressOption.ChunkBasedCompression: options = options | BuildAssetBundleOptions.ChunkBasedCompression; break;
                }
                if (forceBuild)
                {
                    options = options | BuildAssetBundleOptions.ForceRebuildAssetBundle;
                }

                string path = Application.dataPath + "/" + outputPath + "/" + buildTarget.ToString();
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                BuildPipeline.BuildAssetBundles(path, options, buildTarget);
                AssetDatabase.Refresh();
            }
        }



        private string GetAssetPath(string result)
        {
            if (result.StartsWith(Application.dataPath))
                return result == Application.dataPath ? "" : result.Substring(Application.dataPath.Length + 1);
            else if (result.StartsWith("Assets"))
                return result == "Assets" ? "" : result.Substring("Assets/".Length);
            return null;
        }
    }

}