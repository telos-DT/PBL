using UnityEngine;
using UnityEngine.Splines;

public class VehicleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject vehiclePrefab;   // Self_Driving_NPCVehicleController 컴포넌트가 포함된 프리팹
    [SerializeField] private SplineContainer targetSpline; // 주행할 경로 스플라인
    [SerializeField] private KeyCode spawnKey = KeyCode.F1;


    public DistanceProximitySensor distanceProximitySensor;

    private void Start()
    {
        Invoke("SpawnVehicle", 0.5f);
    }
    private void Update()
    {
        // 1. 키보드 입력 체크 (F1)
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnVehicle();
        }
    }

    private void SpawnVehicle()
    {
        if (vehiclePrefab == null || targetSpline == null)
        {
            Debug.LogWarning("[Self_Driving_NPCVehicleController] 프리팹 또는 스플라인 컨테이너가 할당되지 않았습니다.");
            return;
        }

        // 2. 차량 인스턴스화 (생성)
        GameObject newVehicle = Instantiate(vehiclePrefab);
        distanceProximitySensor.targetObject = newVehicle.transform;

        // 3. 자율주행 컴포넌트 제어
        Self_Driving_NPCVehicleController av = newVehicle.GetComponent<Self_Driving_NPCVehicleController>();

        if (av != null)
        {
            // 경로 주입 및 즉시 시작
            av.Initialize(targetSpline);
            Debug.Log($"[VehicleSpawnerTest] 차량 생성 완료 및 주행 시작 (Time: {Time.time})");
        }
        else
        {
            Debug.LogError("[VehicleSpawnerTest] 생성된 프리팹에 AutonomousVehicleTest 컴포넌트가 없습니다.");
        }
    }

}
