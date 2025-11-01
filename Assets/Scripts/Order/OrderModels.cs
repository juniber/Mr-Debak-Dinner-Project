using System;
using System.Collections.Generic;

// �ֹ��� �� �ִ� �ڽ��� ����
public enum CourseType
{
    ValentineDinner,
    FrenchDinner,
    EnglishDinner,
    ChampagneFeastDinner
}

// �ڽ��� ������ ��Ÿ���� ����
public enum StyleType
{
    None, // �̼���
    Simple,
    Grand,
    Deluxe
}

// �ֹ��� ���� ����
public enum OrderStatus
{
    Pending,        // �ֹ� �Ϸ� �� (���� ��ٱ��� ����)
    Confirmed,      // �ֹ� �Ϸ�
    Cooking,        // ���� ��
    Delivering,     // ��� ��
    Completed       // ��� �Ϸ� 
}

// �� �ڽ��� �� ���� (��Ÿ��, ��û���� ��)
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

// �ϳ��� �ֹ� ��ü (Ȯ�� ����)
[Serializable]
public class Order
{
    public string orderId;
    public string userId;
    public OrderStatus status;
    public long orderTimestamp;

    // �ٽ� ������: { "ValentineDinner": [CourseDetail1, CourseDetail2], "FrenchDinner": [CourseDetail1] }
    // �̷� ������ "�߷�Ÿ�� 2��, ����ġ 1��" ���� ǥ��
    public Dictionary<string, List<CourseDetail>> courses;

    public Order(string userId)
    {
        this.userId = userId;
        this.orderId = Guid.NewGuid().ToString(); // �ӽ� ���� ID ����
        this.status = OrderStatus.Pending;
        this.courses = new Dictionary<string, List<CourseDetail>>();
    }

    // �� �ڽ��� �ֹ��� �߰��ϴ� �Լ�
    public void AddCourse(CourseType type)
    {
        string courseKey = type.ToString();
        CourseDetail newCourse = new CourseDetail();

        if (!courses.ContainsKey(courseKey))
            courses[courseKey] = new List<CourseDetail>();

        courses[courseKey].Add(newCourse);
    }

    // �� �ֹ��� �ڽ� ������ ��ȯ�ϴ� �Լ�
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