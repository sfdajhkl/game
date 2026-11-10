# 아르비토 · ARVITO

**Unity 6.3 LTS / C# / Windows PC**용 텍스트 로그라이크입니다. 열여섯에 시작한 인물이 사건을 선택하고, 동료와 턴제 전투를 치르며, 사망하거나 수명이 다할 때까지 살아가는 게임입니다.

현재 프로젝트: [`Unity/Arvito`](Unity/Arvito). 이전 Android/Web 시제품은 `Legacy/AndroidPrototype`에 보관했습니다.

## 현재 제공 상태

게임 규칙과 네이티브 uGUI 화면, 자동 저장, Unity 에디터 설정 및 Windows 빌드/설치 패키징 스크립트를 작성했습니다. **현재 클라우드에 활성 Unity 라이선스가 없어 에디터 실행과 Windows EXE 빌드는 완료하지 못했습니다.** 소스 ZIP은 설치 파일이 아닙니다.

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

에디터에서는 Unity Hub로 프로젝트를 추가하고 `Arvito > Prepare Project`, `Open Game Scene`, Play 순서로 실행합니다. 처음 열면 준비 작업을 자동으로 시도합니다. `Arvito > Build Windows` 메뉴도 제공합니다.

## 게임 규칙

- 23개 사건과 후속 사건: 동료 영입, 아이 구조와 재회, 무기 획득, 유적 단서, 파수꾼, 용, 수련, 귀향, 후진 양성, 노년 등.
- 선택마다 최소 1주, 긴 사건은 수년. 나이는 16세부터 시작하며 52주가 1년입니다.
- 주인공의 고정 직업은 없습니다. 7종 무기와 직접 배운 기술로 전투 방식이 달라집니다. 17개 기술, 성장 단계, 무기 숙련을 구현했습니다.
- 주인공 포함 최대 4명, 영입 가능한 동료 3명. 모든 아군의 기술과 대상을 직접 선택합니다.
- 속도 순서, 전열/후열, 공격 가능한 위치, 출혈, 기절, 방어, 기력, 회복약, 긴장, 자리 교대, 퇴각.
- 주인공 사망은 한 생의 끝입니다. 동료 사망은 해당 생에 영구적입니다. 긴장 100은 한 차례 행동 불능 후 65로 감소합니다.
- 선택 플래그가 이후 사건과 생애 별칭에 반영됩니다. 최종 적을 이겨도 삶은 계속됩니다.
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

`Assets/Resources/Arvito/Art`에 마을·숲·유적·야영·전투 **다섯 장**의 도트 기반 시안이 있습니다. 이전에 생성한 시안을 재사용했으며 새 장면이나 인물 삽화를 추가하지 않았습니다. Unity 임포트 시 Point 필터와 무압축을 적용합니다.

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
