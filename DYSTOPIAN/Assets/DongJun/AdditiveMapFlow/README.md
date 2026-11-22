# Figma 맵 구조 테스트

`Assets/DongJun/Scenes/AddtiveScenes/Core.unity`를 열고 Play합니다.
맵은 같은 폴더의 `Stage_Test/`에 있습니다. 기존 40개 맵 씬을 수정했고, 분기점 `Junction1`만 추가했습니다.

## 조작

- 좌우 방향키: 통로를 따라 이동. 평면도에서 꺾이거나 휘는 곳에서도 같은 좌우 조작을 사용합니다.
- 위 방향키: 점프. 잠깐 유지하면 더 높이 뜁니다.
- E: 문 열기, 테스트 보스 클리어, 별도 입구 이용.
- 얇은 발판: 아래에서 통과하고 위에서 착지합니다. 끝에서 걸어 나가 내려가거나 번갈아 밟아 다시 올라갑니다.

## 흐름

- 로비 → 스테이지 1 → 보스 1 → 분기점 1 → 스테이지 2.
- 스테이지 2의 2번 구간은 올라가는 3층 구조, 4번 구간은 내려가는 구조입니다. 5번 구간의 문에서 E로 별도 보스방에 들어갑니다. 보스를 클리어하고 돌아오면 스테이지 3 방향의 장벽이 열립니다.
- 스테이지 3은 평면도의 곡선 통로를 따라 중앙 배경 오브젝트 주위를 돕니다. 보스 클리어 후 문에서 E로 분기점 2에 들어가고 바닥 입구를 통해 스테이지 4로 내려갑니다.
- 스테이지 4는 세로 통로이며 3번과 4번 구간 사이에는 옆으로 건너가는 다리가 있습니다. 보스 앞에서 E로 클리어하면 즉시 스테이지 1 보스방으로 돌아갑니다.
- 보스 1 방의 바닥 입구에서 E를 누르면 스테이지 5로 들어갑니다. 보스 4 클리어가 필요합니다. 5번 구간은 아래 오른쪽이 보스 5, 아래 왼쪽이 스테이지 6 입구입니다. 보스 5를 클리어한 뒤 되돌아와 왼쪽으로 나갑니다.
- 스테이지 6의 5번 구간과 보스방은 하나로 이어진 공간입니다. 보스 6 클리어 후 최종 연결 통로를 지나 보스 7로 갑니다.
- 보스 7 앞에서 E를 누르면 `TEST COMPLETE`가 표시됩니다.

보스는 테스트용 빨간 도형입니다. 클리어 상태는 Core가 유지하고 Play 종료 시 초기화합니다.
기존 `Stage7_EntryShaft` 씬은 이번 도면의 최종 연결 통로로 재사용했습니다.

## 카메라

- `PrototypeMap/CameraFrame`의 BoxCollider가 화면 경계입니다. 충돌 판정용이 아니므로 비활성화해 두었습니다.
- 가로 구간은 프레임의 높이에 맞춰 바닥·천장이 화면 위아래를 채우고, 가로로만 추적합니다.
- 세로 구간은 프레임의 너비에 맞춰 양쪽 벽이 화면 좌우를 채우고, 세로로만 추적합니다. `SectionTrigger > Vertical`로 방향을 지정합니다.
- 전체 맵에서 공통 최소 Size 6을 강제하던 처리를 제거했습니다. 카메라 배율은 구역 크기와 화면 비율로 계산합니다.
- 보스 1·2·3·5·6·7은 고정 화면, 보스 4는 세로 스크롤입니다. 고정 보스방은 전체 폭이 보이면서 화면 위아래가 채워지도록 16:9 기준으로 테스트 천장 높이를 맞췄습니다.
- `FixedCameraZone`에서도 가로 높이·세로 너비 맞춤 배율을 유지하며 지정된 위치를 잠급니다. 나가면 구역의 추적 카메라로 돌아갑니다.
- 구역 변경은 카메라 위치를 초기화하지 않고 부드럽게 이어집니다. `SnapToTarget`은 최초 배치와 텔레포트에서 사용합니다.
- 연결된 세로 구간 4-1~4-3, 4-4~4-5는 같은 높이 범위의 프레임을 사용해 경계마다 세로 추적이 멈추지 않습니다.
- 비동기 로딩의 메인 스레드 작업은 낮은 우선순위로 분산하고, 로딩이 끝나면 기존 우선순위로 복구합니다.


## 씬과 컴포넌트

기존 씬 인덱스 0~39는 유지했고 `Junction1`을 40에 추가했습니다.

