using UnityEngine;
using UnityEngine.UI;

// 각 '추가/제외' 토글 UI에 붙여, 해당 토글이 어떤 데이터(AddonKey)에
// 연결되는지 알려주는 꼬리표 역할을 하는 스크립트
[RequireComponent(typeof(Toggle))] // 이 스크립트는 Toggle이 있는 곳에만 붙일 수 있다.
public class AddonToggleLinker : MonoBehaviour
{
    [Header("이 토글이 대표하는 항목의 키 (AddonKeys 클래스와 일치)")]
    public string addonKey;

    private Toggle _toggle; // 토글 컴포넌트를 저장할 비공개 변수

    // 이 스크립트의 Toggle 컴포넌트에 안전하게 접근
    public Toggle Toggle
    { 
        get
        {
            if (_toggle == null)
            {
                // 지금 이 순간 자신의 게임 오브젝트에서 GetComponent를 실행
                _toggle = GetComponent<Toggle>();
            }
            return _toggle;
        }
    }
}
