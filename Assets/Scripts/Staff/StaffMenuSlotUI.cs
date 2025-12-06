using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffMenuSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TMP_InputField priceInput;
    // PlaceholderText 변수는 이제 필요 없지만, 연결되어 있어도 상관없습니다.
    [SerializeField] private Button changeBtn;

    private string menuKey;
    private System.Action<string, int> onChangeCallback;

    public void Setup(string key, string displayName, int currentPrice, System.Action<string, int> onChange)
    {
        this.menuKey = key;
        this.onChangeCallback = onChange;

        // 1. 데이터 확인 로그 (콘솔창 확인용)
        // 만약 여기서 price가 0으로 찍히면, DB에서 데이터를 못 가져온 것입니다.
        Debug.Log($"[UI 설정] 메뉴: {displayName} / 키: {key} / 가격: {currentPrice}");

        // 2. 이름 설정
        if (nameText) nameText.text = displayName;

        // 3. InputField 및 Placeholder 설정
        if (priceInput)
        {
            // 텍스트를 비워야 Placeholder가 보입니다.
            priceInput.text = "";
            priceInput.contentType = TMP_InputField.ContentType.IntegerNumber;

            // ★ [핵심 수정] InputField가 알고 있는 Placeholder 컴포넌트를 직접 가져옵니다.
            // 인스펙터 연결 실수를 방지하는 가장 확실한 방법입니다.
            if (priceInput.placeholder != null)
            {
                TextMeshProUGUI placeholderTMP = priceInput.placeholder.GetComponent<TextMeshProUGUI>();
                if (placeholderTMP != null)
                {
                    placeholderTMP.text = $"{currentPrice:N0}"; // "10,000"

                    // 혹시 색상이 투명하거나 안 보일 수 있으니 강제로 잘 보이는 색 설정 (선택사항)
                    // placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 회색
                }
            }
        }

        // 4. 버튼 연결
        if (changeBtn)
        {
            changeBtn.onClick.RemoveAllListeners();
            changeBtn.onClick.AddListener(OnChangeClicked);
        }
    }

    private void OnChangeClicked()
    {
        if (priceInput != null && int.TryParse(priceInput.text, out int newPrice))
        {
            if (newPrice >= 0)
            {
                onChangeCallback?.Invoke(menuKey, newPrice);
                priceInput.text = ""; // 변경 후 비우기 -> 다시 Placeholder 보임

                // 변경된 가격을 즉시 Placeholder에 반영 (UX 향상)
                if (priceInput.placeholder != null)
                {
                    var ph = priceInput.placeholder.GetComponent<TextMeshProUGUI>();
                    if (ph) ph.text = $"{newPrice:N0}";
                }
            }
            else
            {
                Debug.LogWarning("0원 이상 입력해주세요.");
            }
        }
    }
}