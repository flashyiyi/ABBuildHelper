using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace ABBuildHelper
{
    public class ABViewer : EditorWindow
    {
        [MenuItem("Assets/AB Viewer", false)]
        [MenuItem("Window/AB BuildHelper/AB Viewer", false, 1)]
        static void Init()
        {
            ABViewer w = EditorWindow.GetWindow<ABViewer>(false, "AB Viewer", true);
            w.Show();
        }

        [MenuItem("Assets/AB Viewer", true)]
        static bool IsAssetVaild()
        {
            foreach (Object target in Selection.objects)
            {
                if (target is UnityEditor.DefaultAsset)
                    return true;
            }
            return false;
        }

        public class ABEntiy
        {
            public AssetBundle ab;
            public string[] abDepends;
            public AssetEntiy[] assets;
            public Object[] depends;
        }

        public class AssetEntiy
        {
            public Object asset;
            public Object[] depends;
        }

        public List<ABEntiy> enties;

        Vector2 scrollPosition;
        static bool showDependencies = false;
        HashSet<Object> openedAsset;

        private void OnEnable()
        {
            LoadAssetBoundles();
        }

        private void OnDisable()
        {
            UnloadAssetBoundles();
        }

        private void OnSelectionChange()
        {
            if (IsAssetVaild())
                LoadAssetBoundles();
        }

        public void LoadAssetBoundles()
        {
            UnloadAssetBoundles();
            openedAsset = new HashSet<Object>();

            enties = new List<ABEntiy>();
            foreach (Object target in Selection.objects)
            {
                if (target is DefaultAsset)
                {
                    string path = AssetDatabase.GetAssetPath(target);
                    AssetBundle ab = AssetBundle.LoadFromFile(path);
                    if (ab == null)
                        break;

                    Object[] assets = ab.LoadAllAssets().Where(x => !(x is MonoScript) && x != null).OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray();
                    int count = assets.Length;
                    AssetEntiy[] assetEntiys = new AssetEntiy[count];
                    for (int i = 0;i < count;i++)
                    {
                        Object asset = assets[i];
                        assetEntiys[i] = new AssetEntiy() { asset = asset, depends = EditorUtility.CollectDependencies(new Object[] { asset }).Where(x => !(x is MonoScript) && x != null).Except(new Object[] { asset }).OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray() };
                    }

                    enties.Add(new ABEntiy()
                    {
                        ab = ab,
                        abDepends = AssetDatabase.GetAssetBundleDependencies(ab.name, false),
                        assets = assetEntiys,
                        depends = assetEntiys.SelectMany(x => x.depends).Distinct().OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray()
                    });
                }
            }

            this.Repaint();
        }

        public void UnloadAssetBoundles()
        {
            if (enties == null)
                return;

            openedAsset = null;

            foreach (ABEntiy entiy in enties)
            {
                if (entiy.ab != null)
                    entiy.ab.Unload(false);
            }
            enties = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            showDependencies = EditorGUILayout.ToggleLeft("Show Dependencies", showDependencies);
            if (GUILayout.Button("Unload All AB"))
            {
                AssetBundle.UnloadAllAssetBundles(false);
                LoadAssetBoundles();
            }
            EditorGUILayout.EndHorizontal();
            if (enties == null || enties.Count == 0)
            {
                EditorGUILayout.LabelField("Select a AssetBoundle File", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                return;
            }
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (ABEntiy entiy in enties)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.button);
                EditorGUILayout.LabelField(string.IsNullOrEmpty(entiy.ab.name) ? "(No Name)" : entiy.ab.name);
                if (entiy.abDepends != null && entiy.abDepends.Length > 0)
                {
                    EditorGUILayout.LabelField("Dependency: " + string.Join(",", entiy.abDepends));
                }
                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;
                if (entiy.assets != null)
                {
                    foreach (AssetEntiy item in entiy.assets)
                    {
                        DrawAsset(item.asset, item.depends.Length > 0);
                        if (openedAsset.Contains(item.asset))
                        {
                            EditorGUI.indentLevel++;
                            foreach (Object obj in item.depends)
                            {
                                if (obj != item.asset)
                                {
                                    DrawAsset(obj,false);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                if (showDependencies)
                {
                    GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    foreach (Object asset in entiy.depends)
                    {
                        DrawAsset(asset,false);
                    }
                    GUI.color = Color.white;
                }

            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAsset(Object asset, bool isFolder)
        {
            Rect r = EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));
            if (isFolder)
            {
                EditorGUI.BeginChangeCheck();
                bool flag = EditorGUILayout.Foldout(openedAsset.Contains(asset), EditorGUIUtility.ObjectContent(asset, asset.GetType()));
                if (EditorGUI.EndChangeCheck())
                {
                    if (flag)
                        openedAsset.Add(asset);
                    else
                        openedAsset.Remove(asset);
                }
            }
            else
            {
                EditorGUILayout.LabelField(EditorGUIUtility.ObjectContent(asset, asset.GetType()));
            }

            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                if (asset is GameObject)
                {
                    string url = EditorUtility.SaveFilePanelInProject("Export To", asset.name, "prefab", null);
                    if (!string.IsNullOrEmpty(url))
                    {
                        PrefabUtility.CreatePrefab(url, asset as GameObject);

                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(url);
                        foreach (SkinnedMeshRenderer skinMesh in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            if (skinMesh.sharedMesh != null)
                            {
                                string name = skinMesh.sharedMesh.name;
                                skinMesh.sharedMesh = Object.Instantiate(skinMesh.sharedMesh);
                                skinMesh.sharedMesh.name = name;
                                AssetDatabase.AddObjectToAsset(skinMesh.sharedMesh, url);
                            }
                        }
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                else
                {
                    string url = EditorUtility.SaveFilePanelInProject("Export To", asset.name, "asset", null);
                    if (!string.IsNullOrEmpty(url))
                    {
                        AssetDatabase.CreateAsset(Object.Instantiate(asset), url);
                    }
                }
                
            }
            EditorGUILayout.EndHorizontal();
            
            if (Event.current.clickCount >= 1 && r.Contains(Event.current.mousePosition))
            {
                AssetDatabase.OpenAsset(asset);
            }
            else if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                DragAndDrop.objectReferences = new Object[] { asset };
                DragAndDrop.StartDrag("Move Asset");
                Event.current.Use();
            }
        }
    }

}
