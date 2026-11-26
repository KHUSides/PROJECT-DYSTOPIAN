# Additive 맵과 게임플로우 사용법

`Assets/DongJun/Scenes/AddtiveScenes/Core.unity`를 열고 Play합니다. Core는 유지하고 현재·이전·다음 구역과 Additional Neighbors만 Additive로 유지합니다.

## Core 배치

```text
Core
├─ MapLoader
│  └─ AdditiveMapFlow
├─ CameraManager
│  ├─ SideViewCameraFollow
│  └─ LevelMovementPlane
├─ Main Camera
│  └─ Camera / AudioListener / URP 설정
└─ Player
   └─ 기존 PlayerCameraShake와 플레이·전투·VFX 컴포넌트
```

MapLoader의 Player, Movement Plane, Camera Controller를 각각 연결합니다. CameraManager의 Controlled Camera는 Main Camera, Target과 이동 평면의 Actor는 Player입니다. 카메라 제어는 CameraManager의 Transform 대신 실제 Controlled Camera를 갱신합니다.

## 블록처럼 순서대로 연결하기

큰 게임플로우는 `IEnumerator`로 만들고, 완료를 기다릴 행동에 `yield return`을 붙입니다.

```csharp
yield return maps.WaitUntilReady();
yield return maps.TeleportTo(sceneIndex, worldPosition);
yield return cameraController.MoveTo(introView, 1.2f, 6f);
yield return new WaitForSecondsRealtime(0.5f);
cameraController.ResumeFollow();
yield return maps.WaitForBossClear(1);
```

`yield return`이 있으면 해당 행동이나 조건이 끝난 뒤 다음 줄로 갑니다. Wait 계열은 IEnumerator이므로 yield return으로 실행해야 합니다. `TeleportTo`, `MoveTo`, `FixAt`은 호출 즉시 시작하므로 yield를 생략하면 다음 줄도 바로 실행합니다. 일반 void 함수에서 시작하려면 `StartCoroutine(MainFlow())`를 사용합니다. `IEnumerator Start()`는 Unity가 자동으로 실행합니다.

## AdditiveMapFlow: 게임플로우에서 호출할 함수

| 호출 | 반환 | 의미 |
| --- | --- | --- |
| `WaitUntilReady()` | IEnumerator | 최초 맵 로딩과 이동·카메라 초기화 완료 대기 |
| `TeleportTo(int index, Vector3 worldPosition)` | Coroutine | 구역과 인접 씬 준비 후 플레이어 이동. 다른 로딩·이동 중이면 기다렸다 진행 |
| `WaitForSection(int index)` | IEnumerator | 해당 구역이 현재 구역이고 스트리밍·이동도 끝날 때까지 대기 |
| `WaitForBossClear(int stage)` | IEnumerator | 해당 보스 클리어 상태가 기록될 때까지 대기 |
| `ClearBoss(int stage)` | void | 보스 클리어 기록 및 조건부 장벽 갱신. 중복 호출 가능 |
| `HasClearedBoss(int stage)` | bool | 클리어 여부 확인 |

씬 index는 0부터, 보스 stage는 1부터입니다. **보스 1 방의 씬 index는 6이고, 보스 클리어 번호는 1입니다.** 이동 위치는 월드 좌표입니다.

`WaitForSection`은 현재 구역을 기다리므로 그 구역에 도착하기 전에 흐름에 배치합니다. `WaitForBossClear`는 기록된 상태를 확인하므로 이미 클리어했다면 바로 진행합니다. 보스를 생성하거나 전투를 시작하는 함수는 아닙니다.

`Player`, `MovementPlane`, `CurrentIndex`, `IsReady`, `IsBusy`는 조회용입니다. 순차 진행에서는 IsBusy를 직접 반복 검사하는 대신 반환된 이동 코루틴을 기다리는 것이 명확합니다. 여러 이동을 정확한 순서로 처리하려면 각 호출에 yield를 붙입니다.

