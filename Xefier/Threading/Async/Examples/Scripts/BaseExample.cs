using Xefier.Threading.Asynchronous;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class BaseExample : MonoBehaviour
{
    public Transform prefab;
    public int count = 1000;
    public float zIncrement = 1;
    [Tooltip("Threading is optional, but will be faster when enabled")]
    public bool useThread = true; //
    [Range(0, 100)]
    public float createDelay = 0.1f;
    [Range(0.1f, 100)]
    public float destroyDelay = 2.1f;

    private List<Async<Transform>> asyncObjects;
    private float z;

    private void OnEnable()
    {
        z = 0;
        asyncObjects = new List<Async<Transform>>();

        Invoke("InstantiateAfterDelay", createDelay);
        Invoke("DestroyAfterDelay", destroyDelay);
    }

    private void InstantiateAfterDelay()
    {
        //Example: How to pass transform so separate thread (You cannot access transform directly in a thread!)
        //Here is a simple C# thread, but any threading utility from Unity Asset Store should be compatible
        if(useThread)
        { 
            new Thread(new ParameterizedThreadStart(InstantiateAll_Thread)).Start(transform);
        }
        else
        {
            InstantiateAll(transform);
        }
    }

    private void DestroyAfterDelay()
    {
        if (useThread)
        {
            new Thread(new ThreadStart(DestroyAll)).Start(transform);
        }
        else
        {
            DestroyAll();
        }

    }
    
    //object parameter is only needed when using threading
    private void InstantiateAll_Thread(object parameter)
    {
        InstantiateAll(parameter as Transform);
    }

    private void InstantiateAll(Transform parent)
    {
        for (int i = 0; i < count; i++)
        {
            asyncObjects.Add(Instantiate(parent));
        }
    }

    private void DestroyAll()
    {
        foreach(var asyncObject in asyncObjects)
        {
            //Destroy(true) means destroy the entire game object rather than the component
            asyncObject.Destroy(true);
        }
    }

    protected void DoSomething(Async<Transform> asyncTransform)
    {
        var instance = asyncTransform.Result;
        instance.localPosition = Vector3.forward * z;
        z += zIncrement;
    }

    protected abstract Async<Transform> Instantiate(Transform parent);
}
