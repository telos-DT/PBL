using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Self_Driving_NPCVehicleController : MonoBehaviour
{
    // ... (기존 변수들 유지) ...
    protected SplineContainer _currentSpline;
    protected float _progress;
    protected TrafficSpawner _spawner;
    protected string _myPoolName;
    protected TrafficLightUnit _waitingLight;

    [Header("Specs")]
    public float maxSpeedKmH = 60f;
    public float acceleration = 5f;
    public float deceleration = 20f;

    [Header("Sensors")]
    public float sensorDist = 15f;
    public float safetyMargin = 6f;
    public LayerMask vehicleLayer;
    public LayerMask obstacleLayer;
    public Vector3 sensorOffset = new Vector3(0, 0.5f, 1.5f);
    public float obstacleRadius = 0.8f;

    [Header("Avoidance & Rotation")]
    public Transform vehicleModel;
    public float avoidOffset = -2.5f;
    public float transitionSpeed = 1.5f;
    public float returnDelay = 0.5f;
    public float maxRotationAngle = 8f;
    public float rotationSmoothness = 1.5f;

    protected float _currentSpeedMS;
    protected bool _isStopped;
    protected Rigidbody _rb;

    protected float _targetSideOffset;
    protected float _currentSideOffset;
    protected float _lastSideOffset;
    protected float _smoothedYaw;
    protected bool _isAvoiding;
    protected Coroutine _returnRoutine;
    protected bool _isFollowingVehicle;

    [Header("Visuals")]
    public MeshRenderer brakeLightRenderer;
    public int materialsCount;
    [Range(0, 1)] public float normalWeight = 0f;
    [Range(0, 1)] public float brakeWeight = 1f;
    public float weightSmoothSpeed = 10f; // 너무 빠르면(100) 깜빡거려 보일 수 있어 10~15 추천
    private float _currentWeight;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if(brakeLightRenderer != null)
        Debug.Log(brakeLightRenderer.materials[materialsCount]);
    }

    public void Initialize(SplineContainer spline, TrafficSpawner spawner = null, string poolName = null)
    {
        _currentSpline = spline;
        _spawner = spawner;
        _myPoolName = poolName;
        _progress = 0;
        _currentSpeedMS = 0;
        _isStopped = false;
        _currentSideOffset = 0;
        _targetSideOffset = 0;
    }

    public void SwitchSpline(SplineContainer newSpline)
    {
        if (newSpline == null) return;
        float savedSpeed = _currentSpeedMS;
        _currentSpline = newSpline;
        _progress = 0f;
        _currentSpeedMS = savedSpeed;
    }

    protected virtual void FixedUpdate()
    {
        if (!_currentSpline) return;

        HandleBaseMovement();
        UpdateBrakeVisuals(); // 비주얼 업데이트 메서드 호출 추가
    }

    protected void HandleBaseMovement()
    {
        float targetSpeed = _isStopped ? 0 : CalculateSafetySpeed(maxSpeedKmH / 3.6f);

        if (!_isStopped && !_isFollowingVehicle) CheckObstacles();
        else _targetSideOffset = 0;

        // 실제 가감속 처리
        _currentSpeedMS = Mathf.MoveTowards(_currentSpeedMS, targetSpeed, (targetSpeed < _currentSpeedMS ? deceleration : acceleration) * Time.fixedDeltaTime);

        if (_currentSpeedMS < 0.05f && targetSpeed <= 0) { _currentSpeedMS = 0; }

        _progress += (_currentSpeedMS * Time.fixedDeltaTime) / _currentSpline.CalculateLength();
        if (_progress >= 1f) { if (_spawner) _spawner.ReturnToPool(this, _myPoolName); return; }

        _lastSideOffset = _currentSideOffset;
        _currentSideOffset = Mathf.Lerp(_currentSideOffset, _targetSideOffset, Time.fixedDeltaTime * transitionSpeed);

        UpdateTransform();
        ApplyModelRotation();
    }

    // [신규] 브레이크 등 비주얼 제어 로직
    protected void UpdateBrakeVisuals()
    {
        if (brakeLightRenderer == null) return;

        // 1. 현재 차량이 도달하려는 목표 속도를 먼저 계산합니다.
        float targetSpeed = _isStopped ? 0 : CalculateSafetySpeed(maxSpeedKmH / 3.6f);

        // 2. 핵심 조건: 목표 속도가 현재 속도보다 낮으면 '감속(브레이크) 중'입니다.
        // 추가로 _currentSpeedMS > 0.1f 조건을 넣으면 완전히 멈췄을 때는 등이 꺼집니다.
        bool isDecelerating = targetSpeed < _currentSpeedMS && _currentSpeedMS > 0.1f;

        // 3. 감속 중일 때만 brakeWeight 적용
        float targetWeight = isDecelerating ? brakeWeight : normalWeight;

        // HDRP 머티리얼 프로퍼티 적용
        brakeLightRenderer.materials[materialsCount].SetFloat("_EmissiveExposureWeight", targetWeight);
    }

    // ... (이하 UpdateTransform, CalculateSafetySpeed, CheckObstacles 등 기존 코드 동일) ...
    protected virtual void UpdateTransform()
    {
        Vector3 splinePos = (Vector3)_currentSpline.EvaluatePosition(_progress);
        float3 tan = _currentSpline.EvaluateTangent(_progress);
        Quaternion splineRot = Quaternion.identity;
        if (!tan.Equals(float3.zero)) splineRot = Quaternion.LookRotation((Vector3)tan);

        Vector3 finalPos = splinePos + (splineRot * Vector3.right * _currentSideOffset);
        _rb.MovePosition(finalPos);
        _rb.MoveRotation(splineRot);
    }

    protected float CalculateSafetySpeed(float desired)
    {
        Vector3 start = transform.TransformPoint(sensorOffset);
        if (Physics.Raycast(start, transform.forward, out RaycastHit hit, sensorDist, vehicleLayer))
        {
            _isFollowingVehicle = true;
            return hit.distance <= safetyMargin ? 0 : desired * Mathf.Clamp01((hit.distance - safetyMargin) / (sensorDist - safetyMargin));
        }
        _isFollowingVehicle = false;
        return desired;
    }

    protected void CheckObstacles()
    {
        Vector3 sensorStartPos = transform.TransformPoint(sensorOffset);
        bool hasObstacle = Physics.SphereCast(sensorStartPos, obstacleRadius, transform.forward, out _, sensorDist, obstacleLayer);
        if (hasObstacle)
        {
            _isAvoiding = true;
            _targetSideOffset = avoidOffset;
            if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
        }
        else if (_isAvoiding && _returnRoutine == null)
        {
            _returnRoutine = StartCoroutine(ReturnToPathAfterDelay());
        }
    }

    private IEnumerator ReturnToPathAfterDelay()
    {
        yield return new WaitForSeconds(returnDelay);
        _targetSideOffset = 0f;
        _isAvoiding = false;
        _returnRoutine = null;
    }

    protected void ApplyModelRotation()
    {
        if (vehicleModel == null) return;
        float moveDelta = (Time.fixedDeltaTime > 0) ? (_currentSideOffset - _lastSideOffset) / Time.fixedDeltaTime : 0;
        float rawTargetYaw = moveDelta * maxRotationAngle * 0.5f;
        float clampedTargetYaw = Mathf.Clamp(rawTargetYaw, -maxRotationAngle, maxRotationAngle);
        _smoothedYaw = Mathf.Lerp(_smoothedYaw, clampedTargetYaw, Time.fixedDeltaTime * rotationSmoothness);
        vehicleModel.localRotation = Quaternion.Euler(0, _smoothedYaw, 0);
    }

    public void SetStopStatus(bool stop, TrafficLightUnit light = null)
    {
        _isStopped = stop;
        if (stop && light != null)
        {
            if (_waitingLight != null) _waitingLight.OnSignalChanged -= HandleSignal;
            _waitingLight = light;
            _waitingLight.OnSignalChanged += HandleSignal;
        }
        else if (!stop) Unsubscribe();
    }

    private void HandleSignal(TrafficLightState s) { if (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft) SetStopStatus(false); }
    private void Unsubscribe() { if (_waitingLight != null) { _waitingLight.OnSignalChanged -= HandleSignal; _waitingLight = null; } }
    void OnDisable() => Unsubscribe();
}