잘못된 씬 index와 0 이하의 보스 번호는 예외로 알립니다. 초기 설정이 잘못되면 Console에 원인을 출력하고 준비 대기에서도 실패를 알립니다.

## SideViewCameraFollow: 카메라 행동 블록

| 호출 | 반환 | 의미 |
| --- | --- | --- |
| `StopFollow()` | void | 현재 위치·회전·배율에 정지 |
| `FixAt(Transform view, float size = -1, bool instant = false)` | Coroutine | 기본 0.6초 이동 후 고정. instant=true는 즉시 배치 |
| `MoveTo(Transform view, float duration = 0.8f, float size = -1, bool resumeAfter = false)` | Coroutine | 지정 시간 동안 위치·회전·줌 연출 |
| `ResumeFollow(bool snap = false)` | void | 현재 구역의 자동 카메라 규칙으로 복귀. true면 즉시 배치 |
| `SnapToTarget()` | void | 현재 제어 규칙에 맞는 시점으로 즉시 배치 |
| `SetTarget(Transform value)` | void | 자동 추적 대상 변경 |
| `SetZoom(float size, bool instant = false)` | void | 배율 목표 설정. 일반 호출은 서서히 반영 |
| `ResetZoom()` | void | 구역 크기에 맞춘 배율로 복귀 |
| `SetSmoothing(float seconds)` | void | 일반 자동 추적의 위치·배율 감쇠 시간 |
| `SetRotationSpeed(float degreesPerSecond)` | void | 일반 자동 방향 전환의 회전 속도 |
| `SetShakeOffset(Vector3 value)` | void | 카메라 로컬 축의 흔들림 오프셋. 자동 복귀하지 않으므로 종료 시 Vector3.zero 설정 |

`FixAt`과 `MoveTo`에는 Vector3 위치와 Quaternion 회전을 받는 오버로드도 있습니다. view는 카메라 위치·회전을 배치한 Empty Transform이며, 호출 시점의 값을 복사합니다. 구역이 언로드돼도 유지할 연출 마커는 Core에 둡니다.

- size는 직교 카메라의 화면 절반 높이입니다. 작으면 줌인, 크면 줌아웃합니다. -1이면 현재 배율을 유지합니다.
- MoveTo와 FixAt은 자동 추적을 중단하고 수동 연출을 시작합니다. 기본값은 도착 후 고정입니다.
- `resumeAfter: true`는 MoveTo 도착 후 자동 규칙으로 복귀합니다. 잠시 고정해 둘 때는 기본값을 사용하고 나중에 ResumeFollow를 호출합니다.
- ResumeFollow는 구역 규칙을 복구합니다. 고정 보스방에서는 다시 보스방의 고정 화면이 됩니다.
- 수동 시점은 구역 변경과 TeleportTo에도 유지됩니다. 이동 후 자동 화면으로 돌아오려면 ResumeFollow를 호출합니다.
- 수동 연출은 unscaled time을 사용하므로 Time.timeScale이 0이어도 진행합니다. 대기에도 WaitForSecondsRealtime을 쓰면 정지 중 동작합니다.
- 새 MoveTo/FixAt 또는 StopFollow/ResumeFollow는 이전 이동을 취소합니다. 취소된 이동을 기다리던 코루틴도 종료돼 다음 단계로 진행할 수 있습니다. 이때 이전 연출을 기다리던 GameFlow도 다음 줄로 진행합니다. 전체 시퀀스의 취소 정책은 GameFlow에서 별도로 관리합니다. GameObject 자체를 비활성화하는 대신 이 API로 연출을 전환합니다.
- 자동 추적 SetZoom은 구역 경계보다 넓어지는 줌아웃을 제한합니다. 자유로운 줌아웃 연출은 MoveTo/FixAt의 size로 지정합니다.

`SetBounds(CameraBounds2D)`는 기존 단일 씬 경계 설정용입니다. Additive 구역에서는 구역의 CameraFrame이 우선합니다.

