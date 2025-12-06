using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffInventoryItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;      // 예: 베이컨(g) :
    [SerializeField] private TextMeshProUGUI quantityText;  // 예: 3784
    [SerializeField] private TMP_InputField inputField;     // 숫자 입력
    [SerializeField] private Button addBtn;                 // 추가 버튼

    private string itemKey; // DB 키 (예: bacon_g)

    // 추가 버튼 눌렀을 때 실행할 콜백 (키, 추가할 양)
    private System.Action<string, int> onAddCallback;

    public void Setup(string key, long currentAmount, System.Action<string, int> onAdd)
    {
        this.itemKey = key;
        this.onAddCallback = onAdd;

        // 1. 이름 설정 (DB 키를 한글로 변환해서 표시)
        nameText.text = $"{GetKoreanName(key)} :";

        // 2. 현재 수량 표시 (쉼표 포맷)
        quantityText.text = $"{currentAmount:N0}";

        // 3. 입력창 초기화
        inputField.text = "";
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber; // 숫자만 입력 가능

        // 4. 버튼 리스너 연결
        addBtn.onClick.RemoveAllListeners();
        addBtn.onClick.AddListener(OnAddClicked);
    }

    private void OnAddClicked()
    {
        // 입력값 파싱 (비어있거나 0이면 무시)
        if (int.TryParse(inputField.text, out int amount) && amount > 0)
        {
            // 부모 패널에게 "이 아이템에 이만큼 추가해줘"라고 요청
            onAddCallback?.Invoke(itemKey, amount);
            inputField.text = ""; // 입력창 비우기
        }
        else
        {
            Debug.LogWarning("올바른 수량을 입력하세요.");
        }
    }

    // DB 키를 한글 이름으로 바꿔주는 헬퍼 함수
    private string GetKoreanName(string key)
    {
        switch (key)
        {
            case "steakMeat_g": return "스테이크용 고기(g)";
            case "miniCorn_pcs": return "미니콘(개)";
            case "potatoSalad_g": return "감자 샐러드(g)";
            case "saladGreens_g": return "샐러드 야채(g)";
            case "eggs_pcs": return "계란(개)";
            case "bacon_g": return "베이컨(g)";
            case "baguette_pcs": return "바게트(개)";
            case "wine_servings": return "와인(잔)";
            case "coffee_servings": return "커피(잔)";
            case "champagne_bottles": return "샴페인(병)";
            default: return key; // 등록 안 된 건 영어 그대로 표시
        }
    }
}