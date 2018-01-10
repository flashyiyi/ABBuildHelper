using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class AssetBoundleViewer : EditorWindow
{
    [MenuItem("Window/AssetBoundleViewer")]
    static void Init()
    {
        AssetBoundleViewer w = (AssetBoundleViewer)EditorWindow.GetWindow(typeof(AssetBoundleViewer), false, "AssetBoundleViewer", true);
        w.Show();
    }

    private void OnSelectionChange()
    {
        LoadAllAssetsFromPath(Selection.objects);
    }

    public class Entiy
    {
        public string path;
        public string[] abDepends;
        public Object[] assets;
    }
    
    public List<Entiy> enties;

    Vector2 scrollPosition;
    bool showDependencies;
    
    public void LoadAllAssetsFromPath(Object[] targets)
    {
        foreach (Object target in targets)
        {
            string path = AssetDatabase.GetAssetPath(target);

            Object obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (!(obj is UnityEditor.DefaultAsset))
                return;
        }
        
        AssetBundle.UnloadAllAssetBundles(false);
        

        enties = new List<Entiy>();
        foreach (Object target in targets)
        {
            string path = AssetDatabase.GetAssetPath(target);

            AssetBundle ab = AssetBundle.LoadFromFile(path);
            if (ab == null)
                break;

            enties.Add(new Entiy()
            {
                path = ab.name,
                abDepends = AssetDatabase.GetAssetBundleDependencies(ab.name, false),
                assets = EditorUtility.CollectDependencies(ab.LoadAllAssets()).OrderBy(x => x.name).ToArray()
            });
        }

        this.Repaint();
    }

    private void OnGUI()
    {
        if (enties == null)
            return;

        showDependencies = EditorGUILayout.Toggle("Show Dependency Assets", showDependencies);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (Entiy entiy in enties)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entiy.path);
            if (entiy.abDepends != null && entiy.abDepends.Length > 0)
            {
                EditorGUILayout.LabelField("Dependency AB: " + string.Join(",", entiy.abDepends));
            }
            EditorGUILayout.EndHorizontal();

            if (entiy.assets != null)
            {
                foreach (Object asset in entiy.assets)
                {
                    EditorGUILayout.ObjectField(asset, typeof(Object), true);
                    DragLastUI(asset);
                    if (showDependencies)
                    {
                        Object[] dependencies = EditorUtility.CollectDependencies(new Object[] { asset });
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
