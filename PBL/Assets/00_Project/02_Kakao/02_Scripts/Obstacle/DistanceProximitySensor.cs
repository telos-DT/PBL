using UnityEngine;
using UnityEngine.Events;

public class DistanceProximitySensor : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("감지할 대상 오브젝트를 지정하세요.")]
    public Transform targetObject;

    [Tooltip("이 거리(미터) 안으로 들어오면 이벤트가 실행됩니다.")]
    public float detectionRange = 5.0f;

    [Tooltip("한 번만 실행할지, 근처에 갈 때마다 실행할지 설정합니다.")]
    public bool triggerOnce = true;

    [Header("Event")]
    public UnityEvent OnDetected;

    private bool _hasTriggered = false;

    void Update()
    {
        // 대상이 없거나 이미 실행되었으면 검사하지 않음
        if (targetObject == null || (_hasTriggered && triggerOnce)) return;

        // 두 물체 사이의 거리 계산 (제곱근 연산이 없는 sqrMagnitude가 성능에 더 좋음)
        float sqrDistance = (transform.position - targetObject.position).sqrMagnitude;
        float sqrRange = detectionRange * detectionRange;

        if (sqrDistance <= sqrRange)
        {
            if (!_hasTriggered)
            {
                ExecuteEvent();
            }
        }
        else
        {
            // 범위를 벗어나면 다시 트리거될 준비 (triggerOnce가 false일 때만 의미 있음)
            if (!triggerOnce) _hasTriggered = false;
        }
    }

    private void ExecuteEvent()
    {
        _hasTriggered = true;
        OnDetected?.Invoke();
        Debug.Log($"[Sensor] {targetObject.name} 근접 감지 성공!");
    }

    // 에디터 씬 뷰에서 범위를 시각적으로 표시 (기획/조정용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (targetObject != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetObject.position);
        }
    }
}