| 인덱스 | 씬 |
| --- | --- |
| 0 | Lobby_Test |
| 1~5 / 6 | 스테이지 1 / 보스 1. 첫 구역은 기존 Stage_Test 이름 유지 |
| 7~11 / 12 | 스테이지 2 / 보스 2 |
| 13~17 / 18 / 19 | 스테이지 3 / 보스 3 / 분기점 2 |
| 20~24 / 25 | 스테이지 4 / 보스 4 |
| 26~30 / 31 | 스테이지 5 / 보스 5 |
| 32~36 / 37 | 스테이지 6 / 보스 6 |
| 38 / 39 | 최종 연결 통로 / 보스 7 |
| 40 | 분기점 1 |

- `Core/MapLoader`: 씬 경로 목록과 보스 클리어 상태를 유지합니다. 현재·이전·다음 씬을 로드하며 분기나 공동 공간에 필요한 씬은 Additional Neighbors로 지정합니다.
- `PrototypeMap/SectionTrigger`: 구역 콜라이더와 이전·다음 인덱스, 이동 평면, 카메라 모드를 지정합니다. 연결 없는 인덱스는 -1입니다. 구역 경계가 겹칠 때는 떠난 구역의 콜라이더를 벗어나기 전까지 그 구역을 다시 활성화하지 않습니다.
- `LevelMovementPlane`: 해당 통로의 방향 또는 Path Points를 따라 좌우 이동을 변환합니다. 플레이어 루트와 카메라 방향도 통로에 맞춥니다.
- `LevelCameraZone`: 고정 카메라의 콜라이더와 시점을 지정합니다. 겹친 경우 Priority가 높은 구역을 사용합니다.
- `TestMapInteraction`: 문과 보스, 텔레포트입니다. Required Boss는 이용 조건, Clears Boss는 클리어할 보스, Destination은 도착 구역·월드 위치입니다.
- `TestOneWayPlatform`: 발판의 단방향 충돌 처리입니다.

맵 생성 도구나 경로 동기화 도구는 프로젝트에 추가하지 않았습니다. 플레이어와 VFX는 Additive 맵에서만 이동 평면 변환을 적용하며, 해당 컴포넌트가 없는 다른 씬은 기존 XY 이동을 유지합니다.

검사 결과와 배치 기록은 `Library/AdditiveMapFlowChecks/figma-*`에 저장합니다.
수정 전 씬·스크립트 백업은 `Library/AdditiveMapFlowBackups/Figma_20261122_034840`에 있습니다.

확인한 항목: 41개 구역의 로딩·착지, 카메라 16개 항목, 곡선·직각·다리 등 연결부 7개, 문과 보스 분기 12개, 발판 상승 경로 10개(물리 점프 97회). 보스 1 방은 직접 올라오는 복귀를 위해 스테이지 5 첫 구역도 함께 유지합니다.

최종 카메라 검증: 41개 구역의 화면 경계, 9개 실제 연결 이동(4스테이지 입구·스테이지 6 연속 입구 포함), 16개 고정·추적 카메라 동작, 12개 문·보스 분기를 통과했습니다. 로비부터 보스 1 방까지 직접 걸어 진입했습니다. 해당 에디터 이동 측정에서 99% 프레임은 약 8ms 이내, 최대 약 39ms였습니다. 보스 1 입구를 막던 5-1 벽 돌출을 제거했고, 5 보스 클리어 후 6 입구는 자동 텔레포트 없이 개방되어 걸어서 이어집니다.

## 외부 스크립트에서 카메라 제어

Core의 `SideViewCameraFollow`를 다른 스크립트의 SerializeField에 연결합니다. 실제 카메라 Transform은 이 컨트롤러가 갱신하고, 연출·보스·맵 스크립트는 공개 메서드를 호출합니다.

```csharp
[SerializeField] private SideViewCameraFollow cameraController;
[SerializeField] private Transform bossView;

private IEnumerator BossIntro()
{
    cameraController.StopFollow();
    yield return cameraController.MoveTo(bossView, 1.5f, 6f);
    yield return new WaitForSecondsRealtime(0.5f);
    cameraController.ResumeFollow();
}
```

`bossView`에는 카메라를 놓을 위치·회전으로 배치한 Empty Transform을 넣습니다. `MoveTo`는 Coroutine을 반환하므로 `yield return`으로 이동 완료를 기다릴 수 있습니다. 지정한 위치·회전은 호출 시점에 복사합니다. 수동 이동은 unscaled time을 사용하므로 Time.timeScale이 0인 연출에서도 진행합니다.

