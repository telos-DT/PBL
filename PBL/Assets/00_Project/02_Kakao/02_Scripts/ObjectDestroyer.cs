using UnityEngine;

public class ObjectDestroyer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetTag = "Vehicle"; // 삭제할 대상의 태그
    [SerializeField] private bool useTagOnly = true;      // 태그가 일치할 때만 삭제할지 여부
    [SerializeField] private GameObject explosionEffect;    // (선택) 삭제 시 출력할 이펙트

    // 1. 트리거 충돌 (Is Trigger가 체크된 Collider인 경우)
    private void OnTriggerEnter(Collider other)
    {
        TryDestroy(other.gameObject);
    }

    // 2. 물리 충돌 (일반적인 Collider인 경우)
    private void OnCollisionEnter(Collision collision)
    {
        TryDestroy(collision.gameObject);
    }

    private void TryDestroy(GameObject target)
    {
        // 태그 체크 (설정된 경우)
        if (useTagOnly && !target.CompareTag(targetTag))
        {
            return;
        }

        // 이펙트 생성 (설정된 경우)
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, target.transform.position, Quaternion.identity);
        }

        Debug.Log($"[ObjectDestroyer] {target.name} 오브젝트가 충돌하여 삭제되었습니다.");

        // 대상 오브젝트 삭제
        Destroy(target);
    }
}