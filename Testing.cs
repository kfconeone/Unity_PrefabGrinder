using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Xefier.Threading.Asynchronous;
using Xefier.Threading.Tasks;
using Xefier.Threading.Asynchronous.Scheduler;
using System.Threading;
using Kfc.Grinder;

public class Testing : MonoBehaviour {
    string url;
    public Transform son;
    public Transform outsider;


    
    void OnGUI()
    {
        if (GUILayout.Button("Old",GUILayout.Width(100),GUILayout.Height(100)))
        {
            //GetComponent<IApplier>().SetReference();
            url = "file://" + Application.streamingAssetsPath + "/pnl_blackjack_sp.assetBundles";
            StartCoroutine(DownloadWebBundle_Old(url));
        }
        if (GUILayout.Button("New", GUILayout.Width(100), GUILayout.Height(100)))
        {
            //GetComponent<IApplier>().SetReference();
            Caching.ClearCache();
            url = "file://" + Application.streamingAssetsPath + "/pnl_blackjack_sp.assetBundles";
            StartCoroutine(DownloadWebBundle_Old(url));
        }


    }




    private IEnumerator DownloadWebBundle_Old(string _url)
    {
        //以下是下載
        var www = UnityWebRequest.GetAssetBundle(_url);
        yield return www.SendWebRequest();
        DownloadHandlerAssetBundle tempAsset = (DownloadHandlerAssetBundle)www.downloadHandler;
        AssetBundle bundle = tempAsset.assetBundle;

        var request = bundle.LoadAssetAsync<GameObject>("pnl_blackjack.prefab");       
        yield return request;

        //以下是讀取
        GameObject rootPrefab = request.asset as GameObject;
        GrinderAsyncOperation asyncOperation = new GrinderAsyncOperation();
        yield return asyncOperation.LoadAssetAsync(this,rootPrefab, bundle);
        //while (!asyncOperation.loadedAsset.isDone)
        //{
        //    Debug.Log("loadingPercent : " + asyncOperation.loadingPercent);
        //    yield return null;
        //}
        //以下是生成
        yield return asyncOperation.InstantiateAsync(this, rootPrefab,transform, asyncOperation.loadedAsset.prefabsDic);
        //StartCoroutine(asyncOperation.Instantiate(root));
        //while (!asyncOperation.instantiateIsDone)
        //{
        //    Debug.Log("instantiatePercent : " + asyncOperation.instantiatePercent);
        //    yield return null;
        //}
        asyncOperation.instantiateAsset.root.GetComponent<IApplier>().SetReference();
        Debug.Log("結束");
        bundle.Unload(false);
    }



}
