using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class DynamicScroll : MonoBehaviour
{
    [Header("설정값")]
    public float minHeight = 200f; // 최소 높이
    public float maxHeight = 800f; // 최대 높이 (이걸 넘으면 스크롤 생김)

    private RectTransform myRect;
    private RectTransform contentRect;
    private LayoutElement myLayoutElement;

    void Start()
    {
        myRect = GetComponent<RectTransform>();
        ScrollRect scroll = GetComponent<ScrollRect>();
        contentRect = scroll.content;

        // LayoutElement가 없으면 자동으로 추가해줍니다.
        myLayoutElement = GetComponent<LayoutElement>();
        if (myLayoutElement == null)
        {
            myLayoutElement = gameObject.AddComponent<LayoutElement>();
        }
    }

    void Update()
    {
        if (contentRect == null) return;

        // 1. 알맹이(Content)가 얼마나 커지고 싶은지 계산합니다.
        // (Content Size Fitter에 의해 계산된 높이를 가져옵니다)
        float contentHeight = LayoutUtility.GetPreferredHeight(contentRect);

        // 2. 최소~최대 사이로 제한을 겁니다.
        float targetHeight = Mathf.Clamp(contentHeight, minHeight, maxHeight);

        // 3. 내 몸집(LayoutElement)을 그 크기로 맞춥니다.
        myLayoutElement.preferredHeight = targetHeight;

        // 4. (중요) LayoutElement를 안 쓰는 상황일 수도 있으니 SizeDelta도 같이 바꿉니다.
        // 이렇게 하면 부모가 LayoutGroup이든 아니든 무조건 작동합니다.
        Vector2 size = myRect.sizeDelta;
        size.y = targetHeight;
        myRect.sizeDelta = size;
    }
}