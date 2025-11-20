using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

// 음성 주문 단계별 구현 - 1단계: 음성 녹음 및 재생 확인
public class VoiceOrderManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button recordButton; // 녹음 버튼
    public Button closeButton;  // 닫기 버튼
    public TMP_Text statusText; // 상태 메시지
    public TMP_Text resultText; // 녹음 결과 확인용 (용량 등 표시)

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
        statusText.text = "녹음 완료! 소리를 재생합니다.";

        // 녹음된 데이터 처리
        ProcessRecordedAudio();
    } 

    private void ProcessRecordedAudio()
    {
        if (recordedClip == null) return;

        // 1. (확인용) WAV 변환 테스트
        // WavUtility 스크립트가 프로젝트에 있어야 합니다.
        byte[] wavData = WavUtility.FromAudioClip(recordedClip);

        string logMsg = $"WAV 변환 성공! 크기: {wavData.Length} bytes";
        Debug.Log(logMsg);
        resultText.text = logMsg;

        // 2. (확인용) 녹음된 소리 즉시 재생
        // 사용자가 자신의 목소리를 들을 수 있어야 녹음이 잘 된 것입니다.
        audioSource.clip = recordedClip;
        audioSource.Play();

        // --- 다음 단계: 여기에 STT API 전송 코드가 들어갑니다 ---
        // _ = SendToGoogleSTT(wavData);
    }
}
