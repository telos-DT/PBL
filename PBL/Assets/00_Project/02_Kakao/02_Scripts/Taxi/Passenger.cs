using UnityEngine;

public class Passenger : MonoBehaviour
{
    public bool IsMatched { get; private set; }
    public Color MyColor { get; private set; }
    private GameObject _indicator;

    public void Initialize(Color color)
    {
        MyColor = color;
        IsMatched = true;

        _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _indicator.transform.SetParent(transform);
        _indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        _indicator.transform.localScale = Vector3.one * 10f; // 7f는 너무 클 수 있어 조절했습니다.

        var renderer = _indicator.GetComponent<MeshRenderer>();

        // 1. HDRP 전용 Unlit 셰이더 찾기
        Shader unlitShader = Shader.Find("HDRP/Unlit");

        if (unlitShader != null)
        {
            renderer.material = new Material(unlitShader);

            // 2. Unlit 머티리얼의 색상 및 에미션 설정
            // HDRP Unlit은 _UnlitColor 또는 _BaseColor를 사용합니다.
            renderer.material.SetColor("_UnlitColor", color);

            // 3. Emission 활성화 (필요한 경우)
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissiveColor", color * 2.0f); // 강도를 높여 발광 효과 부여
        }
        else
        {
            Debug.LogWarning("HDRP/Unlit 셰이더를 찾을 수 없습니다. 기본 머티리얼을 사용합니다.");
            renderer.material.color = color;
        }
    }

    public void OnPickedUp()
    {
        Destroy(gameObject);
    }
}

//using UnityEngine;

//public class Passenger : MonoBehaviour
//{
//    public bool IsMatched { get; private set; }
//    public Color MyColor { get; private set; }
//    private GameObject _indicator;

//    // 승객 초기화 및 시각적 구체(Indicator) 생성
//    public void Initialize(Color color)
//    {
//        MyColor = color;
//        IsMatched = true;

//        // 승객 머리 위에 매칭 표시 생성
//        _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//        _indicator.transform.SetParent(transform);
//        _indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
//        _indicator.transform.localScale = Vector3.one * 7f;

//        // 머티리얼 설정 (GC 최소화 위해 전용 메서드 사용 권장)
//        var renderer = _indicator.GetComponent<MeshRenderer>();
//        renderer.material.color = color;
//        renderer.material.EnableKeyword("_EMISSION");
//        renderer.material.SetColor("_EmissionColor", color * 0.5f);
//    }

//    public void OnPickedUp()
//    {
//        // 픽업 시 연출 후 제거
//        Destroy(gameObject);
//    }
//}