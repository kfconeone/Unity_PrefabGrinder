using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using Xefier.Core.UnityEvents;
using Xefier.Threading.Asynchronous;

/// <summary>
/// Calls Async.Instantiate(prefab)
/// </summary>
public class AsyncInstantiateGameObject : MonoBehaviour
{
    #region Fields
    [Header("Prefab")]
    [SerializeField, Tooltip("The prefab to instantiate. If specified, resourcePath is cleared")]
    private GameObject prefab = null;
    [SerializeField, Tooltip("Optionally reference prefab by path (Uses Resources.Load)")]
    private string resourcePath = null;

    [Header("When")]
    [SerializeField, Tooltip("When to call Instantiate")]
    private When when;

    [Header("Transform")]
    [SerializeField, Tooltip("Parent transform of new object")]
    private Transform parent;
    [SerializeField, Tooltip("Whether or not this transform is the parent. When true, overrides parent to this transform")]
    private bool isThisParent = true;
    [SerializeField, Tooltip("If true, the parent-relative transforms are modified to keep world space transforms")]
    private bool worldPositionStays = true;
    [SerializeField, Tooltip("Transforms to reset when instantiated")]
    private ResetTransform reset;

    [Header("Events")]
    [SerializeField]
    private Events events;

#if UNITY_EDITOR
    [Header("Editor")]
    [SerializeField, Tooltip("Shows an editor-only preview of the prefab that does not show up hierarchy or get saved.")]
    private bool showPreview = false;

    private EditorPreview preview;
    private bool lastIsThisParent = true;
#endif

    private GameObject prefabCache;
    private Async<GameObject> asyncObj;
    #endregion

    #region Public Interface
    /// <summary>
    /// Calls Async.Instantiate
    /// When ready Instantiated Event will be triggered
    /// </summary>
    public void Instantiate()
    {
        prefab = GetPrefab();
        if (prefab == null)
        {
            Debug.LogError("prefab not specified", this);
            return;
        }

        asyncObj = Async.Instantiate(prefab, parent, worldPositionStays);
        asyncObj.Ready += (obj) =>
        {
            Reset(obj.Result);
            OnInstantiated(obj.Result);
        };
    }
    #endregion

    #region Unity
    private void Awake()
    {
        if (when.awake)
        {
            Instantiate();
        }
    }

    private void Start()
    {
        if (when.start)
        {
            Instantiate();
        }
    }

    private void OnEnable()
    {
        if(when.onEnable)
        {
            Instantiate();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(isThisParent && (parent == null || !lastIsThisParent))
        {
            parent = transform;
        }
        else
        {
            isThisParent = parent == transform;
        }
        lastIsThisParent = isThisParent;

        //Preview
        if (showPreview)
        {
            var prefab = GetPrefab();
            if(prefab != null)
            { 
                preview = EditorPreview.Create(GetPrefab(), transform, worldPositionStays, (go) => Reset(go));
            }
        }
        else
        {
            EditorPreview.Remove(preview);
        }

        if(prefab != null)
        {
            resourcePath = null;
        }
        if(string.IsNullOrEmpty(resourcePath))
        {
            prefabCache = null;
        }
    }
#endif
    #endregion

    #region Private Methods
    private GameObject GetPrefab()
    {
        if (prefab != null)
        {
            return prefab;
        }
        else
        { 
            if(prefabCache == null && !string.IsNullOrEmpty(resourcePath))
            { 
                Profiler.BeginSample("AsyncInstantiateGameObject.GetPrefab (Resources.Load)", this);
                //TODO: Use Resources.LoadAsync
                prefabCache = Resources.Load<GameObject>(resourcePath);
                Profiler.EndSample();

                if(prefabCache == null)
                {
                    Debug.LogError(string.Format("Failed to load resource: {0}", resourcePath));
                }
            }
            return prefabCache;
        }
    }

    private void Reset(GameObject instance)
    {
        if (reset.localPosition)
        {
            instance.transform.localPosition = Vector3.zero;
        }
        if (reset.localRotation)
        {
            instance.transform.localRotation = Quaternion.identity;
        }
        if (reset.localScale)
        {
            instance.transform.localScale = Vector3.one;
        }
    }

    private void OnInstantiated(GameObject instance)
    {
        events.Instantiated.Invoke(instance);
    } 
    #endregion

    #region Serializable Classes
    [System.Serializable]
    private class Events
    {
        /// <summary>
        /// Event that triggers when the object has instantiated
        /// </summary>
        public UnityEvent_GameObject Instantiated;
    }

    [System.Serializable]
    private class ResetTransform
    {
        public bool localPosition = false;
        public bool localRotation = false;
        public bool localScale = false;
    }

    [System.Serializable]
    private class When
    {
        public bool awake = false;
        public bool start = true;
        public bool onEnable = false;
        //TODO:
    } 
    #endregion
}
