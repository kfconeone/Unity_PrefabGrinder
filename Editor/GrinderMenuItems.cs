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

        var prefabType = PrefabUtility.GetPrefabType(selectedGameObject);
        if (prefabType != PrefabType.PrefabInstance)
        {
            Debug.LogError("需先設定成 Prefab 物件");
            return;
        }

        CustomHierarchyView.grinderPrefabs.Add(PrefabUtility.FindPrefabRoot(selectedGameObject));

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
        GameObject selectedPrefabRoot = PrefabUtility.FindPrefabRoot(Selection.gameObjects[0]);
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

        PrefabUtility.CreatePrefab(PrefabPath + "/Origin/" + _selectedPrefabRoot.name + ".prefab", _selectedPrefabRoot);

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
            PrefabUtility.CreatePrefab(PrefabPath + "/Packed/" + swapper.prefabName + ".prefab", go);
            Debug.Log("建立prefab : " + swapper.prefabName + ".prefab");
            swapGo.transform.parent = go.transform.parent;
            swapGo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            UnityEngine.Object.DestroyImmediate(go);
            yield return null;
            ++count;
            EditorUtility.DisplayProgressBar("生成Prefab中", "物件 : " + name, (float)count / (float)total);


        }
        PrefabUtility.CreatePrefab(PrefabPath + "/Packed/" + tempGo.name + ".prefab", tempGo);
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
        if (selectedGameObject.GetComponentsInChildren<AssetSwapper>().Length == 0 && !CustomHierarchyView.grinderPrefabs.Contains(PrefabUtility.FindPrefabRoot(selectedGameObject)))
        {
            Debug.Log("並非是Grinder物件");
            return;
        }

        if (EditorUtility.DisplayDialog("還原成普通Prefab", "是否還原？(不可逆)", "確定", "取消"))
        {
            var components = PrefabUtility.FindPrefabRoot(selectedGameObject).GetComponentsInChildren(typeof(AssetSwapper), true);
            foreach (Component com in components)
            {
                GameObject.DestroyImmediate(com);
            }

            if (CustomHierarchyView.grinderPrefabs.Contains(PrefabUtility.FindPrefabRoot(selectedGameObject)))
            {
                CustomHierarchyView.grinderPrefabs.Remove(PrefabUtility.FindPrefabRoot(selectedGameObject));
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

    [MenuItem("GameObject/Prefab建立/進行Prefab生成效能檢測", priority = 17)]
    static void InstantiationAnalyze()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject targetPrefab = PrefabUtility.FindPrefabRoot(Selection.gameObjects[0]);

        if (EditorUtility.DisplayDialog("性能檢測", "檢測時間根據prefab大小，相當耗時，確定進行？\n\n父物件權重正規化至1000，其餘子物件則根據父物件進行比對。\n\n檢測結果前者為生成效能，後者為操作效能。", "確定", "取消"))
        {
            Swing.Editor.EditorCoroutine.start(Loading(targetPrefab));       
        }
        else
        {
            Debug.Log("取消還原");
        }

    }

    static int count;
    static int total;
    static IEnumerator Loading(GameObject targetPrefab)
    {
        var allPrefabs = targetPrefab.GetComponentsInChildren<Transform>(true);
        count = 0;
        total = allPrefabs.Length;
        float standardToken = 0;
        float standardToken2 = 0;
        foreach (var trans in allPrefabs)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (int i = 0; i < 100; ++i)
            {
                GameObject go = GameObject.Instantiate(trans.gameObject);
                GameObject.DestroyImmediate(go);
            }
            sw.Stop();
            float token = sw.Elapsed.Ticks / 100f;

            if (targetPrefab == trans.gameObject)
            {
                standardToken = token;
            }
            token = token * (1000f / standardToken);
            double[] tokens = new double[2];
            tokens[0] = (int)token;
            


            GameObject TempGo = GameObject.Instantiate(trans.gameObject);
            sw2.Start();
            for (int j = 0; j < 1000; ++j)
            {
                TempGo.transform.position += new Vector3(j, j, j);
                TempGo.transform.localScale += new Vector3(j, j, j);
            }
            sw2.Stop();
            GameObject.DestroyImmediate(TempGo);

            float token2 = sw2.Elapsed.Ticks / 1000f;

            if (targetPrefab == trans.gameObject)
            {
                standardToken2 = token2;
            }
            token2 = token2 * (1000f / standardToken2);
            tokens[1] = (int)token2;

            yield return null;
            ++count;
            EditorUtility.DisplayProgressBar("檢測中... (權重誤差根據CPU而異，同物件誤差約為30)", "檢測物件 : " + trans.name, (float)count / (float)total);

            CustomHierarchyView.performances.Put(trans.gameObject, tokens);
        }
        Debug.Log("檢測完畢");
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
}