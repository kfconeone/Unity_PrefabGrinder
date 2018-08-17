using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xefier.Threading.Asynchronous;

namespace Kfc.Grinder
{
    public class GrinderAssetBundleAsyncOperation : CustomYieldInstruction
    {
        public GameObject prefab;
        public Dictionary<string, GameObject> prefabsDic;
        public bool isDone;
        public float progress;

        public GrinderAssetBundleAsyncOperation LoadAssetAsync(MonoBehaviour _mono, string _assetName, AssetBundle _bundle)
        {
            _mono.StartCoroutine(inner_LoadAssetAsync(_assetName, _bundle));
            return this;
        }
        IEnumerator inner_LoadAssetAsync(string _assetName, AssetBundle _bundle)
        {
            var request = _bundle.LoadAssetAsync<GameObject>("pnl_blackjack.prefab");
            yield return request;

            prefab = request.asset as GameObject;
            var prefabSwapperList = prefab.GetComponentsInChildren<AssetSwapper>();
            var prefabNameList = prefabSwapperList.Select(tempSwapper => tempSwapper.prefabName).ToList();
            prefabsDic = new Dictionary<string, GameObject>();
            progress = 0;
            int finishCount = 0;
            isDone = false;
            int totalDownloadCount = prefabNameList.Count;
            foreach (string prefabName in prefabNameList)
            {
                var req = _bundle.LoadAssetAsync<GameObject>(prefabName + ".prefab");
                progress = (finishCount + req.progress) / totalDownloadCount;
                yield return req;
                ++finishCount;
                prefabsDic.Add(prefabName, req.asset as GameObject);
            }
            isDone = true;
        }

        public override bool keepWaiting
        {
            get
            {
                return !isDone;
            }
        }
    }

    public class GrinderInstantiateAsyncOperation : CustomYieldInstruction
    {
        public GameObject instantiateGameObject;
        public bool isDone;
        public float progress;

        public GrinderInstantiateAsyncOperation InstantiateAsync(MonoBehaviour _mono, GameObject _instantiateGameObject, Dictionary<string, GameObject> _prefabsDic)
        {
            instantiateGameObject = _instantiateGameObject;
            _mono.StartCoroutine(inner_Instantiate(instantiateGameObject, _prefabsDic));
            return this;
        }

        public GrinderInstantiateAsyncOperation InstantiateAsync(MonoBehaviour _mono, GameObject _instantiateGameObject, string _path)
        {
            instantiateGameObject = _instantiateGameObject;
            _mono.StartCoroutine(inner_InstantiateFromResources(_mono,instantiateGameObject, _path));
            return this;
        }

        IEnumerator inner_InstantiateFromResources(MonoBehaviour _mono, GameObject _root, string _path)
        {
            var objectSwpapperList = _root.GetComponentsInChildren<AssetSwapper>();
            var prefabNameList = objectSwpapperList.Select(tempSwapper => tempSwapper.prefabName).ToList();
            Dictionary<string, GameObject> prefabsDic = new Dictionary<string, GameObject>();
            foreach (string name in prefabNameList)
            {
                var req = Resources.LoadAsync(_path + "/" + name);
                yield return req;
                prefabsDic.Add(name, req.asset as GameObject);
            }
            _mono.StartCoroutine(inner_Instantiate(instantiateGameObject, prefabsDic));

        }


        IEnumerator inner_Instantiate(GameObject _root, Dictionary<string, GameObject> _prefabsDic)
        {

            var objectSwpapperList = _root.GetComponentsInChildren<AssetSwapper>();
            var objectDic = objectSwpapperList.Select(tempSwapper => tempSwapper).ToDictionary(tempSwapper => tempSwapper.prefabName, tempSwapper => tempSwapper.gameObject);

            List<Async<GameObject>> taskList = new List<Async<GameObject>>();
            progress = 0;
            int finishCount = 0;
            isDone = false;
            int totalDownloadCount = objectDic.Keys.Count;
            foreach (string prefabName in objectDic.Keys)
            {
                Debug.Log(prefabName);
                var task = Async.Instantiate(_prefabsDic[prefabName], objectDic[prefabName].transform.parent, false);

                task.Ready += (_task) =>
                {
                    GameObject tempObj = _task.Result;
                    tempObj.name = objectDic[prefabName].name;
                    tempObj.transform.SetParent(objectDic[prefabName].transform.parent, false);
                    tempObj.transform.SetSiblingIndex(objectDic[prefabName].transform.GetSiblingIndex());
                    GameObject.Destroy(objectDic[prefabName]);
                    GameObject.Destroy(tempObj.GetComponent<AssetSwapper>());
                    ++finishCount;
                    progress = (float)finishCount / (float)totalDownloadCount;
                };
                taskList.Add(task);
            }

            while (!taskList.All(tempTask => tempTask.IsCompleted))
            {
                yield return null;
            }
            isDone = true;
        }