`ControlledCamera`로 실제 카메라를 읽을 수 있습니다. `HasManualOverride`, `IsFollowing`, `IsTransitioning`, `TurnProgress`, `TurnStrength`는 상태 조회용입니다. 다른 스크립트에서 실제 카메라 Transform을 계속 변경하면 컨트롤러의 LateUpdate가 덮어쓸 수 있으므로 연출은 위 함수로 요청합니다.

## 전체 흐름 스크립트 예제

나중에 만드는 GameFlow에 아래와 같이 연결합니다. introView와 exitView에는 Core의 카메라 시점 마커를 연결합니다.

```csharp
using System.Collections;
using UnityEngine;

public class GameFlow : MonoBehaviour
{
    [SerializeField] private AdditiveMapFlow maps;
    [SerializeField] private SideViewCameraFollow cameraController;
    [SerializeField] private Transform introView;
    [SerializeField] private Transform exitView;

    private IEnumerator Start()
    {
        yield return maps.WaitUntilReady();
        yield return Stage1BossIntro();
        yield return maps.WaitForBossClear(1);
        yield return cameraController.FixAt(exitView, 5f);
        yield return new WaitForSecondsRealtime(0.5f);
        cameraController.ResumeFollow();
    }

    private IEnumerator Stage1BossIntro()
    {
        yield return maps.WaitForSection(6);
        bool wasEnabled = maps.Player.enabled;
        maps.Player.enabled = false;
        try
        {
            yield return cameraController.MoveTo(introView, 1.2f, 6f);
            yield return new WaitForSecondsRealtime(0.5f);
        }
        finally
        {
            cameraController.ResumeFollow();
            maps.Player.enabled = wasEnabled;
        }
    }

    public void OnBoss1Defeated()
    {
        maps.ClearBoss(1);
    }
}
```

실제 보스의 사망 이벤트에서 OnBoss1Defeated를 호출하면 대기 중인 흐름이 이어집니다. 현재 테스트 보스는 E 상호작용에서 ClearBoss를 호출합니다.

직접 구역 이동을 담당하는 단계는 다음처럼 연결합니다.

```csharp
yield return maps.TeleportTo(6, new Vector3(146f, 0.08f, 0f));
cameraController.ResumeFollow();
```

현재 TestMapInteraction에도 문 이동과 보스 4 복귀 등의 테스트 흐름이 있습니다. 실제 GameFlow로 해당 이동을 옮길 때는 해당 테스트 상호작용을 비활성화하거나 Destination 설정을 제거해서 같은 이동이 두 번 실행되지 않게 합니다.

## 자동 처리 스크립트와 내부 함수

| 스크립트 | 자동 처리 | GameFlow에서의 사용 |
| --- | --- | --- |
| AdditiveMapSection | 콜라이더 진입·이탈, 이동 평면과 구역 카메라 활성화, 장벽 갱신 | 보통 직접 호출하지 않고 구역 데이터를 설정 |
| LevelMovementPlane | Prepare(), Displacement(), Project()로 플레이어 방향·이동량·경로 보정 | PlayerController가 호출. GameFlow가 반복 호출할 필요 없음 |
| LevelCameraZone | 범위에 들어가면 고정하고 나가면 자동 규칙 복귀 | 강제 연출에는 FixAt/MoveTo 사용 |
| LevelCameraTurn | 플레이어 통과율로 회전·줌·높이 계산 | 경로와 연출 수치를 Inspector에서 설정 |
| TestMapInteraction | E/자동 문·보스·입구 테스트 | TryInteract()는 플레이어가 범위 안이고 조건을 만족해야 true 반환 |
| TestOneWayPlatform | 아래에서 통과하고 위에서 착지하는 발판 | 직접 호출할 공개 행동 없음 |

AdditiveMapFlow.EnterSection/ReleaseSection, AdditiveMapSection.Activate/SetNeighborsReady, 카메라의 SetSectionView/EnterZone/ExitZone은 구역 시스템의 연결용입니다. EnterSection은 플레이어를 이동시키지 않습니다. 실제 이동에는 TeleportTo를 사용합니다.

## 카메라 구역 설정

