using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

public class ABPackRule : EditorWindow
{
    [MenuItem("Window/AB BuildHelper/AB PackRule", false, 2)]
    static void Init()
    {
        ABPackRule w = (ABPackRule)EditorWindow.GetWindow(typeof(ABPackRule), false, "AB PackRule", true);
        w.Show();
    }
    
    private Vector2 scrollPosition;
    private int selectIndex = -1;
    
    private void OnGUI()
    {
        ABPackRuleConfig config = AutoABNamePostprocessor.config;
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        AutoABNamePostprocessor.autoPack = EditorGUILayout.ToggleLeft("autoPack", AutoABNamePostprocessor.autoPack);
        if (GUILayout.Button("Pack"))
        {
            AutoABNamePostprocessor.PackAll();
        }
        EditorGUILayout.EndHorizontal();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        int count = config.rules.Count;
        EditorGUI.BeginChangeCheck();
        for (int i = 0;i < count;i++)
        {
            OnGUIRule(config.rules[i], selectIndex == i);
            if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                selectIndex = i;
                Event.current.Use();
            }
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(config);
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Rule"))
        {
            config.rules.Add(new ABPackRuleConfig.Rule());
            EditorUtility.SetDirty(config);
        }
        if (count > 0 && selectIndex >= 0 && GUILayout.Button("Remove Rule"))
        {
            config.rules.RemoveAt(selectIndex);
            selectIndex = -1;
            EditorUtility.SetDirty(config);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

    }

    void OnGUIRule(ABPackRuleConfig.Rule rule, bool selected)
    {
        ABPackRuleConfig config = AutoABNamePostprocessor.config;

        EditorGUILayout.BeginVertical(selected ? GUI.skin.button : GUI.skin.box);
        EditorGUILayout.BeginHorizontal();
        rule.path = EditorGUILayout.TextField("Path: ", rule.path);
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
            rule.path = GetAssetPath(DragAndDrop.paths[0]);
            Event.current.Use();
            EditorUtility.SetDirty(config);
        }
        if (GUILayout.Button("Select",GUILayout.Width(100)))
        {
            string result = EditorUtility.OpenFolderPanel("", "选择目录", "");
            if (result != null)
            {
                rule.path = GetAssetPath(result);
                GUI.FocusControl(null);
                EditorUtility.SetDirty(config);
            }
        }
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(rule.path))
        {
            rule.typeFilter = EditorGUILayout.TextField("TypeFilter: ", rule.typeFilter);
            if (Event.current.type == EventType.DragUpdated && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                DragAndDrop.AcceptDrag();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                rule.typeFilter = string.Join(",", DragAndDrop.objectReferences.Select(x => x.GetType().Name).Distinct().ToArray());
                Event.current.Use();
                EditorUtility.SetDirty(config);
            }
            rule.ruleType = EditorGUILayout.Popup("Rule: ", rule.ruleType, AutoABNamePostprocessor.packRuleNames.ToArray());
        }
        EditorGUILayout.EndVertical();
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

public class AutoABNamePostprocessor : AssetPostprocessor
{
    public delegate void PackRule(AssetImporter ai, string rulePath);
    public static string[] packRuleNames = 
    {
        "PackInOne",
        "PackByFirstDir",
        "PackByDir"
    };
    public static PackRule[] packRuleValues = 
    {
        PackInOne,
        PackByFirstDir,
        PackByDir
    };
    public static bool autoPack
    {
        get { return EditorPrefs.GetBool("ABPackRuleConfig.autoPack"); }
        set { EditorPrefs.SetBool("ABPackRuleConfig.autoPack", value); }
    }
    static ABPackRuleConfig m_Config;
    static public ABPackRuleConfig config
    {
        get {
            if (m_Config == null)
            {
                m_Config = AssetDatabase.LoadAssetAtPath<ABPackRuleConfig>("Assets/ABPackRuleConfig.asset");
                if (m_Config == null)
                {
                    m_Config = ScriptableObject.CreateInstance<ABPackRuleConfig>();
                    AssetDatabase.CreateAsset(m_Config, "Assets/ABPackRuleConfig.asset");
                    AssetDatabase.Refresh();
                }
            }
            return m_Config;
        }
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!autoPack)
            return;

        foreach (string str in importedAssets)
        {
            PackOne(str);
        }

        foreach (string str in movedAssets)
        {
            PackOne(str);
        }
    }

    public static void PackAll()
    {
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            PackOne(path);
        }
    }

    public static void PackOne(string path)
    {
        foreach (var rule in config.rules)
        {
            if (path.StartsWith("Assets/" + rule.path + "/") && rule.MatchType(AssetDatabase.GetMainAssetTypeAtPath(path).Name))
            {
                packRuleValues[rule.ruleType](AssetImporter.GetAtPath(path), rule.path);
            }
        }
    }

    private static void PackInOne(AssetImporter ai, string rulePath)
    {
        ai.assetBundleName = rulePath;
    }

    private static void PackByFirstDir(AssetImporter ai, string rulePath)
    {
        string path = ai.assetPath.Substring(8 + rulePath.Length);//"Assets/" + rulePath + "/"
        int index = path.IndexOf('/');
        if (index >= 0)
        {
            ai.assetBundleName = rulePath + "/" + path.Substring(0, index);
        }
        else
        {
            ai.assetBundleName = rulePath;
        }
    }

    private static void PackByDir(AssetImporter ai, string rulePath)
    {
        string path = ai.assetPath.Substring(8 + rulePath.Length);//"Assets/" + rulePath + "/"
        int index = path.LastIndexOf('/');
        if (index >= 0)
        {
            ai.assetBundleName = rulePath + "/" + path.Substring(0, index);
        }
        else
        {
            ai.assetBundleName = rulePath;
        }
    }
}

[Serializable]
public class ABPackRuleConfig : ScriptableObject
{
    [Serializable]
    public class Rule
    {
        public string path;
        public string typeFilter;
        public int ruleType;

        public bool MatchType(string type)
        {
            if (type == "MonoScript")
                return false;

            return string.IsNullOrEmpty(typeFilter) ? true : Array.IndexOf(typeFilter.Split(','), type) >= 0;
        }
    }
    public List<Rule> rules = new List<Rule>();
}
