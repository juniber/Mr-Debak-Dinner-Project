using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking; // API 통신용
using System;
using System.Text;

// 음성 주문 단계별 구현 - 2단계: STT (Speech-to-Text) 연동
// 녹음된 음성을 Google Cloud STT API로 전송하고 텍스트 결과를 받는다. 
public class VoiceOrderManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button recordButton; // 녹음 버튼
    public Button closeButton;  // 닫기 버튼
    public TMP_Text statusText; // 상태 메시지
    public TMP_Text resultText; // 변환된 텍스트 표시

    [Header("API Settings")]
    // ★ 중요: Google Cloud Console에서 발급받은 API 키를 인스펙터 창에서 입력
    public string googleApiKey = "";

    // 오디오 관련 변수
    private AudioSource audioSource;
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private float maxRecordingTime = 15.0f; // 최대 녹음 시간 5초

    private void Awake()
    {
        // 1. AudioSource 컴포넌트 설정 (녹음된 소리 확인용)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 2. 마이크 장치 확인
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"[VoiceOrderManager] 사용할 마이크: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("마이크 장치가 없습니다!");
            // 마이크가 없으면 버튼을 아예 비활성화
            if (recordButton != null) recordButton.interactable = false;
        }

        // 3. 버튼 리스너 연결
        if (recordButton != null) recordButton.onClick.AddListener(OnRecordButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(() => {
            StopAllCoroutines();
            if (isRecording) Microphone.End(microphoneDevice);
            UIManager.Instance.ShowPanel("CustomerMainPanel");
        });
    }

    private void OnEnable()
    { 
        // 패널이 열릴 때마다 상태 메시지 리셋
        if (Microphone.devices.Length > 0)
        {
            statusText.text = "버튼을 눌러 주문을 말씀해주세요.\n(최대 15초)";
        }
        else
        {
            statusText.text = "마이크를 찾을 수 없습니다.";
        }

        if (resultText != null) resultText.text = "";
        isRecording = false;
    }

    // 녹음 버튼 클릭 시 호출
    private void OnRecordButtonClicked()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording(); // 사용자가 15초 전에 버튼을 눌러 수동으로 멈출 때
        }
    }

    private void StartRecording()
    {
        if (isRecording) return;

        isRecording = true;
        statusText.text = "듣고 있어요... (말씀하세요)";
        resultText.text = "녹음 중...";

        // 녹음 시작 (최대 5초, 44100Hz)
        // 중요: loop를 false로 해도 Unity 마이크는 시간이 지나면 녹음을 멈추지 않고 덮어쓴다.
        // 따라서 코루틴으로 시간을 재서 수동으로 멈춰줘야 한다.
        recordedClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingTime, 44100);

        // 최대 시간이 지나면 자동으로 멈추도록 코루틴 시작
        StartCoroutine(StopRecordingAfterTime(maxRecordingTime));
    }

    // 최대 시간이 지나면 자동으로 녹음 중지
    private IEnumerator StopRecordingAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        // 시간이 다 되었는데도 아직 녹음 중이라면 자동 중지
        if (isRecording)
        {
            StopRecording();
        }
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        Microphone.End(microphoneDevice); // 녹음 하드웨어 중지
        statusText.text = "녹음 완료. 변환 준비 중...";  

        // 녹음된 데이터 처리
        ProcessRecordedAudio();
    } 

    private void ProcessRecordedAudio()
    {
        if (recordedClip == null) return;

        // 1. WAV 변환 (WavUtility 필요)
        byte[] wavData = WavUtility.FromAudioClip(recordedClip);

        // 2. Base64 인코딩 (API 전송을 위해 필수)
        string base64Audio = Convert.ToBase64String(wavData);

        // 3. Google STT API 호출
        StartCoroutine(SendToGoogleSTT(base64Audio));
    }

    // Google Cloud STT API 호출
    private IEnumerator SendToGoogleSTT(string base64Audio)
    {
        // 1. API 엔드포인트 설정
        // Google Cloud STT API의 v1 'recognize' 메서드를 호출
        // API 키를 쿼리 파라미터로 전달하여 인증
        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}";

        // 2. JSON 요청 본문 생성
        // API에 보낼 데이터(설정 정보 및 오디오 데이터)를 JSON 형식으로 만든다.
        // - config: 오디오 파일의 형식과 언어 설정
        //   - encoding: 오디오 인코딩 방식. LINEAR16은 WAV 파일의 기본 포맷
        //   - sampleRateHertz: 오디오의 샘플링 레이트. Unity 녹음 기본값인 44100Hz를 사용
        //   - languageCode: 인식할 언어 코드. 한국어는 'ko-KR'
        //   - model: 사용할 인식 모델. 'default'는 일반적인 음성 인식에 사용
        // - audio: 변환할 오디오 데이터
        //   - content: Base64 문자열로 인코딩된 오디오 데이터 자체
        string jsonBody = $@"{{
            ""config"": {{
                ""encoding"": ""LINEAR16"",
                ""sampleRateHertz"": 44100,
                ""languageCode"": ""ko-KR"",
                ""model"": ""default""
            }},
            ""audio"": {{
                ""content"": ""{base64Audio}""
            }}
        }}";

        // 3. UnityWebRequest 생성 및 설정
        // POST 방식으로 HTTP 요청을 생성
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // JSON 문자열을 바이트 배열로 변환하여 업로드 핸들러에 설정
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // 서버로부터 받을 응답 데이터를 처리할 다운로드 핸들러를 설정
            request.downloadHandler = new DownloadHandlerBuffer();

            // HTTP 헤더에 전송할 데이터 타입이 JSON임을 명시
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. 요청 전송 및 대기
            // 비동기적으로 요청을 보내고, 응답이 올 때까지 기다린다.
            yield return request.SendWebRequest();

            // 5. 응답 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 요청 성공 시, 서버가 보낸 응답 텍스트(JSON)를 가져온다. 
                string response = request.downloadHandler.text;
                Debug.Log($"STT Response: {response}");

                // JSON 파싱 (아래 정의된 GoogleSTTResponse 데이터 클래스 사용)
                // JsonUtility를 사용하여 JSON 문자열을 C# 객체로 변환
                GoogleSTTResponse sttResponse = JsonUtility.FromJson<GoogleSTTResponse>(response);

                // 변환된 객체에 유효한 결과가 있는지 확인
                if (sttResponse != null && sttResponse.results != null && sttResponse.results.Length > 0)
                {
                    // 가장 신뢰도 높은 첫 번째 인식 결과 추출
                    string transcript = sttResponse.results[0].alternatives[0].transcript;
                     
                    // 결과 텍스트를 UI에 표시
                    resultText.text = $"\"{transcript}\"";
                    statusText.text = "인식 성공!";
                }
                else
                {
                    // 결과가 없으면 인식 실패 메시지를 표시
                    resultText.text = "";
                    statusText.text = "인식된 내용이 없습니다. 다시 말씀해 주세요.";
                }
            }
            else
            {
                // 요청 실패 시 (네트워크 오류, API 키 오류 등)
                // 에러 메시지와 상세 응답 내용을 콘솔에 출력
                Debug.LogError($"STT API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                statusText.text = "음성 인식 실패 (API 오류)";
                resultText.text = $"Error: {request.error}";
            }
        }
    }

    // --- JSON 데이터 클래스 (구글 응답용) ---
    [Serializable]
    public class GoogleSTTResponse
    {
        public STTResult[] results;
    }

    [Serializable]
    public class STTResult
    {
        public STTAlternative[] alternatives;
    }

    [Serializable]
    public class STTAlternative
    {
        public string transcript; // 인식된 텍스트
        public float confidence;  // 신뢰도
    }
}
