using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffMenuSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;       // 메뉴 이름
    [SerializeField] private TMP_InputField priceInput;      // 가격 입력 필드
    [SerializeField] private TextMeshProUGUI placeholderText;// ★ 여기에 Placeholder 텍스트 컴포넌트를 연결해야 함
    [SerializeField] private Button changeBtn;               // 변경 버튼

    private string menuKey;
    private System.Action<string, int> onChangeCallback;

    public void Setup(string key, string displayName, int currentPrice, System.Action<string, int> onChange)
    {
        this.menuKey = key;
        this.onChangeCallback = onChange;

        // 1. 이름 표시
        if (nameText) nameText.text = displayName;

        // 2. ★ [핵심] InputField 초기화 (비워야 Placeholder가 보임)
        if (priceInput)
        {
            priceInput.text = "";
            priceInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        // 3. ★ [핵심] Placeholder에 현재 가격 넣기
        string priceStr = $"{currentPrice:N0}"; // "10,000" 형식

        // A. 인스펙터에 연결된 변수 우선 사용
        if (placeholderText != null)
        {
            placeholderText.text = priceStr;
        }
        // B. 연결 안 되어 있으면 InputField 설정에서 찾기
        else if (priceInput != null && priceInput.placeholder != null)
        {
            var placeholderTMP = priceInput.placeholder as TextMeshProUGUI;
            if (placeholderTMP != null)
            {
                placeholderTMP.text = priceStr;
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
                priceInput.text = ""; // 변경 후 비우기
            }
            else
            {
                Debug.LogWarning("0원 이상 입력해주세요.");
            }
        }
    }
}