//using UnityEngine;
//using UnityEngine.Splines;
//using Unity.Mathematics;
//using System.Collections;

//[RequireComponent(typeof(Rigidbody))]
//public class Self_Driving_NPCVehicleController : MonoBehaviour
//{
//    // [수정] private -> protected: 자식 클래스에서 접근 가능하도록 변경
//    protected SplineContainer _currentSpline;
//    protected float _progress;
//    protected TrafficSpawner _spawner;
//    protected string _myPoolName;
//    protected TrafficLightUnit _waitingLight;

//    [Header("Specs")]
//    public float maxSpeedKmH = 60f;
//    public float acceleration = 5f;
//    public float deceleration = 20f;

//    [Header("Sensors")]
//    public float sensorDist = 15f;
//    public float safetyMargin = 6f;
//    public LayerMask vehicleLayer;
//    public LayerMask obstacleLayer;
//    public Vector3 sensorOffset = new Vector3(0, 0.5f, 1.5f);
//    public float obstacleRadius = 0.8f;

//    [Header("Avoidance & Rotation")]
//    public Transform vehicleModel;
//    public float avoidOffset = -2.5f;
//    public float transitionSpeed = 1.5f;
//    public float returnDelay = 0.5f;
//    public float maxRotationAngle = 8f;
//    public float rotationSmoothness = 1.5f;

