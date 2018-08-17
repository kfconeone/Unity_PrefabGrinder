using UnityEngine;

public class Testing_ReferenceApplier : MonoBehaviour,IApplier {
	public void SetReference()
	{
		Testing mTesting = gameObject.GetComponent<Testing>();
		if(mTesting.son == null) mTesting.son = mTesting.transform.Find("son").GetComponent<UnityEngine.RectTransform>();
		if(mTesting.outsider == null) mTesting.outsider = GameObject.Find("Canvas").transform.Find("Image").GetComponent<UnityEngine.RectTransform>();
	}
}