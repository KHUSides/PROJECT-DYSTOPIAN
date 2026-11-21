# Additive 맵 이동

Core를 열고 Play합니다. 좌우 방향키로 걷고 위 방향키로 점프합니다.

- Core의 MapLoader > Scenes 배열: 로비, 서브스테이지 1~5, 보스방 순서의 현재 씬 경로입니다.
- 맵 씬은 Assets/DongJun/Scenes/AddtiveScenes/Stage_Test/에 있습니다.
- 각 맵의 AdditiveMapSection/SectionTrigger에 플레이어가 들어오면 현재 구역이 바뀝니다.
- SectionTrigger의 BoxCollider를 Scene 뷰에서 이동하거나 크기를 바꾸어 감지 범위를 편집합니다. 감지 영역이 서로 겹치지 않도록 배치합니다.
- AdditiveMapSection의 Route Index는 Scenes 배열의 인덱스입니다. 로비 0, 서브스테이지 1~5는 1~5, 보스방 6입니다.
- 현재 구역과 이전·다음 구역을 유지하고 나머지는 언로드합니다. 다음 바닥이 로드될 때까지 좌우 로딩 게이트가 진입을 막습니다.
- Player와 Camera는 Core에 유지됩니다. 구역 전환에 X 좌표 비교나 순간이동을 사용하지 않습니다.
- 씬 생성 메뉴, 경로 자동 동기화, 상태 표시 UI와 자동 추락 복구는 제거했습니다.
- 씬 위치를 바꾸면 Scenes 배열의 경로와 Build Settings를 직접 수정합니다.

런타임 코드는 Scripts/AdditiveMapFlow.cs와 Scripts/AdditiveMapSection.cs 두 파일입니다.

검증 완료: 콜라이더 진입으로 모든 구역 왕복, 바닥 및 외곽 게이트 충돌, 인접 씬 정리, 동일 플레이어/카메라 유지, Core 단독 시작.