- CameraFrame은 화면 경계용 비활성 BoxCollider입니다. 일반 가로 구간은 바닥·천장 높이, 세로 구간은 양쪽 벽 너비에 맞춰 화면을 채웁니다.
- 같은 스테이지의 서브 구역은 같은 Camera Group을 사용합니다. 스테이지 끝에서 멈췄다가 다음 그룹에 실제로 진입하면 입구 화면을 거쳐 자동 추적합니다.
- 보스 1·2·3·5·6·7은 고정 화면, 보스 4는 세로 스크롤입니다.
- LevelCameraTurn은 Route Points의 통과 거리로 진행합니다. 멈추면 유지하고 후진하면 거꾸로 진행합니다. 기본 줌아웃 1.3배와 최대 15도 내려보기를 사용하며, 3스테이지 원형 구간은 1.6배입니다.
- Fade Fraction은 진입·이탈에 쓰는 경로 비율입니다. 수평 회전은 Start/End Yaw 또는 Follow Path Heading을 사용합니다.
- 이 회전은 통과 거리 기반이므로 SetRotationSpeed/SetSmoothing/MoveTo duration과 별개입니다. 연출 거리를 늘리면 같은 이동 속도에서 천천히 진행합니다.
- 회전 중 실제로 플레이어를 가리는 앞쪽 오브젝트만 일시적으로 숨기고 복구합니다. 충돌은 유지합니다. 원형 바닥·천장의 연속 표시 메시는 Meshes 폴더에 있습니다.

## 씬 인덱스와 현재 테스트 흐름

| 인덱스 | 구역 |
| --- | --- |
| 0 | 로비 |
| 1~5 / 6 | 스테이지 1 / 보스 1 |
| 7~11 / 12 | 스테이지 2 / 보스 2 |
| 13~17 / 18 / 19 | 스테이지 3 / 보스 3 / 분기점 2 |
| 20~24 / 25 | 스테이지 4 / 보스 4 |
| 26~30 / 31 | 스테이지 5 / 보스 5 |
| 32~36 / 37 | 스테이지 6 / 보스 6 |
| 38 / 39 | 최종 연결 통로 / 보스 7 |
| 40 | 분기점 1 |

로비 → 1 → 보스 1 → 분기점 1 → 2 → 보스 2 별도 방 → 3 → 보스 3 → 분기점 2 → 4 → 보스 4 클리어 후 보스 1 방 복귀 → 다른 입구로 5 → 보스 5 클리어 후 되돌아와 6 → 보스 6 → 최종 연결 통로 → 보스 7 순서입니다.

좌우 방향키로 통로를 따라 이동하고 위 방향키로 점프합니다. E는 테스트 문·보스·입구 상호작용입니다. 보스 클리어 상태는 현재 Core의 플레이 동안 유지됩니다.

## 리팩토링과 검증

`TeleportTo`와 `FixAt`은 이제 Coroutine을 반환합니다. 기존처럼 반환값을 무시해도 호출할 수 있고, yield return을 붙이면 완료를 기다립니다. 내부용 `LevelMovementPlane.Prepare(float)`는 `Prepare()`로, `AdditiveMapSection.Activate(PlayerController)`는 `Activate()`로 정리했습니다.

씬 생성·경로 동기화 도구와 namespace는 추가하지 않았습니다. 스크립트 파일 구성과 현재 오브젝트 배치는 유지합니다. 미사용 구역 크기 필드와 미사용 인자, 중복 검색을 정리했고, 카메라와 경로 계산을 역할별 메서드로 풀어 썼습니다. 기존 PlayerCameraShake와 다른 테스트 씬의 카메라 경계 옵션은 유지했습니다.

검증 결과와 실행 기록은 `Library/AdditiveMapFlowChecks/`에, 이번 수정 전 백업은 `Library/AdditiveMapFlowBackups/FlowBlocks_20261125_133849/`에 있습니다.



