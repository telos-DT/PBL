using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [Header("Global Timing")]
    public float cycleDuration = 20f; // 전체 신호 한 주기의 길이

    [Header("Intersections")]
    public List<TrafficLightGroup> managedIntersections = new List<TrafficLightGroup>();

    void Update()
    {
        // 서버 시간이나 게임 시간을 기준으로 전역 타임스탬프 계산
        // 교차로가 몇 개가 추가되어도 동일한 '기준 시각'을 공유함
        float globalTime = Time.time % cycleDuration;
        float normalizedTime = globalTime / cycleDuration; // 0.0 ~ 1.0

        foreach (var intersection in managedIntersections)
        {
            if (intersection != null)
            {
                intersection.UpdateByGlobalTime(normalizedTime);
            }
        }
    }

    // 에디터 툴: 씬의 모든 교차로를 자동으로 찾아 리스트업
    [ContextMenu("Refresh All Intersections")]
    public void RefreshIntersections()
    {
        managedIntersections.Clear();
        managedIntersections.AddRange(FindObjectsByType<TrafficLightGroup>(FindObjectsSortMode.None));
    }
}