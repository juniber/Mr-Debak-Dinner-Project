using Firebase.Database;
using System; // Convert.ToBoolean
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;

public class DinnerDetailManager : MonoBehaviour
{
    [Header("Style UI")]
    public Toggle simpleToggle;
    public Toggle grandToggle;
    public Toggle deluxeToggle;
    public TMP_Text styleDescriptionText;

    // '추가' 토글들이 들어있는 부모 오브젝트 (예: 'Content')
    [Header("Option Containers")] 
    public Transform addonContainer;
    public GameObject valentineRemoveGroup;
    public GameObject frenchRemoveGroup;
    public GameObject englishRemoveGroup;
    public GameObject champagneRemoveGroup;

    [Header("Navigation Buttons")]
    public Button addCourseButton;
    public Button confirmOrderButton;

    private DatabaseReference dbReference;
    private CourseType currentCourseType;
    private CourseDetail currentCourseDetail;

    // 찾은 토글들을 저장할 리스트와 딕셔너리
    private List<AddonToggleLinker> addonToggles = new List<AddonToggleLinker>();
    private Dictionary<CourseType, List<AddonToggleLinker>> removeTogglesMap = new Dictionary<CourseType, List<AddonToggleLinker>>();
    // '추가' 항목의 재고 소모량을 정의하는 딕셔너리 (중앙 관리)
    private Dictionary<string, AddonInventoryInfo> addonInventoryCosts;

    // '제외' 키와 충돌하는 '추가' 키들의 맵
    // Key: RemoveKey (예: "RemoveWine"), Value: List of conflicting AddKeys (예: ["AddWineGlass", "AddWineBottle"])
    private Dictionary<string, List<string>> conflictMap;
    // '추가' 키가 어떤 '제외' 키와 충돌하는지 반대로 찾는 맵 (빠른 검색용)
    // Key: AddKey (예: "AddWineGlass"), Value: Conflicting RemoveKey (예: "RemoveWine")
    private Dictionary<string, string> reverseConflictMap;

    // DB에서 가져온 재고 스냅샷 (캐시)
    private DataSnapshot inventorySnapshot;

    // 재고 소모량 정의를 위한 작은 헬퍼 클래스
    private class AddonInventoryInfo
    {
        public string InventoryKey;
        public long Amount;
        public AddonInventoryInfo(string key, long amount) { InventoryKey = key; Amount = amount; }
    }

    void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 1. '추가' 항목 재고 소모량 맵 초기화
        InitializeInventoryCostMap();

        // 2. (신규) 충돌 맵 초기화
        InitializeConflictMap();

        // 3. '추가' 토글들 자동 스캔
        // addonContainer의 모든 자식(비활성화 포함)에서 AddonToggleLinker를 찾아 리스트에 추가
        addonContainer.GetComponentsInChildren<AddonToggleLinker>(true, addonToggles);

