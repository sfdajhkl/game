# 아르비토 · ARVITO

**Unity 6.3 LTS / C# / Windows PC**용 텍스트 로그라이크입니다. 열여섯에 시작한 인물이 사건을 선택하고, 동료와 턴제 전투를 치르며, 사망하거나 수명이 다할 때까지 살아가는 게임입니다.

현재 프로젝트: [`Unity/Arvito`](Unity/Arvito). 이전 Android/Web 시제품은 `Legacy/AndroidPrototype`에 보관했습니다.

## 현재 제공 상태

게임 규칙과 네이티브 uGUI 화면, 자동 저장, Unity 에디터 설정 및 Windows 빌드/설치 패키징 스크립트를 작성했습니다. **현재 클라우드에 활성 Unity 라이선스가 없어 에디터 실행과 Windows EXE 빌드는 완료하지 못했습니다.** 소스 ZIP은 설치 파일이 아닙니다.

## Unity에서 바로 플레이 (Windows)

1. 프로젝트를 모두 압축 해제하고 Unity Hub에서 **Unity 6.3 LTS (권장 6000.3.28f1)**와 활성 라이선스를 확인합니다. 에디터 플레이에는 Windows Build Support 모듈이 필요하지 않습니다.
2. 저장소 루트 또는 `Unity/Arvito`의 **`Play-In-Unity.cmd`**를 실행합니다. 이 프로젝트가 이미 Unity에서 열려 있다면 먼저 작업을 저장하고 닫습니다. Unity Hub는 켜 두어도 됩니다.
3. 필요한 패키지를 확인한 뒤 Unity가 열립니다. 게임 씬·한글 폰트를 준비한 뒤 **Play 모드에 자동 진입**합니다. 첫 실행은 패키지 다운로드와 에셋 임포트 시간이 필요합니다.

프로젝트가 정상적으로 열린 뒤에는 **Arvito > Play Game (F5)**으로 다시 실행할 수 있습니다. 실행 로그는 `Unity/Arvito/Builds/unity-play.log`에 남습니다. 별도 설치 경로는 `Play-In-Unity.cmd -UnityPath "D:\Unity\6000.3.28f1\Editor\Unity.exe"`로 지정합니다.

최신 main은 Input System **1.20.1**, URP **17.3.0**을 요청하며, GitHub를 사용하는 이 프로젝트에는 `com.unity.collab-proxy`가 필요하지 않아 제외했습니다. GitHub에서 Pull한 뒤 Unity Hub로 `Unity/Arvito`를 직접 열어도 됩니다. Unity가 변경된 요청을 해석하고 기존 PackageCache를 갱신할 때까지 기다린 다음 **Arvito > Play Game (F5)**을 실행하세요.

## 로그인된 Windows PC에서 플레이테스트 빌드

1. `Unity/Arvito` 폴더 또는 프로젝트 ZIP을 압축 해제합니다.
2. Unity Hub에서 **Unity 6.3 LTS**, 권장 **6000.3.28f1**, 라이선스 활성화를 확인합니다.
3. 이 프로젝트가 Unity에서 열려 있다면 닫고, 압축 푼 **`game-main/Build-Playtest.cmd`**를 실행합니다. `Unity/Arvito/Build-Playtest.cmd`로도 실행할 수 있습니다.
4. 처음에는 패키지를 다운로드하고 씬과 한글 글꼴을 생성합니다.
5. 성공 시 `Builds` 폴더에 다음 파일이 생깁니다.

| 파일 | 용도 |
| --- | --- |
| `Windows/Arvito.exe` | 바로 실행. 같은 폴더의 DLL과 Data 디렉터리도 필요합니다. |
| `Arvito-Windows.zip` | 게임 폴더 전체를 묶은 휴대용 버전 |
| `Arvito-Setup.exe` | Windows 기본 IExpress로 만드는 사용자별 설치 프로그램 |
| `unity-build.log` | Unity 빌드 오류 진단 |

다른 위치에 설치한 Unity는 `Build-Playtest.cmd -UnityPath "D:\Unity\6000.3.28f1\Editor\Unity.exe"`로 지정합니다. 설치는 `%LOCALAPPDATA%\Programs\Arvito`를 사용하고 관리자 권한을 요구하지 않습니다. Unity 빌드 실패를 감지하면 설치 파일을 만들지 않습니다.

