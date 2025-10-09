using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

// Firebase SDK 네임스페이스
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class LoginManager : MonoBehaviour
{
    // UI 요소들을 Inspector 창에서 연결
    [Header("UI Elements")]
    public TMP_InputField idInput;          // 아이디 입력창
    public TMP_InputField passwordInput;    // 비밀번호 입력창
    public Button loginButton;              // 로그인 버튼
    public TMP_Text statusText;             // 상태 메시지를 표시할 텍스트

    // Firebase 관련 변수
    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private void Start()
    {
        // Firebase 정보 가져오기 
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 로그인 버튼에 연결 함수
    public void OnLoginButtonClicked()
    {
        string userId = idInput.text;
        string password = passwordInput.text;

        // 입력값 유효성 검사
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "아이디와 비밀번호를 모두 입력해주세요.";
            return;
        }

        // Firebase에 보낼 가상 이메일 주소 생성
        string emailForFirebase = userId + "@mrdebak.app";

        // 로그인 처리 
        _ = LoginUserAsync(emailForFirebase, password);
    }

    // 실제 Firebase 로그인을 처리하는 비동기 함수
    private async Task LoginUserAsync(string email, string password)
    {
        try
        {
            // Firebase Authenication을 사용하여 이메일/비밀번호로 로그인 시도 
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = result.User;
            Debug.Log($"로그인 성공 : {user.Email} ({user.UserId})");

            // Realtime Database에서 사용자 역할(role) 정보 가져오기
            DataSnapshot snapshot = await dbReference.Child("users").Child(user.UserId).GetValueAsync();
            if (snapshot.Exists)
            {
                // DB에 저장된 데이터를 UserData 객체로 변환
                UserData userData = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                string role = userData.role;

                // 역할에 따라 다른 패널 보여주기 
                if (role == "Staff")
                {
                    UIManager.Instance.ShowPanel("StaffMainPanel");
                }
                else if (role == "Customer")
                {
                    UIManager.Instance.ShowPanel("CustomerMainPanel");
                }
                else
                {
                    statusText.text = "알 수 없는 사용자 역할입니다.";
                }
            }
            else
            {
                statusText.text = "사용자 데이터를 찾을 수 없습니다.";
            }
        }
        catch (FirebaseException ex)
        {
            // Friebase 관련 에러 처리 
            Debug.LogError($"Friebase 로그인 에러 : {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    statusText.text = "비밀번호가 틀렸습니다.";
                    break;
                case AuthError.UserNotFound:
                    statusText.text = "존재하지 않는 아이디입니다.";
                    break;
                default:
                    statusText.text = "로그인에 실패했습니다.";
                    break;
            }
        }
    }
}
