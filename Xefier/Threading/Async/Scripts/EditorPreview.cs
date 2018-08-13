using System;
using UnityEngine;

[ExecuteInEditMode]
public class EditorPreview : MonoBehaviour
{
#if UNITY_EDITOR
    private GameObject preview;
    private Transform parent;
    private bool worldPositionStays;
    private Action<GameObject> onInstantiate;
    private bool destroyPreview;
    private bool destroySelf;

    private GameObject previewInstance;

    /// <summary>
    /// Initializes an EditorPreview and attaches it to parent
    /// WARNING: Editor Only
    /// </summary>
    /// <param name="preview"></param>
    /// <param name="parent"></param>
    /// <param name="worldPositionStays"></param>
    /// <param name="onInstantiate"></param>
    /// <returns></returns>
    public static EditorPreview Create(GameObject preview, Transform parent, bool worldPositionStays = true, Action<GameObject> onInstantiate = null)
    {
        if(preview == null || parent == null)
        {
            Debug.LogError("EditorPreview.Create: preview/parent cannot be null");
            return null;
        }

        var editorPreview = parent.gameObject.GetComponent<EditorPreview>();
        if (editorPreview == null)
        {
            editorPreview = parent.gameObject.AddComponent<EditorPreview>();
        }

        if(editorPreview.preview != preview || editorPreview.parent != parent || editorPreview.worldPositionStays != worldPositionStays)
        {
            editorPreview.destroyPreview = true;
        }

        editorPreview.preview = preview;
        editorPreview.parent = parent;
        editorPreview.worldPositionStays = worldPositionStays;
        editorPreview.onInstantiate = onInstantiate;
        return editorPreview;
    }

    /// <summary>
    /// Removed EditorPreview
    /// WARNING: Editor Only
    /// </summary>
    /// <param name="preview"></param>
    public static void Remove(EditorPreview preview)
    {
        if(preview != null)
        { 
            preview.destroySelf = true;
        }
    }

    private void Awake()
    {
        hideFlags = HideFlags.HideAndDontSave;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            enabled = false; //Stop updating
            return;
        }
        if (preview == null)
        {
            return;
        }

        if(destroySelf)
        {
            DestroyPreview();
            DestroyImmediate(this);
            return;
        }

        if(destroyPreview)
        {
            DestroyPreview();
        }

        if (previewInstance == null)
        {
            previewInstance = Instantiate(preview, parent, worldPositionStays);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            if(onInstantiate != null)
            {
                onInstantiate(previewInstance);
            }
        }        
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnDestroy()
    {
        DestroyPreview();
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
        destroyPreview = false;
    }
#endif
}
