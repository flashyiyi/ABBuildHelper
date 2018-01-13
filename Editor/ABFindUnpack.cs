using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ABFindUnpack : EditorWindow
{
    [MenuItem("Window/AB BuildHelper/AB FindUnpack",false,2)]
    static void Init()
    {
        ABFindUnpack w = (ABFindUnpack)EditorWindow.GetWindow(typeof(ABFindUnpack), false, "AB FindUnpack", true);
        w.Show();
    }

    List<Object> assets;
    Vector2 scrollPosition;
    string folder;

    public void CollectData()
    {
        List<string> assetPaths = new List<string>();
        foreach (string abName in AssetDatabase.GetAllAssetBundleNames())
        {
            assetPaths.AddRange(AssetDatabase.GetAssetPathsFromAssetBundle(abName));
        }
        assets = new List<Object>();
        string filter = "Assets/" + folder + (folder == "" ? "" : "/");
        foreach (string path in AssetDatabase.GetAllAssetPaths().Except(AssetDatabase.GetDependencies(assetPaths.ToArray())).OrderBy(x => x))
        {
            if (path.StartsWith(filter))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (!(obj is DefaultAsset || obj is MonoScript))
                    assets.Add(obj);
            }
        }
    }

    private void OnEnable()
    {
        folder = EditorPrefs.GetString("ABFindUnpack.folder");
        if (string.IsNullOrEmpty(folder))
        {
            folder = "";
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        folder = EditorGUILayout.TextField(folder);
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
            SetFolder(DragAndDrop.paths[0]);
            Event.current.Use();
        }
        if (GUILayout.Button("Select Root Path"))
        {
            string result = EditorUtility.OpenFolderPanel("", "选择目录", "");
            if (result != null)
            {
                SetFolder(result);
                GUI.FocusControl(null);
            }
        }
        if (GUILayout.Button("Find"))
        {
            CollectData();
        }
        EditorGUILayout.EndHorizontal();
        if (assets != null)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (Object asset in assets)
            {
                EditorGUILayout.ObjectField(asset, typeof(Object), true);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void SetFolder(string result)
    {
        if (result.StartsWith(Application.dataPath))
            folder = result == Application.dataPath ? "" : result.Substring(Application.dataPath.Length + 1);
        else if (result.StartsWith("Assets"))
            folder = result == "Assets" ? "" : result.Substring("Assets/".Length);

        EditorPrefs.SetString("ABFindUnpack.folder", folder);
    }
}
