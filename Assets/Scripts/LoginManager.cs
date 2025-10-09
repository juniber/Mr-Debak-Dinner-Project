using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

// Firebase SDK ���ӽ����̽�
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class LoginManager : MonoBehaviour
{
    // UI ��ҵ��� Inspector â���� ����
    [Header("UI Elements")]
    public TMP_InputField idInput;          // ���̵� �Է�â
    public TMP_InputField passwordInput;    // ��й�ȣ �Է�â
    public Button loginButton;              // �α��� ��ư
    public TMP_Text statusText;             // ���� �޽����� ǥ���� �ؽ�Ʈ

    // Firebase ���� ����
    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private void Start()
    {
        // Firebase ���� �������� 
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // �α��� ��ư�� ���� �Լ�
    public void OnLoginButtonClicked()
    {
        string userId = idInput.text;
        string password = passwordInput.text;

        // �Է°� ��ȿ�� �˻�
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "���̵�� ��й�ȣ�� ��� �Է����ּ���.";
            return;
        }

        // Firebase�� ���� ���� �̸��� �ּ� ����
        string emailForFirebase = userId + "@mrdebak.app";

        // �α��� ó�� 
        _ = LoginUserAsync(emailForFirebase, password);
    }

    // ���� Firebase �α����� ó���ϴ� �񵿱� �Լ�
    private async Task LoginUserAsync(string email, string password)
    {
        try
        {
            // Firebase Authenication�� ����Ͽ� �̸���/��й�ȣ�� �α��� �õ� 
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = result.User;
            Debug.Log($"�α��� ���� : {user.Email} ({user.UserId})");

            // Realtime Database���� ����� ����(role) ���� ��������
            DataSnapshot snapshot = await dbReference.Child("users").Child(user.UserId).GetValueAsync();
            if (snapshot.Exists)
            {
                // DB�� ����� �����͸� UserData ��ü�� ��ȯ
                UserData userData = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                string role = userData.role;

                // ���ҿ� ���� �ٸ� �г� �����ֱ� 
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
                    statusText.text = "�� �� ���� ����� �����Դϴ�.";
                }
            }
            else
            {
                statusText.text = "����� �����͸� ã�� �� �����ϴ�.";
            }
        }
        catch (FirebaseException ex)
        {
            // Friebase ���� ���� ó�� 
            Debug.LogError($"Friebase �α��� ���� : {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    statusText.text = "��й�ȣ�� Ʋ�Ƚ��ϴ�.";
                    break;
                case AuthError.UserNotFound:
                    statusText.text = "�������� �ʴ� ���̵��Դϴ�.";
                    break;
                default:
                    statusText.text = "�α��ο� �����߽��ϴ�.";
                    break;
            }
        }
    }
}
