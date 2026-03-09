using UnityEngine;
using UnityEngine.Splines;

public class RoadSegment : MonoBehaviour
{
    [Header("Lane Settings")]
    public SplineContainer laneSplines; // 각 차선 데이터
    public float speedLimit = 60f;      // 도로별 제한 속도

    [Header("Network")]
    public Transform[] entryPoints;     // 진입점
    public Transform[] exitPoints;      // 진출점
}