최종 검증: 실제 IEnumerator 순차 연결, 연속 이동 대기, 구역·보스 조건 대기, 일시정지 중 연출, 취소된 카메라 연출의 대기 해제, 41개 맵 로딩·착지와 프레이밍, 48개 회전 위치, 고정 카메라 16개 항목, 물리 연결 9곳, 문·보스 분기 12개, 기존 플레이어 흔들림 이벤트를 통과했습니다. Unity 오류 로그 0개를 확인했습니다.

## 함수 소속과 파라미터 참조

GameFlow에서 maps 필드에는 MapLoader의 AdditiveMapFlow를, cameraController 필드에는 CameraManager의 SideViewCameraFollow를 연결합니다. introView/exitView는 Core에 둔 카메라 시점용 Empty Transform입니다.

### AdditiveMapFlow — MapLoader

| 함수 | 역할·반환 | 파라미터 |
| --- | --- | --- |
| WaitUntilReady() | 초기 로딩 완료 대기. IEnumerator | 없음 |
| TeleportTo(index, worldPosition) | 씬 준비와 이동을 실행. Coroutine | index: MapLoader의 Scenes 배열 번호. worldPosition: 도착할 월드 좌표 Vector3 |
| WaitForSection(index) | 현재 구역과 로딩 완료 대기. IEnumerator | index: Scenes 배열 번호 |
| WaitForBossClear(stage) | 보스 클리어 기록 대기. IEnumerator | stage: 1부터 시작하는 보스 구분 번호 |
| ClearBoss(stage) | 클리어 상태 기록과 장벽 갱신. void | stage: 클리어한 보스 번호 |
| HasClearedBoss(stage) | 기록 여부 조회. bool | stage: 확인할 보스 번호 |

index는 Unity Build Settings의 번호가 아닙니다. 현재 보스 1 방은 index=6이고 보스 1 클리어 키는 stage=1입니다. WaitForSection은 과거 방문 기록이 아니라 현재 구역을 확인합니다. ClearBoss는 보스를 직접 처치하지 않고 상태만 기록합니다.

### SideViewCameraFollow — CameraManager

| 함수 | 역할 | 파라미터 |
| --- | --- | --- |
| StopFollow() | 현재 시점에 자동 추적 정지 | 없음 |
| FixAt(view, size=-1, instant=false) | 기본 0.6초 이동 후 고정. Coroutine | view: 목표 카메라 위치·회전. size: 화면 절반 높이. instant: 즉시 배치 여부 |
| MoveTo(view, duration=0.8, size=-1, resumeAfter=false) | 이동·회전·줌 연출. Coroutine | view: 목표 시점. duration: 소요 초. size: 화면 절반 높이. resumeAfter: 도착 후 자동 구역 규칙 복귀 여부 |
| ResumeFollow(snap=false) | 자동 구역 규칙으로 복귀 | snap=false: 부드럽게, true: 즉시 배치 |
| SnapToTarget() | 현재 제어 규칙에 따라 즉시 배치 | 없음. 수동 고정 중이면 수동 시점 유지 |
| SetTarget(value) | 추적 대상 교체 | value: 새 대상의 Transform |
| SetZoom(size, instant=false) | 줌 목표 설정 | size: 직교 화면 절반 높이. instant: 즉시 배율 적용 여부 |
| ResetZoom() | 구역 배율 복귀 | 없음 |
| SetSmoothing(seconds) | 일반 추적의 위치·배율 감쇠 설정 | seconds: 감쇠 기준 초. 정확한 완료 시간은 아님. 최소 0.01 |
| SetRotationSpeed(degreesPerSecond) | 일반 자동 방향 전환 속도 | degreesPerSecond: 초당 회전 각도. 최소 1. 통과율 회전에는 적용하지 않음 |
| SetShakeOffset(value) | 흔들림 위치 오프셋 | value: 카메라 로컬 축 기준 Vector3. 종료 시 Vector3.zero로 복구 |
| SetBounds(bounds) | 기존 단일 씬의 범위 설정 | bounds: CameraBounds2D 컴포넌트. Additive CameraFrame이 있으면 그 프레임 우선 |

