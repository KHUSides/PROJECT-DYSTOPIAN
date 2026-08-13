# NPC 연동 / 사용 가이드

이 문서는 다른 파트 작업자나 다른 AI가 JiSeon NPC 시스템을 씬에 가져갈 때 필요한 내용을 정리한 가이드입니다.

현재 NPC 시스템은 Enemy / Boss / Rhythm 코드에 의존하지 않고 독립적으로 동작합니다.
다만 `NPC_Test.unity`는 실제 플레이어 동작 확인을 위해 `MerlinBoss_Test.unity`에서 쓰던 `Player`와 `Rhythm System`을 복사해 둔 상태입니다.

---

## 바로 테스트할 씬

- `Assets/JiSeon/Scenes/NPC_Test.unity`

씬 포함 요소:

- `Player`: `MerlinBoss_Test.unity`에서 사용하던 실제 플레이어 오브젝트
- `Rhythm System`: 실제 플레이어의 리듬 판정/공격 관련 참조 오류를 막기 위해 함께 배치
- `NpcDialogueManager`
- `TutorialNPC`

테스트 입력:

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 테스트 플레이어 좌우 이동 |
| UpArrow | 테스트 플레이어 점프 |
| E | NPC 대화 시작 / 다음 대사 |
| Space / Enter | 다음 대사 |
| Escape | 대화 취소 |

---

## 씬에 가져가야 하는 프리팹

### 1. NPC 대화 매니저

- `Assets/JiSeon/Prefabs/NPCs/NpcDialogueManager.prefab`

역할:

- NPC 머리 위 World Space 대화 UI 표시
- 대화 시작 / 종료 처리
- 마지막 대사 종료 직후 같은 입력으로 대화가 바로 재시작되지 않도록 방지
- 대화 중 플레이어 조작 제한
- 대화 완료 시 조건값 / 이벤트 처리

사용 방법:

1. 씬에 `NpcDialogueManager.prefab`을 하나만 배치합니다.
2. 이 프리팹은 런타임에 NPC 머리 위 대화 UI를 자동 생성합니다.
3. 나중에 UI 파트의 정식 대화 UI가 완성되면 `NpcDialogueManager`의 UI 참조만 교체하면 됩니다.
4. 실제 `Player`를 사용하는 테스트 씬에서는 `Rhythm System`도 함께 배치하는 것이 안전합니다.

### 2. 튜토리얼 NPC

- `Assets/JiSeon/Prefabs/NPCs/TutorialNPC.prefab`

역할:

- 플레이어 접근 감지
- `E : 대화` 프롬프트 표시
- E 키 입력 시 대화 시작
- 조건별 대화 분기 예시 제공
- 대화 완료 시 튜토리얼 진행도 변경 예시 제공

사용 방법:

1. 씬에 `TutorialNPC.prefab`을 원하는 위치에 배치합니다.
2. 플레이어가 NPC 근처에 오면 `E : 대화` 프롬프트가 표시됩니다.
3. E를 누르면 대화가 시작되고, 대화 중에는 플레이어 조작이 잠깁니다.
4. 대화창은 NPC 머리 위에 표시됩니다.
5. 대화가 끝나면 대화창이 닫히고 플레이어 조작이 다시 복구됩니다.

---

## 대화 UI 위치 조정

대화 UI는 `NpcInteractable` 기준으로 위치를 잡습니다.

조정 대상:

- 스크립트: `Assets/JiSeon/Scripts/NPC/NpcInteractable.cs`
- 컴포넌트 필드:
  - `dialogueAnchor`
  - `dialogueAnchorOffset`

기본값:

- `dialogueAnchorOffset`: `(0, 2.75, 0)`

사용 기준:

- 캐릭터 머리 위에 빈 오브젝트를 따로 두고 싶으면 `dialogueAnchor`에 연결합니다.
- 별도 앵커가 없으면 NPC 위치 + `dialogueAnchorOffset`을 사용합니다.
- 캐릭터 크기가 다르면 `dialogueAnchorOffset.y` 값을 조정하면 됩니다.
- Scene View에서 NPC를 선택하면 대화 UI 앵커 위치를 Gizmo로 확인할 수 있습니다.

`NpcDialogueManager` 쪽 현재 UI 기본값:

- Canvas Render Mode: `World Space`
- Panel Size: `560 x 220`
- World Canvas Scale: `0.0085`
- Speaker Font Size: `24`
- Body Font Size: `28`
- 3줄 대사 기준으로 NPC 이름 영역과 본문 영역이 겹치지 않도록 분리
- Camera Forward Offset: `0.05`
- 대화 중 매 프레임 NPC 머리 위 위치로 갱신
- Main Camera를 바라보도록 회전 갱신

---

## 플레이어 쪽 요구사항

NPC 시스템은 플레이어 원본 스크립트를 직접 수정하지 않습니다.

플레이어 탐색 기준:

- 우선 `Player` 태그가 붙은 오브젝트를 찾습니다.
- 태그가 없으면 `PlayerController` 또는 `DummyPlayerController`가 붙은 오브젝트를 찾습니다.

