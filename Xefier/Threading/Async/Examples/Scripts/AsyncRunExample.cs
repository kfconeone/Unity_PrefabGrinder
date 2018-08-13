using UnityEngine;
using System.Threading;
using Xefier.Threading.Asynchronous;

public class AsyncRunExample : MonoBehaviour
{

    void Start ()
    {
        AsyncRunOnMain();
        //Supports threads like Async.Instantiate/Destroy
        new Thread(new ThreadStart(AsyncRunOnThread)).Start();
    }

    private void AsyncRunOnMain()
    {
        Async.Run(() => Debug.Log("Async.Run"));
        Async.Run(DoSomething);
    }

    private void AsyncRunOnThread()
    {
        Async.Run(() => Debug.Log("Async.Run"));
        Async.Run(DoSomething);
        //This would throw an error on a separate thread (Uncomment to see)
        //DoSomething();
    }

    private void DoSomething()
    {
        //This won't throw an error because it will be ran on the main thread
        transform.localPosition = Vector3.zero;
        Debug.Log("Async.Run");
    }
}