FixAt/MoveTo에는 view 대신 position(Vector3 월드 위치), rotation(Quaternion 월드 회전)을 받는 오버로드가 있습니다. size는 배수가 아니라 직교 화면 절반 높이입니다. MoveTo/FixAt의 size가 0 이하이면 현재 배율을 유지합니다. SetZoom은 최소 0.1로 제한합니다. duration이 0 이하이거나 instant=true이면 즉시 처리합니다.

view는 호출할 때 위치·회전을 복사하므로 이후 마커를 움직여도 이미 시작한 연출의 도착점은 바뀌지 않습니다. Quaternion.Euler(15, 90, 0)은 도 단위 각도로 Quaternion을 만드는 Unity 함수입니다.

### 구역 시스템의 자동 연결 함수

| 소속 함수 | 호출 연결·역할 | 파라미터 |
| --- | --- | --- |
| AdditiveMapFlow.EnterSection(index, entered=null) | AdditiveMapSection이 진입 알림. 현재 구역·스트리밍 변경 | index: 구역 번호. entered: 진입한 AdditiveMapSection. 플레이어 위치는 이동하지 않음 |
| AdditiveMapFlow.ReleaseSection(section) | AdditiveMapSection이 이탈 알림 | section: 떠난 구역 컴포넌트 |
| AdditiveMapSection.Activate() | AdditiveMapFlow가 호출. 이동 경로와 카메라 구역 전달 | 없음 |
| AdditiveMapSection.SetNeighborsReady(previousReady, nextReady) | AdditiveMapFlow가 장벽 상태 갱신 | 두 bool은 이전·다음 씬의 로딩 완료 여부. 보스 조건도 함께 확인 |
| SideViewCameraFollow.SetSectionView(value) | AdditiveMapSection이 구역 설정 전달 | value: 현재 AdditiveMapSection |
| SideViewCameraFollow.EnterZone(zone) / ExitZone(zone) | LevelCameraZone이 고정 구역 진입·이탈 알림 | zone: 해당 LevelCameraZone. 영역 밖에서 강제 고정하려면 FixAt 사용 |
| LevelMovementPlane.SetTarget(target) | AdditiveMapFlow가 이동 대상 연결 | target: 이동 계산 대상인 플레이어 Transform |
| LevelMovementPlane.SetSection(movementPlane, path) | AdditiveMapSection이 이동 경로 전달 | movementPlane: 이동 기준 Transform. path: 순서대로 연결한 경로 Transform 배열. 점이 2개 미만이면 평면 사용 |
| LevelMovementPlane.Prepare() | PlayerController가 통로 방향으로 루트 회전 | 없음 |
| LevelMovementPlane.Displacement(horizontal, vertical) | PlayerController가 이번 프레임의 월드 이동량 계산 | horizontal: 통로를 따라 이동할 거리. vertical: 월드 Y 이동 거리. 속도·입력축 값이 아님 |
| LevelMovementPlane.Project(position, depth=0) | PlayerController/VFX가 경로에 위치 보정 | position: 월드 좌표. depth: 플레이어 정면 축으로 띄울 거리. 보정한 Vector3 반환 |
| LevelCameraZone.Contains(player) | 카메라가 영역 겹침 여부 확인 | player: CharacterController가 붙은 플레이어 Transform. bool 반환 |
| LevelCameraTurn.Sample(position, out rotation, out zoom, out progress, out strength) | SideViewCameraFollow가 통과율 연출 계산 | position: 플레이어 월드 좌표. 나머지는 계산해 돌려주는 출력값 |
| TestMapInteraction.TryInteract() | 자체 Update/E 입력 또는 테스트 호출. 상호작용 수락 여부 bool | 없음. 범위·보스 조건·로딩 상태 검사. true여도 씬 이동 완료를 뜻하지는 않음 |
| TestOneWayPlatform | Unity 물리 갱신에서 자동 처리 | 직접 호출할 공개 행동 함수 없음 |

