# if UNITY_EDITOR 
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Scrutinizer : MonoBehaviour {

    public GameObject targetPrefabRoot;
    public string targetPrefabRootAssetName;
    public List<string> prefabPaths;
    public List<GameObject> prefabPathMap;
    public string assetBundlePath;
    public IEnumerator CreatePrefabs(GameObject _targetPrefab)
    {
        targetPrefabRoot = _targetPrefab;
        return CreatePrefabsAsync();
    }

    IEnumerator CreatePrefabsAsync()
    {
        var allPrefabs = targetPrefabRoot.GetComponentsInChildren<Transform>(true);
        int count = 0;
        int total = allPrefabs.Length;
        string prefabName;
        EditorUtility.DisplayProgressBar("生成測試prefab", "物件 : " + "開始掃描Prefab", (float)count / (float)total);
        prefabPaths = new List<string>();
        prefabPathMap = new List<GameObject>();
        string path = "Assets/Prefab_Temporary_Detector";
        if (AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("已存在Prefab，需要刪除");
            //foreach (string file in System.IO.Directory.GetFiles(path + "/Resources"))
            //{
            //    if (file.Contains(".meta")) continue;
            //    prefabPaths.Add(file);
            //}
           
            //CreateAssetBundle();
        }
        else
        {
            AssetDatabase.CreateFolder("Assets", "Prefab_Temporary_Detector");
            AssetDatabase.CreateFolder(path, "Resources");
            foreach (var trans in allPrefabs)
            {
                if (trans.gameObject == targetPrefabRoot)
                {
                    prefabName = "Detector-" + trans.name;
                }
                else
                {
                    GameObject tempGo = trans.gameObject;
                    prefabName = trans.name;
                    
                    while (tempGo != targetPrefabRoot)
                    {
                        
                        tempGo = tempGo.transform.parent.gameObject;
                        prefabName = tempGo.name + "-" + prefabName;
                    }
                    prefabName = "Detector-" + prefabName;
                    
                }
                
                GameObject tempPrefab = PrefabUtility.SaveAsPrefabAsset(trans.gameObject, path + "/Resources/" + prefabName + ".prefab");
                string assetPath = AssetDatabase.GetAssetPath(tempPrefab);
                if (trans.gameObject == targetPrefabRoot)
                {
                    targetPrefabRootAssetName = assetPath.ToLower();
                }
                prefabPaths.Add(assetPath.ToLower());
                prefabPathMap.Add(trans.gameObject);
                AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(targetPrefabRoot.name + "-scrutinizing", "");
                yield return null;
                ++count;
                EditorUtility.DisplayProgressBar("生成測試prefab", "物件 : " + prefabName, (float)count / (float)total);

            }
            
            EditorUtility.DisplayProgressBar("生成測試prefab", "生成完畢，開始建立測試AssetBundles", (float)count / (float)total);
            CreateAssetBundle();
        }
        
    }

    void CreateAssetBundle()
    {
        assetBundlePath = "Assets/StreamingAssets/" + targetPrefabRoot.name + "-scrutinizing.assetBundles";

        if (System.IO.File.Exists(assetBundlePath))
        {
            System.IO.File.Delete(assetBundlePath);
            System.IO.File.Delete(assetBundlePath + ".meta");
            AssetDatabase.Refresh();
        }
        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = targetPrefabRoot.name + "-scrutinizing.assetBundles";
        build.assetNames = prefabPaths.ToArray();
        BuildPipeline.BuildAssetBundles("Assets/StreamingAssets",new AssetBundleBuild[] { build }, BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
    }

}
#endif