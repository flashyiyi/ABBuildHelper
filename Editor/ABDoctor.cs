using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
namespace ABBuildHelper
{
    public class ABDoctor : EditorWindow
    {
        [MenuItem("Window/AB BuildHelper/AB Doctor", false, 0)]
        static void Init()
        {
            ABDoctor w = EditorWindow.GetWindow<ABDoctor>(false, "AB Doctor", true);
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

        class RepeatData
        {
            public string abName;
            public List<Object> objects;
            public bool opened;
        }

        static bool showRepeat
        {
            get { return EditorPrefs.HasKey("ABDoctor.showRepeat") ? EditorPrefs.GetBool("ABDoctor.showRepeat") : true; }
            set { EditorPrefs.SetBool("ABDoctor.showRepeat", value); }
        }

        static bool showBuildIn
        {
            get { return EditorPrefs.HasKey("ABDoctor.showBuildIn") ? EditorPrefs.GetBool("ABDoctor.showBuildIn") : true; }
            set { EditorPrefs.SetBool("ABDoctor.showBuildIn", value); }
        }

        static bool showABList
        {
            get { return EditorPrefs.HasKey("ABDoctor.showABList") ? EditorPrefs.GetBool("ABDoctor.showABList") : true; }
            set { EditorPrefs.SetBool("ABDoctor.showABList", value); }
        }

        Dictionary<string, List<Object>> abAssets;
        Dictionary<Object, List<RepeatData>> assetDenpendGroups;
        Dictionary<Object, List<RepeatData>> repeatAssets;
        Dictionary<Object, List<RepeatData>> buildInAssets;

        bool showSubAsset = true;
        private void CollectAssets()
        {
            Dictionary<string, HashSet<Object>> abAssetDict = new Dictionary<string, HashSet<Object>>();

            //获得ab依赖的所有资源
            string[] abNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (string abName in abNames)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
                List<Object> objects = new List<Object>();
                foreach (string assetPath in assetPaths)
                {
                    if (assetPath.EndsWith(".unity"))
                    {
                        objects.Add(AssetDatabase.LoadMainAssetAtPath(assetPath));
                    }
                    else
                    {
                        objects.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath));
                    }
                }
                HashSet<Object> abAssets = new HashSet<Object>(EditorUtility.CollectDependencies(objects.ToArray()).Where(x => !(x is MonoScript)));
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
            //排序
            abAssets = new Dictionary<string, List<Object>>();
            foreach (var pair in abAssetDict)
            {
                List<Object> list = pair.Value
                    .OrderBy(x => AssetDatabase.GetAssetPath(x))
                    .ThenByDescending(x => AssetDatabase.IsMainAsset(x))
                    .ToList();
                abAssets.Add(pair.Key, list);
            }
            //统计
            assetDenpendGroups = new Dictionary<Object, List<RepeatData>>();
            foreach (var pair in abAssetDict)
            {
                foreach (Object obj in pair.Value)
                {
                    RepeatData repeatData = new RepeatData();
                    repeatData.abName = pair.Key;
                    if (!assetDenpendGroups.ContainsKey(obj))
                    {
                        assetDenpendGroups.Add(obj, new List<RepeatData>() { repeatData });
                    }
                    else
                    {
                        assetDenpendGroups[obj].Add(repeatData);
                    }
                }
            }
            //分类
            repeatAssets = new Dictionary<Object, List<RepeatData>>();
            buildInAssets = new Dictionary<Object, List<RepeatData>>();
            foreach (var pair in assetDenpendGroups)
            {
                if (pair.Value.Count > 1)
                {
                    repeatAssets.Add(pair.Key, pair.Value);
                }
                if (IsBuildIn(AssetDatabase.GetAssetPath(pair.Key)))
                {
                    //foreach (var repeatData in pair.Value)
                    //{
                    //    CollectRepeatDependencies(repeatData, pair.Key);
                    //}
                    buildInAssets.Add(pair.Key, pair.Value);
                }
            }
        }

        private void CollectRepeatDependencies(RepeatData repeatData, Object target)
        {
            Dictionary<Object, Object[]> repeatDataDepends = new Dictionary<Object, Object[]>();
            HashSet<Object> result = new HashSet<Object>();
            foreach (Object obj in abAssets[repeatData.abName])
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
            repeatData.objects = result.ToList();
        }

        private void OnEnable()
        {
            CollectAssets();
        }