| 메서드 | 동작 |
| --- | --- |
| `StopFollow()` | 현재 위치·회전에서 플레이어 추적을 중단 |
| `ResumeFollow(bool snap = false)` | 현재 구역의 자동 카메라 규칙으로 복귀. true면 즉시 배치 |
| `FixAt(Transform view, float size = -1, bool instant = false)` | 지정 시점으로 0.6초 이동한 뒤 고정. instant=true면 즉시 고정 |
| `MoveTo(Transform view, float duration = 0.8f, float size = -1, bool resumeAfter = false)` | 지정 시간 동안 이동·회전·줌. 기본은 도착 후 고정, resumeAfter=true면 자동 규칙 복귀 |
| `SetTarget(Transform target)` | 추적 대상의 루트 Transform 교체 |
| `SetZoom(float size, bool instant = false)` | 줌 지정. 자동 추적에서는 구역 경계보다 넓어지는 줌아웃을 제한 |
| `ResetZoom()` | 구역 크기에 맞춘 자동 배율 복귀 |
| `SetRotationSpeed(float degreesPerSecond)` | 자동 회전 속도. 기본 60도/초, 90도 회전은 약 1.5초 |
| `SetSmoothing(float seconds)` | 추적 위치·배율의 감쇠 시간 |
| `SetShakeOffset(Vector3 offset)` | 화면 흔들림 오프셋 지정 |
| `SnapToTarget()` | 현재 제어 규칙에 따른 위치로 즉시 배치 |
| `SetSectionView(AdditiveMapSection section)` | 자동 카메라 구역 교체 |
| `EnterZone(LevelCameraZone zone)` / `ExitZone(...)` | 고정 구역 진입·이탈 알림 |

`FixAt`과 `MoveTo`에는 Vector3 위치와 Quaternion 회전을 직접 받는 오버로드도 있습니다. size=-1은 현재 배율을 유지합니다. `MoveTo`의 회전은 지정 duration에 맞추며, 자동 추적의 회전 속도와 독립적입니다.

수동 제어 중에는 구역 콜라이더와 씬 전환이 연출 시점을 덮어쓰지 않습니다. `ResumeFollow`는 현재 구역 규칙으로 복귀하므로 보스방에서는 보스방의 고정 규칙을 다시 적용합니다. `IsFollowing`, `HasManualOverride`, `IsTransitioning`으로 상태를 확인할 수 있습니다.

## 스테이지 경계 카메라

- 같은 스테이지의 서브 구역에는 같은 `Camera Group`을 지정합니다. 보스방·분기점은 별도 그룹입니다.
- 가로 스테이지 첫 구역은 `Clamp Camera Left`, 마지막 구역은 `Clamp Camera Right`를 켭니다. 내부 서브 구역의 연결은 계속 추적합니다.
- 끝에 도착하면 카메라만 벽에서 멈춥니다. 통행과 보스 클리어 조건은 기존 게이트가 관리합니다.
- 플레이어가 다음 그룹에 실제로 진입하면 입구 쪽 화면으로 이동한 후 추적을 재개합니다. 경계 이동 중에도 플레이어 조작은 유지합니다.
- 분기점 1과 5스테이지의 회전은 위치 감쇠와 별도의 `Rotation Speed`로 조절합니다. 실제 회전 각도에 맞춰 카메라 위치도 같은 초점을 중심으로 돌립니다.

## 방향을 알아보기 위한 테스트 지형

- 3스테이지 원형 구간: W/N/E 면 색과 글자가 다른 중앙 타워, 비대칭 주변 기둥, 곡선 난간과 구간 표식을 배치했습니다.
- 4-3 → 4-4: 진입 계단 → 첫 코너 → 다리 → 도착 코너로 이어지는 실제 데크를 만들었습니다. 도착 전 평면 전환을 늦추고 난간·뒷벽·조명·도착 표식을 추가했습니다. 왕복은 실제 경로로 진행하므로 이전 E 복귀 텔레포트는 제거했습니다.
- 5스테이지: 구간별 벽 색, Lit 재질, 구조 기둥, 방향 표식, 조명으로 회전 시 면과 깊이의 차이가 드러나도록 했습니다.
- 관련 재질은 `Assets/DongJun/AdditiveMapFlow/Materials/Readability/`에 있습니다. 새 씬이나 상시 맵 생성 도구는 추가하지 않았습니다.

이번 수정 검증: 외부 제어 API(일시정지 중 이동 포함), 2-5→3-1 경계 정지·진입 이동·추적 재개, 분기점 1 회전 속도, 41개 구역 프레이밍, 고정 카메라 16개 항목, 3스테이지 곡선 실이동, 4-3→4-4 계단·다리 실이동, 5스테이지 회전 연결 3곳, 문·보스 진행 12개 항목을 통과했습니다.
수정 직전 백업: `Library/AdditiveMapFlowBackups/CameraApiReadability_20261122_161346`.
