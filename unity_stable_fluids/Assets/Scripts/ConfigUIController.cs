using UnityEngine;
using UnityEngine.UI;

public class ConfigUIController : MonoBehaviour
{
    public Slider m_dyeSlider;
    public InputField m_dyeInputField;

    public Slider m_iterNumSlider;
    public InputField m_iterNumInputField;

    // Start is called before the first frame update
    void Start()
    {
        Init();
        m_dyeSlider.onValueChanged.AddListener(OnDyeSliderValueChanged);
        m_dyeInputField.onValueChanged.AddListener(OnDyeInputFieldValueChanged);
        m_iterNumSlider.onValueChanged.AddListener(OnIterNumSliderValueChanged);
        m_iterNumInputField.onValueChanged.AddListener(OnIterNumInputFieldValueChanged);
    }

    private void Init()
    {
        m_dyeSlider.value = FluidSimulation.Instance.m_config.DyeDiffuse / FluidConfig.DiffuseRange;
        m_dyeInputField.text = m_dyeSlider.value.ToString();
        m_iterNumSlider.value = (FluidSimulation.Instance.m_config.PressureIterNum-20) / FluidConfig.PressureIterRange;
        m_iterNumInputField.text = FluidSimulation.Instance.m_config.PressureIterNum.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region eventListener
    private void OnDyeSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        m_dyeInputField.text = value.ToString();
        FluidSimulation.Instance.m_config.DyeDiffuse = value * FluidConfig.DiffuseRange;
    }

    private void OnDyeInputFieldValueChanged(string value)
    {

    }

    private void OnIterNumSliderValueChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        int num = 20 + (int)(value * FluidConfig.PressureIterRange);
        m_iterNumInputField.text = num.ToString();
        FluidSimulation.Instance.m_config.PressureIterNum = num;
    }

    private void OnIterNumInputFieldValueChanged(string value)
    {

    }
    #endregion
}
