아르비토 — 양피지 이벤트 화면 디자인 시안

index.html을 열면 화면과 선택지 상태를 확인할 수 있습니다.
이 자료는 디자인 예시이며 Unity 게임의 UI를 교체하거나 세이브를 변경하지 않습니다.
이벤트 본문·선택지·소요 시간은 현재 게임의 q080_offer에서 가져왔습니다.
체력, 스트레스, 식량, 소지금, 날짜는 예시 값입니다.

미술 방향: 양피지 / 세피아 / 짙은 갈색, 재질별 기본색과 그림자색 두 단계,
절제된 외곽선 유지. 그림에는 종이 얼룩·디더링·입자 무늬를 추가하지 않습니다.
삽화 원본: assets/event.png, 192×128, 불투명, 4색.
큰 화면에서 3배(576×384), 중간 화면에서 2배, 작은 화면에서 1배로 표시합니다.
제목·본문은 아산시 이순신체 Regular를 사용합니다. Unity는 아산시 공식 TTF 원본, 브라우저 시안은 같은 글자 윤곽의 WOFF를 사용합니다.
공식 TTF의 kern 테이블을 Chromium이 거부하므로 시안에는 기존 후보 미리보기의 WOFF를 포함했습니다.
WOFF 출처: https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_two@1.0/YiSunShinRegular.woff
정보·버튼은 기존 서체를 유지합니다. 사용 안내: assets/LICENSE-YiSunShin.txt. 사람의 손으로 그렸다고 표시하지 않는 생성 삽화입니다.

기존 Unity 프로젝트는 Canvas/uGUI 및 TextMeshPro 기반입니다.
이 시안의 계층은 Header → Status → Event header → Illustration / Story & Choices → Footer입니다.
