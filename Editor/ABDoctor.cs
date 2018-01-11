using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ABDoctor : EditorWindow
{
    [MenuItem("Window/AB BuildHelper/AB Doctor")]
    static void Init()
    {
        ABDoctor w = (ABDoctor)EditorWindow.GetWindow(typeof(ABDoctor), false, "AB Doctor", true);
        w.Show();
    }

    private string[] GetPaths(UnityEngine.Object[] objects)
    {
        int count = objects.Length;
        string[] paths = new string[count];
        for (int i = 0; i < count; i++)
        {
            paths[i] = AssetDatabase.GetAssetPath(objects[i]);
        }
        return paths;
    }

    Dictionary<string, HashSet<Object>> abAssetDict;
    class RepeatData
    {
        public string abName;
        public List<Object> objects;
        public bool opened;
    }
    Dictionary<Object, List<RepeatData>> repeatCount;
    bool showSubAsset = true;
    private void CollectRepeatAssets()
    {
        abAssetDict = new Dictionary<string, HashSet<Object>>();

        //获得ab依赖的所有资源
        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string abName in abNames)
        {
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
            List<Object> objects = new List<Object>();
            foreach (string assetPath in assetPaths)
            {
                objects.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath));
            }
            HashSet<Object> abAssets = new HashSet<Object>(EditorUtility.CollectDependencies(objects.ToArray()));
            abAssetDict.Add(abName, abAssets);
        }
        //移除ab间依赖
        foreach (var pair in abAssetDict)
        {
            string[] dependAbs = AssetDatabase.GetAssetBundleDependencies(pair.Key, true);
            foreach (string depend in dependAbs)
            {
                foreach (Object obj in abAssetDict[depend])
                {
                    pair.Value.Remove(obj);
                }
            }
        }
        //统计
        repeatCount = new Dictionary<Object, List<RepeatData>>();
        foreach (var pair in abAssetDict)
        {
            foreach (Object obj in pair.Value)
            {
                if (obj is MonoScript)
                    continue;

                RepeatData repeatData = new RepeatData();
                repeatData.abName = pair.Key;
                if (!repeatCount.ContainsKey(obj))
                {
                    repeatCount.Add(obj, new List<RepeatData>() { repeatData });
                }
                else
                {
                    repeatCount[obj].Add(repeatData);
                }
            }
        }
    }

    private void CollectRepeatDependencies(RepeatData repeatData, Object target)
    {
        Dictionary<Object, Object[]> repeatDataDepends = new Dictionary<Object, Object[]>();
        HashSet<Object> result = new HashSet<Object>();
        foreach (Object obj in abAssetDict[repeatData.abName])
        {
            if (obj == target)
                continue;

            Object[] depends = EditorUtility.CollectDependencies(new Object[] { obj });
            if (System.Array.IndexOf<Object>(depends, target) >= 0)
            {
                result.Add(obj);
                repeatDataDepends.Add(obj, depends);
            }
        }

        foreach (var pair in repeatDataDepends)
        {
            if (result.Contains(pair.Key))
            {
                foreach (var depend in pair.Value)
                {
                    if (depend != pair.Key && result.Contains(depend))
                    {
                        result.Remove(pair.Key);
                        break;
                    }
                }
            }
        }
        repeatData.objects = result.OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToList();
    }

    private void OnEnable()
    {
        CollectRepeatAssets();
    }

    Vector2 scrollPosition;
    Dictionary<string, bool> toggleGroupData = new Dictionary<string, bool>();
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
        {
            CollectRepeatAssets();
        }
        showSubAsset = GUILayout.Toggle(showSubAsset, "Show Sub Asset");
        EditorGUILayout.EndHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("重复打包的资源：");
        EditorGUI.indentLevel++;
        foreach (var pair in repeatCount)
        {
            if (pair.Value.Count > 1)
            {
                ShowAsset(pair.Key);
                EditorGUI.indentLevel++;
                foreach (RepeatData repeatData in pair.Value)
                {
                    repeatData.opened = EditorGUILayout.Foldout(repeatData.opened,repeatData.abName);
                    if (repeatData.opened)
                    {
                        if (repeatData.objects == null)
                        {
                            CollectRepeatDependencies(repeatData, pair.Key);
                        }

                        EditorGUI.indentLevel++;
                        foreach (Object obj in repeatData.objects)
                        {
                            ShowAsset(obj);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("AssetBundles列表：");
        EditorGUI.indentLevel++;
        foreach (var pair in abAssetDict)
        {
            if (!toggleGroupData.ContainsKey(pair.Key))
                toggleGroupData.Add(pair.Key, false);
            toggleGroupData[pair.Key] = EditorGUILayout.Foldout(toggleGroupData[pair.Key], pair.Key);
            if (toggleGroupData[pair.Key])
            {
                ShowAssets(pair.Key);
            }
        }
        EditorGUI.indentLevel--;
        GUILayout.EndScrollView();
    }

    private void ShowAssets(string abName)
    {
        HashSet<Object> abValues = abAssetDict[abName];
        EditorGUI.indentLevel++; 
        foreach (var asset in abValues.OrderBy(x => x.GetType().Name).ThenBy(x => x.name))
        {
            ShowAsset(asset);
        }
        string[] dependAbs = AssetDatabase.GetAssetBundleDependencies(abName, false);
        foreach (string depend in dependAbs)
        {
            EditorGUILayout.LabelField(depend);
            ShowAssets(depend);
        }
        EditorGUI.indentLevel--;
    }

    private void ShowAsset(Object asset)
    {
        Color oldColor = GUI.color;
        string path = AssetDatabase.GetAssetPath(asset);
        
        if (string.IsNullOrEmpty(path))
        {
            GUI.color = Color.blue;
        }
        else if (path.StartsWith("Resources/unity_builtin_extra") || path == "Library/unity default resources")
        {
            GUI.color = Color.yellow;
        }
        else if (!AssetDatabase.IsMainAsset(asset))
        {
            if (showSubAsset)
                GUI.color = new Color(0.7f,0.7f,0.7f,1f);
            else
                return;
        }

        EditorGUILayout.ObjectField(asset, typeof(Object), true);

        if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            if (GUI.color == Color.yellow)
            {
                if (EditorUtility.DisplayDialog("", "内置资源无法依赖打包，是否要现在解决这个问题？", "确认","取消"))
                {
                    Debug.Log("开发中");
                }
            }
            else if (GUI.color == Color.blue)
            {
                EditorUtility.DisplayDialog("", "这是图集纹理，将相关的Sprite打入同一个包即可避免重复", "确认");
            }
        }
        GUI.color = oldColor;
    }
}
