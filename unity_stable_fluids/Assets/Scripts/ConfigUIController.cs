using UnityEngine;
using UnityEngine.UI;

public class ConfigUIController : MonoBehaviour
{
    public Slider m_dyeSlider;
    public InputField m_dyeInputField;

    // Start is called before the first frame update
    void Start()
    {
        Init();
        m_dyeSlider.onValueChanged.AddListener(OnDyeSliderValueChanged);
        m_dyeInputField.onValueChanged.AddListener(OnDyeInputFieldValueChanged);
    }

    private void Init()
    {
        m_dyeSlider.value = FluidSimulation.Instance.m_config.DyeDiffuse / FluidConfig.DiffuseRange;
        m_dyeInputField.text = m_dyeSlider.value.ToString();
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
    #endregion
}
