using Firebase.Auth; // 로그아웃/탈퇴용
using Firebase.Database;
using System.Threading.Tasks; // 비동기 처리용
using UnityEngine;
using UnityEngine.UI;

public class StaffSelfService : MonoBehaviour
{
    [Header("1. 매장 관리 버튼")]
    [SerializeField] private Button inventoryBtn;      // 재고관리
    [SerializeField] private Button menuSettingBtn;    // 메뉴설정

    [Header("2. 업무 화면 이동 버튼")]
    [SerializeField] private Button kitchenBtn;        // 주방 (조리 현황 등)
    [SerializeField] private Button deliveryBtn;       // 배달 (배달 현황 등)

    [Header("3. 계정 관리 버튼")]
    [SerializeField] private Button logoutBtn;         // 로그아웃
    [SerializeField] private Button deleteAccountBtn;  // 회원탈퇴

    [SerializeField] private GameObject menuPanel;
    private void Start()
    {
        // 버튼 리스너 연결
        if (inventoryBtn) inventoryBtn.onClick.AddListener(OnInventoryClicked);
        if (menuSettingBtn) menuSettingBtn.onClick.AddListener(OnMenuSettingClicked);
        if (kitchenBtn) kitchenBtn.onClick.AddListener(OnKitchenClicked);
        if (deliveryBtn) deliveryBtn.onClick.AddListener(OnDeliveryClicked);
        if (logoutBtn) logoutBtn.onClick.AddListener(OnLogoutClicked);
        if (deleteAccountBtn) deleteAccountBtn.onClick.AddListener(OnDeleteAccountClicked);
    }

    // --- 이벤트 핸들러 ---

    private void OnInventoryClicked()
    {
        // 아직 안 만드셨다면 패널 이름만 정해두세요 (예: SInventoryPanel)
        UIManager.Instance.ShowPanel("SInventoryPanel");
    }

    private void OnMenuSettingClicked()
    {
        UIManager.Instance.ShowPanel("SMenuSettingPanel");
    }

    private void OnKitchenClicked()
    {
        // 주방 화면이 따로 있다면 거기로, 없다면 주문 처리 화면으로
        UIManager.Instance.ShowPanel("SKitchenPanel");
    }

    private void OnDeliveryClicked()
    {
        // 배달 현황 화면으로 이동
        UIManager.Instance.ShowPanel("SDeliveryStatusPanel");
    }

    // [로그아웃]
    private void OnLogoutClicked()
    {
        Debug.Log("로그아웃 시도...");
        FirebaseAuth.DefaultInstance.SignOut(); // Firebase 로그아웃
        
        if(menuPanel.activeSelf)
            menuPanel.SetActive(false);

        // 로그인 화면으로 강제 이동
        UIManager.Instance.ShowPanel("LoginPanel");
        Debug.Log("로그아웃 완료. 로그인 화면으로 이동합니다.");
    }

    // [회원탈퇴]
    private void OnDeleteAccountClicked()
    {
        if (menuPanel.activeSelf)
            menuPanel.SetActive(false);

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            string uid = user.UserId;
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            dbRef.Child("users").Child(uid).RemoveValueAsync().ContinueWith(dbTask =>
            {
                if (dbTask.IsFaulted)
                {
                    Debug.LogError($"DB 삭제 실패 (권한 문제 등): {dbTask.Exception}");
                    return;
                }

                Debug.Log("DB 데이터 삭제 완료. 이제 인증 계정을 삭제합니다.");

                user.DeleteAsync().ContinueWith(authTask =>
                {
                    if (authTask.IsCanceled || authTask.IsFaulted)
                    {
                        Debug.LogError("계정 삭제 실패 (재로그인 필요할 수 있음)");
                        return;
                    }

                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        Debug.Log("직원 탈퇴 완료.");
                        UIManager.Instance.ShowPanel("LoginPanel");
                    });
                });
            });
        }
    }
}