//    protected float _currentSpeedMS;
//    protected bool _isStopped;
//    protected Rigidbody _rb;

//    protected float _targetSideOffset;
//    protected float _currentSideOffset;
//    protected float _lastSideOffset;
//    protected float _smoothedYaw;
//    protected bool _isAvoiding;
//    protected Coroutine _returnRoutine;
//    protected bool _isFollowingVehicle;

//    [Header("Visuals")]
//    public MeshRenderer brakeLightRenderer; // 브레이크 등 메쉬나 특정 오브젝트의 렌더러
//    [Range(0, 1)] public float normalWeight = 0f;  // 평상시 weight
//    [Range(0, 1)] public float brakeWeight = 1f;   // 감속/정지 시 weight
//    public float weightSmoothSpeed = 100f;           // 값 변경 속도
//    private float _currentWeight;

//    protected virtual void Awake()
//    {
//        _rb = GetComponent<Rigidbody>();
//        _rb.interpolation = RigidbodyInterpolation.Interpolate;
//    }

//    public void Initialize(SplineContainer spline, TrafficSpawner spawner = null, string poolName = null)
//    {
//        _currentSpline = spline;
//        _spawner = spawner;
//        _myPoolName = poolName;
//        _progress = 0;
//        _currentSpeedMS = 0;
//        _isStopped = false;
//        _currentSideOffset = 0;
//        _targetSideOffset = 0;
//    }

//    // [추가] 이미지에서 에러가 났던 경로 전환 기능
//    public void SwitchSpline(SplineContainer newSpline)
//    {
//        if (newSpline == null) return;
//        float savedSpeed = _currentSpeedMS;
//        _currentSpline = newSpline;
//        _progress = 0f;
//        _currentSpeedMS = savedSpeed;
//        Debug.Log($"[Path Switch] {newSpline.name}으로 경로 변경.");
//    }

//    // 자식 클래스에서 덮어쓸 수 있도록 virtual 선언
//    protected virtual void FixedUpdate()
//    {
//        if (!_currentSpline) return;
//        HandleBaseMovement();
//    }

//    protected void HandleBaseMovement()
//    {
//        float targetSpeed = _isStopped ? 0 : CalculateSafetySpeed(maxSpeedKmH / 3.6f);

//        if (!_isStopped && !_isFollowingVehicle) CheckObstacles();
//        else _targetSideOffset = 0;

//        _currentSpeedMS = Mathf.MoveTowards(_currentSpeedMS, targetSpeed, (targetSpeed < _currentSpeedMS ? deceleration : acceleration) * Time.fixedDeltaTime);

