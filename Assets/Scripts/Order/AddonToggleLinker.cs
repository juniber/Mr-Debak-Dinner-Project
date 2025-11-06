using UnityEngine;
using UnityEngine.UI;

// 각 '추가/제외' 토글 UI에 붙여, 해당 토글이 어떤 데이터(AddonKey)에
// 연결되는지 알려주는 꼬리표 역할을 하는 스크립트
[RequireComponent(typeof(Toggle))] // 이 스크립트는 Toggle이 있는 곳에만 붙일 수 있다.
public class AddonToggleLinker : MonoBehaviour
{
    [Header("이 토글이 대표하는 항목의 키 (AddonKeys 클래스와 일치)")]
    public string addonKey;

    [HideInInspector] // Inspector에 노출할 필요 없는 public 변수
    public Toggle toggle;

    private void Awake()
    {
        // 자신의 Toggle 컴포넌트를 자동으로 찾아 변수에 할당
        toggle = GetComponent<Toggle>();
    }
}
