# 사건 작성과 참고

현재 런타임은 **500개 사건 장면**입니다. 기존 23개 사건, 이 파일 옆 `quests.tsv`의 95개 의뢰마다 5개 분기 장면(475개), 지역 공통 사건 2개로 구성됩니다. 한 번의 의뢰에서 두 경로를 모두 보는 구조는 아니며, 선택에 따라 조사 또는 협상과 그 후일담이 열립니다.

`base-catalog.json`은 원래의 장비·기술·동료·사건 원본입니다. `quests.tsv`는 각 의뢰의 지역, 진행 단계, 유형, 제목, 상황, 단서, 두 가지 접근과 각각의 결과, 후일담을 직접 작성한 원본입니다. 제목만 바꾸는 장소별 조합으로 사건 수를 늘리지 않습니다.

`python3 scripts/export-story-content.py`로 Unity Resources의 `catalog.json`을 갱신합니다. `python3 scripts/check-story-content.py`는 출력 동기화, 500개 고유 제목/ID, 장비·기술·적 참조, 조건부 분기 도달 가능성, 단계별 지역 연결과 반복 사건의 고갈 여부를 검사합니다.

참고한 것은 다음 작품의 공개된 시스템 설명입니다. 사건 문장, 인물, 퀘스트 줄거리를 가져오지 않았습니다.

- [모험가 이야기 / Life in Adventure 공식 소개](https://play.google.com/store/apps/details?id=com.StudioWheel.Bard): 능력, 장비, 자산, 과거 선택이 사건과 결과에 영향을 주는 구조.
- [Roadwarden 공식 소개](https://moralanxietystudio.com/presskit/roadwarden): 지역 탐색, 대화 선택, 생존 자원과 시간 제약, 세계에 남는 선택의 영향.

아르비토의 새 의뢰는 마을·숲·교역로·강·항구·산맥·유적·습지·성채·변경을 배경으로 구조, 추리, 협상, 유물, 의식, 생존 등을 다룹니다. 분기는 강제 연결하지 않고 해당 지역의 조건부 가중 추첨에 들어갑니다.
