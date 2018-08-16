using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

[ExecuteInEditMode]
public class ReferenceRecoverer : MonoBehaviour {

    public MonoBehaviour monoScript;
    public bool isRefresh;
    //[SerializeField]
    //public List<string> refPath;
    public string jsonReference;


    private void OnEnable()
    {
        if (!isRefresh) enabled = false;
        if (monoScript != null)
        {
            FindReference();
            isRefresh = false;
            enabled = false;
        }
    }

    void FindReference()
    {
        //refPath = new List<string>();
        var fields = monoScript.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        JObject referenceObj = new JObject();
        foreach (var field in fields)
        {

            var fieldObject = field.GetValue(monoScript);
            if (fieldObject == null) continue;
            JToken token = FindObject(field.Name,fieldObject);
            if (!token.IsNullOrEmpty())
            {
                referenceObj.Add(field.Name, FindObject(field.Name,fieldObject));
            }           
        }
        jsonReference = JsonConvert.SerializeObject(referenceObj);
    }


    JToken FindObject(string _fieldName,object _object)
    {
        Debug.Log(_object.GetType());
        //先判斷是不是IEnumrable
        if ((_object.GetType().IsAssignableFrom(typeof(Transform)) || _object.GetType().IsAssignableFrom(typeof(RectTransform))) ||!IsCollectionType(_object.GetType()))
        {
            //再判斷是不是struct
            if (_object.GetType().ToString().Contains("+"))
            {
                JObject tempObject = new JObject();
                var fields = _object.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    var fieldObject = field.GetValue(_object);
                    if (fieldObject == null) continue;
                    JToken token = FindObject(_fieldName + "." + field.Name, fieldObject);
                    if (!token.IsNullOrEmpty())
                    {
                        tempObject.Add(field.Name, token);
                    }

                }
                return tempObject;
            }

        }
        else
        {
            JArray tempArray = new JArray();
            IList objList = (IList)_object;
            foreach (var obj in objList)
            {
                JToken token = FindObject(_fieldName + "[" + objList.IndexOf(obj).ToString() + "]",obj);
                if (!token.IsNullOrEmpty())
                {
                    tempArray.Add(token);
                }                              
            }

            return tempArray;
        }
        JObject resultObject = new JObject();
        
        Transform trans = GetTransform(_object);
        if (trans != null)
        {
            string path = trans.name;
            if (trans == transform) path = string.Empty;
            while (trans != transform)
            {
                if (trans.parent == transform) break;
                if (trans.parent == null)
                {
                    resultObject.Add("isOutsider", true);
                    resultObject.Add("outsiderParent", trans.name);
                    if (path.Contains("/"))
                    {
                        path = path.Substring(path.IndexOf("/") + 1);
                    }
                    else
                    {
                        path = string.Empty;

                    }
                    break;
                }
                trans = trans.parent;
                path = trans.name + "/" + path;
            }
            resultObject.Add("isJsonTerminal", true);
            resultObject.Add("fieldName", _fieldName);
            resultObject.Add("path", path);
            resultObject.Add("type", _object.GetType().ToString());
            return resultObject;

        }
        return string.Empty;
    }



    Transform GetTransform(object _object)
    {
        Transform t = null;
        try
        {
            t = (Transform)_object.GetType().GetProperty("transform").GetValue(_object, null);
        }
        catch { }
        return t;
    }

    bool IsCollectionType(Type type)
    {
        return (type.GetInterface("IEnumerable") != null);
    }

    //Dictionary<string, string> GetDictionary()
    //{
    //    Dictionary<string, string> tempDic = new Dictionary<string, string>();
    //    foreach (string str in refPath)
    //    {
    //        string[] splitString = str.Split('@');
    //        tempDic.Add(splitString[0], splitString[1] + "@" + splitString[2]);
    //    }
    //    return tempDic;
    //}
}