대화 중 조작 제한 방식:

- `NpcPlayerControlLock`이 다음 타입 이름의 컴포넌트를 일시적으로 비활성화합니다.
  - `PlayerController`
  - `PlayerRhythmAttackBridge`
  - `DummyPlayerController`
  - `DummyPlayerAttack`

따라서 실제 플레이어 씬에 적용할 때는 다음 중 하나만 맞으면 됩니다.

- 플레이어 오브젝트에 `Player` 태그가 있음
- 플레이어 오브젝트에 `PlayerController`가 있음

---

## 조건별 대화 구조

조건값은 `NpcDialogueState`에 저장됩니다.

현재 제공되는 상태:

- Flag: 문자열 키 기반 bool 값
- TutorialStep: int 튜토리얼 진행도

`TutorialNPC.prefab`에는 예시로 3개 분기가 들어 있습니다.

| 분기 | 조건 | 완료 결과 |
|---|---|---|
| `first_talk` | 튜토리얼 단계 0 이하 | `tutorial_npc_met = true`, `TutorialStep = 1` |
| `tutorial_step_1_rhythm_hint` | `tutorial_npc_met = true`, `TutorialStep = 1` | `TutorialStep` 1 증가 |
| `tutorial_step_2_repeat` | `TutorialStep >= 2` | 반복 안내 |

중요:

- `NpcInteractable`은 위에서부터 순서대로 조건을 검사하고, 처음 만족한 분기를 실행합니다.
- 그래서 조건이 구체적인 분기를 위쪽에 두고, 기본 대화는 아래쪽에 두는 방식으로 쓰면 됩니다.

---

## 다른 파트에서 호출 가능한 인터페이스

### 상태값 제어

스크립트:

- `Assets/JiSeon/Scripts/NPC/NpcDialogueState.cs`

주요 API:

```csharp
NpcDialogueState.GetFlag("flag_key");
NpcDialogueState.SetFlag("flag_key", true);
NpcDialogueState.SetTutorialStep(1);
NpcDialogueState.AdvanceTutorialStep();
NpcDialogueState.ResetAll();
```

### UnityEvent에서 호출하기

스크립트:

- `Assets/JiSeon/Scripts/NPC/NpcDialogueEventBridge.cs`

Inspector / UnityEvent에서 호출 가능한 메서드:

- `SetFlagTrue(string key)`
- `SetFlagFalse(string key)`
- `SetTutorialStep(int step)`
- `AdvanceTutorialStep()`
- `AdvanceTutorialStepBy(int amount)`
- `ResetDialogueState()`
- `LogEvent(string eventName)`

### 대화 시작 / 종료 이벤트 구독

스크립트:

- `Assets/JiSeon/Scripts/NPC/NpcDialogueManager.cs`

주요 이벤트:

```csharp
NpcDialogueManager.Instance.DialogueStarted += HandleDialogueStarted;
NpcDialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
```

`DialogueEnded`는 완료 여부를 bool로 전달합니다.

```csharp
private void HandleDialogueEnded(
    NpcInteractable npc,
    NpcDialogueBranch branch,
    bool completed)
{
    // completed == true: 마지막 대사까지 정상 종료
    // completed == false: Escape 등으로 중단
}
```

---

## 구현된 스크립트 위치

- `Assets/JiSeon/Scripts/NPC/NpcInteractable.cs`
- `Assets/JiSeon/Scripts/NPC/NpcDialogueManager.cs`
- `Assets/JiSeon/Scripts/NPC/NpcDialogueData.cs`
- `Assets/JiSeon/Scripts/NPC/NpcDialogueState.cs`
- `Assets/JiSeon/Scripts/NPC/NpcPlayerControlLock.cs`
- `Assets/JiSeon/Scripts/NPC/NpcDialogueEventBridge.cs`
- `Assets/JiSeon/Scripts/Editor/NpcTestAssetBuilder.cs`

---

## 현재 검증 상태

검증한 내용:

- `NPC_Test.unity` 생성 확인
- `NPC_Test.unity`에 보스 테스트 씬 기준 `Player` 배치 확인
- `NPC_Test.unity`에 보스 테스트 씬 기준 `Rhythm System` 배치 확인
- `TutorialNPC.prefab` 생성 확인
- `NpcDialogueManager.prefab` 생성 확인
- Play Mode에서 플레이어가 NPC 범위 안에 있을 때 프롬프트 활성화 확인
- NPC 대화 시작 확인
- NPC 머리 위 World Space 대화 UI 표시 확인
- 마지막 대사 종료 직후 같은 프레임 재상호작용 방지 확인
- 대화 중 `PlayerController` 비활성화 확인
- 대화 종료 후 플레이어 조작 복구 확인
- 첫 대화 완료 후 `tutorial_npc_met = true`, `TutorialStep = 1` 확인
- 두 번째 대화 완료 후 `TutorialStep = 2` 확인
- 세 번째 대화부터 반복 안내 분기로 진입 확인
- Unity 콘솔 에러 0개 확인
