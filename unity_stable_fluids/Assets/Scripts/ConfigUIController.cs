using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigUIController : MonoBehaviour
{
    public Slider m_visocitySlider;
    public InputField m_visocityInputField;

    // Start is called before the first frame update
    void Start()
    {
        m_visocitySlider.onValueChanged.AddListener(OnViscositySliderValueChanged);
        m_visocityInputField.onValueChanged.AddListener(OnViscosityInputFieldValueChanged);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region eventListener
    private void OnViscositySliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_visocityInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.DyeViscosity = value * 1f;
    }

    private void OnViscosityInputFieldValueChanged(string value)
    {

    }
    #endregion
}
