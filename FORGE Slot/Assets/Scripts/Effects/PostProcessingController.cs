using FORGE;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [SerializeField] SurgeController surgeController;
    [SerializeField] Volume pp;
    ColorAdjustments colorAdjustments;

    private void OnEnable()
    {
        surgeController.OnSurgeTriggered += OnSurgeBegin;
        surgeController.OnSurgeEnded += OnSurgeEnd;
    }

    private void OnDisable()
    {
        surgeController.OnSurgeTriggered -= OnSurgeBegin;
        surgeController.OnSurgeEnded -= OnSurgeEnd;
    }

    private void Awake()
    {
        pp.profile.TryGet<ColorAdjustments>(out colorAdjustments);
    }

    void OnSurgeBegin()
    {
        colorAdjustments.hueShift.value = 180;
    }

    void OnSurgeEnd()
    {
        colorAdjustments.hueShift.value = 0;
    }
    
}
