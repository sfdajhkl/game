# 이벤트 삽화 제작 자료

카탈로그의 500개 이벤트를 각각의 192×128 PNG로 연결합니다. 그림은 아르비토를 위해 새로 생성한 삽화이며, 기존 작품 이미지를 사용하거나 사람이 직접 그렸다고 표시하지 않습니다.

## 현재 표현 기준

- 공통 양피지 팔레트: `#D5BE96`, `#AF8E62`, `#79573A`, `#3B2A20`.
- 재질별 두 단계의 평면 명암. 인물과 핵심 소품의 절제된 외곽선을 유지합니다. 그라데이션, 디더링, 종이 얼룩과 무작위 잔무늬를 피합니다.
- 작은 얼굴과 다양한 체형의 차분한 판타지 카툰. 눈의 세부 묘사보다 자세와 실루엣으로 감정을 전달합니다.
- 주인공과 임의의 동료를 몸·손·뒷모습·그림자·무기로 고정하지 않습니다. 사건에 필요한 고정 NPC, 적, 동물과 장소만 등장합니다.
- 첫 만남, 단서, 협상과 두 후일담의 구도를 구분합니다. 선택 전 그림에 구조나 협상 완료 결과를 미리 넣지 않습니다.
- 불투명한 실제 192×128 픽셀. 표시할 때 보간 없이 정수 배율로 확대합니다.

## 자료와 이력

`scenes.json`은 이벤트 ID·원문·파일 이름을 연결합니다. 현재 리터칭 자료는 `retouch/`에 있습니다.

- `plan.json`: 기준 커밋, 원본 PNG와 .meta의 SHA-256, 현재 스타일 버전, 네 장씩 생성하는 시트와 별도 보정 시트.
- `art-direction.txt`, `directions.json`: 공통 표현 기준과 500개 장면의 개별 구도.
- `prompts/`, `batches/`, `records/`: 실제 지시문, 생성 원본·후보 이미지 해시, 장면별 검토와 적용 기록.
- `repairs.json`: 발견한 시각적 문제와 수정 검토 결과. 완료 처리에는 원래 그림과 다른 수정본 해시가 필요합니다.
- `palette.png`: 네 색으로 변환하기 위한 4×1 기술용 색상표.

이전 4×4 시트의 `prompts/`, `sources.json`, `review.json`, `repairs.json`은 리터칭 전 제작 이력입니다. 현재 런타임 그림의 검토 근거는 `retouch/records/`입니다. 이전 그림은 `retouch/plan.json`에 기록된 기준 커밋에서도 복원할 수 있습니다.

## 편집과 검증

그림 생성과 수정은 이미지 생성 도구로 합니다. 스크립트는 자르기·Point 축소·팔레트 변환과 기록만 처리하며 장면을 새로 그리지 않습니다. Node.js, ImageMagick과 Python/Pillow가 필요합니다.

1. `retouch/directions.json`에서 구도를 정리하고 `node scripts/retouch-event-art.mjs prepare q001`로 참고 시트와 지시문을 만듭니다. 이미 검토한 시트는 별도 보정 시트를 추가하여 고칩니다.
2. 스타일 참고와 해당 시트를 직접 확인한 뒤 1536×1024의 2×2 시트로 생성합니다. 각 768×512 셀은 서로 다른 사건입니다.
3. `node scripts/retouch-event-art.mjs import q001 /path/to/generated.png`로 후보를 만듭니다. 각 셀을 192×128로 Point 축소하고 디더링 없이 공통 네 색에 대응시킵니다. 생성 원본은 보존합니다.
4. `previews/q001.png`에서 실제 픽셀 결과를 직접 검토합니다. 인물·선택 단계·줄의 연결·작은 소품을 확인한 뒤 `node scripts/retouch-event-art.mjs approve q001 '구체적인 검토 내용'`으로 게임 PNG에 적용합니다. 보정은 적용 후 `resolve <eventId> '수정 확인 내용'`으로 완료합니다.
5. `python3 scripts/check-event-art.py`로 전체 500개 연결과 현재 검토 기록을 검사합니다. 리터칭 검증도 실행되어 크기·불투명도·공통 팔레트·중복·검토 해시·유지된 .meta와 미해결 보정을 확인합니다. 작업 중에는 `python3 scripts/check-event-retouch.py --partial`을 사용합니다.
6. `python3 scripts/build-event-art-gallery.py --before /path/to/original/pngs`로 검색과 전후 비교가 가능한 오프라인 삽화집을 만듭니다. `--output`으로 목적지를 바꿀 수 있습니다. 전체 완료 전에는 `--partial`이 필요하며 검토 완료된 현재 버전만 포함합니다.

작업 폴더 기본값은 `/workspace/artifacts/Arvito-Event-Retouch`이며 `ARVITO_RETOUCH_WORK`로 변경할 수 있습니다. 원본은 `before/`, 생성 시트는 `sources/`, 후보와 검토용 확대본은 `candidates/`, `previews/`에 보존합니다. 기존 기준 파일이 있으므로 `init`을 다시 실행하지 않습니다. 검증 스크립트는 그림의 의미를 자동 판단하지 않으므로 직접 보는 검토를 생략할 수 없습니다.

## Unity 연결과 UI 예시

게임 이미지는 `Unity/Arvito/Assets/Resources/Arvito/Art/Events/<eventId>.png`입니다. 같은 파일 이름과 기존 .meta/GUID를 유지합니다. `EventArt`가 현재 사건의 그림을 지연 로드하고, `EventArtImporter`가 Point·무압축·밉맵 없음·NPOT 유지 설정을 적용합니다. `PixelArtImage`는 전체 3:2 그림을 정수 배율로 표시합니다.

`Design/ParchmentEventUI/`에는 실제 사건 ‘인장을 삼킨 상자’를 사용한 양피지 이벤트 화면 디자인 예시가 있습니다. HTML, PC·모바일 미리보기와 검증 기록을 포함합니다. UI 디자인 예시이며 Unity 런타임 UI 구현과는 별개입니다.
