using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using System.Threading.Tasks;

public class StaffStoreManageUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button restartBtn;      // 영업 재개 (Restart)
    [SerializeField] private Button pauseBtn;        // 영업 일시정지 (Pause)
    [SerializeField] private Button breakTimeBtn;    // n분간 (Reservation) - 휴게시간 설정
    [SerializeField] private Button setTimeBtn;      // 영업시간 설정 (Option)
    [SerializeField] private Button confirmBtn;      // 확인 (Confirm) - 저장
    [SerializeField] private Button backspaceBtn;    // 뒤로가기

    [Header("Display Texts")]
    [SerializeField] private TextMeshProUGUI timeDisplayInfo; // 버튼 안의 "09:00 - 20:00" 텍스트

    [Header("Popup")]
    [SerializeField] private StaffTimeSettingPopup timePopup;
    [SerializeField] private SBreakTimePopup breakPopup;

    // 내부 상태 관리용 변수
    private bool currentIsOpen = false;
    private string currentOpenTime = "09:00";
    private string currentCloseTime = "22:00";

    // Firebase 참조
    private DatabaseReference dbReference;

    private void Awake()
    {
        // 버튼 리스너 연결
        restartBtn.onClick.AddListener(() => SetOpenStatus(true));
        pauseBtn.onClick.AddListener(() => SetOpenStatus(false));

        // "n분간" 버튼과 "영업시간" 버튼은 팝업 기능이 필요하므로 일단 로그로 대체합니다.
        breakTimeBtn.onClick.AddListener(OnBreakTimeClicked);
        setTimeBtn.onClick.AddListener(OnSetTimeClicked);

        confirmBtn.onClick.AddListener(OnConfirmClicked);
        backspaceBtn.onClick.AddListener(OnBackClicked);
    }

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void OnEnable()
    {
        // 패널이 열릴 때마다 Firebase에서 최신 정보 가져오기
        LoadStoreInfo();
    }

    // --- 1. 데이터 불러오기 ---
    private async void LoadStoreInfo()
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        var snapshot = await dbReference.Child("store_info").GetValueAsync();

        if (snapshot.Exists)
        {
            // 데이터 파싱 (안전한 도우미 함수 사용 권장)
            // 여기서는 간단히 작성합니다. (Helper 함수가 같은 클래스에 없으면 추가 필요)
            currentIsOpen = bool.Parse(snapshot.Child("isOpen").Value.ToString());
            currentOpenTime = snapshot.Child("openTime").Value.ToString();
            currentCloseTime = snapshot.Child("closeTime").Value.ToString();

            // UI 갱신
            UpdateUI();
        }
    }

    // --- 2. 내부 로직 (상태 변경) ---
    private void SetOpenStatus(bool isOpen)
    {
        currentIsOpen = isOpen;
        UpdateUI(); // 선택된 상태를 시각적으로 보여줌
    }

    private void UpdateUI()
    {
        // 영업시간 텍스트 갱신
        timeDisplayInfo.text = $"영업시간\n{currentOpenTime} - {currentCloseTime}";

        // 버튼 색상 변경 등으로 현재 상태 표시 (선택사항)
        // 예: 영업중이면 '재개' 버튼을 초록색으로, '정지' 버튼을 회색으로
        Color activeColor = new Color(0.3f, 0.8f, 0.3f); // 연두색
        Color inactiveColor = Color.white;

        restartBtn.image.color = currentIsOpen ? activeColor : inactiveColor;
        pauseBtn.image.color = !currentIsOpen ? activeColor : inactiveColor;
    }

    // --- 3. 버튼 이벤트 핸들러 ---

    private void OnBreakTimeClicked()
    {
        // 팝업 열기
        breakPopup.Open((minutes) =>
        {
            // 1. 현재 시간에서 n분을 더함
            System.DateTime targetTime = System.DateTime.Now.AddMinutes(minutes);

            // 2. "HH:mm" 포맷으로 변환 (예: 14:30)
            string breakEndString = targetTime.ToString("HH:mm");

            Debug.Log($"{minutes}분 휴식 설정. 종료 예정: {breakEndString}");

            // 3. 즉시 Firebase에 저장 (휴식은 바로 적용되어야 하므로)
            SaveBreakStatusToFirebase(breakEndString);
        });
    }

    private void OnSetTimeClicked()
    {
        // 팝업 열기 (현재 시간 전달 + 콜백 함수 등록)
        timePopup.Open(currentOpenTime, currentCloseTime, (newOpen, newClose) =>
        {
            // 팝업에서 [확인]을 누르면 이 코드가 실행됩니다.
            currentOpenTime = newOpen;
            currentCloseTime = newClose;

            // UI 갱신 (저장은 아직 안 함, Confirm 버튼 눌러야 저장됨)
            UpdateUI();
            Debug.Log($"시간 변경됨: {currentOpenTime} ~ {currentCloseTime}");
        });
    }

    private void OnConfirmClicked()
    {
        // 변경된 내용을 Firebase에 저장
        SaveToFirebase();
    }

    private void OnBackClicked()
    {
        // 저장하지 않고 닫기 (UIManager 이용)
        // 이전 화면(StaffMainPanel)으로 돌아가야 함
        UIManager.Instance.ShowPanel("StaffMainPanel");
    }

    // --- 4. Firebase 저장 ---
    private void SaveToFirebase()
    {
        // 여러 값을 한 번에 업데이트 (Dictionary 사용)
        var updates = new System.Collections.Generic.Dictionary<string, object>();
        updates["store_info/isOpen"] = currentIsOpen;
        updates["store_info/openTime"] = currentOpenTime;
        updates["store_info/closeTime"] = currentCloseTime;

        dbReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("영업 정보 설정 완료!");

                // 성공 후 메인 화면으로 이동 (Main Thread에서 실행)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    UIManager.Instance.ShowPanel("StaffMainPanel");
                });
            }
            else
            {
                Debug.LogError($"저장 실패: {task.Exception}");
            }
        });
    }

    private void SaveBreakStatusToFirebase(string endTimeStr)
    {
        var updates = new System.Collections.Generic.Dictionary<string, object>();

        updates["store_info/isOpen"] = false;          // 영업 끔
        updates["store_info/breakEndTime"] = endTimeStr; // 종료 시간 기록
        // 필요하다면 상태 메시지도 변경
        // updates["store_info/statusMessage"] = $"잠시 휴식중입니다. ({endTimeStr} 오픈 예정)";

        dbReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("휴식 설정 완료!");
                // UI 갱신을 위해 메인 스레드에서 실행
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // 로컬 변수도 갱신해주면 좋음
                    currentIsOpen = false;
                    UpdateUI(); // "영업종료" 등으로 텍스트 바뀜

                    // (선택) 팝업 띄워서 알려주기
                    // UIManager.Instance.ShowTemporaryStatus("휴식 설정이 완료되었습니다.", 2f);
                });
            }
        });
    }
}