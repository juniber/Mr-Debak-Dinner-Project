using System;
using System.Collections.Generic;

// Firebase DB의 재고(inventory) 키 이름들을 상수로 관리
public static class InventoryKeys
{
    public const string SteakMeatG = "steakMeat_g";
    public const string MiniCornPcs = "miniCorn_pcs";
    public const string PotatoSaladG = "potatoSalad_g";
    public const string SaladGreensG = "saladGreens_g";
    public const string EggsPcs = "eggs_pcs";
    public const string BaconG = "bacon_g";
    public const string BaguettePcs = "baguette_pcs";
    public const string WineServings = "wine_servings";
    public const string CoffeeServings = "coffee_servings";
    public const string ChampagneBottles = "champagne_bottles";
}

// 추가/제외 항목의 키들을 관리하는 상수 클래스
public static class AddonKeys
{
    // 추가 항목 키
    public const string AddSteak80g = "AddSteak80g";
    public const string AddSteak160g = "AddSteak160g";
    public const string AddMiniCorn2P = "AddMiniCorn2P";
    public const string AddPotatoSalad180g = "AddPotatoSalad180g";
    public const string AddSalad70g = "AddSalad70g";
    public const string AddBacon18g = "AddBacon18g";
    public const string AddScrambledEggs = "AddScrambledEggs";
    public const string AddBaguette3P = "AddBaguette3P";
    public const string AddBaguette6P = "AddBaguette6P";
    public const string AddWineGlass = "AddWineGlass";
    public const string AddWineBottle = "AddWineBottle";
    public const string AddCoffeeGlass = "AddCoffeeGlass";
    public const string AddCoffeePot = "AddCoffeePot"; 
    public const string AddChampagneBottle = "AddChampagneBottle";

    // 제외 항목 키
    public const string RemoveWine = "RemoveWine";
    public const string RemoveCoffee = "RemoveCoffee";
    public const string RemoveSalad = "RemoveSalad";
    public const string RemoveEggs = "RemoveEggs";
    public const string RemoveBacon = "RemoveBacon";
    public const string RemoveBaguette = "RemoveBaguette";
}

// 메뉴 레시피, 추가/제외 항목의 재료/환불 정보를 중앙에서 관리하는 static 클래스
public static class MenuData
{
    // '추가' 항목의 재고 소모량 정의를 위한 헬퍼 클래스
    public class AddonInventoryInfo
    {
        public string InventoryKey;
        public long Amount;
        public AddonInventoryInfo(string key, long amount) { InventoryKey = key; Amount = amount; }
    }

    // 코스 타입에 따라 필요한 기본 식재료 딕셔너리를 반환
    public static Dictionary<string, long> GetCourseBaseRequirements(CourseType courseType)
    {
        var requirements = new Dictionary<string, long>();

        switch (courseType)
        {
            case CourseType.ValentineDinner:
                requirements.Add(InventoryKeys.WineServings, 5); // 와인 1병 == 5잔
                requirements.Add(InventoryKeys.SteakMeatG, 200);
                break;

            case CourseType.FrenchDinner:
                requirements.Add(InventoryKeys.CoffeeServings, 1);
                requirements.Add(InventoryKeys.WineServings, 1);
                requirements.Add(InventoryKeys.SaladGreensG, 70);
                requirements.Add(InventoryKeys.SteakMeatG, 200);
                break;

            case CourseType.EnglishDinner:
                requirements.Add(InventoryKeys.EggsPcs, 2);
                requirements.Add(InventoryKeys.BaconG, 18);
                requirements.Add(InventoryKeys.BaguettePcs, 1);
                requirements.Add(InventoryKeys.SteakMeatG, 200);
                break;

            case CourseType.ChampagneFeastDinner:
                requirements.Add(InventoryKeys.ChampagneBottles, 1);
                requirements.Add(InventoryKeys.BaguettePcs, 4);
                requirements.Add(InventoryKeys.CoffeeServings, 4); // 커피 1 포트 == 4잔
                requirements.Add(InventoryKeys.WineServings, 5);
                requirements.Add(InventoryKeys.SteakMeatG, 400);
                break;

            default:
                // 정의되지 않은 코스 타입의 경우, 빈 딕셔너리 반환
                break;
        }
        return requirements;
    }

