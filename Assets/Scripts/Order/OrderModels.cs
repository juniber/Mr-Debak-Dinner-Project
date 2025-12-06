using System;
using System.Collections.Generic;

// Firebase DB에서 재고(inventory) 키 이름들을 상수로 관리
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

// 추가/삭제 옵션의 키들을 관리하는 상수 클래스
public static class AddonKeys
{
    // 추가 옵션 키
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

    // 제거 옵션 키
    public const string RemoveWine = "RemoveWine";
    public const string RemoveCoffee = "RemoveCoffee";
    public const string RemoveSalad = "RemoveSalad";
    public const string RemoveEggs = "RemoveEggs";
    public const string RemoveBacon = "RemoveBacon";
    public const string RemoveBaguette = "RemoveBaguette";
}

// 메뉴 구성, 추가/삭제 옵션의 재고/환불 정보를 중앙에서 관리하는 static 클래스
public static class MenuData
{
    // '추가' 옵션 하나가 소모하는 재고 정보
    public class AddonInventoryInfo
    {
        public string InventoryKey;
        public long Amount;
        public AddonInventoryInfo(string key, long amount) { InventoryKey = key; Amount = amount; }
    }

    // 코스 타입에 따른 기본 재고 요구량 반환
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
                requirements.Add(InventoryKeys.CoffeeServings, 4); // 커피 1pot == 4잔
                requirements.Add(InventoryKeys.WineServings, 5);
                requirements.Add(InventoryKeys.SteakMeatG, 400);
                break;