실행 화면의 `Project:`는 `game-main/Unity/Arvito`여야 합니다. 빌드 스크립트는 `Assets`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`가 있는 폴더를 확인하고, 그 폴더를 작업 경로와 Unity의 `-projectPath`로 지정합니다. 저장소 루트의 `Tools`에 이전 수정 파일을 풀어둔 경우에도 하위 Unity 프로젝트를 찾습니다.

빌드 중에는 Unity 로그를 실시간으로 표시하고, 5초마다 경과 시간·최근 로그에서 확인한 작업 단계·새 로그가 없는 시간을 보여줍니다. ZIP과 설치 파일을 만들 때도 단계가 표시됩니다. 이미 실행 중인 빌드는 `Watch-Build.cmd`를 별도 창에서 실행해 확인할 수 있습니다. 로그를 찾지 못하면 파일 선택 창에서 `Arvito/Builds/unity-build.log`를 선택하세요. 보기 창을 닫아도 별도 창에서 실행 중인 빌드는 계속됩니다.

이전 버전의 프로젝트 사본을 실행하는 경우에는 Unity 6.3과 맞지 않는 템플릿 패키지를 자동 복구합니다. Input System은 `1.20.1`, URP는 `17.3.0`으로 맞추고 `com.unity.collab-proxy`를 제거합니다. 임시 빈 프로젝트에서 Unity `Client.AddAndRemove` API로 구성과 잠금을 생성한 뒤 원본에 적용합니다. 기존 파일은 `Builds/PackageBackup-*`에 보관합니다. 최신 main은 이미 호환 버전을 요청하므로 이 별도 복구를 건너뜁니다.

복구가 필요한 이전 사본의 로그는 `Builds/package-repair-create.log`, `Builds/package-repair.log`입니다. 사용자 PC에서 복구한 이후에는 같은 작업을 반복하지 않습니다. `Library/PackageCache`의 C# 파일을 직접 고칠 필요는 없습니다.

에디터에서는 `Play-In-Unity.cmd` 또는 `Arvito > Play Game (F5)`로 실행합니다. `Prepare Project`, `Open Game Scene`, `Build Windows` 메뉴도 제공합니다.

## 게임 규칙

- **500개 사건 장면, 1,317개 선택지**: 기존 사건 23개, 새 의뢰 95줄기의 분기 장면 475개, 공통 사건 2개입니다. 의뢰마다 조사·협상·각 경로의 후일담이 있으며 한 생에 모든 경로가 강제로 이어지지 않습니다.
- **첫 사건부터 무작위**입니다. 처음의 영입→늑대→장비 고정 순서와 후속 사건 65% 우선 추첨을 제거했습니다. 최근 12개 사건은 피하고, 반복한 사건의 가중치는 낮춥니다.
- **지역 10곳**: 브린 마을, 이끼 숲, 교역로, 강 유역, 항구, 산맥, 유적, 습지, 성채, 변경. 5개 초반 지역 중 출발지를 추첨하며, `다른 지역으로`에서 연결된 길로 이동합니다. 이동은 2주와 식량 1개(없으면 6골드)를 씁니다.
- 사건은 현재 지역, 초·중·후반, 선택 플래그, 경과 시간, 장비, 평판, 일행 수 조건을 사용합니다. 중반은 성장 단계 3·선택 18회·5년 경과 중 하나, 후반은 성장 단계 7·선택 65회·20년 경과 중 하나에 도달하면 시작합니다.
- 선택마다 최소 1주, 긴 사건은 수년. 나이는 16세부터 시작하며 52주가 1년입니다.
- 주인공의 고정 직업은 없습니다. 7종 무기와 직접 배운 기술로 전투 방식이 달라집니다. 기본 공격을 포함한 18개 기술, 성장 단계, 무기 숙련을 구현했습니다.
- 주인공 포함 최대 4명, 영입 가능한 동료 3명. **주인공만 직접 조작**합니다. 동료는 회복·긴장·기력·무기 위치를 판단해 자동 행동하며 직접 명령을 받을 수 없습니다.
- 전투는 **적이 정면에 나란히 보이는 RPG 화면**입니다. 적 11종과 아군 4명은 기존 카툰풍을 유지한 64×64 도트 그림으로 표시합니다. 캐릭터당 최대 16색을 쓰며 정수 배율로 확대합니다. 아래에는 일행 상태창·주인공 명령창·전투 기록이 있고, 아군 그림은 여정 사이드바와 일행 화면에도 표시됩니다.
- 속도 순서, 전열/후열, 공격 가능한 위치, 출혈, 기절, 방어, 기력, 회복약, 긴장, 자리 교대, 퇴각.
- 주인공 사망은 한 생의 끝입니다. 동료 사망은 해당 생에 영구적입니다. 긴장 100은 한 차례 행동 불능 후 65로 감소합니다.
- 선택 플래그가 이후 사건과 생애 별칭에 반영됩니다. 최종 적을 이겨도 삶은 계속됩니다.
- 아이와의 재회는 구조 완료 시점부터 최소 2년 뒤에 열립니다. 기존 저장은 연대기에서 구조 시점을 복원하며, 오래된 기록이 남아 있지 않으면 불러온 시점부터 2년을 기다립니다.
- 치명상을 입으면 같은 사건의 성장 보상으로 되살아나지 않습니다. 완전 회복 사건은 성장으로 증가한 최대 체력까지 회복합니다.
- 유대는 현재 서사 수치입니다. 영구 능력치 강화, 제작, 사운드, 대규모 사건 확장은 포함하지 않았습니다.

### 수명

16~39세에는 노화로 사망하지 않습니다. 새 생마다 체질을 0.8~1.2 사이에서 정하고, 40세부터 매주 다음 확률로 판정합니다.

```text
연간 위험 = min(0.98, 0.0015 × exp((나이 − 40) / 8) / 체질)
주간 위험 = 1 − (1 − 연간 위험)^(1/52)
```

긴 사건도 매주 판정합니다. 도중에 사망하면 사건 완료 보상을 받지 않습니다. 난수 상태를 저장해 이어하기가 결과를 다시 뽑지 않도록 했습니다.

## 화면과 저장

시작 화면, 여정, 일행/진형, 소지품, 연대기, 턴제 전투, 결과, 생애 결말, 야영 확인, 설정을 C# uGUI로 구성했습니다. TextMeshPro와 포함된 한글 폰트를 사용합니다. 마우스, 선택 단축키 1~9, Enter, E/C/I/J, Esc, F11을 지원합니다.

행동마다 JSON을 원자적으로 교체하고 이전 유효 저장을 백업합니다. 전투 순서, 상태 이상, 난수도 이어집니다. 새 생을 시작할 때 이전 상태를 `Lives` 폴더에 보관합니다.

Windows 저장 위치: `%USERPROFILE%\AppData\LocalLow\Arvito\아르비토`. 게임 제거 시에도 이 저장 폴더는 유지합니다.

## 삽화와 글꼴

`Assets/Resources/Arvito/Art`에 마을·숲·유적·야영·전투 배경 다섯 장과 캐릭터 15종의 아틀라스가 있습니다. 캐릭터는 64×64 실제 픽셀, 제한 팔레트, 완전 투명/불투명 알파를 사용합니다. 임포트 시 Point 필터와 무압축을 적용하고 화면에서는 정수 배율로 표시합니다. 편집 규격과 내보내기 방법은 `Content/ART.md`에 있습니다.

한글은 Noto Serif KR 정적 Regular TTF를 포함합니다. 라이선스는 `Assets/Resources/Arvito/Fonts/OFL-NotoSerifKR.txt`입니다. 첫 에디터 준비 단계에서 동적 TMP 폰트 에셋과 아틀라스를 생성합니다.

## 코드와 검증

| 위치 | 역할 |
| --- | --- |
| `Assets/Arvito/Core` | Unity 의존성이 없는 C# 규칙, 모델, 저장소 |
| `Assets/Arvito/Runtime` | uGUI, TextMeshPro, Input System 화면과 조작 |
| `Assets/Arvito/Editor/ProjectSetup.cs` | 실제 씬/폰트 생성, Windows 빌드 |
| `Assets/Resources/Arvito/catalog.json` | 사건, 장비, 기술, 적, 동료 데이터 |
| `Tools` | Windows 빌드, ZIP/EXE 패키징, 설치/제거 |
| `Tests/Arvito.CoreChecks` | 자원/판정/시간/전투/저장 C# 검사 |

규칙 검사는 .NET 8 SDK로 실행합니다.

```bash
dotnet run --project Tests/Arvito.CoreChecks -- Unity/Arvito/Assets/Resources/Arvito/catalog.json
# NuGet 접근 없이 같은 검사 실행:
python3 scripts/check-core.py --dotnet /path/to/dotnet
```

`scripts/check-unity-source.py`는 설치된 Unity의 실제 참조 DLL로 Player/Editor C# 코드를 정적으로 컴파일합니다. **이 검사는 에셋 임포트나 화면 렌더링 검증을 대신하지 않습니다.** 실행하지 못한 범위는 Unity 에디터 임포트/Play Mode, Windows 플레이어, IExpress 설치/제거입니다. 상세 기록은 `Verification/README.txt`를 참조하세요.

사건 원본, 분기 구조와 참고한 작품은 [`Content/README.md`](Content/README.md)에 정리했습니다. 사건을 수정한 뒤 `python3 scripts/export-story-content.py`로 런타임 JSON을 갱신하고 `python3 scripts/check-story-content.py`로 조건·참조·지역 연결을 검사합니다.
