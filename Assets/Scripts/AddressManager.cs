using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking; // UnityWebRequest 사용
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
    private string fetchedAddress = ""; // API로부터 받은 주소를 저장할 변수

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
        statusText.text = "위치 정보 권한을 확인 중입니다...";

#if UNITY_ANDROID
    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
    {
        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
        yield return new WaitForSeconds(1); // 사용자가 권한을 선택할 시간을 준다.
    }
#endif
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "위치 정보 서비스가 비활성화되어 있습니다.";
            yield break;
        }

        statusText.text = "위치 정보를 수신 중입니다...";
        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "위치 정보를 가져오는 데 실패했습니다.";
            yield break;
        }

        float latitude = Input.location.lastData.latitude;
        float longitude = Input.location.lastData.longitude;
        Input.location.Stop();

        statusText.text = "주소로 변환 중입니다...";

        // OpenStreetMap API를 사용하여 리버스 지오코딩 요청
        string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&accept-language=ko";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("User-Agent", "Unity-Game");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                // 간단한 JSON 파싱으로 주소(display_name)만 추출
                var data = JsonUtility.FromJson<GeocodingResponse>(jsonResponse);
                if (data != null && !string.IsNullOrEmpty(data.display_name))
                {
                    fetchedAddress = data.display_name;
                    adressDisplayText.text = fetchedAddress;
                    statusText.text = "";
                }
                else
                {
                    StartCoroutine(ShowTemporaryStatusMessage("주소 변환에 실패했습니다.", 2f));
                }

            }
            else
            {
                StartCoroutine(ShowTemporaryStatusMessage("주소 API 요청에 실패했습니다.", 2f));
            }
        }
    }

    private void OnSaveAddressButtonClicked()
    {
        if (string.IsNullOrEmpty(fetchedAddress))
        {
            StartCoroutine(ShowTemporaryStatusMessage("먼저 현재 위치로 주소를 찾아주세요.", 2f));
            return;
        }

        string detailAddress = detailAddressInput.text;
        string fullAddress = $"{fetchedAddress} {detailAddress}"; // 기본 주소와 상세 주소를 합침

        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            statusText.text = "주소를 저장 중...";
            dbReference.Child("users").Child(user.UserId).Child("address").SetValueAsync(fullAddress).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("주소 저장 성공!");
                    // 주소 저장이 메인 스레드에서 UI 변경을 하도록 스케줄링해야 할 수 있음
                    // 지금은 간단하게 다음 패널로 이동
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

    // 지정된 시간 후 메시지를 자동으로 지우는 코루틴
    private IEnumerator ShowTemporaryStatusMessage(string message, float delay)
    {
        // 1. 메시지를 statusText에 표시
        statusText.text = message;

        // 2. delay 변수에 지정된 시간(초)만큼 기다린다.
        yield return new WaitForSeconds(delay);

        // 3. 시간이 지나면 statusText를 비운다. 
        statusText.text = "";
    }

    [System.Serializable]
    private class GeocodingResponse
    {
        public string display_name;
    }
}
