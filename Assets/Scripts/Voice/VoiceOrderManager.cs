using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking; // API 통신용
using System;
using System.Text;
using System.Collections.Generic;

// 음성 주문 단계별 구현 - 3단계: AI 서버 통신 및 명령 실행
// STT로 변환된 텍스트를 로컬 FastAPI 서버로 보내고, AI의 답변과 명령을 처리 
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

    // 로컬 파이썬 서버 주소
    private string localServerUrl = "http://localhost:8000/chat";
    // 대화 맥락 유지를 위한 세션 ID
    private string currentSessionId = "";

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

        // 닫기 버튼 클릭 시 세션 초기화
        if (closeButton != null) closeButton.onClick.AddListener(() => {
            StopAllCoroutines();
            if (isRecording) Microphone.End(microphoneDevice);

            // 창을 닫으면 세션 ID를 지워버림 (대화 기억 삭제)
            ResetSession();
            UIManager.Instance.ShowPanel("CustomerMainPanel");
        });
    }

    private void OnEnable()
    {
        // 새로운 주문을 시작할 때마다 기억을 깨끗하게 비움
        ResetSession();

        // 패널이 열릴 때마다 상태 메시지 리셋
        if (Microphone.devices.Length > 0)
            statusText.text = "버튼을 눌러 주문을 말씀해주세요.\n(최대 15초)";
        else
            statusText.text = "마이크를 찾을 수 없습니다.";

        if (resultText != null) resultText.text = "";
        isRecording = false;
    }

    // 세션 초기화 함수 (기억 지우기)
    private void ResetSession()
    {
        currentSessionId = ""; // ID를 공란으로 만듦 -> 서버는 새로운 대화로 인식함
    }

    // 녹음 버튼 클릭 시 호출
    private void OnRecordButtonClicked()
    {
        if (!isRecording) StartRecording();
        else StopRecording();
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
        if (isRecording) StopRecording();
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        Microphone.End(microphoneDevice); // 녹음 하드웨어 중지
        statusText.text = "음성 인식 중...";  

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
                // JSON 파싱 (아래 정의된 GoogleSTTResponse 데이터 클래스 사용)
                // JsonUtility를 사용하여 JSON 문자열을 C# 객체로 변환
                GoogleSTTResponse sttResponse = JsonUtility.FromJson<GoogleSTTResponse>(response);

                // 변환된 객체에 유효한 결과가 있는지 확인
                if (sttResponse != null && sttResponse.results != null && sttResponse.results.Length > 0)
                {
                    // 가장 신뢰도 높은 첫 번째 인식 결과 추출
                    string transcript = sttResponse.results[0].alternatives[0].transcript;
                     
                    // 결과 텍스트를 UI에 표시
                    resultText.text = $"나: {transcript}";
                    statusText.text = "AI에게 전송 중...";

                    // 변환된 텍스트를 로컬 파이썬 서버로 전송!
                    StartCoroutine(SendToLocalServer(transcript));
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
                Debug.LogError($"STT Error: {request.error}");
                statusText.text = "음성 인식 실패";
            }
        }
    }

    // 로컬 파이썬 서버(FastAPI)로 텍스트 전송
    private IEnumerator SendToLocalServer(string userText)
    {
        // ---------------------------------------------------------------
        // [1단계] 보낼 데이터 포장하기 (Request 준비)
        // ---------------------------------------------------------------

        // ChatRequest 객체를 생성합니다. 이 구조는 파이썬 서버의 Pydantic 모델과 일치해야 한다. 
        ChatRequest chatReq = new ChatRequest
        {
            // session_id: 대화의 맥락(Context)을 유지하기 위한 핵심 키
            // 첫 요청엔 빈 값("")이지만, 두 번째부터는 서버가 준 ID를 넣어서 "아까 그 사람이야"라고 알려준다.
            session_id = currentSessionId,

            // user_input: 사용자가 방금 말한 내용 (예: "메뉴 추천해줘")
            user_input = userText
        };

        // C# 객체를 JSON 문자열로 변환
        // 예시 결과: {"session_id": "abc-123", "user_input": "메뉴 추천해줘"}
        string jsonBody = JsonUtility.ToJson(chatReq);

        // ---------------------------------------------------------------
        // [2단계] HTTP 요청 보내기 (Networking)
        // ---------------------------------------------------------------

        // POST 방식의 UnityWebRequest를 생성 (주소: http://localhost:8000/chat)
        using (UnityWebRequest request = new UnityWebRequest(localServerUrl, "POST"))
        {
            // 보낼 데이터(JSON 문자열)를 바이트 배열로 변환 (네트워크 전송을 위해)
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            // UploadHandler: 데이터를 서버로 밀어 넣는 담당자
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // DownloadHandler: 서버가 주는 답장을 받아오는 담당자
            request.downloadHandler = new DownloadHandlerBuffer();

            // [중요] 헤더 설정: "내가 보내는 데이터는 JSON 형식이니까 그렇게 해석해!"라고 서버에 알림
            request.SetRequestHeader("Content-Type", "application/json");

            // 요청을 전송하고, 응답이 올 때까지 여기서 코루틴이 대기(yield)합니다.
            yield return request.SendWebRequest();

            // ---------------------------------------------------------------
            // [3단계] 응답 처리하기 (Response Handling)
            // ---------------------------------------------------------------

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 성공! 서버가 보낸 응답 텍스트(JSON)를 꺼낸다.
                // 예시: {"session_id": "abc-123", "reply": "발렌타인 디너는 어떠세요?"}
                string responseJson = request.downloadHandler.text;
                Debug.Log($"Server Response: {responseJson}");

                // JSON 문자열을 다시 C# 객체(ChatResponse)로 변환(파싱)
                // 로컬에 정의된 ServerResponse 클래스 사용
                ServerResponse chatRes = JsonUtility.FromJson<ServerResponse>(responseJson);

                if (chatRes != null)
                {
                    // [매우 중요] 서버가 갱신해준 세션 ID를 저장
                    // 다음번 요청 때 이 ID를 다시 보내야 AI가 대화를 기억할 수 있다.
                    currentSessionId = chatRes.session_id;

                    // UI에 AI의 답변을 표시합니다.
                    resultText.text = $"AI: {chatRes.reply}";
                    statusText.text = "답변 완료!";

                    // [핵심 기능] AI 행동(Action) 감지 및 실행
                    // action이 있고, parameters 데이터가 존재할 경우 Bridge 호출
                    if (!string.IsNullOrEmpty(chatRes.action) && chatRes.parameters != null && chatRes.parameters.commands != null)
                    {
                        Debug.Log($"AI 행동 감지: {chatRes.action}");

                        // 파라미터 객체를 다시 JSON 문자열로 변환하여 Bridge에 전달
                        // (AICommandBridge는 JSON 문자열을 받아서 처리하도록 설계됨)
                        string paramJson = JsonUtility.ToJson(chatRes.parameters);
                        Debug.Log($"[Bridge 전송 JSON]: {paramJson}");

                        AICommandBridge.Instance.ProcessAICommand(chatRes.action, paramJson);
                    }
                }
            }
            else
            {
                // 실패! (서버가 꺼져있거나, 인터넷이 끊겼거나, 파이썬 코드 에러 등)
                Debug.LogError($"Server Error: {request.error}");

                // 사용자에게 에러 상황을 알린다.
                resultText.text = "서버 연결 실패. (server.py가 켜져 있나요?)";
                statusText.text = "AI 통신 오류";
            }
        }
    }

    // --- 데이터 클래스 (VoiceOrderManager 내부 전용) ---
    // AICommandBridge의 클래스를 쓰지 않고, 여기서 직접 정의해서 JsonUtility 오류를 방지합니다.

    [Serializable]
    public class GoogleSTTResponse { public STTResult[] results; }
    [Serializable]
    public class STTResult { public STTAlternative[] alternatives; }
    [Serializable]
    public class STTAlternative { public string transcript; public float confidence; }

    [Serializable]
    public class ChatRequest
    {
        public string session_id;
        public string user_input; 
    }

    // ★ 서버 응답 구조체 재정의 (중요)
    [Serializable]
    public class ServerResponse
    {
        public string session_id;
        public string reply;
        public string action;
        public ParameterData parameters; // 아래 정의된 클래스 사용
    }

    // ★ parameters 내부 구조체 (중요)
    // server.py가 {"commands": [...]} 형태로 보내므로 변수명 commands 일치 필수
    [Serializable]
    public class ParameterData
    {
        public List<CommandData> commands;
    }

    // ★ 개별 명령어 데이터
    [Serializable]
    public class CommandData
    {
        public string type;
        public string course;
        public string style;
        public List<string> add;
        public List<string> remove;
        public bool is_reservation;
        public string date;
    }
}
 