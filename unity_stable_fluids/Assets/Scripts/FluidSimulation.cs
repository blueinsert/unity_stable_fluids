using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class FluidConfig
{
    public const float DiffuseRange = 0.245f;
    public const float PressureIterRange = 200;

    public int SimResolution = 1024;
    public int DyeResolution = 1024;
    public float SplatForce = 6000f;
    public float SplatRadius = 0.2f;
    public float DyeDiffuse = 0.23f;
    public float VelocityDiffuse = 0.20f;
    public int PressureIterNum = 20;
}

public class PointerData
{
    public int ID = 0;
    public bool IsDown = false;
    public bool IsMove = false;
    public float x;
    public float y;
    public float lastX;
    public float lastY;
    public float deltaX;
    public float deltaY;
    public Color color;
}

public class OutputTextureInfo
{
    public RenderTexture m_outputTexture;
    public string m_desc;
}

public class FluidSimulation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    public static FluidSimulation Instance;

    public FluidConfig m_config;

    public Vector2Int m_simResolution;
    public Vector2Int m_dyeResolution;
    public Vector2 m_simTexelSize;
    public Vector2 m_dyeTexelSize;

    public RenderTexture m_velocity = null;
    public RenderTexture m_dye = null;
    public RenderTexture m_divergence = null;
    public RenderTexture m_press = null;

    public RenderTexture m_temp = null;
    public RenderTexture m_temp2 = null;

    public List<OutputTextureInfo> m_outputTextures = new List<OutputTextureInfo>();
    public int m_outputIndex = 0;

    public Material m_addSourceMaterial = null;
    public Material m_advectMaterial = null;
    public Material m_diffuseMaterial = null;
    public Material m_divergenceMaterial = null;
    public Material m_pressMaterial = null;
    public Material m_subtractPressureGradientMaterial = null;
    public Material m_boundMaterial = null;
    public Material m_linSolverMaterial = null;
    public Material m_dissipateMaterial = null;
    public Material m_displayMaterial = null;
    public Material m_copyMaterial = null;
    public Material m_clearMaterial = null;

    public RawImage m_output = null;

    public List<PointerData> m_pointerDatas = new List<PointerData>();

    public bool m_clearFlag = false;

    public const int FrameRate = 60;
    public const float DeltaTime = 1.0f / FrameRate;

    public OutputTextureInfo CurrentOutputTextureInfo
    {
        get
        {
            var texture = m_outputTextures[m_outputIndex];
            return texture;
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        m_config = new FluidConfig();
        m_simResolution = GetResolution(m_config.SimResolution);
        m_dyeResolution = GetResolution(m_config.DyeResolution);
        m_simTexelSize = new Vector2(1.0f / m_simResolution.x, 1.0f / m_simResolution.y);
        m_dyeTexelSize = new Vector2(1.0f / m_dyeResolution.x, 1.0f / m_dyeResolution.y);

        PrepareFrameBuffers();
        AddOutputTexture(m_dye, "染料");
        AddOutputTexture(m_velocity, "速度");
        AddOutputTexture(m_divergence, "散度");
        AddOutputTexture(m_press, "压力");

        m_pointerDatas.Add(new PointerData());

        m_output.material = m_displayMaterial;
        m_outputIndex = 0;
        SwitchOutput();
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void AddOutputTexture(RenderTexture output, string desc)
    {
        m_outputTextures.Add(new OutputTextureInfo() { m_outputTexture = output, m_desc = desc });
    }

    private void SwitchOutput()
    {
        var texture = m_outputTextures[m_outputIndex];
        m_output.texture = texture.m_outputTexture;
        //m_displayMaterial.SetTexture("_MainTex", texture);
        //m_output.material = m_displayMaterial;
    }

    RenderTexture CreateRenderTexture(int w, int h)
    {
        var renderTexture = new RenderTexture(w, h, 0);
        renderTexture.format = RenderTextureFormat.ARGBHalf;
        renderTexture.enableRandomWrite = true; // 允许随机写入  
        renderTexture.filterMode = FilterMode.Bilinear; // 过滤模式为点采样  
        renderTexture.antiAliasing = 1; // 抗锯齿级别  
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.Create(); // 创建 RenderTexture  
        return renderTexture;
    }

    Vector2Int GetResolution(int resolution)
    {
        float radio = Screen.width / (float)Screen.height;
        int h = Mathf.Min(resolution, Screen.height);
        int w = Mathf.FloorToInt(radio * h);
        return new Vector2Int(w, h);
    }

    void PrepareFrameBuffers()
    {
        if (m_velocity == null)
            m_velocity = CreateRenderTexture(m_simResolution.x, m_simResolution.y);
        if (m_dye == null)
            m_dye = CreateRenderTexture(m_dyeResolution.x, m_dyeResolution.y);
        if (m_divergence == null)
            m_divergence = CreateRenderTexture(m_simResolution.x, m_simResolution.y);
        if (m_press == null)
            m_press = CreateRenderTexture(m_simResolution.x, m_simResolution.y);
        if (m_temp == null)
            m_temp = CreateRenderTexture(m_simResolution.x, m_simResolution.y);
        if (m_temp2 == null)
            m_temp2 = CreateRenderTexture(m_dyeResolution.x, m_dyeResolution.y);
    }

    void Blit(RenderTexture source, RenderTexture target, Material m)
    {
        if (source != target)
        {
            Graphics.Blit(source, target, m);
        }
        else
        {
            if (target.width == m_temp.width)
            {
                Graphics.Blit(source, m_temp, m);
                Graphics.Blit(m_temp, target, m_copyMaterial);
            }
            else
            {
                Graphics.Blit(source, m_temp2, m);
                Graphics.Blit(m_temp2, target, m_copyMaterial);
            }
        }
    }

    void Advect(RenderTexture velocity, RenderTexture source, float bound, Vector2 texelSize)
    {
        m_advectMaterial.SetTexture("_velocity", velocity);
        m_advectMaterial.SetTexture("_source", source);
        m_advectMaterial.SetFloat("_dt", DeltaTime);
        m_advectMaterial.SetVector("_texelSize", new Vector4(texelSize.x, texelSize.y, 1.0f, 1.0f));
        Blit(source, source, m_advectMaterial);

        SetBound(bound, source);
    }

    void Diffuse(RenderTexture source, float a, float bound, Vector2 texelSize)
    {
        //Debug.Log(string.Format("diffuse a:{0}", a));
        bool useImplicit = false;
        if (useImplicit)
        {
            //m_linSolverMaterial.SetFloat("_a", a);
            //m_linSolverMaterial.SetFloat("_c", 1 + 4 * a);
            //m_linSolverMaterial.SetTexture("_Right", source);
            //m_linSolverMaterial.SetTexture("_Left", m_temp);
            //m_linSolverMaterial.SetVector("_texelSize", new Vector4(texelSize.x, texelSize.y, 1.0f, 1.0f));
            //for (int i = 0; i < 20; i++)
            //{
            //    m_linSolverMaterial.SetTexture("_Left", m_temp);
            //    Graphics.Blit(m_temp, m_temp2, m_linSolverMaterial);
            //    Graphics.Blit(m_temp2, m_temp, m_copyMaterial);
            //}
            //Graphics.Blit(m_temp, source, m_copyMaterial);
        }
        else
        {
            m_diffuseMaterial.SetVector("_texelSize", new Vector4(texelSize.x, texelSize.y, 1.0f, 1.0f));
            m_diffuseMaterial.SetTexture("_Source", source);
            m_diffuseMaterial.SetFloat("_a", a);//0.23
            for (int i = 0; i < 7; i++)
            {
                Blit(source, source, m_diffuseMaterial);
                SetBound(bound, source);
            }

        }
    }

    void CalcDivergence()
    {
        m_divergenceMaterial.SetTexture("_Source", m_velocity);
        m_divergenceMaterial.SetVector("_texelSize", new Vector4(m_simTexelSize.x, m_simTexelSize.y, 1.0f, 1.0f));
        Blit(m_divergence, m_divergence, m_divergenceMaterial);
        SetBound(0, m_divergence);
    }

    void ResolvePress()
    {
        m_pressMaterial.SetTexture("_Divergence", m_divergence);  
        m_pressMaterial.SetVector("_texelSize", new Vector4(m_simTexelSize.x, m_simTexelSize.y, 1.0f, 1.0f));
        for (int i = 0; i < m_config.PressureIterNum; i++)
        {
            m_pressMaterial.SetTexture("_Pressure", m_press);
            Blit(m_press, m_press, m_pressMaterial);
            SetBound(0, m_press);
        }
    }

    void Project()
    {
        CalcDivergence();
        //resove press
        Clear(m_press, 0.4f);
        ResolvePress();

        m_subtractPressureGradientMaterial.SetTexture("_Velocity", m_velocity);
        m_subtractPressureGradientMaterial.SetTexture("_Pressure", m_press);
        m_subtractPressureGradientMaterial.SetVector("_texelSize", new Vector4(m_simTexelSize.x, m_simTexelSize.y, 1.0f, 1.0f));
        Blit(m_velocity, m_velocity, m_subtractPressureGradientMaterial);
        SetBound(1, m_velocity);
    }

    void Dissipate(RenderTexture source, float speed)
    {
        m_dissipateMaterial.SetTexture("_MainTex", source);
        m_dissipateMaterial.SetFloat("_dissipation", speed);
        m_dissipateMaterial.SetFloat("_dt", DeltaTime);
        Debug.Log(string.Format("Dissipate strength:{0} dt:{1}", speed, DeltaTime));
        Blit(source, source, m_dissipateMaterial);
    }

    void SetBound(float b, RenderTexture source)
    {
        //m_boundMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        //m_boundMaterial.SetFloat("_b", b);
        //m_boundMaterial.SetTexture("_Source", source);
        //Blit(source, source, m_boundMaterial);
    }

    void Step()
    {
        Advect(m_velocity, m_velocity, 1, m_simTexelSize);
        //Diffuse(m_velocity, 0.25f,1.0f,m_simTexelSize);
        Project();
        //Dissipate(m_velocity, 1.5f);

        Advect(m_velocity, m_dye, 0, m_dyeTexelSize);
        //float viscosity = m_config.DyeViscosity;
        //float a = DeltaTime * viscosity * m_config.SimResolution * m_config.SimResolution;
        //Diffuse(m_dye, m_config.DyeDiffuse, 0);
        //Dissipate(m_dye, 1.0f);
    }

    void AddSource(float x, float y, float deltaX, float deltaY, Color color)
    {
        float radio = Screen.width / (float)Screen.height;
        m_addSourceMaterial.SetFloat("_aspectRadio", radio);
        m_addSourceMaterial.SetVector("_color", new Vector4(color.r, color.g, color.b, color.a));
        m_addSourceMaterial.SetVector("_point", new Vector4(x, y));
        m_addSourceMaterial.SetFloat("_radius", m_config.SplatRadius / 100f * radio);
        Blit(m_dye, m_dye, m_addSourceMaterial);

        m_addSourceMaterial.SetVector("_color", new Vector4(deltaX, deltaY));
        Blit(m_velocity, m_velocity, m_addSourceMaterial);

    }

    void ApplyInput()
    {
        foreach (var pointerData in m_pointerDatas)
        {
            if (pointerData.IsMove)
            {
                pointerData.IsMove = false;
                AddSource(pointerData.x, pointerData.y, pointerData.deltaX * m_config.SplatForce, pointerData.deltaY * m_config.SplatForce, pointerData.color);
            }
        }
    }

    void Clear(RenderTexture target, float value)
    {
        m_clearMaterial.SetFloat("_value", 0);
        m_clearMaterial.SetTexture("_Source", target);
        Blit(target, target, m_clearMaterial);
    }

    void Clear()
    {
        m_clearMaterial.SetFloat("_value", 0);
        m_clearMaterial.SetTexture("_Source", m_dye);
        Blit(m_dye, m_dye, m_clearMaterial);

        m_clearMaterial.SetTexture("_Source", m_velocity);
        Blit(m_velocity, m_velocity, m_clearMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        ApplyInput();
        Step();
        if (m_clearFlag)
        {
            m_clearFlag = false;
            Clear();
        }
        EventListerner();
    }

    Color HSV2RGB(float h, float s, float v)
    {
        float r = 0, g = 0, b = 0, i, f, p, q, t;
        i = Mathf.Floor(h * 6);
        f = h * 6 - i;
        p = v * (1 - s);
        q = v * (1 - f * s);
        t = v * (1 - (1 - f) * s);

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Color(r, g, b);
    }

    Color GenerateColor()
    {
        var c = HSV2RGB(Random.Range(0, 100) / 100.0f, 1.0f, 1.0f);
        c.r *= 0.15f;
        c.g *= 0.15f;
        c.b *= 0.15f;
        return c;
    }

    void UpdatePointerDownData(PointerData pointerData, int id, float x, float y)
    {
        pointerData.x = x;
        pointerData.y = y;
        pointerData.IsDown = true;
        pointerData.IsMove = false;
        pointerData.lastX = x;
        pointerData.lastY = y;
        pointerData.deltaX = 0;
        pointerData.deltaY = 0;
        pointerData.color = GenerateColor();
    }

    void UpdatePointerMoveData(PointerData pointerData, int id, float x, float y)
    {
        pointerData.ID = id;
        pointerData.lastX = pointerData.x;
        pointerData.lastY = pointerData.y;
        float radio = Screen.width / (float)Screen.height;
        pointerData.deltaX = (x - pointerData.lastX)* radio;
        pointerData.deltaY = y - pointerData.lastY;
        pointerData.x = x;
        pointerData.y = y;
        pointerData.IsMove = pointerData.deltaX != 0 || pointerData.deltaY != 0;
    }

    void UpdatePointerUpData(PointerData pointerData, int id, float x, float y)
    {
        pointerData.IsMove = false;
    }

    void EventListerner()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Input.mousePosition;
            float u = mousePos.x / (float)Screen.width;
            float v = mousePos.y / (float)Screen.height;
            var pointer = m_pointerDatas[0];

            UpdatePointerDownData(pointer, -1, u, v);
        }
        if (Input.GetMouseButton(0))
        {
            var mousePos = Input.mousePosition;
            float u = mousePos.x / (float)Screen.width;
            float v = mousePos.y / (float)Screen.height;
            var pointer = m_pointerDatas[0];

            UpdatePointerMoveData(pointer, -1, u, v);
        }
        if (Input.GetMouseButtonUp(0))
        {
            var mousePos = Input.mousePosition;
            float u = mousePos.x / (float)Screen.width;
            float v = mousePos.y / (float)Screen.height;
            var pointer = m_pointerDatas[0];

            UpdatePointerUpData(pointer, -1, u, v);
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            if (!m_clearFlag)
                m_clearFlag = true;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            m_outputIndex++;
            if (m_outputIndex >= m_outputTextures.Count)
            {
                m_outputIndex = 0;
            }
            SwitchOutput();
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            m_outputIndex--;
            if (m_outputIndex < 0)
            {
                m_outputIndex = m_outputTextures.Count - 1;
            }
            SwitchOutput();
        }
    }

    private void OnRenderObject()
    {

    }

    private void OnDestroy()
    {
        if (m_velocity != null)
            m_velocity.Release();
        if (m_dye != null)
            m_dye.Release();
        if (m_temp != null)
            m_temp.Release();
        if (m_temp2 != null)
            m_temp2.Release();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var pos = eventData.position;
        Debug.Log(string.Format("OnPointerDown:{0}", pos));

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        var pos = eventData.position;
        Debug.Log(string.Format("OnPointerUp:{0}", pos));
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        var pos = eventData.position;
        Debug.Log(string.Format("OnPointerMove:{0}", pos));
    }
}
