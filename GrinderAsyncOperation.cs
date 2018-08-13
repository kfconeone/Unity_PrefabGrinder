using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xefier.Threading.Asynchronous;

public class GrinderAsyncOperation {
    public Dictionary<string, GameObject> prefabsDic;
    public IEnumerator LoadAssetAsync(GameObject _root,AssetBundle _bundle)
    {
        var prefabSwapperList = _root.GetComponentsInChildren<AssetSwapper>();
        var prefabNameList = prefabSwapperList.Select(tempSwapper => tempSwapper.prefabName).ToList();
        prefabsDic = new Dictionary<string, GameObject>();
        foreach (string prefabName in prefabNameList)
        {
            var req = _bundle.LoadAssetAsync<GameObject>(prefabName + ".prefab");
            yield return req;
            Debug.Log("Time : " + prefabName + "   " + Time.deltaTime);
            prefabsDic.Add(prefabName, req.asset as GameObject);
        }
    }

    public IEnumerator Instantiate(GameObject _root, Dictionary<string, GameObject> _prefabsDic = null)
    {
        Dictionary<string, GameObject> tempPrefabDic;
        if (_prefabsDic == null)
        {
            tempPrefabDic = prefabsDic;
        }
        else
        {
            tempPrefabDic = _prefabsDic;
        }
        
        var objectSwpapperList = _root.GetComponentsInChildren<AssetSwapper>();
        var objectDic = objectSwpapperList.Select(tempSwapper => tempSwapper).ToDictionary(tempSwapper => tempSwapper.prefabName, tempSwapper => tempSwapper.gameObject);

        List<Async<GameObject>> taskList = new List<Async<GameObject>>();

        foreach (string prefabName in objectDic.Keys)
        {

            var task = Async.Instantiate(tempPrefabDic[prefabName], objectDic[prefabName].transform.parent, false);

            task.Ready += (_task) =>
            {
                GameObject tempObj = _task.Result;
                Debug.Log(tempObj.name + "   " + objectDic[prefabName].name + Time.deltaTime);
                tempObj.name = objectDic[prefabName].name;
                tempObj.transform.SetParent(objectDic[prefabName].transform.parent, false);
                tempObj.transform.SetSiblingIndex(objectDic[prefabName].transform.GetSiblingIndex());
                GameObject.Destroy(objectDic[prefabName]);
                GameObject.Destroy(tempObj.GetComponent<AssetSwapper>());
            };
            taskList.Add(task);
        }

        while (!taskList.All(tempTask => tempTask.IsCompleted))
        {
            yield return null;
        }
    }
}
