using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigUIController : MonoBehaviour
{
    public TMPro.TMP_Dropdown m_velocityResolutionDropDown;
    public TMPro.TMP_Dropdown m_dyeResolutionDropDown;
    public Slider m_velocityDisspiateSlider;
    public InputField m_velocityDisspiateInputField;
    public Slider m_dyeDisspiateSlider;
    public InputField m_dyeDisspiateInputField;
    public Slider m_velocityDiffuseSlider;
    public InputField m_velocityDiffuseInputField;
    public Slider m_dyeDiffuseSlider;
    public InputField m_dyeDiffuseInputField;
    public Slider m_splatRadiusSlider;
    public InputField m_splatRadiusInputField;
    public Slider m_iterNumSlider;
    public InputField m_iterNumInputField;
    public Slider m_splatForceSlider;
    public InputField m_splatForceInputField;
    public Button m_randomButton;
    public Button m_randomButton2;
    public Toggle m_colorfulToggle;


    // Start is called before the first frame update
    void Start()
    {
        Init();
        m_velocityDisspiateSlider.onValueChanged.AddListener(OnVelocityDisspiateSliderValueChanged);
        m_dyeDisspiateSlider.onValueChanged.AddListener(OnDyeDisspiateSliderValueChanged);
        m_velocityDiffuseSlider.onValueChanged.AddListener(OnVelocityDiffuseSliderValueChanged);
        m_dyeDiffuseSlider.onValueChanged.AddListener(OnDyeDiffuseSliderValueChanged);
        m_iterNumSlider.onValueChanged.AddListener(OnIterNumSliderValueChanged);
        m_splatRadiusSlider.onValueChanged.AddListener(OnSplatRadiusSliderValueChanged);
        m_velocityResolutionDropDown.onValueChanged.AddListener(OnSimResolutionChanged);
        m_dyeResolutionDropDown.onValueChanged.AddListener(OnDyeResolutionChanged);
        m_splatForceSlider.onValueChanged.AddListener(OnSplatForceSliderValueChanged);
        m_randomButton.onClick.AddListener(OnRandomButtonClick);
        m_randomButton2.onClick.AddListener(OnRandomButtonClick);
        m_colorfulToggle.onValueChanged.AddListener(OnColorfulToggleChanged);
    }

    private void Init()
    {
        List<string> m_velocityResolutionOptions = new List<string>();
        for(int i = 0; i < FluidConfig.VelocityResolutionOptions.Count; i++)
        {
            m_velocityResolutionOptions.Add(FluidConfig.VelocityResolutionOptions[i].ToString());
        }
        m_velocityResolutionDropDown.ClearOptions();
        m_velocityResolutionDropDown.AddOptions(m_velocityResolutionOptions);
        int index = FluidConfig.VelocityResolutionOptions.FindIndex((e) => { return e == FluidSimulation.Instance.m_config.SimResolution; });
        m_velocityResolutionDropDown.value = index;

        List<string> m_dyeResolutionOptions = new List<string>();
        for (int i = 0; i < FluidConfig.DyeResolutionOptions.Count; i++)
        {
            m_dyeResolutionOptions.Add(FluidConfig.DyeResolutionOptions[i].ToString());
        }
        m_dyeResolutionDropDown.ClearOptions();
        m_dyeResolutionDropDown.AddOptions(m_velocityResolutionOptions);
        index = FluidConfig.DyeResolutionOptions.FindIndex((e) => { return e == FluidSimulation.Instance.m_config.DyeResolution; });
        m_dyeResolutionDropDown.value = index;

        m_dyeDiffuseSlider.value = FluidSimulation.Instance.m_config.DyeDiffuse / FluidConfig.DiffuseRange;
        m_dyeDiffuseInputField.text = m_dyeDiffuseSlider.value.ToString();
        m_velocityDiffuseSlider.value = FluidSimulation.Instance.m_config.VelocityDiffuse / FluidConfig.DiffuseRange;
        m_velocityDiffuseInputField.text = m_velocityDiffuseSlider.value.ToString();
        m_velocityDisspiateSlider.value = FluidSimulation.Instance.m_config.VelocityDisspiate / FluidConfig.DisspiateRange;
        m_velocityDisspiateInputField.text = m_velocityDisspiateSlider.value.ToString();
        m_dyeDisspiateSlider.value = FluidSimulation.Instance.m_config.DyeDisspiate / FluidConfig.DisspiateRange;
        m_dyeDisspiateInputField.text = m_dyeDisspiateSlider.value.ToString();
        m_splatRadiusSlider.value = FluidSimulation.Instance.m_config.SplatRadius;
        m_splatRadiusInputField.text = m_splatRadiusSlider.value.ToString();
        m_iterNumSlider.value = (FluidSimulation.Instance.m_config.PressureIterNum-20) / FluidConfig.PressureIterRange;
        m_iterNumInputField.text = FluidSimulation.Instance.m_config.PressureIterNum.ToString();

        var range = FluidConfig.SplatForceRange;
        var value = (FluidSimulation.Instance.m_config.SplatForce - range.x) / (range.y - range.x);
        m_splatForceSlider.value = value;
        m_splatForceInputField.text = FluidSimulation.Instance.m_config.SplatForce.ToString();

        m_colorfulToggle.isOn = FluidSimulation.Instance.m_config.Colorful;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region eventListener
    private void OnVelocityDisspiateSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_velocityDisspiateInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.VelocityDisspiate = value * FluidConfig.DisspiateRange;
    }


    private void OnDyeDisspiateSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_dyeDisspiateInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.DyeDisspiate = value * FluidConfig.DisspiateRange;
    }


    private void OnVelocityDiffuseSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_velocityDiffuseInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.VelocityDiffuse = value * FluidConfig.DiffuseRange;
    }


    private void OnDyeDiffuseSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_dyeDiffuseInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.DyeDiffuse = value * FluidConfig.DiffuseRange;
    }

    private void OnIterNumSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        int num = 20 + (int)(value * FluidConfig.PressureIterRange);
        m_iterNumInputField.text = num.ToString();
        FluidSimulation.Instance.m_config.PressureIterNum = num;
    }

    private void OnSplatRadiusSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0.1f, 1);

        m_splatRadiusInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.SplatRadius = value;
    }

    private void OnSplatForceSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0.0f, 1);
        var range = FluidConfig.SplatForceRange;
        var num = range.x + (range.y - range.x) * value;
        m_splatRadiusInputField.text = num.ToString();
        FluidSimulation.Instance.m_config.SplatForce = num;
    }

    private void OnSimResolutionChanged(int index)
    {
        Debug.Log(string.Format("OnSimResolutionChanged:{0}", index));
        FluidSimulation.Instance.ChangeSimFrameBufferSizeByIndex(index);
    }

    private void OnDyeResolutionChanged(int index)
    {
        Debug.Log(string.Format("OnDyeResolutionChanged:{0}", index));
        FluidSimulation.Instance.ChangeDyeFrameBufferSizeByIndex(index);
    }

    private void OnRandomButtonClick()
    {
        FluidSimulation.Instance.RandomAdd();
    }

    private void OnColorfulToggleChanged(bool value)
    {
        FluidSimulation.Instance.m_config.Colorful = value;
    }

    #endregion
}