            default:
                // 정의되지 않은 코스는 재고 요구 없음
                break;
        }
        return requirements;
    }

    // '추가' 옵션 키(AddonKey) → 재고 키/수량 매핑
    public static Dictionary<string, AddonInventoryInfo> GetAddonCosts()
    {
        return new Dictionary<string, AddonInventoryInfo>
        {
            { AddonKeys.AddSteak80g,         new AddonInventoryInfo(InventoryKeys.SteakMeatG, 80) },
            { AddonKeys.AddSteak160g,        new AddonInventoryInfo(InventoryKeys.SteakMeatG, 160) },
            { AddonKeys.AddMiniCorn2P,       new AddonInventoryInfo(InventoryKeys.MiniCornPcs, 2) },
            { AddonKeys.AddPotatoSalad180g,  new AddonInventoryInfo(InventoryKeys.PotatoSaladG, 180) },
            { AddonKeys.AddSalad70g,         new AddonInventoryInfo(InventoryKeys.SaladGreensG, 70) },
            { AddonKeys.AddBacon18g,         new AddonInventoryInfo(InventoryKeys.BaconG, 18) },
            { AddonKeys.AddScrambledEggs,    new AddonInventoryInfo(InventoryKeys.EggsPcs, 1) },
            { AddonKeys.AddBaguette3P,       new AddonInventoryInfo(InventoryKeys.BaguettePcs, 3) },
            { AddonKeys.AddBaguette6P,       new AddonInventoryInfo(InventoryKeys.BaguettePcs, 6) },
            { AddonKeys.AddWineGlass,        new AddonInventoryInfo(InventoryKeys.WineServings, 1) },
            { AddonKeys.AddWineBottle,       new AddonInventoryInfo(InventoryKeys.WineServings, 5) },
            { AddonKeys.AddCoffeeGlass,      new AddonInventoryInfo(InventoryKeys.CoffeeServings, 1) },
            { AddonKeys.AddCoffeePot,        new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4) },
            { AddonKeys.AddChampagneBottle,  new AddonInventoryInfo(InventoryKeys.ChampagneBottles, 1) }
        };
    }

    // '제거' 옵션에 대해 환불되는 재고 정보
    public static AddonInventoryInfo GetRefundInfo(CourseType courseType, string removeKey)
    {
        switch (courseType)
        {
            case CourseType.ValentineDinner:
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 5);
                break;

            case CourseType.FrenchDinner:
                if (removeKey == AddonKeys.RemoveCoffee) return new AddonInventoryInfo(InventoryKeys.CoffeeServings, 1);
                if (removeKey == AddonKeys.RemoveWine)   return new AddonInventoryInfo(InventoryKeys.WineServings, 1);
                if (removeKey == AddonKeys.RemoveSalad)  return new AddonInventoryInfo(InventoryKeys.SaladGreensG, 70);
                break;

            case CourseType.EnglishDinner:
                if (removeKey == AddonKeys.RemoveEggs)     return new AddonInventoryInfo(InventoryKeys.EggsPcs, 2);
                if (removeKey == AddonKeys.RemoveBacon)    return new AddonInventoryInfo(InventoryKeys.BaconG, 18);
                if (removeKey == AddonKeys.RemoveBaguette) return new AddonInventoryInfo(InventoryKeys.BaguettePcs, 1);
                break;

            case CourseType.ChampagneFeastDinner:
                if (removeKey == AddonKeys.RemoveBaguette) return new AddonInventoryInfo(InventoryKeys.BaguettePcs, 4);
                if (removeKey == AddonKeys.RemoveCoffee)   return new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4);
                if (removeKey == AddonKeys.RemoveWine)     return new AddonInventoryInfo(InventoryKeys.WineServings, 5);
                break;
        }
        return null;
    }

    public static string GetMenuName(string courseKey)
    {
        if (Enum.TryParse(courseKey, out CourseType type))
        {
            return GetMenuName(type);
        }
        return courseKey;
    }

    public static string GetMenuName(CourseType type)
    {
        switch (type)
        {
            case CourseType.ValentineDinner:       return "발렌타인 디너";
            case CourseType.FrenchDinner:          return "프렌치 디너";
            case CourseType.EnglishDinner:         return "잉글리시 디너";
            case CourseType.ChampagneFeastDinner:  return "샴페인 피스트 디너";
            default: return type.ToString();
        }
    }

    public static string GetAddonName(string key)
    {
        switch (key)
        {
            case AddonKeys.AddSteak80g:        return "안심 스테이크 80g";
            case AddonKeys.AddSteak160g:       return "안심 스테이크 160g";
            case AddonKeys.AddMiniCorn2P:      return "미니콘 2조각";
            case AddonKeys.AddPotatoSalad180g: return "감자샐러드 180g";
            case AddonKeys.AddSalad70g:        return "샐러드 70g";
            case AddonKeys.AddBacon18g:        return "베이컨 18g";
            case AddonKeys.AddScrambledEggs:   return "스크램블 에그";
            case AddonKeys.AddBaguette3P:      return "바게트 3조각";
            case AddonKeys.AddBaguette6P:      return "바게트 6조각";
            case AddonKeys.AddWineGlass:       return "와인 1잔";
            case AddonKeys.AddWineBottle:      return "와인 1병";
            case AddonKeys.AddCoffeeGlass:     return "커피 1잔";
            case AddonKeys.AddCoffeePot:       return "커피 1포트";
            case AddonKeys.AddChampagneBottle: return "샴페인 1병";

            case AddonKeys.RemoveWine:     return "와인 제거";
            case AddonKeys.RemoveCoffee:   return "커피 제거";
            case AddonKeys.RemoveSalad:    return "샐러드 제거";
            case AddonKeys.RemoveEggs:     return "계란 제거";
            case AddonKeys.RemoveBacon:    return "베이컨 제거";
            case AddonKeys.RemoveBaguette: return "바게트 제거";

            default: return key;
        }
    }
}

// 주문에서 선택 가능한 코스 종류
public enum CourseType
{
    ValentineDinner,
    FrenchDinner,
    EnglishDinner,
    ChampagneFeastDinner
}

// 코스의 스타일(연출 정도 등)
public enum StyleType
{
    None,   // 미선택
    Simple,
    Grand,
    Deluxe
}

// 주문 상태
public enum OrderStatus
{
    Pending,     // 0 (장바구니에만 있는 상태)
    Reserved,    // 1 (예약 접수)
    Confirmed,   // 2 (즉시 주문 확정)
    Cooking,     // 3 (조리 중)
    Delivering,  // 4 (배달 중)
    Completed,   // 5 (배달 완료)
    Canceled     // 6 (주문 취소됨)
}

