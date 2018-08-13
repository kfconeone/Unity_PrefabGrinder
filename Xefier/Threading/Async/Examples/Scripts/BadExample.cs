using UnityEngine;

public class BadExample : MonoBehaviour
{
    public Transform prefab;
    public int count = 1000;
    public float zIncrement = 1;
    private float z;

    //How you might spawn tons of objects without AsyncObjects (For comparing performance)
    private void Start()
    {
        for (int i = 0; i < count; i++)
        {
            var instance = Instantiate(prefab, transform);
            instance.localPosition = Vector3.forward * z;
            z += zIncrement;
        }
    }
}
