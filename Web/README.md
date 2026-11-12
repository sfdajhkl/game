# 아르비토 — 브라우저 RPG

Unity 없이 실행되는 HTML/CSS/JavaScript 게임. `web/`이 실제 게임 소스이며 `node build.mjs`로 `dist/`를 만듭니다. `node --test tests/game.test.mjs`로 핵심 게임 규칙과 저장을 검사합니다.

500개 사건, 10개 지역, 단계별 가중치, 수명·성장·장비·진형·주인공 조작/동료 자동 전투를 포함합니다. 삽화/폰트/종이 결은 기존 Unity 프로젝트와 같은 자료를 사용합니다. 파일은 `web/art/Events/<event-id>.png`로 개별 로드합니다.

저장: 현재 브라우저의 localStorage에 자동 저장. 설정에서 JSON 내보내기/가져오기 가능. 이전 Unity 저장의 배열형 flags/counters를 읽을 수 있지만 기기 간 자동 동기화하지 않습니다. 웹 저장은 별도 키 `arvito.browser.v1`을 사용합니다.

`web/catalog.mjs`는 GitHub 게임 저장소의 Unity catalog.json에서 내보낸 공유 콘텐츠입니다. 새 이벤트를 추가할 때 두 버전의 콘텐츠를 함께 갱신합니다. 새 사이트의 .openai/hosting.json은 기존 Site ID를 유지해야 합니다.

정식 플레이는 배포 URL에서 가능합니다. 현재 Sites 접근 범위는 소유자 전용입니다. 별도 공개 요청 없이 공개 범위를 넓히지 않습니다.

## 배포와 수정

실행 주소: https://arvito-travelers-book.choibumsoo.chatgpt.site

GitHub의 `Web/`을 웹 버전 수정 원본으로 관리합니다. Sites 소스 저장소에는 이 디렉터리 내용을 루트로 동기화합니다. `web/` 수정 후 테스트·빌드하고, 정확히 같은 소스를 Sites 저장소에 Push한 뒤 `.openai/hosting.json`과 `dist/`를 패키징하여 배포합니다. GitHub Push 자체는 자동 배포를 실행하지 않습니다. 비밀 토큰은 저장소에 넣지 않습니다.

Unity로 이식할 때 삽화와 사건 데이터는 재사용할 수 있지만 화면 및 JavaScript 게임 로직은 C# 쪽에 별도로 반영해야 합니다.
