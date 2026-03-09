using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class NPCVehicleController : MonoBehaviour
{
    private SplineContainer _currentSpline;
    private float _progress;
    private TrafficSpawner _spawner;
    private string _myPoolName;
    private TrafficLightUnit _waitingLight;

    [Header("Specs")]
    public float maxSpeedKmH = 40f;
    public float acceleration = 5f;
    public float deceleration = 10f;

    [Header("Sensors")]
    public float sensorDist = 15f;
    public float safetyMargin = 5f;
    public LayerMask vehicleLayer;
    public Vector3 sensorOffset = new Vector3(0, 0.5f, 1.5f);

    private float _currentSpeedMS;
    private bool _isStopped;
    private Rigidbody _rb;

    void Awake() { _rb = GetComponent<Rigidbody>(); _rb.interpolation = RigidbodyInterpolation.Interpolate; }

    public void Initialize(SplineContainer spline, TrafficSpawner spawner, string poolName)
    {
        _currentSpline = spline; _spawner = spawner; _myPoolName = poolName;
        _progress = 0; _currentSpeedMS = 0; _isStopped = false;
    }

    public void SetStopStatus(bool stop, TrafficLightUnit light = null)
    {
        _isStopped = stop;
        if (stop && light != null)
        {
            if (_waitingLight != null) _waitingLight.OnSignalChanged -= HandleSignal;
            _waitingLight = light; _waitingLight.OnSignalChanged += HandleSignal;
        }
        else if (!stop) Unsubscribe();
    }

    private void HandleSignal(TrafficLightState s) { if (s == TrafficLightState.Green || s == TrafficLightState.GreenWithLeft) SetStopStatus(false); }
    private void Unsubscribe() { if (_waitingLight != null) { _waitingLight.OnSignalChanged -= HandleSignal; _waitingLight = null; } }

    void FixedUpdate()
    {
        if (!_currentSpline) return;
        float target = _isStopped ? 0 : CalculateSafetySpeed(maxSpeedKmH / 3.6f);
        _currentSpeedMS = Mathf.MoveTowards(_currentSpeedMS, target, (target < _currentSpeedMS ? deceleration : acceleration) * Time.fixedDeltaTime);

        if (_currentSpeedMS < 0.05f) return;
        _progress += (_currentSpeedMS * Time.fixedDeltaTime) / _currentSpline.CalculateLength();
        if (_progress >= 1f) { _spawner.ReturnToPool(this, _myPoolName); return; }

        _rb.MovePosition((Vector3)_currentSpline.EvaluatePosition(_progress));
        float3 tan = _currentSpline.EvaluateTangent(_progress);
        if (!tan.Equals(float3.zero)) _rb.MoveRotation(Quaternion.LookRotation((Vector3)tan));
    }

    private float CalculateSafetySpeed(float desired)
    {
        if (Physics.Raycast(transform.TransformPoint(sensorOffset), transform.forward, out RaycastHit hit, sensorDist, vehicleLayer))
            return hit.distance <= safetyMargin ? 0 : desired * Mathf.Clamp01((hit.distance - safetyMargin) / (sensorDist - safetyMargin));
        return desired;
    }
    void OnDisable() => Unsubscribe();
}