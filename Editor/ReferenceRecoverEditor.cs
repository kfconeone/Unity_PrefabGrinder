using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kfc.Grinder;
[CustomEditor(typeof(ReferenceRecoverer))]
public class ReferenceRecoverEditor : Editor {

    public override void OnInspectorGUI()
    {
        
        // 單純顯示文字
        GUILayout.Label("選擇要建立Reference的MonoBehavior腳本 : ", EditorStyles.boldLabel);
        ReferenceRecoverer recoverer = (ReferenceRecoverer)target;
        recoverer.monoScript = (MonoBehaviour)EditorGUILayout.ObjectField("MonoBehavior : ", recoverer.monoScript, typeof(MonoBehaviour), true);
        if (GUILayout.Button("產生Reference Script"))
        {
            recoverer.FindReference();
            GrinderMenuItems.GetReference();
        }
    }
}
