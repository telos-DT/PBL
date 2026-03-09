using UnityEngine;
using UnityEngine.Events;

public class LinearMover : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float duration = 2.0f;     // 이동에 소요될 시간 (초)
    [SerializeField] private Vector3 moveOffset = Vector3.forward * 5f; // 이동할 거리/방향

    [Header("애니메이션 설정")]
    [SerializeField] private Animator targetAnimator;   // 실행할 애니메이터
    [SerializeField] private string animationTriggerName = "OnArrived"; // 파라미터 이름

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _elapsedTime = 0f;
    private bool _isMoving = false;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        // 초기화 시점에 Animator가 없다면 스스로 찾아 할당 (방어적 프로그래밍)
        if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
    }

    // 트리거 센서에서 호출될 메서드
    public void StartMovement()
    {
        if (_isMoving) return; // 이미 이동 중이면 중복 실행 방지

        _startPosition = _transform.position;
        // 로컬 좌표계 기준으로 목표 지점 계산
        _targetPosition = _startPosition + _transform.TransformDirection(moveOffset);
        _elapsedTime = 0f;
        _isMoving = true;
    }

    private void Update()
    {
        if (!_isMoving) return;

        _elapsedTime += Time.deltaTime;
        float progress = _elapsedTime / duration; // 0에서 1까지의 진행률

        if (progress < 1.0f)
        {
            // 선형 보간 이동 (SmoothStep 등을 적용하여 가속/감속 추가 가능)
            _transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        }
        else
        {
            CompleteMovement();
        }
    }

    private void CompleteMovement()
    {
        _transform.position = _targetPosition;
        _isMoving = false;

        // 도착 후 애니메이션 실행
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(animationTriggerName);
            Debug.Log($"[Mover] 이동 완료 및 애니메이션 '{animationTriggerName}' 실행");
        }
    }
}