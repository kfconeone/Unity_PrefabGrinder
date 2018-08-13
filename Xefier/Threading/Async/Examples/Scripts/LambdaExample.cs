using Xefier.Threading.Asynchronous;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LambdaExample : BaseExample
{
    protected override Async<Transform> Instantiate(Transform parent)
    {
        var asyncObj = Async.Instantiate(prefab, parent, false);
        asyncObj.Ready += (o) =>
        {
            DoSomething(o); //BaseExample.DoSomething
        };
        return asyncObj;
    }
}
