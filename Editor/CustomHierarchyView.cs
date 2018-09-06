using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CustomHierarchyView {

    public static Dictionary<GameObject, double> performances = new Dictionary<GameObject, double>();
    static List<GameObject> mGrinderPrefabs;
    public static bool isErrorExist;
    delegate void grinderGuiDelegate(int instanceID, Rect selectionRect);
    public static List<GameObject> grinderPrefabs
    {
        get
        {
            if (mGrinderPrefabs == null)
            {
                mGrinderPrefabs = new List<GameObject>();
            }
            return mGrinderPrefabs;
        }
    }


    static CustomHierarchyView()
    {
        if(performances.Count == 0) EditorUtility.ClearProgressBar();
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;


    }
    
    static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {

        //Debug.Log(string.Format("{0} : {1} - {2}", EditorUtility.InstanceIDToObject(instanceID), instanceID, selectionRect));
        GameObject hierarchyGameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (hierarchyGameObject == null)
        {
            return;
        }
        else
        {
            var prefabType = PrefabUtility.GetPrefabType(hierarchyGameObject);
            if (prefabType == PrefabType.PrefabInstance)    
            {
                if (PrefabUtility.FindPrefabRoot(hierarchyGameObject).GetComponentInChildren<AssetSwapper>(true) == null && !grinderPrefabs.Contains(PrefabUtility.FindPrefabRoot(hierarchyGameObject))) return;
                #region 這段是著色背景
                //一定要先著色背景，不然字會消失
                //if (hierarchyGameObject.GetComponent<AssetSwapper>() != null)
                //{
                //    EditorGUI.DrawRect(selectionRect, grinderSwapperColor);
                //}
                #endregion
                Texture2D rootTexture = (Texture2D)Resources.Load("green_prefab");
                Texture2D swapperTexture = (Texture2D)Resources.Load("swap");
                Texture2D swapperErrorTexture = (Texture2D)Resources.Load("swap_error");
                Texture2D swapperDuplicatedTexture = (Texture2D)Resources.Load("swap_duplicated");
                Texture2D arrowDownTexture = (Texture2D)Resources.Load("down_arrow");
                Texture2D arrowDownErrorTexture = (Texture2D)Resources.Load("down_arrow_error");
                Texture2D referenceRecoveryTexture = (Texture2D)Resources.Load("recovery");
                GUIStyle swapperTextureStyle = new GUIStyle();
                GUIStyle arrowDownTextureStyle = new GUIStyle();
                GUIStyle grinderPrefabTextStyle = new GUIStyle();
                GUIStyle referenceRecoveryStyle = new GUIStyle();
                Color grinderSwapperColor = new Color(87f / 255f, 87f / 255f, 87f / 255f, 1f);
                Color fontColor;
                if (hierarchyGameObject.activeSelf)
                {
                    fontColor = new Color(87f / 255f, 202f / 255f, 1f / 255f, 1.0f);
                }
                else
                {
                    fontColor = new Color(71f / 255f, 129f / 255f, 28f / 255f, 1.0f);
                }
                Rect offsetRect = new Rect(selectionRect.position + new Vector2(0, 2), selectionRect.size);
                string name;
                if (performances.ContainsKey(hierarchyGameObject))
                {
                    string performanceText = performances[hierarchyGameObject].ToString();
                    if (performances[hierarchyGameObject] == 0)
                    {
                        performanceText = " < 1";
                    }

                    name = hierarchyGameObject.name + string.Format("  [{0}]", performanceText);
                }
                else
                {
                    name = hierarchyGameObject.name;
                }

                EditorGUI.LabelField(offsetRect, name, new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = fontColor }
                });

                #region 這段是設定Icon
                if (hierarchyGameObject.GetComponent<AssetSwapper>() != null)
                {
                    Rect iconRect = new Rect(selectionRect);    //這是Icon的位置大小定義
                    iconRect.x = iconRect.width -20;   //這是Icon的位置,從selectionRect的寬度往左 15 pixel
                    iconRect.width = 15;            //寬
                    iconRect.height = iconRect.height - 2;  //高

                    //親子之間都存在assetSwapper,不合法的操作
                    if (hierarchyGameObject.GetComponentsInChildren<AssetSwapper>(true).Length > 1 ||
                       hierarchyGameObject.GetComponentsInParent<AssetSwapper>(true).Length > 1)
                    {
                        isErrorExist = true;
                        swapperTextureStyle.normal.background = swapperErrorTexture;
                    }
                    else
                    {
                        isErrorExist = false;
                        swapperTextureStyle.normal.background = swapperTexture;
                    }

                    var duplicatedGrp = hierarchyGameObject.transform.parent.GetComponentsInChildren<AssetSwapper>().GroupBy(tempSwapper => tempSwapper.name).SelectMany(grp => grp.Skip(1)).Select(tempSwapper => tempSwapper.name);
                    if (duplicatedGrp.Count() > 0)
                    {
                        if(duplicatedGrp.Contains(hierarchyGameObject.name))
                        {
                            isErrorExist = true;
                            swapperTextureStyle.normal.background = swapperDuplicatedTexture;
                        }
                    }

                    GUI.Label(iconRect, "", swapperTextureStyle);

                    Rect toggleRect = new Rect(selectionRect);
                    toggleRect.x = toggleRect.width;
                    toggleRect.y -= 2; 
                    bool toggleStatus = GUI.Toggle(toggleRect, hierarchyGameObject.GetComponent<AssetSwapper>() != null, "");
                    if (!toggleStatus)
                    {
                        GameObject.DestroyImmediate(hierarchyGameObject.GetComponent<AssetSwapper>());
                    }
                }
                else
                {

                    //根物件不允許出現toggle
                    if (!PrefabUtility.FindPrefabRoot(hierarchyGameObject).Equals(hierarchyGameObject))
                    {
                        //要上下都沒有assetSwapper才會出現toggle
                        if (hierarchyGameObject.GetComponentsInChildren<AssetSwapper>(true).Length == 0 &&
                           hierarchyGameObject.GetComponentsInParent<AssetSwapper>(true).Length == 0)
                        {
                            Rect toggleRect = new Rect(selectionRect);
                            toggleRect.x = toggleRect.width;
                            toggleRect.y -= 2;
                            bool toggleStatus = GUI.Toggle(toggleRect, hierarchyGameObject.GetComponent<AssetSwapper>() != null, "");
                            if (toggleStatus)
                            {
                                hierarchyGameObject.AddComponent<AssetSwapper>();
                            }
                        }

                    }

                        
                }


                if (hierarchyGameObject.GetComponent<AssetSwapper>() == null && hierarchyGameObject.GetComponentsInChildren<AssetSwapper>(true).Length > 0)
                {
                    Rect arrowDownRect = new Rect(selectionRect);    //這是Icon的位置大小定義
                    arrowDownRect.x = arrowDownRect.width - 40;   //這是Icon的位置,從selectionRect的寬度往左 15 pixel
                    arrowDownRect.width = 15;            //寬
                    arrowDownRect.height = arrowDownRect.height - 2;  //高     
                    arrowDownTextureStyle.normal.background = arrowDownTexture;
                    if (PrefabUtility.FindPrefabRoot(hierarchyGameObject).Equals(hierarchyGameObject))
                    {
                        arrowDownTextureStyle.normal.background = rootTexture;
                    }
                    else
                    {
                        arrowDownTextureStyle.normal.background = arrowDownTexture;
                        var swappers = hierarchyGameObject.GetComponentsInChildren<AssetSwapper>(true);
                        if (swappers.Where(tempSwapper => tempSwapper.GetComponentsInParent<AssetSwapper>(true).Length > 1 || tempSwapper.GetComponentsInChildren<AssetSwapper>(true).Length > 1).Count() > 0)
                        {
                            arrowDownTextureStyle.normal.background = arrowDownErrorTexture;
                        }
                    }
                    GUI.Label(arrowDownRect, "", arrowDownTextureStyle);
                }

                if (hierarchyGameObject.GetComponent<IApplier>() != null)
                {
                    Rect recoveryRect = new Rect(selectionRect);    //這是Icon的位置大小定義
                    recoveryRect.x = recoveryRect.width - 60;   //這是Icon的位置,從selectionRect的寬度往左 15 pixel
                    recoveryRect.width = 15;            //寬
                    recoveryRect.height = recoveryRect.height - 4;  //高     
                    referenceRecoveryStyle.normal.background = referenceRecoveryTexture;
                    GUI.Label(recoveryRect, "", referenceRecoveryStyle);
                }
                #endregion
            }

        }
        
    }
}
