using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class FluidConfig
{
    public const float DiffuseRange = 0.245f;

    public int SimResolution = 512;
    public float SplatForce = 6000f;

    public float DyeDiffuse = 0.23f;
    public float VelocityDiffuse = 0.20f;
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

public class FluidSimulation : MonoBehaviour
{
    public static FluidSimulation Instance;

    public FluidConfig m_config;
    public RenderTexture m_velocity = null;
    public RenderTexture m_dye = null;
    public RenderTexture m_divergence = null;
    public RenderTexture m_press = null;

    public RenderTexture m_temp = null;
    public RenderTexture m_temp2 = null;

    public List<RenderTexture> m_outputTextures = new List<RenderTexture>();
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

    private void Awake()
    {
        Application.targetFrameRate = 60;
        m_config = new FluidConfig();
        PrepareFrameBuffers();
        m_outputTextures.Add(m_dye);
        m_outputTextures.Add(m_velocity);
        m_outputTextures.Add(m_divergence);
        m_outputTextures.Add(m_press);

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

    private void SwitchOutput()
    {
        var texture = m_outputTextures[m_outputIndex];
        m_output.texture = texture;
        //m_displayMaterial.SetTexture("_MainTex", texture);
        //m_output.material = m_displayMaterial;
    }

    RenderTexture CreateRenderTexture(int width,int height)
    {
        var renderTexture = new RenderTexture(width, height, 24);
        renderTexture.format = RenderTextureFormat.ARGBFloat;
        renderTexture.enableRandomWrite = true; // 允许随机写入  
        renderTexture.filterMode = FilterMode.Bilinear; // 过滤模式为点采样  
        renderTexture.antiAliasing  = 1; // 抗锯齿级别  
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.Create(); // 创建 RenderTexture  
        return renderTexture;
    }

    void PrepareFrameBuffers()
    {
        if(m_velocity == null)
            m_velocity = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
        if (m_dye == null)
            m_dye = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
        if (m_divergence == null)
            m_divergence = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
        if (m_press == null)
            m_press = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
        if (m_temp == null)
            m_temp = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
        if (m_temp2 == null)
            m_temp2 = CreateRenderTexture(m_config.SimResolution, m_config.SimResolution);
    }

    void Blit(RenderTexture source, RenderTexture target,Material m)
    {
        if (source != target)
        {
            Graphics.Blit(source, target, m);
        }
        else
        {
            Graphics.Blit(source, m_temp, m);
            Graphics.Blit(m_temp, target, m_copyMaterial);
        }
    }

    void Advect(RenderTexture velocity, RenderTexture source,float bound)
    {
        m_advectMaterial.SetTexture("_velocity", velocity);
        m_advectMaterial.SetTexture("_source", source);
        m_advectMaterial.SetFloat("_dt", DeltaTime);
        m_advectMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        Blit(source, source, m_advectMaterial);

        SetBound(bound, source);
    }

    void Diffuse(RenderTexture source, float a,float bound)
    {
        //Debug.Log(string.Format("diffuse a:{0}", a));
        bool useImplicit = false;
        if (useImplicit)
        {
            m_linSolverMaterial.SetFloat("_a", a);
            m_linSolverMaterial.SetFloat("_c", 1 + 4 * a);
            m_linSolverMaterial.SetTexture("_Right", source);
            m_linSolverMaterial.SetTexture("_Left", m_temp);
            m_linSolverMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
            for (int i = 0; i < 20; i++)
            {
                m_linSolverMaterial.SetTexture("_Left", m_temp);
                Graphics.Blit(m_temp, m_temp2, m_linSolverMaterial);
                Graphics.Blit(m_temp2, m_temp, m_copyMaterial);
            }
            Graphics.Blit(m_temp, source, m_copyMaterial);
        }
        else
        {
            m_diffuseMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
            m_diffuseMaterial.SetTexture("_Source", source);
            m_diffuseMaterial.SetFloat("_a", a);//0.23
            for(int i = 0; i < 7; i++)
            {
                Blit(source, source, m_diffuseMaterial);
                SetBound(bound, source);
            }
            
        }
    }

    void CalcDivergence()
    {
        m_divergenceMaterial.SetTexture("_Source", m_velocity);
        m_divergenceMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        Blit(m_divergence, m_divergence, m_divergenceMaterial);
        SetBound(0, m_divergence);
    }

    void ResolvePress()
    {
        m_pressMaterial.SetTexture("_Divergence", m_divergence);
        m_pressMaterial.SetTexture("_Pressure", m_press);
        m_pressMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        for (int i = 0; i < 20; i++)
        {
            Graphics.Blit(m_press, m_temp, m_pressMaterial);
            Graphics.Blit(m_temp, m_press, m_copyMaterial);
            SetBound(0, m_press);
        }
    }

    void Project(RenderTexture source)
    {
        CalcDivergence();
        //resove press
        Clear(m_press, 0);
        ResolvePress();

        m_subtractPressureGradientMaterial.SetTexture("_Velocity", m_velocity);
        m_subtractPressureGradientMaterial.SetTexture("_Pressure", m_press);
        m_subtractPressureGradientMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        Blit(m_velocity, m_velocity, m_subtractPressureGradientMaterial);
        SetBound(1, m_velocity);
    }

    void Dissipate(RenderTexture source,float speed)
    {
        m_dissipateMaterial.SetTexture("_MainTex", source);
        m_dissipateMaterial.SetFloat("_dissipation", speed);
        m_dissipateMaterial.SetFloat("_dt", DeltaTime);
        Debug.Log(string.Format("Dissipate strength:{0} dt:{1}", speed, DeltaTime));
        Blit(source, source, m_dissipateMaterial);
    }

    void SetBound(float b,RenderTexture source)
    {
        m_boundMaterial.SetVector("_resolution", new Vector4(m_config.SimResolution, m_config.SimResolution, 1.0f, 1.0f));
        m_boundMaterial.SetFloat("_b", b);
        m_boundMaterial.SetTexture("_Source", source);
        Blit(source, source, m_boundMaterial);
    }

    void Step()
    {
        Advect(m_velocity, m_velocity,1);
        Diffuse(m_velocity, 0.25f,1.0f);
        Project(m_velocity);
        //Dissipate(m_velocity, 1.5f);

        Advect(m_velocity, m_dye,0);
        //float viscosity = m_config.DyeViscosity;
        //float a = DeltaTime * viscosity * m_config.SimResolution * m_config.SimResolution;
        Diffuse(m_dye, m_config.DyeDiffuse, 0);
        //Dissipate(m_dye, 1.0f);
    }

    void AddSource(float x,float y,float deltaX,float deltaY,Color color)
    {

        m_addSourceMaterial.SetVector("_color", new Vector4(color.r,color.g,color.b,color.a));
        m_addSourceMaterial.SetVector("_point", new Vector4(x, y));
        m_addSourceMaterial.SetFloat("_radius", 0.005f);
        Blit(m_dye, m_dye, m_addSourceMaterial);

        m_addSourceMaterial.SetVector("_color", new Vector4(deltaX, deltaY));
        Blit(m_velocity, m_velocity, m_addSourceMaterial);
       
    }

    void ApplyInput()
    {
        foreach(var pointerData in m_pointerDatas)
        {
            if (pointerData.IsMove)
            {
                pointerData.IsMove = false;
                AddSource(pointerData.x, pointerData.y, pointerData.deltaX * m_config.SplatForce, pointerData.deltaY * m_config.SplatForce, pointerData.color);
            }
        }
    }

    void Clear(RenderTexture target,float value)
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

    Color HSV2RGB(float h,float s,float v)
    {
        float r=0, g=0, b=0, i, f, p, q, t;
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

    void UpdatePointerDownData(PointerData pointerData,int id, float x,float y)
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
        if (pointerData.x != x || pointerData.y != y)
        {
            pointerData.ID = id;
            pointerData.lastX = pointerData.x;
            pointerData.lastY = pointerData.y;
            pointerData.deltaX = x - pointerData.lastX;
            pointerData.deltaY = y - pointerData.lastY;
            pointerData.x = x;
            pointerData.y = y;
            pointerData.IsMove = true;
        }
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
            if(m_outputIndex >= m_outputTextures.Count)
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
}
