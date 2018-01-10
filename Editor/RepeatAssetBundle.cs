using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BsDiff;
using System.IO;

public class RepeatAssetBundle : EditorWindow
{
    [MenuItem("Window/Repeat AssetBundle")]
    static void Init()
    {
        RepeatAssetBundle w = (RepeatAssetBundle)EditorWindow.GetWindow(typeof(RepeatAssetBundle), false, "RepeatAssetBundle", true);
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
        repeatData.objects = new List<Object>(result);
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
        EditorGUILayout.LabelField("Repeat Assets:");
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
        EditorGUILayout.LabelField("AssetBundles:");
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
        foreach (var asset in abValues)
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
            GUI.color = Color.red;
        }
        else if (path.StartsWith("Resources/unity_builtin_extra") || path == "Library/unity default resources")
        {
            GUI.color = Color.yellow;
        }
        else if (!AssetDatabase.IsMainAsset(asset))
        {
            if (showSubAsset)
                GUI.color = Color.grey;
            else
                return;
        }

        EditorGUILayout.ObjectField(asset, typeof(Object), true);
        GUI.color = oldColor;
    }
}
