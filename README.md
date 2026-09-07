# Show Me The Money — 포크 변경점

원작: [swiftxp-hub/spt-show-me-the-money](https://github.com/swiftxp-hub/spt-show-me-the-money)
(아이템 가격·최고가 상인 정보를 툴팁에 표시하는 모드)
4.1 포팅 참조: [mattpsvreis/spt-show-me-the-money](https://github.com/mattpsvreis/spt-show-me-the-money)

이 포크에서 손댄 내용만 기록합니다. 현재 기준 **SPT 4.1**.

---

## 빌드

```
dotnet build
```

경로는 `Directory.Build.props`의 `SptRoot`에서 나옵니다. 우선순위는
`Directory.Build.props.user` → `-p:SptRoot=...` → 환경변수 `SPT_ROOT` → 기본값
`E:\SPT 4.1`. `Directory.Build.targets`가 그 경로에 `Assembly-CSharp.dll`이 실제로
있는지 검사하므로 오타는 바로 잡힙니다.

클라이언트는 빌드 후 `$(SptRoot)\BepInEx\plugins\com.swiftxp.spt.showmethemoney\`로
바로 복사됩니다.

서브모듈은 `git submodule update --init` 한 번 필요합니다. `.gitmodules`는 절대 URL
(`swiftxp-hub/spt-common`)을 씁니다 — 상대 URL(`../spt-common.git`)은 **이 레포 자신의
원격 기준**으로 풀려서 포크에서는 `<포크주인>/spt-common`을 가리키게 되고, 그건 없습니다.

---

<26/09/02 상세 변경점>

- **퀵셀 플리 판매가 여전히 몇 초씩 걸리던 문제** — 판매 시간을 0으로 만들어도 그건
  "이 오퍼는 팔릴 때가 됐다"는 판정일 뿐이고, 실제 정산은 서버의 다음 플리 처리
  주기에 이뤄집니다. SPT 기본 주기가 길어서 즉시 판매인데도 눈에 띄게 멈칫하던 것.

  라이드 **밖** 처리 주기를 기본 10초로 단축 (`OutOfRaidCheckIntervalSeconds`,
  0으로 두면 SPT 기본값 유지). 라이드 **안** 주기는 일부러 안 건드렸습니다 — 같은
  처리 과정에서 플리 마켓 갱신도 같이 도는데 그게 싸지 않고, 어차피 퀵셀은 메뉴에서
  하는 거라 라이드 중 주기를 줄일 이유가 없습니다.

---

<26/08/31 상세 변경점>

**기능**

- **플리 퀵셀 즉시 정산** — 퀵셀의 플리 경로는 실제 오퍼를 등록하는 방식이라, SPT의
  래그페어 시뮬레이션이 "팔렸다"고 판정할 때까지 그대로 대기하고 있었음. 판매 확률
  100% + 판매 시간 0으로 설정해서 다음 처리 주기에 바로 완료되게 함.

  아이템·돈을 직접 옮기지 않고 **SPT 자체 판매 시뮬레이션을 조정하는 방식**을 택한
  이유: 직접 전송은 커스텀 라우트가 필요한데, 클라이언트는 자기 아이템 이벤트 라우트로
  돌아온 인벤토리 변경만 반영합니다. 그래서 서버는 아이템을 지웠는데 클라이언트엔
  그대로 남아있는 상태가 되고, 둘을 수동으로 맞춰줘야 합니다. 바닐라 경로를 그대로
  타면 클라이언트·프로필·판매세가 알아서 맞습니다.

  알아둘 점 둘: ① 돈은 완료된 플리 판매가 원래 그렇듯 **메신저로** 들어옵니다.
  ② 서버는 퀵셀과 직접 등록한 오퍼를 구분할 수 없어서(둘 다 같은 래그페어 요청),
  **플레이어가 등록한 모든 오퍼에 적용**됩니다. 수수료는 그대로 둬서 즉시 판매해도
  기다렸을 때와 정확히 같은 금액이 남습니다.

  둘 다 서버 모드 옆에 첫 실행 시 생성되는 `instant-flea-sell.json`에서 끌 수 있습니다.

**성능**

- **아이템에 마우스 올릴 때마다 상인 가격을 처음부터 다시 계산하던 문제** — 프로파일러
  기준 `SimpleTooltipShowPatch.PatchPrefix`가 **호출당 19.2ms, 프레임 타임의 12.2%**.
  비용은 전부 `TraderPriceService`에 있었음: 상인마다 호버한 아이템을 깊은 복사하고
  가격을 두 번씩 조회 → 상인 8명이면 복사 8회 + 조회 16회. 게다가
  `ShowWeaponModsPriceInformation`이 무기 부착물마다 같은 걸 또 돌려서, 풀 개조 무기
  하나 호버하면 복사가 수백 번.

  동작 차이 없이 세 가지 수정:
  - 상인별이 아니라 **아이템당 1회만 복사**. 단일 수량 아이템은 상인과 무관하므로,
    스택이 아닌 아이템은 아예 복사가 필요 없고 총액 = 단가라 두 번째 조회도 제거
  - 아이템별 최고가 견적을 **10초 캐시**. `TradePrice`가 아니라 원시 값으로 저장
    (`TradePrice`는 호버할 때마다 새로 만들어지는 `TradeItem` 인스턴스에 묶여 있음).
    키는 아이템 id + 스택 수 + `RoublesOnly` 설정이고, 상인 데이터 재구성이나 제외
    상인 목록 변경 시 비워지므로 설정·평판 변경 뒤에 가격이 낡을 일은 없음
  - 툴팁 필터의 `Localized()` 조회 3건을 매번이 아니라 **1회만** 수행

- **위 수정의 후속 버그 — 빈 로컬라이제이션 값 캐시 금지.** 로컬라이제이션 키를
  기억하게 만들었는데, 빈 문자열이 캐시되면 영구적으로 해롭습니다:
  `string.Contains("")`는 항상 true라서, 빈 값이 캐시되는 순간 **모든 툴팁이 보험/
  체크마크 툴팁으로 분류되어 그 세션 내내 가격 정보가 전부 사라집니다.** 첫 툴팁이
  뜰 때 로컬라이제이션이 로드되어 있다는 보장이 없으므로, 해석 안 된 키는 저장하지
  않고 다시 시도하도록 수정.

**호환성**

- **Quick Sell을 같이 쓰면 인벤토리 클릭이 전부 먹통이 되던 문제** — Quick Sell 2.3.0은
  "Show Me The Money 2.7.0 대응으로 재컴파일됨"으로 배포되어 있고, 판매 후
  `PluginContextDataHolder.SetHoveredItem(null)`을 호출합니다. 그런데 이 소스 트리에서
  해당 홀더는 `Contexts.Holders.PluginContextHolder`에 있어서, 여기서 빌드한 DLL에는
  Quick Sell이 링크된 이름이 존재하지 않습니다.

  증상이 "타입 없음"과 전혀 안 닮아서 찾기 어려웠음: Quick Sell의 `GridItemView.OnClick`
  프리픽스 해석이 실패하고, 예외가 원래 `OnClick`이 돌기 전에 빠져나가서 **인벤토리
  클릭이 전부 무반응**(아이템 검사도, 컨테이너 열기도 안 됨). Quick Sell을 지우면
  "고쳐지니까" Quick Sell 버그처럼 보였던 것.

  옛 이름을 현재 홀더로 포워딩해서 배포판 Quick Sell DLL이 그대로 작동하게 함.
  Quick Sell이 실제로 쓰는 `SetHoveredItem` 하나만 포워딩합니다.

- **모드 버전 선언 (2.7.0)** — 버전이 레포 어디에도 없어서 로컬 빌드가 MSBuild 기본값
  1.0.0을 받았음. 이 값이 BepInEx 플러그인 버전(`MyPluginInfo.PLUGIN_VERSION`)과 서버
  모드 버전(`AppMetadata.Version`)이 되기 때문에, 직접 빌드한 DLL에 대해 Quick Sell이
  로드를 거부했음: `missing dependencies: com.swiftxp.spt.showmethemoney (v2.6.0 or
  newer)`.

  README가 현재 소스 버전으로 명시한 2.7.0으로 설정. 클라이언트와 서버가 어긋나지
  않도록 프로젝트별이 아니라 `Directory.Build.props`에 선언했고, 릴리스용으로
  `-p:Version=...` 오버라이드는 열어뒀습니다.

**빌드**

- **참조 경로 하드코딩 제거** — 참조가 레포 3단계 위의 `spt4-shared-dlls` 폴더를
  가리키고 있었고, 이건 수동으로 채워야 하는 폴더입니다. 새로 클론하면 당연히 없고,
  없으면 **모든 참조가 조용히 실패해서 빌드가 안 됩니다.**

  대신 SPT 설치 폴더에서 가져오도록 변경. `SPTPath` 속성(기본값 `E:\SPT 4.0.10`,
  `-p:SPTPath=...`로 오버라이드 가능)을 통해 설치본이 원래 어셈블리를 두는 세 폴더
  (`EscapeFromTarkov_Data\Managed`, `BepInEx\core`, `BepInEx\plugins\spt`)를 참조합니다.
  빌드 후 플러그인을 그 설치본 plugins 폴더로 자동 복사도 추가(경로가 없으면 건너뛰므로
  다른 환경에서도 빌드는 성공).

- **서브모듈 URL SSH → HTTPS** — `Sources/Common` 서브모듈이
  `git@github.com:swiftxp-hub/spt-common.git`로 되어 있어서 SSH 키가 없으면 클론
  자체가 실패했음(비주얼 스튜디오에서 서브모듈 단계에 `Could not read from remote
  repository`). 공개 레포라 HTTPS로는 자격 증명 없이 접근됩니다.

- `Assets/toggle-tax-sample.gif` 삭제 (레포 용량 정리)

---

<26/09/07 상세 변경점>

- **SPT 4.1로 포팅.** mattpsvreis의 4.1.2 포팅을 머지로 받고, 이 포크의 수정들을 4.1
  API 위에 다시 얹었습니다. 충돌 6개(`.gitmodules`, `Directory.Build.props`, `README`,
  `TraderPriceService`, 클라이언트 csproj, `ShowMeTheMoneyMod`) 처리 내역:

  - `TraderPriceService` — 상인 견적 캐시를 4.1 타입 위에 다시 씀. `TraderClass` →
    `Trader`, `TraderClass.GStruct300` → `Trader.ItemPrice`, `item.CloneItem()` →
    `item.CloneForPricing()` (4.1의 `CloneItem`은 `IDatabaseIdGenerator`를 요구해서
    업스트림이 `ItemCloneUtility`를 새로 넣었습니다). 환율 조회는 업스트림이 추가한
    `trader.CurrencyCourses` 우선 조회를 그대로 받고 `GetSupplyData()`를 폴백으로 둠.
    이 포크 고유 최적화(트레이더 루프 **밖**에서 단일 수량 클론 1회, 스택이 아니면
    클론도 두 번째 조회도 생략)는 유지

  - `InstantFleaSellService` — 4.1은 `RagfairConfig`를 생성자로 바로 주입해줍니다.
    폐기된 `ConfigServer.GetConfig<T>()` 경로와 그것 때문에 달아뒀던 `CS0618` pragma가
    같이 없어졌습니다. `ISptLogger<T>`는 `SPTarkov.Server.Core.Models.Utils` →
    `SPTarkov.Common.Models.Logging`, 로드 순서 상수는 `OnLoadOrder.PreSptModLoader` →
    `OnLoadOrder.Preload`. 건드리는 설정 필드
    (`Sell.Chance.Base`/`MinSellChancePercent`/`MaxSellChancePercent`, `Sell.Time.Min/Max`,
    `Sell.Fees`, `RunIntervalSeconds`, `RunIntervalValues.OutOfRaid`)는 4.1 서버 소스에서
    전부 그대로인 것 확인

  - `ShowMeTheMoneyMod` — 진입점이 `IPreSptLoadModAsync.PreSptLoadAsync()` →
    `IOnLoad.OnLoadAsync(CancellationToken)`으로 바뀜. 서비스 호출은 그대로 유지

  - 빌드 설정 — 업스트림의 `SptRoot`/`SptManaged`/`SptBepInEx`/`SptRuntime` 체계와
    `Directory.Build.targets` 검증을 채택하고, 이 포크의 `Version` 선언(2.7.0)과
    설치 폴더 자동 복사를 그 위에 유지. 기본값만 `E:\SPT 4.1`로 추가해서 클론 직후에도
    `Directory.Build.props.user` 없이 빌드됩니다

  - `.gitmodules` — 업스트림이 상대 URL로 바꿨는데 포크에서는 안 풀립니다(위 빌드 항목
    참고). 절대 URL 유지

- `PluginContextDataHolder` 호환 shim은 그대로 둡니다. 이 레포의 Quick Sell도 4.1로
  같이 올리지만, 배포본 Quick Sell 2.3.0을 쓰는 경우가 남아 있어서 비용이 없는 쪽을
  택했습니다
