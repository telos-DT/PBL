using UnityEngine;
using System;

public enum TrafficLightState { Red, Yellow, Green, GreenWithLeft }

public class TrafficLightUnit : MonoBehaviour
{
    [System.Serializable]
    public struct LightMaterialMapping
    {
        public TrafficLightState state;
        public Material material;
    }

    public MeshRenderer lightRenderer;
    public int targetSlotIndex = 2;
    public LightMaterialMapping[] lightMappings;

    public event Action<TrafficLightState> OnSignalChanged;
    public TrafficLightState CurrentState { get; private set; }

    public void ChangeState(TrafficLightState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        if (lightRenderer != null)
        {
            Material[] mats = lightRenderer.sharedMaterials;
            foreach (var map in lightMappings)
            {
                if (map.state == newState)
                {
                    mats[targetSlotIndex] = map.material;
                    break;
                }
            }
            lightRenderer.sharedMaterials = mats;
        }
        OnSignalChanged?.Invoke(newState);
    }
}