    // '추가' 항목(AddonKey)과 재료 소모량(InventoryInfo) 맵을 반환
    public static Dictionary<string, AddonInventoryInfo> GetAddonCosts()
    {
        return new Dictionary<string, AddonInventoryInfo>
        {
            { AddonKeys.AddSteak80g, new AddonInventoryInfo(InventoryKeys.SteakMeatG, 80) },
            { AddonKeys.AddSteak160g, new AddonInventoryInfo(InventoryKeys.SteakMeatG, 160) },
            { AddonKeys.AddMiniCorn2P, new AddonInventoryInfo(InventoryKeys.MiniCornPcs, 2) },
            { AddonKeys.AddPotatoSalad180g, new AddonInventoryInfo(InventoryKeys.PotatoSaladG, 180) },
            { AddonKeys.AddSalad70g, new AddonInventoryInfo(InventoryKeys.SaladGreensG, 70) },
            { AddonKeys.AddBacon18g, new AddonInventoryInfo(InventoryKeys.BaconG, 18) },
            { AddonKeys.AddScrambledEggs, new AddonInventoryInfo(InventoryKeys.EggsPcs, 1) },
            { AddonKeys.AddBaguette3P, new AddonInventoryInfo(InventoryKeys.BaguettePcs, 3) },
            { AddonKeys.AddBaguette6P, new AddonInventoryInfo(InventoryKeys.BaguettePcs, 6) },
            { AddonKeys.AddWineGlass, new AddonInventoryInfo(InventoryKeys.WineServings, 1) },
            { AddonKeys.AddWineBottle, new AddonInventoryInfo(InventoryKeys.WineServings, 5) },
            { AddonKeys.AddCoffeeGlass, new AddonInventoryInfo(InventoryKeys.CoffeeServings, 1) },
            { AddonKeys.AddCoffeePot, new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4) },
            { AddonKeys.AddChampagneBottle, new AddonInventoryInfo(InventoryKeys.ChampagneBottles, 1) }
        };
    }

    // '제외' 키(AddonKey)에 따라 환불되는 재료(InventoryKey)와 수량(Amount)을 반환
    public static AddonInventoryInfo GetRefundInfo(CourseType courseType, string removeKey)
    {
        switch (courseType)
        {
            case CourseType.ValentineDinner:
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 5);
                break;
            case CourseType.FrenchDinner:
                if (removeKey == AddonKeys.RemoveCoffee) return new AddonInventoryInfo(InventoryKeys.CoffeeServings, 1);
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 1);
                if (removeKey == AddonKeys.RemoveSalad) return new AddonInventoryInfo(InventoryKeys.SaladGreensG, 70);
                break;
            case CourseType.EnglishDinner:
                if (removeKey == AddonKeys.RemoveEggs) return new AddonInventoryInfo(InventoryKeys.EggsPcs, 2);
                if (removeKey == AddonKeys.RemoveBacon) return new AddonInventoryInfo(InventoryKeys.BaconG, 18);
                if (removeKey == AddonKeys.RemoveBaguette) return new AddonInventoryInfo(InventoryKeys.BaguettePcs, 1);
                break;
            case CourseType.ChampagneFeastDinner:
                if (removeKey == AddonKeys.RemoveBaguette) return new AddonInventoryInfo(InventoryKeys.BaguettePcs, 4);
                if (removeKey == AddonKeys.RemoveCoffee) return new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4);
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 5);
                break;
        }
        return null;
    }
}

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
    Pending,     // 0 (장바구니 상태)
    Reserved,    // 1 (신규! 예약 확정 상태)
    Confirmed,   // 2 (즉시 주문 확정 상태)
    Cooking,     // 3 (조리 중)
    Delivering,  // 4 (배달 중)
    Completed    // 5 (배달 완료)
}

// 각 코스별 상세 설정 (스타일, 요청사항 등)
[Serializable]
public class CourseDetail
{
    public StyleType style;
    public List<string> addedItems;    // 추가된 항목 목록 (AddonKeys 사용)
    public List<string> removedItems;  // 제외된 항목 목록 (AddonKeys 사용)

    public CourseDetail()
    {
        this.style = StyleType.None;
        this.addedItems = new List<string>();
        this.removedItems = new List<string>();
    }
}

// JsonUtility 호환을 위해 사용 따로 class로 만들어 주문을 구분
// (예: "ValentineDinner" 그룹 / "FrenchDinner" 그룹)
[Serializable]
public class CourseGroup
{
    public string courseType; // CourseType.ToString() 값 (예: "ValentineDinner")
    public List<CourseDetail> details; // 이 코스를 여러 개 시켰을 경우의 리스트 (예: 발렌타인 2개)

    public CourseGroup(string type)
    {
        this.courseType = type;
        this.details = new List<CourseDetail>();
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
    public string globalRequests; // 전역 요청사항

    public List<CourseGroup> courseGroups;

    // 가격 계산 로직은 PriceManager가 담당. 변수만 유지.
    public long totalPrice;

    // 배달 예약 날짜
    public string deliveryDate;
    // 이 주문이 즉시 배달인지, 예약인지 여부
    public bool isReservation;

    public Order(string userId)
    {
        this.userId = userId;
        this.orderId = Guid.NewGuid().ToString(); // 임시 고유 ID
        this.status = OrderStatus.Pending;
        this.courseGroups = new List<CourseGroup>();
        this.globalRequests = ""; // 초기화
        this.totalPrice = 0;

        // 기본값은 '오늘' '즉시 배달'
        this.deliveryDate = DateTime.Today.ToString("yyyy-MM-dd");
        this.isReservation = false;
    }

    // 새 코스를 주문에 추가하는 함수
    public void AddCourse(CourseType type)
    {
        string courseKey = type.ToString();
        CourseDetail newCourseDetail = new CourseDetail(); // 새 상세 설정 객체 생성

        // 이미 이 타입의 코스가 주문에 있는지 확인
        CourseGroup group = courseGroups.Find(g => g.courseType == courseKey);

        if (group == null)
        {
            // 없다면 새로 CourseGroup 생성
            group = new CourseGroup(courseKey);
            courseGroups.Add(group);
        }

        // 새 CourseDetail을 이 그룹에 추가 (예: 발렌타인 디너 1개 -> 2개)
        group.details.Add(newCourseDetail);
    }

    // 총 주문한 코스 개수를 반환하는 함수
    public int GetTotalCourseCount()
    {
        int total = 0;
        foreach (var group in courseGroups)
        {
            total += group.details.Count;
        }
        return total;
    }

    // 방금 추가한(가장 마지막의) CourseDetail 객체를 반환하는 함수
    // DinnerDetailManager가 현재 수정할 객체를 찾는 데 사용
    public CourseDetail GetLastAddedCourseDetail()
    {
        if (courseGroups.Count == 0) return null;

        // 가장 마지막에 추가된 코스 그룹 (예: "ValentineDinner" 그룹)
        CourseGroup lastGroup = courseGroups[courseGroups.Count - 1];
        if (lastGroup.details.Count == 0) return null;

        // 그 그룹의 가장 마지막에 추가된 CourseDetail (예: 두 번째 발렌타인 디너)
        return lastGroup.details[lastGroup.details.Count - 1];
    }
}