        // 4. '제외' 토글들 자동 스캔 및 맵핑
        removeTogglesMap[CourseType.ValentineDinner] = valentineRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.FrenchDinner] = frenchRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.EnglishDinner] = englishRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();
        removeTogglesMap[CourseType.ChampagneFeastDinner] = champagneRemoveGroup.GetComponentsInChildren<AddonToggleLinker>(true).ToList();

        // 5. 모든 '제외' 토글에 리스너(이벤트)를 추가
        foreach (var toggleList in removeTogglesMap.Values)
        {
            foreach (var linker in toggleList)
            {
                // '제외' 토글의 값이 변경되면, '추가' 토글들의 재고를 다시 계산
                linker.Toggle.onValueChanged.AddListener(OnRemoveToggleChanged);
            }
        }

        // 6. 스타일 토글 리스너 연결
        simpleToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Simple));
        grandToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Grand));
        deluxeToggle.onValueChanged.AddListener((isOn) => OnStyleToggleChanged(isOn, StyleType.Deluxe));

        // 7. 하단 버튼 리스너 연결
        addCourseButton.onClick.AddListener(OnAddCourseClicked);
        confirmOrderButton.onClick.AddListener(OnConfirmOrderClicked);
    }

    // '추가' 항목 재고 소모량 맵을 초기화하는 함수
    private void InitializeInventoryCostMap()
    {
        addonInventoryCosts = new Dictionary<string, AddonInventoryInfo>
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

    // '추가'와 '제외' 토글 간의 충돌 맵
    private void InitializeConflictMap()
    {
        // 1. 정방향 맵 (RemoveKey -> AddKeys)
        conflictMap = new Dictionary<string, List<string>>
        {
            { AddonKeys.RemoveWine, new List<string> { AddonKeys.AddWineGlass, AddonKeys.AddWineBottle } },
            { AddonKeys.RemoveCoffee, new List<string> { AddonKeys.AddCoffeeGlass, AddonKeys.AddCoffeePot } },
            { AddonKeys.RemoveSalad, new List<string> { AddonKeys.AddSalad70g } },
            { AddonKeys.RemoveEggs, new List<string> { AddonKeys.AddScrambledEggs } },
            { AddonKeys.RemoveBacon, new List<string> { AddonKeys.AddBacon18g } },
            { AddonKeys.RemoveBaguette, new List<string> { AddonKeys.AddBaguette3P, AddonKeys.AddBaguette6P } }
        };

        // 2. 역방향 맵 (AddKey -> RemoveKey) (빠른 검색용)
        reverseConflictMap = new Dictionary<string, string>();
        foreach (var entry in conflictMap)
        {
            string removeKey = entry.Key;
            foreach (string addKey in entry.Value)
            {
                reverseConflictMap[addKey] = removeKey;
            }
        }
    }

    // 제외' 토글이 클릭되면 호출되는 이벤트 함수
    private void OnRemoveToggleChanged(bool isOn)
    {
        // DB에서 새 데이터를 가져오지 않고, 캐시된 스냅샷으로 재고를 다시 계산
        UpdateAddonInteractability();
    }

    // 캐시된 재고 데이터를 기준으로 '추가' 토글의 활성화/비활성화를 업데이트
    private void UpdateAddonInteractability()
    {
        if (inventorySnapshot == null) return; // 재고 데이터가 아직 로드되지 않음

        // 1. 현재 코스의 기본 소모량
        var baseRequirements = MenuData.GetCourseBaseRequirements(currentCourseType);

        // 2. '이전 코스들'의 총 소모량 (제외 항목 포함)
        var committedCost = GetCommittedCost();

        // 3. '현재 코스'의 '제외' 토글로 인한 환불량
        var currentRefunds = GetCurrentRefunds();

        // 현재 켜져 있는 '제외' 토글 키 목록을 가져옴 (충돌 검사용)
        var activeRemoveKeys = GetCurrentActiveRemoveKeys();

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var linker in addonToggles)
            {
                bool isStockAvailable = true; // 재고 있음으로 가정
                bool isConflicted = false;    // 충돌 없음으로 가정

                // 1. 충돌 검사
                // 이 '추가' 토글(linker.addonKey)이 충돌 맵에 등록되어 있는지 확인
                if (reverseConflictMap.TryGetValue(linker.addonKey, out string conflictingRemoveKey))
                {
                    // 충돌하는 '제외' 토글이 현재 활성화(켜져) 있는지 확인
                    if (activeRemoveKeys.Contains(conflictingRemoveKey))
                    {
                        isConflicted = true;
                    }
                }

                // 2. 재고 검사 (충돌하지 않았을 경우에만)
                if (!isConflicted)
                {
                    if (addonInventoryCosts.TryGetValue(linker.addonKey, out AddonInventoryInfo cost))
                    {
                        // 재고 확인 로직을 헬퍼 함수에서 여기로 가져옴
                        long baseAmount = baseRequirements.TryGetValue(cost.InventoryKey, out var b) ? b : 0;
                        long committedAmount = committedCost.TryGetValue(cost.InventoryKey, out var c) ? c : 0;
                        long refundAmount = currentRefunds.TryGetValue(cost.InventoryKey, out var r) ? r : 0;
                        long currentStock = Convert.ToInt64(inventorySnapshot.Child(cost.InventoryKey).Value);

                        long totalDemand = (committedAmount + baseAmount + cost.Amount) - refundAmount;
                        isStockAvailable = (currentStock >= totalDemand);
                    }
                }

                // 3. 최종 결정
                bool canInteract = (isStockAvailable && !isConflicted);
                linker.Toggle.interactable = canInteract;

                // 비활성화되면, 체크(isOn)도 강제로 해제
                if (!canInteract)
                {
                    linker.Toggle.isOn = false;
                }
            }
        });
    }

    // '현재 패널'에서 체크된 '제외' 토글의 AddonKey 목록을 반환
    private HashSet<string> GetCurrentActiveRemoveKeys()
    {
        var activeKeys = new HashSet<string>();
        if (removeTogglesMap.ContainsKey(currentCourseType))
        {
            foreach (var linker in removeTogglesMap[currentCourseType])
            {
                if (linker.Toggle.isOn)
                {
                    activeKeys.Add(linker.addonKey);
                }
            }
        }
        return activeKeys;
    }

    // '현재 패널'에서 체크된 '제외' 토글의 총 환불량을 계산
    private Dictionary<string, long> GetCurrentRefunds()
    {
        var refunds = new Dictionary<string, long>();
        if (!removeTogglesMap.ContainsKey(currentCourseType))
        {
            return refunds;
        }

        // 현재 코스에 해당하는 '제외' 토글 리스트를 가져옴
        var currentRemoveToggles = removeTogglesMap[currentCourseType];

        foreach (var linker in currentRemoveToggles)
        {
            if (linker.Toggle.isOn) // '제외' 토글이 켜져있다면
            {
                AddonInventoryInfo refund = GetRefundInfo(currentCourseType, linker.addonKey);
                if (refund != null)
                {
                    if (!refunds.ContainsKey(refund.InventoryKey)) refunds[refund.InventoryKey] = 0;
                    refunds[refund.InventoryKey] += refund.Amount;
                }
            }
        }
        return refunds;
    }

    // '제외' 키(AddonKey)에 따라 환불되는 재료(InventoryKey)와 수량(Amount)을 반환
    private AddonInventoryInfo GetRefundInfo(CourseType courseType, string removeKey)
    {
        // 코스별로 기본 제공량이 다르므로, 환불량도 다르다.
        switch (courseType)
        {
            case CourseType.ValentineDinner:
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 5); // 1병(5잔)
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
                if (removeKey == AddonKeys.RemoveCoffee) return new AddonInventoryInfo(InventoryKeys.CoffeeServings, 4); // 1포트(4잔)
                if (removeKey == AddonKeys.RemoveWine) return new AddonInventoryInfo(InventoryKeys.WineServings, 5); // 1병(5잔)
                break;
        }
        return null; // 해당하는 환불 항목이 없음
    }

    // 스타일 토글 설명
    private void OnStyleToggleChanged(bool isOn, StyleType style)
    {
        if (!isOn) return; // 켜질 때만 동작

        switch (style)
        {
            case StyleType.Simple:
                styleDescriptionText.text = "플라스틱 접시와 플라스틱 컵, 종이 냅킨이 플라스틱 쟁반에 제공되고, 와인이 포함되면 잔은 플라스틱 잔 제공";
                break;
            case StyleType.Grand:
                styleDescriptionText.text = "도자기 접시와 도자기 컵, 흰색 면 냅킨이 나무 쟁반에 제공되고, 와인이 포함되며 잔은 플라스틱 잔 제공";
                break;
            case StyleType.Deluxe:
                styleDescriptionText.text = "꽃들이 있는 작은 꽃병, 도자기 접시와 도자기 컵, 린넨 냅킨이 나무 쟁반에 제공되고, 와인이 포함되면 잔은 유리 잔 제공";
                break;
        }
    }

    // "코스 추가하기" 버튼 클릭
    private void OnAddCourseClicked()
    {
        // 1. 현재 패널의 선택 사항을 OrderManager의 currentCourseDetail에 저장
        SaveCurrentSelectionsToOrder();

        // 2. 디버그 로그 출력
        Debug.Log("--- [코스 추가] 현재까지의 주문 내역 ---");
        Debug.Log(JsonUtility.ToJson(OrderManager.Instance.CurrentOrder, true)); // JsonUtility로 예쁘게 출력

        // 3. 패널 이동
        UIManager.Instance.ShowPanel("SelectDinnerPanel");
    }

    // "주문 확인하기" 버튼 클릭
    private void OnConfirmOrderClicked()
    {
        // 1. 현재 패널의 선택 사항을 OrderManager의 currentCourseDetail에 저장
        SaveCurrentSelectionsToOrder();

        // 2. 디버그 로그 출력
        Debug.Log("--- [주문 확인] 최종 주문 내역 ---");
        Debug.Log(JsonUtility.ToJson(OrderManager.Instance.CurrentOrder, true)); // JsonUtility로 예쁘게 출력

        // 3. 패널 이동
        UIManager.Instance.ShowPanel("ConfirmOrderPanel");
    }

    // 현재 UI의 모든 선택 사항을 currentCourseDetail 객체에 저장
    private void SaveCurrentSelectionsToOrder()
    {
        if (currentCourseDetail == null) return;

        // 1. 스타일 저장 
        if (simpleToggle.isOn) currentCourseDetail.style = StyleType.Simple;
        else if (grandToggle.isOn) currentCourseDetail.style = StyleType.Grand;
        else if (deluxeToggle.isOn) currentCourseDetail.style = StyleType.Deluxe;

        // 2. '추가' 항목 저장 
        currentCourseDetail.addedItems.Clear();
        foreach (var linker in addonToggles)
        {
            if (linker.Toggle.isOn)
            {
                currentCourseDetail.addedItems.Add(linker.addonKey);
            }
        }

        // 3. '제외' 항목 저장 
        currentCourseDetail.removedItems.Clear();
        if (removeTogglesMap.ContainsKey(currentCourseType))
        {
            foreach (var linker in removeTogglesMap[currentCourseType])
            {
                if (linker.Toggle.isOn)
                {
                    currentCourseDetail.removedItems.Add(linker.addonKey);
                }
            }
        }
    }

    // 패널이 활성화될 때마다 호출
    private void OnEnable()
    {
        // 1. 현재 수정할 CourseDetail 객체를 OrderManager에서 가져온다.
        currentCourseDetail = OrderManager.Instance.GetCurrentCourseDetailForEditing();
        if (currentCourseDetail == null)
        {
            Debug.LogError("수정할 코스 정보가 없습니다. SelectDinnerPanel에서 문제가 발생했습니다.");
            UIManager.Instance.ShowPanel("SelectDinnerPanel"); // 오류 발생 시 이전 화면으로
            return;
        }

        // 2. 현재 코스 타입을 OrderManager의 Order 객체에서 알아낸다.
        string courseTypeString = OrderManager.Instance.CurrentOrder.courseGroups.Last().courseType;
        // courseTypeString으로 CourseType을 가져올 수 있다. 
        currentCourseType = (CourseType)Enum.Parse(typeof(CourseType), courseTypeString);

        // 3. UI 초기화 및 재고 확인
        SetupPanelForCourse();
        _ = RefreshInventoryData(); // 패널이 켜질 때 DB에서 재고를 한 번만 가져온다. 
    }

    // 패널 초기화
    private void SetupPanelForCourse()
    {
        // '제외' 그룹 활성화/비활성화 (기존과 동일)
        valentineRemoveGroup.SetActive(currentCourseType == CourseType.ValentineDinner);
        frenchRemoveGroup.SetActive(currentCourseType == CourseType.FrenchDinner);
        englishRemoveGroup.SetActive(currentCourseType == CourseType.EnglishDinner);
        champagneRemoveGroup.SetActive(currentCourseType == CourseType.ChampagneFeastDinner);

        // 스타일 토글 초기화 (기존과 동일)
        simpleToggle.isOn = true;
        grandToggle.isOn = false;
        deluxeToggle.isOn = false;
        // Simple 스타일 설명 즉시 표시
        OnStyleToggleChanged(true, StyleType.Simple);

        // '추가' 토글들도 모두 끔 (자동화된 루프)
        foreach (var linker in addonToggles)
        {
            linker.Toggle.isOn = false;
        }

        // '제외' 토글들도 모두 끔 (자동화된 루프)
        if (removeTogglesMap.ContainsKey(currentCourseType))
        {
            foreach (var linker in removeTogglesMap[currentCourseType])
            {
                linker.Toggle.isOn = false;
            }
        }
    }

    // (비동기) DB에서 최신 재고 스냅샷을 가져와 캐시하고, 토글 활성화 상태를 업데이트
    private async Task RefreshInventoryData()
    {
        try
        {
            inventorySnapshot = await dbReference.Child("inventory").GetValueAsync();
            if (!inventorySnapshot.Exists)
            {
                Debug.LogError("재고(Inventory) 데이터를 찾을 수 없습니다.");
                return;
            }

            // 최신 재고 데이터를 기준으로 토글 활성화 상태 업데이트
            UpdateAddonInteractability();
        }
        catch (Exception ex)
        {
            Debug.LogError($"재고 확인 중 오류 발생: {ex}");
        }
    }

    // 현재 편집 중인 코스를 '제외한' 장바구니의 모든 재료 소모량을 계산
    private Dictionary<string, long> GetCommittedCost()
    {
        var committedCost = new Dictionary<string, long>();
        var order = OrderManager.Instance.CurrentOrder;

        if (order == null || order.courseGroups == null)
        {
            return committedCost;
        }

        // 장바구니의 모든 코스 그룹(Valentine, French...)을 순회
        foreach (var group in order.courseGroups)
        {
            CourseType type = (CourseType)Enum.Parse(typeof(CourseType), group.courseType);
            var baseReqs = MenuData.GetCourseBaseRequirements(type);

            // 각 코스 상세(Valentine 1, Valentine 2...)를 순회
            foreach (var detail in group.details)
            {
                // 현재 편집 중인 코스(currentCourseDetail)는 계산에서 제외 
                if (detail == currentCourseDetail)
                {
                    continue;
                }

                // (이전 코스)의 기본 재료 소모량 추가
                foreach (var req in baseReqs)
                {
                    if (!committedCost.ContainsKey(req.Key)) committedCost[req.Key] = 0;
                    committedCost[req.Key] += req.Value;
                }

                // (이전 코스)의 추가 재료 소모량 추가
                foreach (string addonKey in detail.addedItems)
                {
                    if (addonInventoryCosts.TryGetValue(addonKey, out AddonInventoryInfo costInfo))
                    {
                        if (!committedCost.ContainsKey(costInfo.InventoryKey)) committedCost[costInfo.InventoryKey] = 0;
                        committedCost[costInfo.InventoryKey] += costInfo.Amount;
                    }
                }

                // (이전 코스) 제외 재료로 인한 환불(차감)
                foreach (string removedKey in detail.removedItems)
                {
                    AddonInventoryInfo refund = GetRefundInfo(type, removedKey);
                    if (refund != null)
                    {
                        if (!committedCost.ContainsKey(refund.InventoryKey)) committedCost[refund.InventoryKey] = 0;
                        committedCost[refund.InventoryKey] -= refund.Amount; // 재료 소모량에서 차감
                    }
                }
            }
        }
        return committedCost;
    }
}
