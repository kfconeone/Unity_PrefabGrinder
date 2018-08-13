using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Xefier.Threading.Asynchronous;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class Testing : MonoBehaviour {
    string url;

    void OnGUI()
    {
        if (GUILayout.Button("Old",GUILayout.Width(100),GUILayout.Height(100)))
        {
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
        yield return asyncOperation.LoadAssetAsync(rootPrefab,bundle);
        //以下是生成
        GameObject root = Instantiate(rootPrefab, transform);
        yield return asyncOperation.Instantiate(root);

        root.GetComponent<IApplier>().SetReference();
        Debug.Log("結束");
        bundle.Unload(false);
    }



}
