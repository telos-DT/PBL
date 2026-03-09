using UnityEngine;
using UnityEngine.Splines;

public class SplineBranchTrigger : MonoBehaviour
{
    public SplineContainer[] branches;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Vehicle"))
        {
            if (other.TryGetComponent<Self_Driving_NPCVehicleController>(out var vehicle))
            {
                if (branches != null && branches.Length > 0)
                {
                    // 랜덤으로 다음 경로 선택
                    int randomIndex = Random.Range(0, branches.Length);
                    SplineContainer selectedBranch = branches[randomIndex];

                    // [수정] 속도를 유지하며 경로만 교체
                    vehicle.SwitchSpline(selectedBranch);
                }
            }
        }
    }
}