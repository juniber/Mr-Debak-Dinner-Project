using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Firebase SDK 네임스페이스
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;

public class RegisterManager : MonoBehaviour
{
    // UI 요소들을 Inspector 창에서 연결
    [Header("UI Elements")]
    public TMP_InputField idInput;          // 아이디 입력창
    public TMP_InputField passwordInput;    // 비밀번호 입력창
    public Toggle staffToggle;              // '직원' 역할 선택 토글
    public Toggle customerToggle;           // '고객' 역할 선택 토글
    public Button registerButton;           // 회원가입 버튼
    public TMP_Text statusText;             // 상태 메시지를 표시할 텍스트

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private void Start()
    {
        // Firebase 정보 가져오기 
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 회원가입 버튼에 연결할 공개 함수 
    public void OnRegisterButtonClicked()
    {
        // 사용자가 입력한 순수 ID
        string userId = idInput.text;

        // Firebase에 보낼 가상 이메일 주소 생성
        // 도메인 부분은 프로젝트에 맞게 자유롭게 설정 가능
        string emailForFirebase = userId + "@mrdebak.app";

        string password = passwordInput.text;
        string role = staffToggle.isOn ? "Staff" : "Customer"; // 토글 선택에 따라 역할 결정

        // 입력값 유효성 검사
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "아이디와 비밀번호를 입력해주세요.";
            return;
        }

        // 회원가입 처리 시작
        statusText.text = "회원가입 진행 중...";
        _= RegisterUserAsync(emailForFirebase, password, role);
    }

    // 실제 Firebase 회원가입 및 데이터 저장을 처리하는 비동기 함수
    private async Task RegisterUserAsync(string email, string password, string role)
    {
        try
        {
            // 1. Friebase Authentication을 사용하여 이메일/비밀번호로 사용자 생성
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = result.User;
            Debug.Log($"사용자 생성 완료 : {newUser.Email} ({newUser.UserId})");

            // 2. Realtime Database 사용자 역할(Role) 정보 저장
            UserData userData = new UserData(role);
            string json = JsonUtility.ToJson(userData);

            // 생성된 사용자의 고유 ID(UserId를 키(Key)로 사용하여 데이터를 저장
            await dbReference.Child("users").Child(newUser.UserId).SetRawJsonValueAsync(json);
            Debug.Log("사용자 역할 정보 데이터베이스에 저장 완료.");

            statusText.text = "회원가입 성공! 잠시 후 로그인 화면으로 이동합니다.";

            // 2초 동안 함수의 실행을 기다린다.
            await Task.Delay(2000);
            // UIManager를 통해 로그인 패널을 킨다.
            UIManager.Instance.ShowPanel("LoginPanel");
        }
        catch (FirebaseException ex)
        {
            // Firebase 관련 에러 처리
            Debug.LogError($"Friebase 회원가입 에러 : {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    statusText.text = "이미 사용 중인 이메일입니다.";
                    break;
                case AuthError.WeakPassword:
                    statusText.text = "비밀번호는 6자리 이상이어야 합니다.";
                    break;
                default:
                    statusText.text = "회원가입에 실패했습니다.";
                    break;
            }
        }
    }
}

[System.Serializable]
public class UserData
{
    public string role;

    public UserData(string role)
    {
        this.role = role;
    }
}