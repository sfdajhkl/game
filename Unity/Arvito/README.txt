ARVITO / 아르비토 — Unity C# PC 프로젝트

이 압축 파일은 Unity 프로젝트입니다. 이미 빌드된 게임이나 설치 파일은 아닙니다.
현재 클라우드에는 Unity 라이선스가 없어 플레이어 빌드와 화면 실행 검증을 하지 못했습니다.

로그인된 Windows PC에서 게임 만들기
1. ZIP을 폴더에 모두 압축 해제합니다.
2. Unity Hub에서 Unity 6.3 LTS(권장 6000.3.28f1)가 설치되어 있는지 확인합니다.
   라이선스가 활성화되어 있어야 합니다. Personal 대상이면 무료 Personal을 사용할 수 있습니다.
3. 이 프로젝트가 Unity에서 열려 있다면 닫습니다.
4. Build-Playtest.cmd를 더블클릭합니다.
   GitHub에서 받은 game-main 폴더에도 실행 파일이 있으며 Unity/Arvito로 연결됩니다.
   첫 실행은 Unity 패키지 다운로드, 씬/한글 폰트 생성, 빌드 때문에 시간이 걸립니다.
5. 성공하면 Builds 폴더가 열립니다.
   Windows/Arvito.exe — 즉시 실행하는 게임
   Arvito-Windows.zip — 폴더 전체를 묶은 이동용 게임
   Arvito-Setup.exe — Windows 기본 IExpress로 만든 테스트 설치 프로그램
   unity-build.log — 실패 원인을 확인할 빌드 로그

Unity를 다른 폴더에 설치했다면 명령 프롬프트에서:
Build-Playtest.cmd -UnityPath "D:\Unity\6000.3.28f1\Editor\Unity.exe"

Unity 에디터에서 직접 실행
1. Unity Hub → Add → Add project from disk → 이 폴더 선택.
2. 첫 임포트가 끝나면 Arvito 메뉴가 생깁니다.
3. Arvito → Prepare Project, Open Game Scene, Play.
   Prepare Project는 보통 첫 임포트 시 자동 실행됩니다.
4. Arvito → Build Windows로도 게임을 만들 수 있습니다.

프로젝트가 이미 열려 있다는 오류가 나오면
- 이 프로젝트의 Unity 에디터 창에서 작업을 저장하고 창을 닫은 뒤 다시 실행합니다.
  Unity Hub는 켜두어도 됩니다.
- 에디터에서 바로 플레이하려면 Arvito → Open Game Scene → 상단 Play를 누릅니다.
- 최신 빌드 스크립트는 남아 있는 잠금 파일만으로 중단하지 않고 Unity가 실행 여부를 판단합니다.
- Unity 실행 전에 실패했다면 콘솔에 나온 오류를 확인합니다. 이때 빌드 로그가 없을 수 있습니다.

프로젝트 경로 오류가 나오면
- 실행 화면의 Project: 경로가 game-main/Unity/Arvito인지 확인합니다.
- 최신 스크립트는 저장소 루트와 Unity 프로젝트 폴더에서 실행하는 경우를 모두 처리합니다.
- Assets, Packages/manifest.json, ProjectSettings/ProjectVersion.txt가 없으면 실행 전에 안내합니다.
- Builds 폴더는 실제 Unity 프로젝트인 Unity/Arvito 아래에 생성됩니다.

조작
마우스로 선택. 1~9 선택지/기술, Enter 다음 사건/행동 확정.
E 여정, C 일행, I 소지품, J 연대기, Esc 설정, F11 전체 화면.
전투에서는 기술을 고르고 적 또는 아군 대상을 지정한 뒤 행동을 확정합니다.
모든 아군을 직접 조작합니다. 무기마다 사용할 수 있는 위치가 다릅니다.

저장
행동마다 자동 저장하며 전투 순서와 난수도 보관합니다.
Windows: %USERPROFILE%\AppData\LocalLow\Arvito\아르비토\arvito-save.json
새 생을 시작하면 이전 생은 같은 폴더의 Lives에 JSON으로 보관합니다.
설치 프로그램은 %LOCALAPPDATA%\Programs\Arvito에 설치하며 관리자 권한이 필요하지 않습니다.
제거해도 LocalLow의 저장 파일은 유지합니다.

현재 내용
16세 시작, 사건별 최소 1주, 노화 확률, 23개 사건, 3명 영입/최대 4명 일행,
7종 무기, 17개 기술, 위치 기반 턴제 전투, 영구 동료 사망, 후속 사건, 생애 결말.
삽화는 마을/숲/유적/야영/전투의 다섯 장만 사용합니다.
한글 폰트: Noto Serif KR (SIL OFL 1.1), Assets/Resources/Arvito/Fonts 참고.

검증 범위
C# 규칙 검사와 Unity 참조 라이브러리에 대한 컴파일을 수행했습니다.
Unity 에디터 임포트/실행, Windows 플레이, IExpress 설치/제거는 아직 실행 검증하지 못했습니다.
빌드 실패 시 Builds/unity-build.log를 보내 주면 그 오류를 기준으로 수정할 수 있습니다.