        Vector2 scrollPosition;
        Dictionary<string, bool> toggleGroupData = new Dictionary<string, bool>();
        private void OnGUI()
        {
            if (GUILayout.Button("Refresh"))
            {
                CollectAssets();
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            if (repeatAssets.Count > 0)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.button);
                showRepeat = EditorGUILayout.ToggleLeft("重复打包的资源：", showRepeat);
                EditorGUILayout.EndHorizontal();
                if (showRepeat)
                {
                    EditorGUI.indentLevel++;
                    foreach (var pair in repeatAssets)
                    {
                        ShowAsset(pair.Key);
                        ShowDependencies(pair.Value, pair.Key);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            if (buildInAssets.Count > 0)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.button);
                showBuildIn = EditorGUILayout.ToggleLeft("不能依赖打包的内置资源：", showBuildIn);
                if (GUILayout.Button("替换为用户资源"))
                {
                    FixBuildInAssets();
                }
                EditorGUILayout.EndHorizontal();
                if (showBuildIn)
                {
                    EditorGUI.indentLevel++;
                    foreach (var pair in buildInAssets)
                    {
                        ShowAsset(pair.Key);
                        ShowDependencies(pair.Value, pair.Key);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.BeginHorizontal(GUI.skin.button);
            showABList = EditorGUILayout.ToggleLeft("AssetBundles内容：", showABList);
            showSubAsset = GUILayout.Toggle(showSubAsset, "Show Sub Asset");
            EditorGUILayout.EndHorizontal();
            if (showABList)
            {
                EditorGUI.indentLevel++;
                foreach (var pair in abAssets)
                {
                    if (!toggleGroupData.ContainsKey(pair.Key))
                        toggleGroupData.Add(pair.Key, false);

                    toggleGroupData[pair.Key] = EditorGUILayout.Foldout(toggleGroupData[pair.Key], pair.Key);
                    CheckDragToAssetBoundles(pair.Key);
                    if (toggleGroupData[pair.Key])
                    {
                        ShowAssets(pair.Key);
                    }
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.EndScrollView();
        }

        private void ShowDependencies(List<RepeatData> boundles, Object target)
        {
            EditorGUI.indentLevel++;
            foreach (RepeatData repeatData in boundles)
            {
                repeatData.opened = EditorGUILayout.Foldout(repeatData.opened, repeatData.abName);
                if (repeatData.opened)
                {
                    if (repeatData.objects == null)
                    {
                        CollectRepeatDependencies(repeatData, target);
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

        private void ShowAssets(string abName)
        {
            var abValues = abAssets[abName];
            EditorGUI.indentLevel++;
            foreach (var asset in abValues)
            {
                ShowAsset(asset, true);
            }
            string[] dependAbs = AssetDatabase.GetAssetBundleDependencies(abName, false);
            foreach (string depend in dependAbs)
            {
                EditorGUILayout.LabelField(depend);
                ShowAssets(depend);
            }
            EditorGUI.indentLevel--;
        }

        private void ShowAsset(Object asset, bool showInList = false)
        {
            Color oldColor = GUI.color;
            string path = AssetDatabase.GetAssetPath(asset);
            bool isSubAsset = !AssetDatabase.IsMainAsset(asset);
            if (string.IsNullOrEmpty(path))
            {
                isSubAsset = false;
                GUI.color = Color.blue;
            }
            else if (IsBuildIn(path))
            {
                isSubAsset = false;
                GUI.color = Color.yellow;
            }
            else if (isSubAsset)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            if (showInList)
            {
                if (showSubAsset || !isSubAsset)
                {
                    if (isSubAsset) EditorGUI.indentLevel++;
                    EditorGUILayout.ObjectField(asset, typeof(Object), true);
                    if (isSubAsset) EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.ObjectField(asset, typeof(Object), true);
            }

            GUI.color = oldColor;
        }

        private static bool IsBuildIn(string path)
        {
            return path.StartsWith("Resources/unity_builtin_extra") || path == "Library/unity default resources";
        }

        private void CheckDragToAssetBoundles(string adName)
        {
            if (Event.current.type == EventType.DragPerform && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)).assetBundleName = adName;
                }
                Event.current.Use();
                CollectAssets();
            }
            else if (Event.current.type == EventType.DragUpdated && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                DragAndDrop.AcceptDrag();
            }
        }

        private void FixBuildInAssets()
        {
            bool fixAll = true;
            foreach (var pair in assetDenpendGroups)
            {
                Object asset = pair.Key;
                if (IsBuildIn(AssetDatabase.GetAssetPath(asset)))
                {
                    Object repeatObject = null;
                    if (asset is Shader)
                    {
                        repeatObject = Shader.Find(asset.name);
                    }
                    else
                    {
                        string[] assetPaths = AssetDatabase.FindAssets(asset.name + " t:" + asset.GetType().Name.ToLower());
                        if (assetPaths.Length > 0)
                        {
                            repeatObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetPaths[0]), asset.GetType());
                        }
                    }
                    if (repeatObject != null)
                    {
                        foreach (RepeatData repeatData in pair.Value)
                        {
                            CollectRepeatDependencies(repeatData, asset);
                            foreach (Object obj in repeatData.objects)
                            {
                                if (IsBuildIn(AssetDatabase.GetAssetPath(obj)))
                                    continue;

                                if (repeatObject is Shader)
                                {
                                    if (obj is Material)
                                        (obj as Material).shader = repeatObject as Shader;
                                }
                                else if (repeatObject is Mesh)
                                {
                                    if (obj is MeshFilter)
                                        (obj as MeshFilter).sharedMesh = repeatObject as Mesh;
                                }
                                else if (repeatObject is Material)
                                {
                                    if (obj is Renderer)
                                        (obj as Renderer).sharedMaterial = repeatObject as Material;
                                }
                            }
                        }
                    }
                    else
                    {
                        fixAll = false;
                    }
                }
            }

            CollectAssets();
            if (!fixAll)
            {
                EditorApplication.delayCall = () =>
                {
                    if (EditorUtility.DisplayDialog("", "需要先到Unity官网下载内建文件（在点击下载后的下拉框中）并复制到工程目录，\n是否跳转到下载网站？", "确定", "取消"))
                    {
                        Application.OpenURL("https://unity3d.com/cn/get-unity/download/archive");
                    }
                };
            }
        }
    }

}
