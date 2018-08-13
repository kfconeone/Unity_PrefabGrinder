using Xefier.Threading.Asynchronous;
using UnityEngine;

public class EventHandlerExample : BaseExample
{
    protected override Async<Transform> Instantiate(Transform parent)
    {
        var asyncObj = Async.Instantiate(prefab, parent, false);
        asyncObj.Ready += DoSomething; //BaseExample.DoSomething
        return asyncObj;
    }
}
