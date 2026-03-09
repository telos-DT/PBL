using UnityEngine;
using UnityEngine.Splines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DispatchManager : MonoBehaviour
{
    [System.Serializable]
    public struct DispatchZone
    {
        public string zoneName;
        public SplineContainer taxiSpline;
        public List<Transform> passengerPoints;
    }

    [Header("Settings")]
    public GameObject taxiPrefab;
    public GameObject passengerPrefab;
    public List<DispatchZone> dispatchZones;
    public Color[] matchColors;
    public KeyCode dispatchKey = KeyCode.F2;

    private Dictionary<string, DispatchZone> _zoneDict = new Dictionary<string, DispatchZone>();

    // 색상 인덱스를 관리하기 위한 큐 (중복 방지 핵심)
    private Queue<Color> _availableColors = new Queue<Color>();

    void Awake()
    {
        InitializeZoneDictionary();
    }

    private void InitializeZoneDictionary()
    {
        foreach (var zone in dispatchZones)
        {
            if (string.IsNullOrEmpty(zone.zoneName)) continue;

            if (!_zoneDict.ContainsKey(zone.zoneName))
                _zoneDict.Add(zone.zoneName, zone);
        }
    }

    private void Start()
    {
        // 지연 실행 시점에 색상 셔플
        PrepareColorQueue();
        Invoke(nameof(SettingTaxi), 0.5f);
    }

    void Update()
    {
        if (Input.GetKeyDown(dispatchKey))
        {
            PrepareColorQueue(); // 새로 배차할 때마다 색상 풀 초기화
            SettingTaxi();
        }
    }

    /// <summary>
    /// 사용 가능한 색상을 무작위로 섞어 큐에 삽입합니다. (Fisher-Yates Shuffle)
    /// </summary>
    private void PrepareColorQueue()
    {
        if (matchColors == null || matchColors.Length == 0)
        {
            Debug.LogError("Match Colors가 설정되지 않았습니다.");
            return;
        }

        List<Color> colorList = new List<Color>(matchColors);
        int n = colorList.Count;

        // 셔플 알고리즘 적용
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Color value = colorList[k];
            colorList[k] = colorList[n];
            colorList[n] = value;
        }

        _availableColors = new Queue<Color>(colorList);
    }

    private void SettingTaxi()
    {
        foreach (var zone in dispatchZones)
        {
            DispatchOneByOne(zone.zoneName);
        }
    }

    public void DispatchOneByOne(string zoneName)
    {
        if (!_zoneDict.TryGetValue(zoneName, out DispatchZone zone)) return;

        for (int i = 0; i < zone.passengerPoints.Count; i++)
        {
            Transform spawnPoint = zone.passengerPoints[i];

            // 1. 색상 결정 (남은 색상이 없으면 기본 색상 할당)
            Color assignedColor = _availableColors.Count > 0
                ? _availableColors.Dequeue()
                : Color.white;

            // 2. 승객 생성 및 초기화
            GameObject pObj = Instantiate(passengerPrefab, spawnPoint.position, Quaternion.identity);
            if (pObj.TryGetComponent(out Passenger passenger))
            {
                passenger.Initialize(assignedColor);
            }

            // 3. 택시 생성 및 초기화
            GameObject tObj = Instantiate(taxiPrefab);
            if (tObj.TryGetComponent(out Taxi_AutonomousController taxi))
            {
                taxi.Initialize(zone.taxiSpline);
                taxi.AssignPassenger(passenger);
            }

            Debug.Log($"[{zoneName}] {spawnPoint.name}: 색상 {assignedColor} 배정 완료.");
        }
    }
}

//using UnityEngine;
//using UnityEngine.Splines;
//using System.Collections;
//using System.Collections.Generic;

//public class DispatchManager : MonoBehaviour
//{
//    [System.Serializable]
//    public struct DispatchZone
//    {
//        public string zoneName;
//        public SplineContainer taxiSpline;      // 해당 구역 택시가 달릴 스플라인
//        public List<Transform> passengerPoints; // 승객이 생성될 고정 지점들
//    }

//    [Header("Settings")]
//    public GameObject taxiPrefab;
//    public GameObject passengerPrefab;
//    public List<DispatchZone> dispatchZones;
//    public Color[] matchColors;
//    public KeyCode dispatchKey = KeyCode.F2;

//    // 구역 이름을 키로 하는 딕셔너리
//    private Dictionary<string, DispatchZone> _zoneDict = new Dictionary<string, DispatchZone>();

//    void Awake()
//    {
//        // 딕셔너리 초기화
//        foreach (var zone in dispatchZones)
//        {
//            if (!_zoneDict.ContainsKey(zone.zoneName))
//                _zoneDict.Add(zone.zoneName, zone);
//        }
//    }

//    private void Start()
//    {
//        Invoke("SettingTaxi", 0.5f);
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(dispatchKey))
//        {
//            SettingTaxi();
//        }
//    }

//    private void SettingTaxi()
//    {
//        foreach (var zone in dispatchZones)
//        {
//            DispatchOneByOne(zone.zoneName);
//        }
//    }

//    public void DispatchOneByOne(string zoneName)
//    {
//        if (!_zoneDict.TryGetValue(zoneName, out DispatchZone zone)) return;

//        // 딕셔너리에 정의된 passengerPoints 리스트를 순회하며 한 명씩 생성
//        for (int i = 0; i < zone.passengerPoints.Count; i++)
//        {
//            Transform spawnPoint = zone.passengerPoints[i];

//            // 1. 승객 생성
//            GameObject pObj = Instantiate(passengerPrefab, spawnPoint.position, Quaternion.identity);
//            Passenger p = pObj.GetComponent<Passenger>();

//            // 승객별로 고유한(또는 순차적인) 색상 부여
//            Color color = matchColors[Random.Range(1, matchColors.Length)];
//            p.Initialize(color);

//            // 2. 택시 생성 (스플라인 시작 지점에 생성)
//            GameObject tObj = Instantiate(taxiPrefab);
//            Taxi_AutonomousController taxi = tObj.GetComponent<Taxi_AutonomousController>();

//            // 3. 택시 초기화 및 승객 할당
//            taxi.Initialize(zone.taxiSpline);
//            taxi.AssignPassenger(p);

//            Debug.Log($"[{zoneName}] 위치 {spawnPoint.name}에 승객 생성 및 택시 배차 완료.");
//        }
//    }
//}