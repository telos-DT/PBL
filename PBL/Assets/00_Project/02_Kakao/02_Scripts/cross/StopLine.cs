using UnityEngine;

public class StopLine : MonoBehaviour
{
    public TrafficLightUnit linkedLight;
    public bool isLeftTurnLane;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Vehicle") && other.TryGetComponent<NPCVehicleController>(out var npc))
        {
            TrafficLightState s = linkedLight.CurrentState;
            bool canGo = isLeftTurnLane ? (s == TrafficLightState.GreenWithLeft) : (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft);
            npc.SetStopStatus(!canGo, linkedLight);
        }
        else  if (other.CompareTag("Vehicle") && other.TryGetComponent<Self_Driving_NPCVehicleController>(out var npc_self))
        {
            TrafficLightState s = linkedLight.CurrentState;
            bool canGo = isLeftTurnLane ? (s == TrafficLightState.GreenWithLeft) : (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft);
            npc_self.SetStopStatus(!canGo, linkedLight);
        }
    }
}