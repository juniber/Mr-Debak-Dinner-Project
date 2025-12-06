using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;

public class StaffWorkerManageUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image profileImage;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Action Buttons")]
    [SerializeField] private Button kitchenBtn;
    [SerializeField] private Button deliveryBtn;
    [SerializeField] private Button fireBtn;
    [SerializeField] private Button backspaceBtn;

    private StaffData currentStaff;
    private DatabaseReference dbReference;
    private string myRole = "";

    private void Awake()
    {
        kitchenBtn.onClick.AddListener(() => OnChangeJobClicked("Kitchen"));
        deliveryBtn.onClick.AddListener(() => OnChangeJobClicked("Delivery"));
        fireBtn.onClick.AddListener(OnFireClicked);
        backspaceBtn.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void Open(StaffData staff)
    {
        currentStaff = staff;
        // 일단 버튼 잠그기 (권한 확인 전까지)
        SetButtonsInteractable(false);
        UpdateUI();
        CheckMyPermission(); // 내 권한 확인
        gameObject.SetActive(true);
    }

    private void CheckMyPermission()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null) return;

        // 내 역할(Role) 확인
        dbReference.Child("users").Child(user.UserId).Child("role").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                myRole = task.Result.Value.ToString();

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // Master만 버튼 활성화
                    if (myRole == "Master")
                    {
                        SetButtonsInteractable(true);
                        Debug.Log("Master 권한 확인됨.");
                    }
                    else
                    {
                        Debug.Log($"권한 부족 ({myRole}). 관리 기능 제한.");
                    }
                });
            }
        });
    }

    private void OnChangeJobClicked(string newJobType)
    {
        if (currentStaff == null) return;

        Debug.Log($" 직무 변경 시도: {newJobType}...");

        // users/{targetID}/jobType 경로 업데이트
        dbReference.Child("users").Child(currentStaff.id).Child("jobType").SetValueAsync(newJobType)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($" 직무 변경 실패: {task.Exception}");
                    // 여기서 Permission Denied가 뜨면 Rules 문제임
                    return;
                }

                if (task.IsCompleted)
                {
                    Debug.Log($" 직무 변경 성공: {newJobType}");

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        // UI 즉시 반영
                        currentStaff.jobType = newJobType;
                        UpdateUI();
                    });
                }
            });
    }

    private void OnFireClicked()
    {
        if (currentStaff == null) return;
        Debug.Log($" 해고 절차 시작: {currentStaff.name}");

        // DB 삭제
        dbReference.Child("users").Child(currentStaff.id).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => ClosePanel());
            }
            else
            {
                Debug.LogError($" 해고 실패: {task.Exception}");
            }
        });
    }

    private void SetButtonsInteractable(bool interactable)
    {
        kitchenBtn.interactable = interactable;
        deliveryBtn.interactable = interactable;
        fireBtn.interactable = interactable;
    }

    private void UpdateUI()
    {
        if (currentStaff == null) return;
        string jobKor = (currentStaff.jobType == "Kitchen") ? "주방 담당" :
                        (currentStaff.jobType == "Delivery") ? "배달 담당" : "미정";

        infoText.text = $"이름: {currentStaff.name}\n" +
                        $"전화번호: {currentStaff.phone}\n\n" +
                        $"현재 직무: <color=yellow>{jobKor}</color>";
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        UIManager.Instance.ShowPanel("SWorkingStatusPanel");
    }
}