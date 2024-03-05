using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainPageUIController : MonoBehaviour
{
    public DragableUIControllerBase m_settingButton = null;

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
