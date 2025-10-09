using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Firebase SDK ���ӽ����̽�
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;

public class RegisterManager : MonoBehaviour
{
    // UI ��ҵ��� Inspector â���� ����
    [Header("UI Elements")]
    public TMP_InputField idInput;          // ���̵� �Է�â
    public TMP_InputField passwordInput;    // ��й�ȣ �Է�â
    public Toggle staffToggle;              // '����' ���� ���� ���
    public Toggle customerToggle;           // '��' ���� ���� ���
    public Button registerButton;           // ȸ������ ��ư
    public TMP_Text statusText;             // ���� �޽����� ǥ���� �ؽ�Ʈ

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private void Start()
    {
        // Firebase ���� �������� 
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // ȸ������ ��ư�� ������ ���� �Լ� 
    public void OnRegisterButtonClicked()
    {
        // ����ڰ� �Է��� ���� ID
        string userId = idInput.text;

        // Firebase�� ���� ���� �̸��� �ּ� ����
        // ������ �κ��� ������Ʈ�� �°� �����Ӱ� ���� ����
        string emailForFirebase = userId + "@mrdebak.app";

        string password = passwordInput.text;
        string role = staffToggle.isOn ? "Staff" : "Customer"; // ��� ���ÿ� ���� ���� ����

        // �Է°� ��ȿ�� �˻�
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            statusText.text = "���̵�� ��й�ȣ�� �Է����ּ���.";
            return;
        }

        // ȸ������ ó�� ����
        statusText.text = "ȸ������ ���� ��...";
        _= RegisterUserAsync(emailForFirebase, password, role);
    }

    // ���� Firebase ȸ������ �� ������ ������ ó���ϴ� �񵿱� �Լ�
    private async Task RegisterUserAsync(string email, string password, string role)
    {
        try
        {
            // 1. Friebase Authentication�� ����Ͽ� �̸���/��й�ȣ�� ����� ����
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser newUser = result.User;
            Debug.Log($"����� ���� �Ϸ� : {newUser.Email} ({newUser.UserId})");

            // 2. Realtime Database ����� ����(Role) ���� ����
            UserData userData = new UserData(role);
            string json = JsonUtility.ToJson(userData);

            // ������ ������� ���� ID(UserId�� Ű(Key)�� ����Ͽ� �����͸� ����
            await dbReference.Child("users").Child(newUser.UserId).SetRawJsonValueAsync(json);
            Debug.Log("����� ���� ���� �����ͺ��̽��� ���� �Ϸ�.");

            statusText.text = "ȸ������ ����! ��� �� �α��� ȭ������ �̵��մϴ�.";

            // 2�� ���� �Լ��� ������ ��ٸ���.
            await Task.Delay(2000);
            // UIManager�� ���� �α��� �г��� Ų��.
            UIManager.Instance.ShowPanel("LoginPanel");
        }
        catch (FirebaseException ex)
        {
            // Firebase ���� ���� ó��
            Debug.LogError($"Friebase ȸ������ ���� : {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    statusText.text = "�̹� ��� ���� �̸����Դϴ�.";
                    break;
                case AuthError.WeakPassword:
                    statusText.text = "��й�ȣ�� 6�ڸ� �̻��̾�� �մϴ�.";
                    break;
                default:
                    statusText.text = "ȸ�����Կ� �����߽��ϴ�.";
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