// 코스 하나의 상세 옵션(스타일, 추가/제거 항목 등)
[Serializable]
public class CourseDetail
{
    public StyleType style;
    public List<string> addedItems;    // 추가된 옵션 키들(AddonKeys)
    public List<string> removedItems;  // 제거된 옵션 키들(AddonKeys)

    public CourseDetail()
    {
        this.style = StyleType.None;
        this.addedItems = new List<string>();
        this.removedItems = new List<string>();
    }
}

// 같은 코스 타입(ValentineDinner 등)의 묶음
[Serializable]
public class CourseGroup
{
    public string courseType;          // CourseType.ToString() (예: "ValentineDinner")
    public List<CourseDetail> details; // 해당 코스를 몇 개 주문했는지에 대한 리스트

    public CourseGroup(string type)
    {
        this.courseType = type;
        this.details = new List<CourseDetail>();
    }
}

// 🔹 쿠폰 데이터
[Serializable]
public class Coupon
{
    public string couponId;        // 쿠폰 ID (예: "WELCOME10")
    public long discountAmount;    // 할인율(%) 또는 고정 금액 등, 현재는 %로 사용
    public bool used;              // 사용 여부

    public Coupon() { }

    public Coupon(string couponId, long discountAmount, bool used)
    {
        this.couponId = couponId;
        this.discountAmount = discountAmount;
        this.used = used;
    }
}

// 한 건의 주문 전체를 나타내는 클래스
[Serializable]
public class Order
{
    public string orderId;
    public string userId;
    public OrderStatus status;
    public long orderTimestamp;
    public string globalRequests;      // 전체 주문에 대한 요청사항(문자열)

    public List<CourseGroup> courseGroups;

    // PriceManager에서 계산하는 총 금액(할인 전 기준 금액)
    public long totalPrice;

    // 할인 적용 후 최종 결제 금액 (쿠폰/프로모션 반영 후)
    public long totalDiscountPrice;

    // 예약일(문자열 yyyy-MM-dd)
    public string deliveryDate;

    // 예약 주문 여부(true면 예약, false면 즉시)
    public bool isReservation;

    // 🔹 쿠폰 리스트 (여러 개를 지원하고 싶으면 리스트, 지금은 보통 1개만 사용)
    public List<Coupon> coupons;

    public Order(string userId)
    {
        this.userId = userId;
        this.orderId = Guid.NewGuid().ToString();
        this.status = OrderStatus.Pending;
        this.courseGroups = new List<CourseGroup>();
        this.globalRequests = "";
        this.totalPrice = 0;
        this.totalDiscountPrice = 0;
        this.deliveryDate = DateTime.Today.ToString("yyyy-MM-dd");
        this.isReservation = false;
        this.coupons = new List<Coupon>();
        this.orderTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    // 코스 하나를 추가
    public void AddCourse(CourseType type)
    {
        string courseKey = type.ToString();
        CourseDetail newCourseDetail = new CourseDetail();

        // 같은 타입의 코스 그룹이 이미 있는지 확인
        CourseGroup group = courseGroups.Find(g => g.courseType == courseKey);
        if (group == null)
        {
            group = new CourseGroup(courseKey);
            courseGroups.Add(group);
        }

        group.details.Add(newCourseDetail);
    }

    // 전체 코스 개수 반환
    public int GetTotalCourseCount()
    {
        int total = 0;
        foreach (var group in courseGroups)
        {
            total += group.details.Count;
        }
        return total;
    }

    // 가장 마지막에 추가된 코스 하나 반환
    public CourseDetail GetLastAddedCourseDetail()
    {
        if (courseGroups.Count == 0) return null;

        CourseGroup lastGroup = courseGroups[courseGroups.Count - 1];
        if (lastGroup.details.Count == 0) return null;

        return lastGroup.details[lastGroup.details.Count - 1];
    }
}
