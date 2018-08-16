using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using Newtonsoft.Json;

public class MenuItems
{
    [MenuItem("GameObject/Swap Prefab Create/Create", priority = 0)]
    static void CreatePrefab()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];

        string PrefabPath = "Assets/Prefab_" + selectedGameObject.name;

        if (!AssetDatabase.IsValidFolder(PrefabPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab_" + selectedGameObject.name);
            AssetDatabase.CreateFolder(PrefabPath, "Origin");
            AssetDatabase.CreateFolder(PrefabPath, "Packed");
        }

        PrefabUtility.CreatePrefab(PrefabPath + "/Origin/" + selectedGameObject.name + ".prefab", selectedGameObject);

        GameObject tempGo = GameObject.Instantiate(selectedGameObject, selectedGameObject.transform.parent);
        tempGo.name = selectedGameObject.name;
        tempGo.SetActive(false);
        var swapList = tempGo.GetComponentsInChildren(typeof(AssetSwapper),true).Select(tempComponent => tempComponent.gameObject).ToList();
        
        foreach (GameObject go in swapList)
        {
            if (go == null)
            {
                Debug.LogError("物件已被刪除，檢查是否重複Add Component [AssetSwapper]");
                return;
            }
            
            GameObject swapGo = new GameObject(go.name);
            var swapper = swapGo.AddComponent<AssetSwapper>();
            swapper.prefabName = GetPrefabName(tempGo.transform,go.transform);
            PrefabUtility.CreatePrefab(PrefabPath + "/Packed/" + swapper.prefabName + ".prefab", go);
            Debug.Log("建立prefab : " + swapper.prefabName + ".prefab");
            swapGo.transform.parent = go.transform.parent;
            swapGo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            UnityEngine.Object.DestroyImmediate(go);
        }
        PrefabUtility.CreatePrefab(PrefabPath + "/Packed/" + tempGo.name + ".prefab", tempGo);
        UnityEngine.Object.DestroyImmediate(tempGo);
        Debug.Log("Prefabs建立完畢");
    }

    [MenuItem("GameObject/Swap Prefab Create/ClearSwapper", priority = 0)]
    static void ClearSwapper()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];

        var components = selectedGameObject.GetComponentsInChildren(typeof(AssetSwapper), true);
        foreach (Component com in components)
        {
            GameObject.DestroyImmediate(com);
        }

        Debug.Log("Asset Swapper清除完畢");
    }

    [MenuItem("GameObject/Reference Recover/GetReference", priority = 0)]
    static void GetReference()
    {
        if (Selection.gameObjects.Length < 1)
        {
            Debug.Log("未選取物件");
            return;
        }
        GameObject selectedGameObject = Selection.gameObjects[0];
        var referenceRecoverer = selectedGameObject.GetComponent<ReferenceRecoverer>();
        CreateCSharpFile(referenceRecoverer.monoScript, referenceRecoverer.jsonReference);


    }

    static string GetPrefabName(Transform _root,Transform _obj)
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

    static void CreateCSharpFile(MonoBehaviour _monoScript,string _jsonReference)
    {
        string codeContent = string.Empty;
        codeContent = "using UnityEngine;" + SetEnter(2);
        string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(_monoScript));

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
                Debug.LogError("存在同名物件，取消建立程式碼");
                return;
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
                        return SetTab(2) + "if(" + leftValue + " == null) " + leftValue + " = " + "GameObject.Find(\"" + tempObject.GetValue("outsiderParent").ToString() + "\")"+ ".transform.Find(\"" + tempObject.GetValue("path").ToString() + "\")" + getComponentCode + ";" + SetEnter(1);
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
}