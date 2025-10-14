using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking; // UnityWebRequest ���
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.UI;

public class AddressManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text adressDisplayText;
    public TMP_InputField detailAddressInput;
    public Button getAddressButton;
    public Button saveAddressButton;
    public Button backButton;
    public TMP_Text statusText;

    private FirebaseAuth auth;
    private DatabaseReference dbReference;
    private string fetchedAddress = ""; // API�κ��� ���� �ּҸ� ������ ����

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        getAddressButton.onClick.AddListener(OnGetAddressButtonClicked);
        saveAddressButton.onClick.AddListener(OnSaveAddressButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnGetAddressButtonClicked()
    {
        StartCoroutine(GetLocationAndAddress());
    }

    private IEnumerator GetLocationAndAddress()
    {
        statusText.text = "��ġ ���� ������ Ȯ�� ���Դϴ�...";

#if UNITY_ANDROID
    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
    {
        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
        yield return new WaitForSeconds(1); // ����ڰ� ������ ������ �ð��� �ش�.
    }
#endif
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "��ġ ���� ���񽺰� ��Ȱ��ȭ�Ǿ� �ֽ��ϴ�.";
            yield break;
        }

        statusText.text = "��ġ ������ ���� ���Դϴ�...";
        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "��ġ ������ �������� �� �����߽��ϴ�.";
            yield break;
        }

        float latitude = Input.location.lastData.latitude;
        float longitude = Input.location.lastData.longitude;
        Input.location.Stop();

        statusText.text = "�ּҷ� ��ȯ ���Դϴ�...";

        // OpenStreetMap API�� ����Ͽ� ������ �����ڵ� ��û
        string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&accept-language=ko";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("User-Agent", "Unity-Game");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                // ������ JSON �Ľ����� �ּ�(display_name)�� ����
                var data = JsonUtility.FromJson<GeocodingResponse>(jsonResponse);
                if (data != null && !string.IsNullOrEmpty(data.display_name))
                {
                    fetchedAddress = data.display_name;
                    adressDisplayText.text = fetchedAddress;
                    statusText.text = "";
                }
                else
                {
                    StartCoroutine(ShowTemporaryStatusMessage("�ּ� ��ȯ�� �����߽��ϴ�.", 2f));
                }

            }
            else
            {
                StartCoroutine(ShowTemporaryStatusMessage("�ּ� API ��û�� �����߽��ϴ�.", 2f));
            }
        }
    }

    private void OnSaveAddressButtonClicked()
    {
        if (string.IsNullOrEmpty(fetchedAddress))
        {
            StartCoroutine(ShowTemporaryStatusMessage("���� ���� ��ġ�� �ּҸ� ã���ּ���.", 2f));
            return;
        }

        string detailAddress = detailAddressInput.text;
        string fullAddress = $"{fetchedAddress} {detailAddress}"; // �⺻ �ּҿ� �� �ּҸ� ��ħ

        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            statusText.text = "�ּҸ� ���� ��...";
            dbReference.Child("users").Child(user.UserId).Child("address").SetValueAsync(fullAddress).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("�ּ� ���� ����!");
                    // �ּ� ������ ���� �����忡�� UI ������ �ϵ��� �����ٸ��ؾ� �� �� ����
                    // ������ �����ϰ� ���� �гη� �̵�
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        UIManager.Instance.ShowPanel("CustomerMainPanel");
                    });
                }
            });
        }
    }

    private void OnBackButtonClicked()
    {
        UIManager.Instance.ShowPanel("CustomerMainPanel");
    }

    // ������ �ð� �� �޽����� �ڵ����� ����� �ڷ�ƾ
    private IEnumerator ShowTemporaryStatusMessage(string message, float delay)
    {
        // 1. �޽����� statusText�� ǥ��
        statusText.text = message;

        // 2. delay ������ ������ �ð�(��)��ŭ ��ٸ���.
        yield return new WaitForSeconds(delay);

        // 3. �ð��� ������ statusText�� ����. 
        statusText.text = "";
    }

    [System.Serializable]
    private class GeocodingResponse
    {
        public string display_name;
    }
}
