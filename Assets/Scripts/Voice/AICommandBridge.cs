using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

// AI의 명령(JSON)을 해석하여 실제 게임 로직(OrderManager 등)을 실행하는 중계자
public class AICommandBridge : MonoBehaviour
{
    public static AICommandBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 변경되어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // VoiceOrderManager에서 호출: 서버로부터 받은 JSON 명령 처리
    public void ProcessAICommand(string actionType, string parametersJson)
    {
        if (string.IsNullOrEmpty(actionType) || string.IsNullOrEmpty(parametersJson)) return;

        Debug.Log($"[AI Bridge] 명령 수신: {actionType}");
        Debug.Log($"[AI Bridge] 파라미터: {parametersJson}");

        // server.py가 보내주는 구조: {"commands": [ ... ]}
        if (actionType == "multi_command")
        {
            try
            {
                // JSON 파싱
                CommandListWrapper commandList = JsonUtility.FromJson<CommandListWrapper>(parametersJson);

                if (commandList != null && commandList.commands != null)
                {
                    // ★ [핵심 수정] AI가 보내주는 JSON은 '누적된 전체 주문 내역'이므로,
                    // 기존 장바구니를 비우고(Clear) 처음부터 다시 쌓아야(Rebuild) 중복을 막을 수 있습니다.
                    Debug.Log("[AI Bridge] 주문 동기화를 위해 장바구니를 초기화합니다.");
                    OrderManager.Instance.ClearOrder();

                    foreach (var cmd in commandList.commands)
                    {
                        ExecuteSingleCommand(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI Bridge] JSON 파싱 오류: {ex.Message}");
            }
        }
    }

    // 개별 명령어 실행 분기
    private void ExecuteSingleCommand(AICommand cmd)
    {
        Debug.Log($"[AI Bridge] 실행 중: {cmd.type}");

        switch (cmd.type)
        {
            case "add_course":
                ExecuteAddCourse(cmd.course);
                break;

            case "modify_course":
                ExecuteModifyCourse(cmd.style, cmd.add, cmd.remove);
                break;

            case "confirm_order":
                ExecuteConfirmOrder(cmd.is_reservation, cmd.date);
                break;

            case "clear_order":
                ExecuteClearOrder();
                break;
        }
    }

    // 1. 코스 추가 명령
    // AI가 "ValentineDinner"라고 보내면 Enum으로 변환해서 추가
    private void ExecuteAddCourse(string courseName)
    {
        if (Enum.TryParse(courseName, out CourseType type))
        {
            OrderManager.Instance.AddCourseToOrder(type);
            Debug.Log($"[AI Bridge] 코스 추가 성공: {type}");
        }
        else
        {
            Debug.LogError($"[AI Bridge] 알 수 없는 코스 이름: {courseName}");
        }
    }

    // 2. 옵션/스타일 변경 명령
    // AI가 "AddSteak80g", "RemoveWine" 같은 정확한 키를 보냄
    private void ExecuteModifyCourse(string styleName, List<string> addItems, List<string> removeItems)
    {
        // 현재 수정 중인 상세 객체 가져오기
        var detail = OrderManager.Instance.GetCurrentCourseDetailForEditing();
        if (detail == null)
        {
            Debug.LogWarning("[AI Bridge] 수정할 코스가 없습니다.");
            return;
        }

        // A. 스타일 변경 (예: "Grand")
        if (!string.IsNullOrEmpty(styleName) && Enum.TryParse(styleName, out StyleType style))
        {
            detail.style = style;
            Debug.Log($"[AI Bridge] 스타일 변경: {style}");
        }

        // B. 추가 항목 처리 (예: "AddSteak80g")
        if (addItems != null)
        {
            foreach (var item in addItems)
            {
                if (!detail.addedItems.Contains(item))
                    detail.addedItems.Add(item);
                Debug.Log($"[AI Bridge] 옵션 추가: {item}");
            }
        }

        // C. 삭제 항목 처리 (예: "RemoveWine")
        if (removeItems != null)
        {
            foreach (var item in removeItems)
            {
                if (!detail.removedItems.Contains(item))
                    detail.removedItems.Add(item);
                Debug.Log($"[AI Bridge] 옵션 삭제: {item}");
            }
        }

        // D. 변경 사항을 UI에 반영하기 위해 패널 갱신
        RefreshDetailPanel();
    }

    // 3. 주문 확정 명령
    // 예약 주문일 경우 날짜(date)가 반드시 포함됨
    private async void ExecuteConfirmOrder(bool isReservation, string date)
    {
        // 현재 주문 객체 가져오기
        var currentOrder = OrderManager.Instance.CurrentOrder;
        if (currentOrder != null)
        {
            currentOrder.isReservation = isReservation;

            // 예약 날짜 설정 (형식: "yyyy-MM-dd" or "MM/dd" 등 AI가 주는 대로)
            // C# DateTime 파싱을 위해 형식을 맞춰주면 좋지만, 일단 문자열 저장
            if (isReservation && !string.IsNullOrEmpty(date))
            {
                // 간단한 날짜 파싱 시도 (예: "12월 25일" -> "2025-12-25" 변환은 AI가 하거나 여기서 처리)
                // 여기서는 AI가 "2025-12-25" 형식으로 준다고 가정하고 그대로 저장
                currentOrder.deliveryDate = date;
                Debug.Log($"[AI Bridge] 예약 날짜 설정: {date}");
            }
        }

        Debug.Log("[AI Bridge] 주문 확정 및 결제 진행...");

        // 결제 로직 호출 (ConfirmOrderManager 기능 활용)
        // 실제 결제 버튼을 누른 것과 동일한 효과
        await OrderManager.Instance.FinalizeAndSubmitOrder(isReservation);

        // [추가된 부분] 3초 대기: 사용자가 AI 답변을 읽거나 들을 시간을 줍니다.
        Debug.Log("[AI Bridge] 3초 후 완료 화면으로 이동합니다...");
        await Task.Delay(3000);

        // 완료 화면 표시
        UIManager.Instance.ShowPanel("OrderCompletePanel");
    }

    // 4. 주문 초기화 명령
    private void ExecuteClearOrder()
    {
        OrderManager.Instance.ClearOrder();

        Debug.Log("[AI Bridge] 주문 초기화 완료");
    }

    // UI 갱신용 헬퍼 함수
    private void RefreshDetailPanel()
    {
        // 현재 보고 있는 화면이 'DinnerDetailPanel'이라면 내용을 갱신해줘야 함
        if (UIManager.Instance.GetCurrentPanelName() == "DinnerDetailPanel")
        {
            // 가장 간단한 갱신 방법: 패널을 다시 Show 호출 (OnEnable이 다시 돌면서 데이터 로드)
            // UIManager 구조에 따라 다를 수 있지만 일반적으로 안전함
            UIManager.Instance.ShowPanel("DinnerDetailPanel");
        }
    }

    // --- 데이터 클래스 (server.py의 JSON 구조와 일치) ---
    [Serializable]
    public class CommandListWrapper
    {
        public List<AICommand> commands;
    }

    [Serializable]
    public class AICommand
    {
        public string type;          // "add_course", "modify_course", "confirm_order", "clear_order"
        public string course;        // "ValentineDinner" (Enum 매칭용)
        public string style;         // "Grand" (Enum 매칭용)
        public List<string> add;     // ["AddSteak80g", "AddWineBottle"] (AddonKeys 매칭용)
        public List<string> remove;  // ["RemoveWine"] (AddonKeys 매칭용)
        public bool is_reservation;  // true/false
        public string date;          // "2025-12-25"
    }
}
