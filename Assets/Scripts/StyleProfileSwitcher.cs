using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 风格验证微单：三档风格切换（Play 验证用，非正式系统）。
///   F1=原始档(全关,对照组) / F2=PSX恐怖档(全开+粗颗粒0.45+RenderScale 0.7) / F3=手绘感档(台阶化色彩+细颗粒0.15,不降采样)。
/// 切换=换 Volume Profile 引用;雾/环境光/RenderScale 由本脚本随档切换,退出时恢复原值。
/// </summary>
public class StyleProfileSwitcher : MonoBehaviour
{
    public Volume volume;                 // 全局 Volume（构建器接线）
    public VolumeProfile psxProfile;      // F2
    public VolumeProfile sketchProfile;   // F3

    [Header("F2 降采样")]
    public float psxRenderScale = 0.7f;

    [Header("雾（F2/F3 开,F1 关）")]
    public float fogDensity = 0.015f;
    public Color fogColor = new Color(0.16f, 0.20f, 0.17f);   // 深灰绿

    [Header("环境光（F2/F3 调暗一档）")]
    public float styledAmbientIntensity = 0.5f;

    // 原始值备份
    float origAmbient;
    bool origFogOn;
    FogMode origFogMode;
    float origFogDensity;
    Color origFogColor;
    float origRenderScale = 1f;

    int current = 2;   // 启动即 F2（验证主角）;按 F1/F2/F3 切换

    void Start()
    {
        origAmbient = RenderSettings.ambientIntensity;
        origFogOn = RenderSettings.fog;
        origFogMode = RenderSettings.fogMode;
        origFogDensity = RenderSettings.fogDensity;
        origFogColor = RenderSettings.fogColor;
        var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rp != null) origRenderScale = rp.renderScale;

        Apply(current);
        Debug.Log("[风格] F1=原始 F2=PSX恐怖 F3=手绘感,按键切换");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f1Key.wasPressedThisFrame) Apply(1);
        else if (kb.f2Key.wasPressedThisFrame) Apply(2);
        else if (kb.f3Key.wasPressedThisFrame) Apply(3);
    }

    void Apply(int mode)
    {
        current = mode;
        var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        switch (mode)
        {
            case 1:   // 原始档：全关,恢复一切
                if (volume != null) volume.enabled = false;
                RenderSettings.fog = origFogOn;
                RenderSettings.fogMode = origFogMode;
                RenderSettings.fogDensity = origFogDensity;
                RenderSettings.fogColor = origFogColor;
                RenderSettings.ambientIntensity = origAmbient;
                if (rp != null) rp.renderScale = origRenderScale;
                break;

            case 2:   // PSX 恐怖档
                if (volume != null) { volume.enabled = true; volume.profile = psxProfile; }
                StyledEnvironment();
                if (rp != null) rp.renderScale = psxRenderScale;   // 低分辨率 PSX 味
                break;

            case 3:   // 手绘感档：不降采样
                if (volume != null) { volume.enabled = true; volume.profile = sketchProfile; }
                StyledEnvironment();
                if (rp != null) rp.renderScale = origRenderScale;
                break;
        }
        HudController.Instance?.ShowMessage($"风格档 F{mode}", 1.2f);
        Debug.Log($"[风格] 切到 F{mode}");
    }

    void StyledEnvironment()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;   // 指数雾,剖面纵深感
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;
        RenderSettings.ambientIntensity = styledAmbientIntensity;   // 让点光源"值钱"
    }

    void OnDestroy()
    {
        // 退出 Play/换场景:恢复 RenderScale(它存在管线资产上,不恢复会污染资产)
        var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rp != null) rp.renderScale = origRenderScale;
    }
}
