using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

public class ABViewer : EditorWindow
{
    [MenuItem("Assets/AB Viewer", false)]
    [MenuItem("Window/AB BuildHelper/AB Viewer",false,1)]
    static void Init()
    {
        ABViewer w = (ABViewer)EditorWindow.GetWindow(typeof(ABViewer), false, "AB Viewer", true);
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

    public class Entiy
    {
        public AssetBundle ab;
        public string[] abDepends;
        public Object[] assets;
        public Object[] depends;
    }

    public List<Entiy> enties;

    Vector2 scrollPosition;
    bool showDependencies;

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

        enties = new List<Entiy>();
        foreach (Object target in Selection.objects)
        {
            if (target is DefaultAsset)
            {
                string path = AssetDatabase.GetAssetPath(target);
                AssetBundle ab = AssetBundle.LoadFromFile(path);
                if (ab == null)
                    break;
                
                Object[] assets = ab.LoadAllAssets().OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray();
                Object[] depends = EditorUtility.CollectDependencies(assets).Where(x => !(x is MonoScript)).Except(assets).OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray();
                enties.Add(new Entiy()
                {
                    ab = ab,
                    abDepends = AssetDatabase.GetAssetBundleDependencies(ab.name, false),
                    assets = assets,
                    depends = depends
                });
            }
        }

        this.Repaint();
    }

    public void UnloadAssetBoundles()
    {
        if (enties == null)
            return;

        foreach (Entiy entiy in enties)
        {
            if (entiy.ab != null)
                entiy.ab.Unload(false);
        }
        enties = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        showDependencies = EditorGUILayout.ToggleLeft("Show Relevance", showDependencies);
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
        foreach (Entiy entiy in enties)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.button);
            EditorGUILayout.LabelField(entiy.ab.name);
            if (entiy.abDepends != null && entiy.abDepends.Length > 0)
            {
                EditorGUILayout.LabelField("Dependency: " + string.Join(",", entiy.abDepends));
            }
            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
            if (entiy.assets != null)
            {
                foreach (Object asset in entiy.assets)
                {
                    //EditorGUILayout.Foldout(true,EditorGUIUtility.ObjectContent(asset, typeof(Object)));
                    EditorGUILayout.ObjectField(asset, typeof(Object), false);
                    DragLastUI(asset);
                    if (showDependencies)
                    {
                        Object[] dependencies = EditorUtility.CollectDependencies(new Object[] { asset }).OrderBy(x => x.GetType().Name).ThenBy(x => x.name).ToArray();
                        EditorGUI.indentLevel++;
                        foreach (Object obj in dependencies)
                        {
                            if (obj != asset)
                            {
                                EditorGUILayout.ObjectField(obj, typeof(Object), true);
                                DragLastUI(obj);
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            if (!showDependencies)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                if (entiy.depends != null)
                {
                    foreach (Object asset in entiy.depends)
                    {
                        EditorGUILayout.ObjectField(asset, typeof(Object), false);
                        DragLastUI(asset);
                    }
                }
                GUI.color = Color.white;
            }
            
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DragLastUI(Object data)
    {
        if (Event.current.type == EventType.MouseDrag && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[] { data };
            DragAndDrop.StartDrag("Move Asset");
            Event.current.Use();
        }
    }
}
