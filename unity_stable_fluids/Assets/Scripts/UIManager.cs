using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class UIRegisterItem
{
    [SerializeField]
    public string m_id;
    [SerializeField]
    public string m_controllerClassName;
    [SerializeField]
    public GameObject m_prefab;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField]
    public List<UIRegisterItem> m_reisterItemList = new List<UIRegisterItem>();

    public void Awake()
    {
        Instance = this;
    }

    private UIRegisterItem GetRegisterItem(string id)
    {
        var item = m_reisterItemList.Find((e) => { return e.m_id == id; });
        return item;
    }

    public static Type GetType(string typeFullName)
    {
        var type = System.Reflection.Assembly.Load("Assembly-CSharp").GetType(typeFullName);
#if UNITY_EDITOR
        if (type == null)
        {
            type = System.Reflection.Assembly.Load("Assembly-CSharp-Editor").GetType(typeFullName);
        }
#endif
        return type;
    }

    public UIControllerBase Show(UIIntent intent)
    {
        var registerItem = GetRegisterItem(intent.ID);
        if(registerItem == null)
        {
            Debug.Log(string.Format("id: {0} registerItem is null", intent.ID));
            return null;
        }
        var go = GameObject.Instantiate(registerItem.m_prefab,this.transform,false);
        //go.transform.SetParent(this.transform);
        //go.transform.localPosition = Vector3.zero;
        //go.transform.localScale = Vector3.one;
        var type = GetType(registerItem.m_controllerClassName);
        if(type == null)
        {
            Debug.Log(string.Format("id: {0} GetControllerType is null", intent.ID));
            return null;
        }
        var controller = go.GetComponent(type) as UIControllerBase;
        if (controller  == null)
        {

            controller = go.AddComponent(type) as UIControllerBase;
        }
        controller.Init(intent);
        return controller;
    }
}
