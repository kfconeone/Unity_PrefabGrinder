using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using Newtonsoft.Json;
using Kfc.Grinder;
using UnityEngine.Networking;

public class GrinderMenuItems : EditorWindow
{

    [MenuItem("GameObject/Prefab建立/1. 將物件設定為Grinder Prefab", priority = 0)]
    static void SetGameObjectToGrinderPrefab()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];

        var prefabType = PrefabUtility.GetPrefabAssetType(selectedGameObject);
        if (prefabType == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("需先設定成 Prefab 物件");
            return;
        }

        CustomHierarchyView.grinderPrefabs.Add(PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject));

        Debug.Log("已設定成Grinder Prefab");
    }


    [MenuItem("GameObject/Prefab建立/2. 安裝Reference產生器", priority = 0)]
    static void InstallReferenceRecoverer()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];

        selectedGameObject.AddComponent<ReferenceRecoverer>();
        if (selectedGameObject.GetComponents<IApplier>().Length > 0)
        {
            foreach (var applier in selectedGameObject.GetComponents<IApplier>())
            {
                DestroyImmediate(applier as Component);
            }
        }

        Debug.Log("安裝完畢");
    }

    [MenuItem("GameObject/Prefab建立/3. 建立Ground_Prefab", priority = 1)]
    static void CreatePrefab()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }

        if (CustomHierarchyView.isErrorExist)
        {
            Debug.Log("存在錯誤，取消建立");
            return;
        }
        GameObject selectedPrefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.gameObjects[0]);
        Swing.Editor.EditorCoroutine.start(Grinding(selectedPrefabRoot));

    }

    static IEnumerator Grinding(GameObject _selectedPrefabRoot)
    {
        string PrefabPath = "Assets/Prefab_" + _selectedPrefabRoot.name;
        
        if (!AssetDatabase.IsValidFolder(PrefabPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab_" + _selectedPrefabRoot.name);
            AssetDatabase.CreateFolder(PrefabPath, "Origin");
            AssetDatabase.CreateFolder(PrefabPath, "Packed");
        }

        PrefabUtility.SaveAsPrefabAsset(_selectedPrefabRoot, PrefabPath + "/Origin/" + _selectedPrefabRoot.name + ".prefab");

        GameObject tempGo = GameObject.Instantiate(_selectedPrefabRoot, _selectedPrefabRoot.transform.parent);
        tempGo.name = _selectedPrefabRoot.name;
        tempGo.SetActive(false);
        var swapList = tempGo.GetComponentsInChildren(typeof(AssetSwapper), true).Select(tempComponent => tempComponent.gameObject).ToList();

        int count = 0;
        int total = swapList.Count;
        foreach (GameObject go in swapList)
        {
            if (go == null)
            {
                Debug.LogError("物件已被刪除，檢查是否重複Add Component [AssetSwapper]");
                break;
            }
            string name = go.name;
            GameObject swapGo = new GameObject(go.name);
            var swapper = swapGo.AddComponent<AssetSwapper>();
            swapper.prefabName = GetPrefabName(tempGo.transform, go.transform);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + "/Packed/" + swapper.prefabName + ".prefab");
            Debug.Log("建立prefab : " + swapper.prefabName + ".prefab");
            swapGo.transform.parent = go.transform.parent;
            swapGo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            UnityEngine.Object.DestroyImmediate(go);
            yield return null;
            ++count;
            EditorUtility.DisplayProgressBar("生成Prefab中", "物件 : " + name, (float)count / (float)total);


        }
        PrefabUtility.SaveAsPrefabAsset(tempGo, PrefabPath + "/Packed/" + tempGo.name + ".prefab");
        UnityEngine.Object.DestroyImmediate(tempGo);
        EditorUtility.ClearProgressBar();
        Debug.Log("結束");
    }

    [MenuItem("GameObject/Prefab建立/還原物件為普通Prefab", priority = 15)]
    static void ClearSwapper()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];
        if (selectedGameObject.GetComponentsInChildren<AssetSwapper>().Length == 0 && !CustomHierarchyView.grinderPrefabs.Contains(PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject)))
        {
            Debug.Log("並非是Grinder物件");
            return;
        }

        if (EditorUtility.DisplayDialog("還原成普通Prefab", "是否還原？(不可逆)", "確定", "取消"))
        {
            var components = PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject).GetComponentsInChildren(typeof(AssetSwapper), true);
            foreach (Component com in components)
            {
                GameObject.DestroyImmediate(com);
            }

            if (CustomHierarchyView.grinderPrefabs.Contains(PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject)))
            {
                CustomHierarchyView.grinderPrefabs.Remove(PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject));
            }

            Debug.Log("還原完畢");
        }
        else
        {
            Debug.Log("取消還原");
        }
        
    }

    [MenuItem("GameObject/Prefab建立/選取物件全部變成Swapper(優先選擇兒子，並自動忽略父子中已經為Swapper的物件)", priority = 16)]
    static void AddSwapperToObjects()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponentsInParent<AssetSwapper>().Length > 0)
            {
                foreach (var swapper in go.GetComponentsInParent<AssetSwapper>())
                {
                    GameObject.DestroyImmediate(swapper);
                }
            }
            go.AddComponent<AssetSwapper>();

        }

        Debug.Log("Swapper設定完畢");
    }

    [MenuItem("GameObject/Prefab建立/進行深度效能檢測(beta)", priority = 17)]
    static void AssetBundleLoadingAnalyze()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject targetPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.gameObjects[0]);

        if (EditorUtility.DisplayDialog("深度性能檢測", "深度測試非常耗時，確定進行？", "確定", "取消"))
        {
            var scrutinizer = new GameObject("[scrutinizer]").AddComponent<Scrutinizer>();
            Swing.Editor.EditorCoroutine.start(scrutinizer.CreatePrefabs(targetPrefab));
        }
        else
        {
            Debug.Log("取消檢測");
        }
    }

    [MenuItem("GameObject/Prefab建立/進行Prefab生成效能檢測", priority = 17)]
    static void InstantiationAnalyze()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject targetPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.gameObjects[0]);

        if (EditorUtility.DisplayDialog("生成性能檢測", "檢測時間根據prefab大小，有可能需要數分鐘，確定進行？", "確定", "取消"))
        {
            Swing.Editor.EditorCoroutine.start(Loading(targetPrefab,0));       
        }
        else
        {
            Debug.Log("取消檢測");
        }
    }

    [MenuItem("GameObject/Prefab建立/進行Prefab操作效能檢測", priority = 17)]
    static void ManipulateAnalyze()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }

        GameObject targetPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.gameObjects[0]);

        if (EditorUtility.DisplayDialog("操作性能檢測", "檢測時間根據prefab大小，有可能需要數分鐘，確定進行？", "確定", "取消"))
        {
            Swing.Editor.EditorCoroutine.start(Loading(targetPrefab,1));
        }
        else
        {
            Debug.Log("取消檢測");
        }

    }

    static int count;
    static int total;
    static IEnumerator Loading(GameObject targetPrefab,int _type)
    {
        var allPrefabs = targetPrefab.GetComponentsInChildren<Transform>(true);
        count = 0;
        total = allPrefabs.Length;
        float standardToken = 0;
        EditorUtility.DisplayProgressBar("檢測中...0% (同物件權重誤差約為30)", "檢測物件 : " + "開始掃描Prefab", (float)count / (float)total);

        foreach (var trans in allPrefabs)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            if (_type == 0)
            {
                for (int i = 0; i < 100; ++i)
                {
                    GameObject go = GameObject.Instantiate(trans.gameObject);
                    GameObject.DestroyImmediate(go);
                    
                }
            }
            else if (_type == 1)
            {
                GameObject go = GameObject.Instantiate(trans.gameObject);
                for (int i = 0; i < 10000; ++i)
                {
                    go.transform.localScale += new Vector3(i,i,i);
                    go.transform.localPosition += new Vector3(i, i, i);
                    go.transform.localEulerAngles += new Vector3(i, i, i);
                }
                GameObject.DestroyImmediate(go);
            }
            
            sw.Stop();
            float token = 0;
            if (_type == 0)
            { 
                token = sw.Elapsed.Ticks / 100f;
            }
            else if (_type == 1)
            {
                token = sw.Elapsed.Ticks / 10000f;
            }

            if (targetPrefab == trans.gameObject)
            {
                standardToken = token;
            }
            token = token * (1000f / standardToken);
            yield return null;
            ++count; 
            EditorUtility.DisplayProgressBar(string.Format("檢測中...{0}% (同物件權重誤差約為30)", (((float)count * 100) / (float)total).ToString("f1")), "檢測物件 : " + trans.name, (float)count / (float)total);

            CustomHierarchyView.performances.Put(trans.gameObject, (int)token);
        }
        Debug.Log("檢測完畢");
        Resources.UnloadUnusedAssets();
        EditorUtility.ClearProgressBar();
    }

    public static void GetReference()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];
        var referenceRecoverer = selectedGameObject.GetComponent<ReferenceRecoverer>();
        if ((referenceRecoverer.monoScript == null || string.IsNullOrEmpty(referenceRecoverer.jsonReference)))
        {
            return;
        }
        CreateCSharpFile(referenceRecoverer.monoScript, referenceRecoverer.jsonReference);

        GameObject tempGo = new GameObject("[ReferenceApplyAide]");
        var aide = tempGo.AddComponent<ReferenceApplyAide>();
        aide.className = referenceRecoverer.monoScript.GetType().Name + "_ReferenceApplier";
        aide.go = selectedGameObject;
    }


    static string GetPrefabName(Transform _root, Transform _obj)
    {
        int level = 0;
        string prefabName = string.Empty;
        Transform pointer = _obj;
        prefabName = pointer.name;
        while (pointer.parent != _root)
        {
            pointer = pointer.parent;
            prefabName = pointer.name + "-" + prefabName;
            ++level;
            if (level > 20)
            {
                Debug.LogError("階層不得超過20層");
                break;
            }
        }
        prefabName = _root.transform.name + "-" + prefabName;
        Debug.Log("prefabName : " + _root.transform.name + "   " + prefabName);
        return prefabName;
    }

    static void CreateCSharpFile(MonoBehaviour _monoScript, string _jsonReference)
    {
        string codeContent = string.Empty;
        codeContent = "using UnityEngine;" + SetEnter(2);
        //string path = AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(_monoScript));
        string path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(_monoScript.GetComponent<ReferenceRecoverer>()));
        path = path.Substring(0, path.LastIndexOf("/")) + "/Reference/";
        JObject jObjReference = JsonConvert.DeserializeObject<JObject>(_jsonReference);

        var scriptName = _monoScript.GetType().Name;


        path = path.Replace(scriptName + ".cs", string.Empty);
        scriptName = scriptName + "_ReferenceApplier";
        path += scriptName + ".cs";

        codeContent += "public class " + scriptName + " : MonoBehaviour,IApplier {" + SetEnter(1);
        codeContent += SetTab(1) + "public void SetReference()" + SetEnter(1);
        codeContent += SetTab(1) + "{" + SetEnter(1);
        string tempRoot = "m" + _monoScript.GetType().Name;
        codeContent += SetTab(2) + _monoScript.GetType().Name + " " + tempRoot + " = gameObject.GetComponent<" + _monoScript.GetType().Name + ">();" + SetEnter(1);
        codeContent += jsonCrawler(tempRoot, jObjReference);
        codeContent += SetTab(1) + "}" + SetEnter(1);
        codeContent += "}";
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                File.Delete(path + ".meta");
                AssetDatabase.Refresh();
                Debug.LogError("存在同名物件，執行覆蓋");

            }

            using (FileStream fs = File.Create(path))
            {
                Byte[] info = new UTF8Encoding().GetBytes(codeContent);
                fs.Write(info, 0, info.Length);
            }
            Debug.Log("成功建立cs檔案：" + scriptName + ".cs");
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    static string jsonCrawler(string _root, JToken _token)
    {
        if (_token.Type == JTokenType.Object)
        {
            JObject tempObject = _token.ToObject<JObject>();
            JToken childToken;
            if (tempObject.TryGetValue("isJsonTerminal", out childToken))   //最後一層
            {
                string leftValue = _root + "." + tempObject.GetValue("fieldName").ToString();
                string type = tempObject.GetValue("type").ToString();
                string getComponentCode;
                if (type.Equals("UnityEngine.GameObject"))
                {
                    getComponentCode = ".gameObject";
                }
                else if (type.Equals("UnityEngine.Transform"))
                {
                    getComponentCode = string.Empty;
                }
                else
                {
                    getComponentCode = ".GetComponent<" + type + ">()";
                }

                if (string.IsNullOrEmpty(tempObject.GetValue("path").ToString()))   //有可能就是自己
                {
                    if (tempObject.TryGetValue("isOutsider", out childToken))
                    {
                        return SetTab(2) + "if(" + leftValue + " == null) " + leftValue + " = " + "GameObject.Find(\"" + tempObject.GetValue("outsiderParent").ToString() + "\")" + ".transform" + getComponentCode + ";" + SetEnter(1);
                    }
                    else
                    {
                        return SetTab(2) + "if(" + leftValue + " == null) " + leftValue + " = " + _root + ".transform" + getComponentCode + ";" + SetEnter(1);
                    }
                }
                else
                {
                    if (tempObject.TryGetValue("isOutsider", out childToken))
                    {
                        return SetTab(2) + "if(" + leftValue + " == null) " + leftValue + " = " + "GameObject.Find(\"" + tempObject.GetValue("outsiderParent").ToString() + "\")" + ".transform.Find(\"" + tempObject.GetValue("path").ToString() + "\")" + getComponentCode + ";" + SetEnter(1);
                    }
                    else
                    {
                        return SetTab(2) + "if(" + leftValue + " == null) " + leftValue + " = " + _root + ".transform.Find(\"" + tempObject.GetValue("path").ToString() + "\")" + getComponentCode + ";" + SetEnter(1);
                    }
                }
            }
            else
            {
                var keys = tempObject.Properties().Select(tempProperty => tempProperty.Name);
                string code = string.Empty;
                foreach (string key in keys)
                {
                    code += jsonCrawler(_root, tempObject.GetValue(key));
                }
                return code;
            }
        }
        else if (_token.Type == JTokenType.Array)
        {
            JArray tempArray = _token.ToObject<JArray>();
            string code = string.Empty;
            foreach (JToken token in tempArray)
            {
                code += jsonCrawler(_root, token);
            }
            return code;
        }
        return string.Empty;
    }

    static string SetTab(int _count)
    {
        string tempTab = string.Empty;
        for (int i = 0; i < _count; ++i)
        {
            tempTab += "\t";
        }
        return tempTab;
    }

    static string SetEnter(int _count)
    {
        string tempTab = string.Empty;
        for (int i = 0; i < _count; ++i)
        {
            tempTab += "\n";
        }
        return tempTab;
    }

    [InitializeOnLoad]
    class ReferenceComponentAdder
    {
        static ReferenceComponentAdder()
        {
            
            GameObject aideObj = GameObject.Find("[ReferenceApplyAide]");
            if (aideObj == null) return;
            try
            {
                ReferenceApplyAide aide = aideObj.GetComponent<ReferenceApplyAide>();
                Debug.Log(ReflectionHelper.GetType(aide.className).Name);
                aide.go.AddComponent(ReflectionHelper.GetType(aide.className));
                DestroyImmediate(aide.go.GetComponent<ReferenceRecoverer>());

                Debug.Log("Reference Class附加成功");
            }
            catch (Exception e)
            {
                Debug.LogError("Reference Class附加失敗 : " + e.Message);
            }
            DestroyImmediate(aideObj);
        }
    }


    [InitializeOnLoad]
    class ScrutinizeAssetBundle
    {
        static ScrutinizeAssetBundle()
        {
            if (GameObject.Find("[scrutinizer]") == null) return;
            Scrutinizer scrutinizer = GameObject.Find("[scrutinizer]").GetComponent<Scrutinizer>();
            Debug.Log(scrutinizer.assetBundlePath);

            string url = "file://" + scrutinizer.assetBundlePath;
            Swing.Editor.EditorCoroutine.start(Download(scrutinizer, url));
        }

        static IEnumerator Download(Scrutinizer _scrutinizer,string _url)
        {
            
            var www = UnityWebRequestAssetBundle.GetAssetBundle(_url);

            yield return www.SendWebRequest();
            DownloadHandlerAssetBundle tempAsset = (DownloadHandlerAssetBundle)www.downloadHandler;
            AssetBundle bundle = tempAsset.assetBundle;

            int count = 0;
            int total = bundle.GetAllAssetNames().Length;
            float standardToken = 0;
            EditorUtility.DisplayProgressBar("檢測中...0% (同物件權重誤差約為30)", "檢測物件 : " + "開始掃描Prefab", (float)count / (float)total);



            System.Diagnostics.Stopwatch swRoot = new System.Diagnostics.Stopwatch();
            swRoot.Start();
            for (int i = 0; i < 100; ++i)
            {
                var loadedAssetRoot = bundle.LoadAsset<GameObject>(_scrutinizer.targetPrefabRootAssetName);
                GameObject goRoot = GameObject.Instantiate(loadedAssetRoot as GameObject);
                GameObject.DestroyImmediate(goRoot);
            }
            swRoot.Stop();
            standardToken = swRoot.Elapsed.Ticks / 100f;
            var tokenRoot = standardToken;
            tokenRoot = tokenRoot * (1000f / standardToken);
            yield return null;
            ++count;
            EditorUtility.DisplayProgressBar(string.Format("檢測中...{0}% (同物件權重誤差約為30)", (((float)count * 100) / (float)total).ToString("f1")), "檢測物件 : " + _scrutinizer.targetPrefabRootAssetName, (float)count / (float)total);
            CustomHierarchyView.performances.Put(_scrutinizer.prefabPathMap[_scrutinizer.prefabPaths.IndexOf(_scrutinizer.targetPrefabRootAssetName)], (int)tokenRoot);



            foreach (string assetName in bundle.GetAllAssetNames())
            {
                if (assetName.Equals(_scrutinizer.targetPrefabRootAssetName)) continue;
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                for (int i = 0; i < 100; ++i)
                {
                    var loadedAsset = bundle.LoadAsset<GameObject>(assetName);      
                    GameObject go = GameObject.Instantiate(loadedAsset as GameObject);
                    GameObject.DestroyImmediate(go);
                }
                sw.Stop();
                float token = sw.Elapsed.Ticks / 100f ;

                if (_scrutinizer.targetPrefabRoot == _scrutinizer.prefabPathMap[_scrutinizer.prefabPaths.IndexOf(assetName)])
                { 
                    standardToken = token;
                }
                token = token * (1000f / standardToken);
                yield return null;
                ++count;
                EditorUtility.DisplayProgressBar(string.Format("檢測中...{0}% (同物件權重誤差約為30)", (((float)count * 100) / (float)total).ToString("f1")), "檢測物件 : " + assetName, (float)count / (float)total);

                try
                { 
                    CustomHierarchyView.performances.Put(_scrutinizer.prefabPathMap[_scrutinizer.prefabPaths.IndexOf(assetName)], (int)token);
                }
                catch
                {
                    bundle.Unload(false);
                    EditorUtility.ClearProgressBar();
                    Resources.UnloadUnusedAssets();
                }
            }
            bundle.Unload(false);
            EditorUtility.ClearProgressBar();
            Resources.UnloadUnusedAssets();
            DestroyImmediate(_scrutinizer.gameObject);
            System.IO.File.Delete("Assets/Prefab_Temporary_Detector");
        }
    }
}