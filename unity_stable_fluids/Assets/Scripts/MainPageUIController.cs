using bluebean.UGFramework.Log;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class MainPageUIController : MonoBehaviour
{
    public DragableUIControllerBase m_settingButton = null;

    public void Awake()
    {
        
        var logMgr = LogManager.CreateLogManager();
        logMgr.Initlize(true, true, Path.Combine(UnityEngine.Application.persistentDataPath,"Log/"), "Log_");
        Debug.Log(TestUI.BuildSystemInfoText(false));
    }

    // Start is called before the first frame update
    void Start()
    {
        m_settingButton.EventOnClick += OnSettingButtonClick;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnSettingButtonClick()
    {
        Debug.Log("OnSettingButtonClick");
        UIManager.Instance.Show(new UIIntent("ConfigUI"));
    }
}