//        if (_currentSpeedMS < 0.05f && targetSpeed <= 0) { _currentSpeedMS = 0; return; }

//        _progress += (_currentSpeedMS * Time.fixedDeltaTime) / _currentSpline.CalculateLength();
//        if (_progress >= 1f) { if (_spawner) _spawner.ReturnToPool(this, _myPoolName); return; }

//        _lastSideOffset = _currentSideOffset;
//        _currentSideOffset = Mathf.Lerp(_currentSideOffset, _targetSideOffset, Time.fixedDeltaTime * transitionSpeed);

//        UpdateTransform();
//        ApplyModelRotation();
//    }

//    protected virtual void UpdateTransform()
//    {
//        Vector3 splinePos = (Vector3)_currentSpline.EvaluatePosition(_progress);
//        float3 tan = _currentSpline.EvaluateTangent(_progress);
//        Quaternion splineRot = Quaternion.identity;
//        if (!tan.Equals(float3.zero)) splineRot = Quaternion.LookRotation((Vector3)tan);

//        Vector3 finalPos = splinePos + (splineRot * Vector3.right * _currentSideOffset);
//        _rb.MovePosition(finalPos);
//        _rb.MoveRotation(splineRot);
//    }

//    protected float CalculateSafetySpeed(float desired)
//    {
//        Vector3 start = transform.TransformPoint(sensorOffset);
//        if (Physics.Raycast(start, transform.forward, out RaycastHit hit, sensorDist, vehicleLayer))
//        {
//            _isFollowingVehicle = true;
//            return hit.distance <= safetyMargin ? 0 : desired * Mathf.Clamp01((hit.distance - safetyMargin) / (sensorDist - safetyMargin));
//        }
//        _isFollowingVehicle = false;
//        return desired;
//    }

//    protected void CheckObstacles()
//    {
//        Vector3 sensorStartPos = transform.TransformPoint(sensorOffset);
//        bool hasObstacle = Physics.SphereCast(sensorStartPos, obstacleRadius, transform.forward, out _, sensorDist, obstacleLayer);
//        if (hasObstacle)
//        {
//            _isAvoiding = true;
//            _targetSideOffset = avoidOffset;
//            if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
//        }
//        else if (_isAvoiding && _returnRoutine == null)
//        {
//            _returnRoutine = StartCoroutine(ReturnToPathAfterDelay());
//        }
//    }

//    private IEnumerator ReturnToPathAfterDelay()
//    {
//        yield return new WaitForSeconds(returnDelay);
//        _targetSideOffset = 0f;
//        _isAvoiding = false;
//        _returnRoutine = null;
//    }

//    protected void ApplyModelRotation()
//    {
//        if (vehicleModel == null) return;
//        float moveDelta = (Time.fixedDeltaTime > 0) ? (_currentSideOffset - _lastSideOffset) / Time.fixedDeltaTime : 0;
//        float rawTargetYaw = moveDelta * maxRotationAngle * 0.5f;
//        float clampedTargetYaw = Mathf.Clamp(rawTargetYaw, -maxRotationAngle, maxRotationAngle);
//        _smoothedYaw = Mathf.Lerp(_smoothedYaw, clampedTargetYaw, Time.fixedDeltaTime * rotationSmoothness);
//        vehicleModel.localRotation = Quaternion.Euler(0, _smoothedYaw, 0);
//    }

//    public void SetStopStatus(bool stop, TrafficLightUnit light = null)
//    {
//        _isStopped = stop;
//        if (stop && light != null)
//        {
//            if (_waitingLight != null) _waitingLight.OnSignalChanged -= HandleSignal;
//            _waitingLight = light;
//            _waitingLight.OnSignalChanged += HandleSignal;
//        }
//        else if (!stop) Unsubscribe();
//    }

//    private void HandleSignal(TrafficLightState s) { if (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft) SetStopStatus(false); }
//    private void Unsubscribe() { if (_waitingLight != null) { _waitingLight.OnSignalChanged -= HandleSignal; _waitingLight = null; } }
//    void OnDisable() => Unsubscribe();
//}