LevelCameraTurn.Sample의 출력은 rotation=카메라 회전 Quaternion, zoom=줌아웃 배수, progress=경로 통과율 0~1, strength=연출 강도 0~1입니다. bool 반환은 해당 위치에 연출을 적용할 수 있는지 뜻합니다. Sample 자체는 카메라를 움직이지 않습니다.

### 예제 GameFlow 안의 함수

앞의 Start, Stage1BossIntro, OnBoss1Defeated는 문서 예제의 GameFlow에 직접 작성하는 함수입니다. 현재 맵 스크립트 안에 들어 있는 함수가 아닙니다.

| 함수 | 호출 연결 | 파라미터·역할 |
| --- | --- | --- |
| IEnumerator Start() | Unity가 자동 호출 | 없음. 전체 순차 흐름 시작 |
| Stage1BossIntro() 또는 BossIntro() | Start에서 yield return으로 호출 | 없음. 도착 대기·카메라 연출을 묶은 사용자 행동 블록 |
| OnBoss1Defeated() | 실제 보스 사망 이벤트에서 연결 | 없음. maps.ClearBoss(1)을 호출하는 사용자 콜백 |
| new WaitForSecondsRealtime(seconds) | Unity 제공 대기 객체를 yield return | seconds: 실제 시간 초. Time.timeScale의 영향을 받지 않음 |

1.2f와 6f에서 f는 float 숫자 표기입니다. MoveTo의 두 번째 인자 1.2f는 소요 시간, 세 번째 인자 6f는 화면 절반 높이입니다. 서로 다른 의미이므로 이름 붙인 인자로 호출해도 됩니다.

```csharp
yield return cameraController.MoveTo(
    view: introView,
    duration: 1.2f,
    size: 6f,
    resumeAfter: false);
```

## 서브스테이지 전투 구역

스테이지 1~6의 기존 서브스테이지 30개에 적용했습니다. 로비, 보스방, 분기점, 스테이지 7 진입로는 제외합니다.

- SectionTrigger의 **SubstageBattle**은 계속 사용하는 전투 구역 컴포넌트입니다. 몸 전체가 입구를 통과한 뒤 양쪽 전투 장벽을 닫고 전투용 카메라 범위를 적용합니다.
- **SubstageBattleToggle → Battle Enabled**만 임시 테스트 옵션입니다. 끄면 해당 구역을 전투 없이 통과합니다. 실행 중 끄면 잠금을 풀지만 클리어로 기록하지 않습니다.
- 임시 옵션을 정리할 때는 씬의 SubstageBattleToggle 컴포넌트들을 제거한 뒤 SubstageBattleToggle.cs를 삭제합니다. SubstageBattle과 전투용 BattleArea는 남깁니다. 영구 전투 코드는 임시 옵션을 참조하지 않으며, 옵션을 제거하면 전투가 항상 활성화됩니다.
- BattleArea의 BattleEntrance/BattleExit는 기존 씬 로딩용 PreviousGate/NextGate와 별개입니다. 전투가 끝나도 로딩/보스 조건 때문에 닫혀 있는 기존 게이트는 그대로 유지됩니다.
- BattleCameraFrame은 해당 서브스테이지의 입출구 안쪽만 담는 전투용 범위입니다. 일반 추적, 수동 구도, 줌, 경로 회전, 흔들림 적용 뒤 최종 화면을 제한합니다. 작은 방에서는 화면이 범위를 벗어나지 않도록 표시 크기가 줄어들 수 있습니다.
- 전투 카메라 범위는 기존 구역 범위를 대신해 목표 구도 계산에 사용합니다. 로비에서 서브스테이지로 진입하는 중 전투가 시작되면, 진행 중인 진입 이동의 목표만 갱신하고 기존 위치·회전·크기 보간을 이어갑니다. 별도의 전투 진입 보간을 중복 적용하지 않습니다. 구역 안에서 플레이어 추적을 유지하고 입출구 경계에서만 제한합니다. 즉시 이동을 명시한 SnapToTarget과 duration 0의 MoveTo는 바로 정렬합니다.
- 테스트 중 Game 뷰에 포커스를 둔 상태에서 **T키**를 누르면 현재 진행 중인 서브스테이지 전투만 완료합니다. 입력 처리는 임시 SubstageBattleToggle에 있으므로 이 컴포넌트를 제거하면 단축키도 함께 제거됩니다.
- 3스테이지의 원형 경로는 평면 방용 BattleCameraFrame으로 줌을 줄이지 않습니다. 경로의 시작/끝 출입구 평면 사이에 보이는 가로 범위가 들어오도록 시선 중심을 조정하고, LevelCameraTurn.Sample이 계산한 회전·줌·내려보기·높이를 유지합니다. 전투 시작/완료 시 XZ 시선 보정만 부드럽게 전환하므로 같은 위치에서 전투를 완료해도 줌이 갑자기 바뀌지 않습니다. 일반 추적에서 경로 카메라로 들어갈 때의 위치 차이도 경로 진행량에 따라 해소하여 진입 순간의 위치 튐을 줄입니다.
- 시작·클리어 이벤트는 SubstageBattle의 On Battle Started / On Battle Cleared에 연결합니다. 적 생성과 적 전멸 판정은 새로 만들지 않았습니다. 기존 적 전멸 이벤트 또는 상위 게임플로우에서 CompleteBattle을 호출하면 됩니다. 현재 테스트에서는 플레이 중 컴포넌트 메뉴의 Complete Battle로 확인할 수 있습니다.
- 클리어 기록은 Core의 AdditiveMapFlow에 남겨 Additive 씬이 언로드되었다가 다시 로딩되어도 중복 전투를 시작하지 않습니다. 실행을 종료하면 초기화되는 상태이며 저장 파일에는 기록하지 않습니다.
- 전투 도중 일반 구역 변경과 테스트 문/포털 상호작용은 막습니다. 상위 게임플로우가 TeleportTo로 강제 이동하면 현재 전투를 취소하고 잠금을 정리합니다.

