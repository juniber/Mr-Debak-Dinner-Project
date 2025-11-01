using System;
using System.Collections.Generic;

// 주문할 수 있는 코스의 종류
public enum CourseType
{
    ValentineDinner,
    FrenchDinner,
    EnglishDinner,
    ChampagneFeastDinner
}

// 코스에 적용할 스타일의 종류
public enum StyleType
{
    None, // 미선택
    Simple,
    Grand,
    Deluxe
}

// 주문의 현재 상태
public enum OrderStatus
{
    Pending,        // 주문 완료 전 (현재 장바구니 상태)
    Confirmed,      // 주문 완료
    Cooking,        // 조리 중
    Delivering,     // 배달 중
    Completed       // 배달 완료 
}

// 각 코스별 상세 설정 (스타일, 요청사항 등)
[Serializable]
public class CourseDetail
{
    public StyleType style;
    public string requests;

    public CourseDetail()
    {
        this.style = StyleType.None;
        this.requests = "";
    }
}

// 하나의 주문 객체 (확장 가능)
[Serializable]
public class Order
{
    public string orderId;
    public string userId;
    public OrderStatus status;
    public long orderTimestamp;

    // 핵심 데이터: { "ValentineDinner": [CourseDetail1, CourseDetail2], "FrenchDinner": [CourseDetail1] }
    // 이런 구조로 "발렌타인 2개, 프렌치 1개" 등을 표현
    public Dictionary<string, List<CourseDetail>> courses;

    public Order(string userId)
    {
        this.userId = userId;
        this.orderId = Guid.NewGuid().ToString(); // 임시 고유 ID 생성
        this.status = OrderStatus.Pending;
        this.courses = new Dictionary<string, List<CourseDetail>>();
    }

    // 새 코스를 주문에 추가하는 함수
    public void AddCourse(CourseType type)
    {
        string courseKey = type.ToString();
        CourseDetail newCourse = new CourseDetail();

        if (!courses.ContainsKey(courseKey))
            courses[courseKey] = new List<CourseDetail>();

        courses[courseKey].Add(newCourse);
    }

    // 총 주문한 코스 개수를 반환하는 함수
    public int GetTotalCourseCount()
    {
        int total = 0;
        foreach (var list in courses.Values)
        {
            total += list.Count;
        }
        return total;
    }
}