        public override bool keepWaiting
        {
            get
            {
                return !isDone;
            }
        }
    }
    public class GrinderAsyncOperation
    {
        public GrinderAssetBundleAsyncOperation loadedAsset;
        public GrinderInstantiateAsyncOperation instantiateAsset;
        public GrinderAssetBundleAsyncOperation LoadAssetAsync(MonoBehaviour _mono, string _assetName, AssetBundle _bundle)
        {
            loadedAsset = new GrinderAssetBundleAsyncOperation();
            loadedAsset.LoadAssetAsync(_mono, _assetName, _bundle);
            return loadedAsset;
        }

        public GrinderInstantiateAsyncOperation InstantiateAsync(MonoBehaviour _mono, GameObject _prefab, Transform _parent, Dictionary<string, GameObject> _prefabsDic)
        {
            instantiateAsset = new GrinderInstantiateAsyncOperation();
            GameObject instantiateGameObject = GameObject.Instantiate(_prefab, _parent);
            instantiateAsset.InstantiateAsync(_mono, instantiateGameObject, _prefabsDic);
            return instantiateAsset;
        }

        public GrinderInstantiateAsyncOperation InstantiateFromResoucesAsync(MonoBehaviour _mono, GameObject _prefab, Transform _parent, string path)
        {
            instantiateAsset = new GrinderInstantiateAsyncOperation();
            GameObject instantiateGameObject = GameObject.Instantiate(_prefab, _parent);
            instantiateAsset.InstantiateAsync(_mono, instantiateGameObject, path);
            return instantiateAsset;
        }
    }

        //public class GrinderAsyncOperation {
        //    public Dictionary<string, GameObject> prefabsDic;
        //    public float loadingPercent;
        //    public float instantiatePercent;
        //    public bool loadingIsDone;
        //    public bool instantiateIsDone;
        //    public IEnumerator LoadAssetAsync(GameObject _root,AssetBundle _bundle)
        //    {
        //        var prefabSwapperList = _root.GetComponentsInChildren<AssetSwapper>();
        //        var prefabNameList = prefabSwapperList.Select(tempSwapper => tempSwapper.prefabName).ToList();
        //        prefabsDic = new Dictionary<string, GameObject>();
        //        loadingPercent = 0;
        //        int finishCount = 0;
        //        loadingIsDone = false;
        //        int totalDownloadCount = prefabNameList.Count;
        //        foreach (string prefabName in prefabNameList)
        //        {
        //            var req = _bundle.LoadAssetAsync<GameObject>(prefabName + ".prefab");
        //            loadingPercent = (finishCount + req.progress) / totalDownloadCount;
        //            yield return req;
        //            ++finishCount;
        //            prefabsDic.Add(prefabName, req.asset as GameObject);
        //        }
        //        loadingIsDone = true;
        //    }

        //    public IEnumerator Instantiate(GameObject _root, Dictionary<string, GameObject> _prefabsDic = null)
        //    {
        //        Dictionary<string, GameObject> tempPrefabDic;
        //        if (_prefabsDic == null)
        //        {
        //            tempPrefabDic = prefabsDic;
        //        }
        //        else
        //        {
        //            tempPrefabDic = _prefabsDic;
        //        }

        //        var objectSwpapperList = _root.GetComponentsInChildren<AssetSwapper>();
        //        var objectDic = objectSwpapperList.Select(tempSwapper => tempSwapper).ToDictionary(tempSwapper => tempSwapper.prefabName, tempSwapper => tempSwapper.gameObject);

        //        List<Async<GameObject>> taskList = new List<Async<GameObject>>();
        //        instantiatePercent = 0;
        //        int finishCount = 0;
        //        instantiateIsDone = false;
        //        int totalDownloadCount = objectDic.Keys.Count;
        //        foreach (string prefabName in objectDic.Keys)
        //        {

        //            var task = Async.Instantiate(tempPrefabDic[prefabName], objectDic[prefabName].transform.parent, false);

        //            task.Ready += (_task) =>
        //            {
        //                GameObject tempObj = _task.Result;
        //                tempObj.name = objectDic[prefabName].name;
        //                tempObj.transform.SetParent(objectDic[prefabName].transform.parent, false);
        //                tempObj.transform.SetSiblingIndex(objectDic[prefabName].transform.GetSiblingIndex());
        //                GameObject.Destroy(objectDic[prefabName]);
        //                GameObject.Destroy(tempObj.GetComponent<AssetSwapper>());
        //                ++finishCount;
        //                instantiatePercent = (float)finishCount / (float)totalDownloadCount;
        //            };
        //            taskList.Add(task);
        //        }

        //        while (!taskList.All(tempTask => tempTask.IsCompleted))
        //        {
        //            yield return null;
        //        }
        //        instantiateIsDone = true;
        //    }
        //}
    }