| 함수 | 소속 스크립트 | 반환형 | 인자 | 역할 |
| --- | --- | --- | --- | --- |
| BeginBattle | SubstageBattle.cs | void | 없음 | 현재 구역에서 몸 전체가 입장했고 아직 클리어하지 않았을 때 전투를 시작 |
| CompleteBattle | SubstageBattle.cs | void | 없음 | 클리어 기록 후 양쪽 전투 장벽과 카메라 범위를 해제 |
| CancelBattle | SubstageBattle.cs | void | 없음 | 클리어 기록 없이 전투 장벽과 카메라 범위만 해제 |
| HasClearedSection | AdditiveMapFlow.cs | bool | index: Scenes 배열의 구역 번호 | 해당 구역의 클리어 기록 확인 |
| ClearSection | AdditiveMapFlow.cs | void | index: 유효한 Scenes 배열의 구역 번호 | 클리어 상태만 기록. 실제 진행 중인 전투 종료에는 CompleteBattle 사용 |
| SetConfinement | SideViewCameraFollow.cs | void | bounds: 제한할 BoxCollider | 카메라 최종 화면을 해당 범위 안으로 제한. null이면 제한 해제 |
| ClearConfinement | SideViewCameraFollow.cs | void | bounds: 해제하려는 BoxCollider | 현재 제한과 같은 참조일 때만 해제하여 다른 연출의 제한을 지우지 않음 |

SubstageBattle.IsRunning은 전투 중 여부, IsCleared는 현재 구역의 클리어 기록, Active는 실행 중인 전투 구역입니다. AdditiveMapFlow.CurrentSection은 현재 활성 구역입니다.

검증: 30개 서브스테이지의 자동 진입, 양쪽 장벽 충돌, 화면 모서리 제한, 옵션 끄기, 옵션 컴포넌트 제거, 전투 종료, 재로딩 후 클리어 유지. 실제 걸어서 입장할 때 몸이 입구에 걸린 동안 닫히지 않는지와 뒤로 걸어 나가거나 높은 위치에서 장벽을 통과할 수 없는지도 확인했습니다.
