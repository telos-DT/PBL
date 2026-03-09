using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class Taxi_AutonomousController : MonoBehaviour
{
    private SplineContainer _currentSpline;
    private float _progress;
    private float _cachedLength;
    private Rigidbody _rb;
    private float _currentSpeedMS;
    private TrafficLightUnit _waitingLight; // 신호등 구독용

    [Header("Specs")]
    public float maxSpeedKmH = 50f;
    public float acceleration = 5f;
    public float deceleration = 40f;

    [Header("Sensors (Obstacles & Signals)")]
    public float sensorDist = 15f;      // 앞차 감지 거리
    public float safetyMargin = 6f;     // 차간 유지 거리
    public LayerMask vehicleLayer;      // 차량 레이어
    public Vector3 sensorOffset = new Vector3(0, 0.5f, 1.5f);

    [Header("Taxi Settings")]
    public float matchDisplayDistance = 15f;
    public float pickupDistance = 3.0f;

    private Passenger _assignedPassenger;
    private GameObject _indicator;
    private bool _isMatchedIndicated = false;
    private bool _isPickingUp = false;
    private bool _isStoppedBySignal = false; // 신호등에 의한 정지 상태

    public void Initialize(SplineContainer spline)
    {
        _currentSpline = spline;
        _cachedLength = spline.CalculateLength();
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void AssignPassenger(Passenger passenger)
    {
        _assignedPassenger = passenger;
        _isMatchedIndicated = false;
        _isPickingUp = false;
    }

    void FixedUpdate()
    {
        if (!_currentSpline) return;

        // 1. 상황별 목표 속도 계산
        float targetSpeedMS = maxSpeedKmH / 3.6f;

        // [추가] 앞차와의 거리 체크 (자율주행 핵심 기능)
        targetSpeedMS = CalculateSafetySpeed(targetSpeedMS);

        // [추가] 신호등에 의한 정지 체크
        if (_isStoppedBySignal) targetSpeedMS = 0;

        // 2. 승객 픽업 거리 체크 및 정차
        float distToPassenger = float.MaxValue;
        if (_assignedPassenger != null)
        {
            distToPassenger = Vector3.Distance(transform.position, _assignedPassenger.transform.position);

            if (_isPickingUp || distToPassenger <= pickupDistance)
            {
                targetSpeedMS = 0;
                _currentSpeedMS = 0; // 즉각 정차
                if (!_isPickingUp) StartCoroutine(PickupRoutine());
            }
        }

        // 3. 속도 적용 (가감속)
        if (!_isPickingUp && distToPassenger > pickupDistance)
        {
            float activeDecel = (_isStoppedBySignal || targetSpeedMS == 0) ? deceleration * 1.5f : deceleration;
            _currentSpeedMS = Mathf.MoveTowards(_currentSpeedMS, targetSpeedMS,
                (targetSpeedMS < _currentSpeedMS ? activeDecel : acceleration) * Time.fixedDeltaTime);
        }

        // 4. 이동 및 회전
        if (_currentSpeedMS > 0.001f)
        {
            _progress += (_currentSpeedMS * Time.fixedDeltaTime) / _cachedLength;
        }

        if (_progress >= 1f) { Destroy(gameObject); return; }

        UpdateTransform();

        // 5. 매칭 표시 (구체 생성)
        if (_assignedPassenger != null && !_isMatchedIndicated && distToPassenger <= matchDisplayDistance)
        {
            ShowMatchIndicator(_assignedPassenger.MyColor);
        }
    }

    // [핵심 추가] 앞차 감지 로직
    private float CalculateSafetySpeed(float desiredSpeed)
    {
        Vector3 start = transform.TransformPoint(sensorOffset);
        if (Physics.Raycast(start, transform.forward, out RaycastHit hit, sensorDist, vehicleLayer))
        {
            // 앞차와의 거리에 따라 속도 조절
            if (hit.distance <= safetyMargin) return 0;
            return desiredSpeed * Mathf.Clamp01((hit.distance - safetyMargin) / (sensorDist - safetyMargin));
        }
        return desiredSpeed;
    }

    // [핵심 추가] 신호등 연동 로직
    public void SetStopStatus(bool stop, TrafficLightUnit light = null)
    {
        _isStoppedBySignal = stop;
        if (stop && light != null)
        {
            if (_waitingLight != null) _waitingLight.OnSignalChanged -= HandleSignalChanged;
            _waitingLight = light;
            _waitingLight.OnSignalChanged += HandleSignalChanged;
        }
        else if (!stop) UnsubscribeSignal();
    }

    private void HandleSignalChanged(TrafficLightState s)
    {
        // 파란불이면 정지 해제
        if (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft)
        {
            _isStoppedBySignal = false;
            UnsubscribeSignal();
        }
    }

    private void UnsubscribeSignal()
    {
        if (_waitingLight != null)
        {
            _waitingLight.OnSignalChanged -= HandleSignalChanged;
            _waitingLight = null;
        }
    }

    private void UpdateTransform()
    {
        Vector3 pos = (Vector3)_currentSpline.EvaluatePosition(_progress);
        float3 tan = _currentSpline.EvaluateTangent(_progress);
        _rb.MovePosition(pos);
        if (math.lengthsq(tan) > 0.001f)
            _rb.MoveRotation(Quaternion.LookRotation((Vector3)tan));
    }

    private void ShowMatchIndicator(Color col)
    {
        _isMatchedIndicated = true;
        _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _indicator.transform.SetParent(transform);
        _indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        _indicator.transform.localScale = Vector3.one * 11f;
        Destroy(_indicator.GetComponent<Collider>());


        var ren = _indicator.GetComponent<MeshRenderer>();
        //ren.material.color = col;
        //ren.material.EnableKeyword("_EMISSION");
        //ren.material.SetColor("_EmissionColor", col * 2.5f);

        // 1. HDRP 전용 Unlit 셰이더 찾기
        Shader unlitShader = Shader.Find("HDRP/Unlit");

        if (unlitShader != null)
        {
            ren.material = new Material(unlitShader);

            // 2. Unlit 머티리얼의 색상 및 에미션 설정
            // HDRP Unlit은 _UnlitColor 또는 _BaseColor를 사용합니다.
            ren.material.SetColor("_UnlitColor", col);

            // 3. Emission 활성화 (필요한 경우)
            ren.material.EnableKeyword("_EMISSION");
            ren.material.SetColor("_EmissiveColor", col * 2.0f); // 강도를 높여 발광 효과 부여
        }
        else
        {
            Debug.LogWarning("HDRP/Unlit 셰이더를 찾을 수 없습니다. 기본 머티리얼을 사용합니다.");
            ren.material.color = col;
        }
    }

    private IEnumerator PickupRoutine()
    {
        _isPickingUp = true;
        yield return new WaitForSeconds(1.5f);
        if (_assignedPassenger != null) _assignedPassenger.OnPickedUp();
        if (_indicator != null) Destroy(_indicator);
        _assignedPassenger = null;
        _isPickingUp = false;
    }

    void OnDisable() => UnsubscribeSignal();
}

//using UnityEngine;
//using UnityEngine.Splines;
//using Unity.Mathematics;
//using System.Collections;

//public class Taxi_AutonomousController : MonoBehaviour
//{
//    private SplineContainer _currentSpline;
//    private float _progress;
//    private float _cachedLength;
//    private Rigidbody _rb;
//    private float _currentSpeedMS;

//    [Header("Specs")]
//    public float maxSpeedKmH = 50f;
//    public float acceleration = 5f;
//    public float deceleration = 40f;        // [조정] 제동력을 기존보다 2배 높임

//    [Header("Sensors")]
//    public float matchDisplayDistance = 15f;
//    public float pickupDistance = 3.0f;     // [조정] 정차 정밀도 향상을 위해 약간 줄임

//    private Passenger _assignedPassenger;
//    private GameObject _indicator;
//    private bool _isMatchedIndicated = false;
//    private bool _isPickingUp = false;

//    public void Initialize(SplineContainer spline)
//    {
//        _currentSpline = spline;
//        _cachedLength = spline.CalculateLength();
//        _rb = GetComponent<Rigidbody>();
//        _rb.interpolation = RigidbodyInterpolation.Interpolate;
//    }

//    public void AssignPassenger(Passenger passenger)
//    {
//        _assignedPassenger = passenger;
//        _isMatchedIndicated = false;
//        _isPickingUp = false;
//    }

//    void FixedUpdate()
//    {
//        if (!_currentSpline) return;

//        // 1. 거리 계산
//        float distToPassenger = float.MaxValue;
//        if (_assignedPassenger != null)
//        {
//            distToPassenger = Vector3.Distance(transform.position, _assignedPassenger.transform.position);
//        }

//        // 2. 속도 제어 로직 (긴급 제동 반영)
//        float targetSpeed = maxSpeedKmH / 3.6f;

//        if (_isPickingUp || distToPassenger <= pickupDistance)
//        {
//            targetSpeed = 0;
//            _currentSpeedMS = 0; // [핵심] 거리 안쪽이면 감속 공식 무시하고 즉시 0
//            if (!_isPickingUp) StartCoroutine(PickupRoutine());
//        }
//        else
//        {
//            // 부드러운 가감속
//            float activeDecel = deceleration;
//            // 승객에게 가까워질수록 더 강하게 제동 (선택 사항)
//            if (distToPassenger < 8f) activeDecel *= 1.5f;

//            _currentSpeedMS = Mathf.MoveTowards(_currentSpeedMS, targetSpeed,
//                (targetSpeed < _currentSpeedMS ? activeDecel : acceleration) * Time.fixedDeltaTime);
//        }

//        // 3. 진행도 업데이트 및 이동
//        if (_currentSpeedMS > 0.001f)
//        {
//            _progress += (_currentSpeedMS * Time.fixedDeltaTime) / _cachedLength;
//        }

//        if (_progress >= 1f) { Destroy(gameObject); return; }

//        UpdateTransform();

//        // 4. 매칭 표시 (구체 생성)
//        if (_assignedPassenger != null && !_isMatchedIndicated && distToPassenger <= matchDisplayDistance)
//        {
//            ShowMatchIndicator(_assignedPassenger.MyColor);
//        }
//    }

//    private void UpdateTransform()
//    {
//        Vector3 pos = (Vector3)_currentSpline.EvaluatePosition(_progress);
//        Vector3 tan = (Vector3)_currentSpline.EvaluateTangent(_progress);
//        _rb.MovePosition(pos);
//        if (tan != Vector3.zero) _rb.MoveRotation(Quaternion.LookRotation(tan));
//    }

//    private void ShowMatchIndicator(Color col)
//    {
//        _isMatchedIndicated = true;
//        _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//        _indicator.transform.SetParent(transform);
//        _indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
//        _indicator.transform.localScale = Vector3.one * 1.6f;

//        Destroy(_indicator.GetComponent<Collider>());
//        var ren = _indicator.GetComponent<MeshRenderer>();
//        ren.material.color = col;
//        ren.material.EnableKeyword("_EMISSION");
//        ren.material.SetColor("_EmissionColor", col * 2.5f);
//    }

//    private IEnumerator PickupRoutine()
//    {
//        _isPickingUp = true;

//        // 정차 후 대기
//        yield return new WaitForSeconds(1.5f);

//        // 매칭 오브젝트 제거
//        if (_assignedPassenger != null) _assignedPassenger.OnPickedUp();
//        if (_indicator != null) Destroy(_indicator);

//        _assignedPassenger = null;
//        _isPickingUp = false;
//    }
//}