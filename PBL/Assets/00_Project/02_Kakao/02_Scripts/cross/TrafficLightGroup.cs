using UnityEngine;

public class TrafficLightGroup : MonoBehaviour
{
    [System.Serializable]
    public struct PhaseGroup
    {
        public TrafficLightUnit[] horizontalLights; // 가로축 신호등들
        public TrafficLightUnit[] verticalLights;   // 세로축 신호등들
    }

    public PhaseGroup groups;

    public void UpdateByGlobalTime(float normalizedTime)
    {
        // 0.0 ~ 0.4  : 가로축 Green (직진)
        // 0.4 ~ 0.5  : 가로축 GreenWithLeft (직진+좌회전)
        // 0.5 ~ 0.9  : 세로축 Green (직진)
        // 0.9 ~ 1.0  : 세로축 GreenWithLeft (직진+좌회전)
        // * Yellow는 TrafficManager에서 별도로 처리하거나 이 사이사이에 단계를 추가하면 됩니다.

        if (normalizedTime < 0.3f)
        {
            SetStates(groups.horizontalLights, TrafficLightState.Green);
            SetStates(groups.verticalLights, TrafficLightState.Red);
        }
        else if (normalizedTime < 0.5f)
        {
            SetStates(groups.horizontalLights, TrafficLightState.GreenWithLeft);
        }
        else if (normalizedTime < 0.8f)
        {
            SetStates(groups.horizontalLights, TrafficLightState.Red);
            SetStates(groups.verticalLights, TrafficLightState.Green);
        }
        else
        {
            SetStates(groups.verticalLights, TrafficLightState.GreenWithLeft);
        }
    }

    private void SetStates(TrafficLightUnit[] units, TrafficLightState state)
    {
        if (units == null) return;
        foreach (var unit in units) unit.ChangeState(state);
    }
}