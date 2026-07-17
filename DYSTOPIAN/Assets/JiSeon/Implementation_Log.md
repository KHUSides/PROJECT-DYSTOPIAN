# Enemy / Boss / NPC 파트 구현 기록

## 2026-07-17 작업 정리

### 현재 범위

1주차 독립 테스트 환경 중 **Enemy 테스트 씬** 구현을 완료하였다.

이번 작업은 다른 팀 코드와 직접 연결하지 않고, `Assets/JiSeon` 내부에서 독립적으로 적 추격, 피격, 체력, 사망 흐름을 확인할 수 있도록 구성하는 것을 목표로 한다.

Boss / NPC는 이번 1주차 구현 범위에서는 실제 기능 구현 대상이 아니다. 현재 씬과 구조는 이후 Boss / NPC 테스트를 추가할 수 있는 기반 용도로만 사용한다.

---

## 1주차 구현 완료 항목

| 항목 | 완료 상태 | 최종 구현 내용 |
|---|---:|---|
| Enemy/Boss/NPC 테스트 씬 제작 | 완료 | Enemy 중심 독립 테스트 씬 제작 |
| 더미 플레이어 제작 | 완료 | 방향키 이동, 위 방향키 점프 |
| 임시 공격 기능 제작 | 완료 | A 접촉 데미지, J 전방 테스트 공격 |
| 단일·롱 노트 이벤트 발생기 제작 | 완료 | A 단일 노트, D 롱 노트 시작/종료 |
| 적 체력·피격·사망 구현 | 완료 | HP 100, 피격 시 감소, 0이면 사망, 2초 후 비활성화 |
| 적 배회·추격 구현 | 완료 | WANDER / CHASE / ATTACK READY / DEAD 상태 |

### 1주차 완료 기준

다른 팀 코드 없이 적이 더미 플레이어를 감지하고, 추격하고, 피격되고, 사망하는 흐름까지 확인 가능하다.

---

## 최종 산출물 위치

### 테스트 씬

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

### 프리팹

- `Assets/JiSeon/Prefabs/DummyPlayer.prefab`
- `Assets/JiSeon/Prefabs/TestEnemy_WanderChase.prefab`

### 스크립트

#### Combat

- `Assets/JiSeon/Scripts/Combat/DamageInfo.cs`

#### Enemy

- `Assets/JiSeon/Scripts/Enemy/EnemyHealth.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyController.cs`

#### Debug

- `Assets/JiSeon/Scripts/Debug/EnemyDebugOverlay.cs`
- `Assets/JiSeon/Scripts/Debug/NoteDebugInput.cs`

#### Test

- `Assets/JiSeon/Scripts/Test/DummyPlayerController.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerAttack.cs`

---

## 테스트 씬 구성

`EnemyBossNPC_Test.unity`는 3D 오브젝트를 사용한 사이드뷰 테스트 씬이다.

### 씬 주요 오브젝트

- `Main Camera`
  - 사이드뷰 확인용 카메라
  - 테스트 맵과 캐릭터가 한 화면에 보이도록 설정

- `Ground_Main`
  - 메인 지면

- `Platform_Left`
  - 왼쪽 점프 테스트 발판

- `Platform_Right`
  - 오른쪽 점프 테스트 발판

- `Wall_Left`
  - 좌측 경계

- `Wall_Right`
  - 우측 경계

- `DummyPlayer`
  - 테스트용 플레이어
  - `DummyPlayer.prefab` 기반

- `TestEnemy_WanderChase`
  - 배회 / 추격 / 피격 / 사망 테스트용 적
  - `TestEnemy_WanderChase.prefab` 기반

- `NoteDebugInput`
  - 단일 노트 / 롱 노트 입력 테스트용 오브젝트

---

## 조작법

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 더미 플레이어 좌우 이동 |
| UpArrow | 더미 플레이어 점프 |
| A | 단일 노트 이벤트 발생 + 접촉 중인 적에게 25 데미지 |
| D 누름 | 롱 노트 시작 이벤트 발생 |
| D 뗌 | 롱 노트 종료 이벤트 발생 |
| J | 더미 플레이어 전방 임시 공격 |

---

## 더미 플레이어 구현

### 사용 스크립트

- `DummyPlayerController.cs`
- `DummyPlayerAttack.cs`

### 이동

`DummyPlayerController`에서 처리한다.

- `CharacterController` 기반 이동
- 좌우 이동 속도: `7`
- 점프 속도: `15`
- 중력 적용
- 사이드뷰 기준으로 Z축 고정
- Player 레이어 자동 적용

### 공격 테스트

`DummyPlayerAttack`에서 처리한다.

- A 키 접촉 데미지
  - 접촉 중인 적에게 25 데미지
  - 입력 키: `A`
  - 실제 저장 값: `KeyCode.A`
  - A 입력 프레임에만 데미지 판정
  - 방향키 입력으로 데미지가 발생하지 않음

- J 키 전방 테스트 공격
  - 플레이어 전방 박스 범위에 있는 `IDamageable` 대상에게 데미지
  - 추후 실제 전투 시스템 연결 전 임시 테스트 용도

### A 접촉 데미지 판정

A 키 접촉 데미지는 현재 프레임의 위치를 기준으로 처리한다.

- A 입력은 `Update()`에서 감지
- 실제 데미지 판정은 같은 프레임의 `LateUpdate()`에서 처리
- 판정 직전 `Physics.SyncTransforms()` 호출
- 오래된 A 입력이 다음 프레임에 남지 않도록 큐 제거
- 플레이어와 적의 bounds 간격을 기준으로 접촉 여부 판단

현재 접촉 판정값은 다음과 같다.

| 값 | 현재 설정 |
|---|---:|
| contactSearchRadius | 2.0 |
| contactDistanceTolerance | 0.65 |
| contactVerticalTolerance | 0.35 |
| contactDepthTolerance | 0.45 |

---

## 노트 디버그 입력 구현

### 사용 스크립트

- `NoteDebugInput.cs`

### 기능

기획 피드백에 따라 별도 리듬 시스템 없이 키 입력으로 노트 이벤트를 디버깅할 수 있도록 구현하였다.

| 입력 | 이벤트 |
|---|---|
| A | Single |
| D Down | LongStart |
| D Up | LongEnd |

### 현재 역할

- 단일 노트 입력 확인
- 롱 노트 시작 / 종료 입력 확인
- 추후 리듬 판정 시스템이나 UI 파트와 연결할 때 사용할 임시 이벤트 제공

---

## 적 체력 / 피격 / 사망 구현

### 사용 스크립트

- `DamageInfo.cs`
- `EnemyHealth.cs`

### DamageInfo

데미지 전달에 필요한 정보를 담는다.

- 데미지량
- 피격 위치
- 데미지 발생 오브젝트

### IDamageable

피격 가능한 대상이 공통으로 사용할 인터페이스다.

- `IsAlive`
- `TakeDamage(DamageInfo damageInfo)`

### EnemyHealth

적 체력과 사망 처리를 담당한다.

| 값 | 현재 설정 |
|---|---:|
| Max Health | 100 |
| Current Health | 100 |
| 사망 후 비활성화 대기 시간 | 2초 |

### 체력 흐름

- 데미지를 받으면 HP 감소
- HP가 0 이하가 되면 사망 처리
- 사망 시 적 상태는 `Dead`
- 사망 후 Collider 비활성화
- 2초 뒤 적 GameObject 비활성화

---

## 적 AI 구현

### 사용 스크립트

- `EnemyController.cs`

### 상태

| 상태 | 의미 |
|---|---|
| Idle | 잠시 정지 |
| Wander | 플랫폼 위를 배회 |
| Chase | 플레이어 감지 후 추격 |
| AttackReady | 공격 가능 거리 내 대기 |
| Dead | 사망 |

### 현재 설정

| 값 | 현재 설정 |
|---|---:|
| Detection Range | 9 |
| Attack Range | 1.5 |
| Wander Speed | 1.8 |
| Chase Speed | 3.2 |
| Wander Move Time | 4 ~ 7초 |
| Wander Idle Time | 0.25 ~ 0.7초 |

### 동작

- 플레이어가 감지 범위 밖이면 배회
- 플레이어가 감지 범위 안에 들어오면 추격
- 플레이어가 공격 가능 거리 안에 들어오면 `AttackReady`
- 체력이 0이 되면 `Dead`
- 플랫폼 끝이나 벽을 감지하면 배회 방향 조정

---

## 적 디버그 표시 구현

### 사용 스크립트

- `EnemyDebugOverlay.cs`

### 표시 항목

- CHASE 감지 범위 원형 표시
- 현재 상태 텍스트 표시
- HP Bar 표시
- HP 숫자 표시

### 현재 UI 표시

- 상태 텍스트 예시: `STATE: WANDER`
- HP 숫자 예시: `100/100`
- HP 텍스트에 별도 `HP` 라벨은 표시하지 않음
- HP Bar는 체력 비율에 따라 색상 변경
  - 체력 높음: 초록
  - 체력 중간: 노랑
  - 체력 낮음: 빨강

---

## 프리팹 구성

### DummyPlayer.prefab

포함 컴포넌트:

- `CharacterController`
- `DummyPlayerController`
- `DummyPlayerAttack`

현재 역할:

- 독립 테스트용 플레이어
- 좌우 이동 / 점프
- A 접촉 데미지
- J 전방 테스트 공격

### TestEnemy_WanderChase.prefab

포함 컴포넌트:

- `CharacterController`
- `EnemyHealth`
- `EnemyController`
- `EnemyDebugOverlay`

현재 역할:

- 배회 / 추격 테스트
- HP / 피격 / 사망 테스트
- 상태 UI / HP Bar / 감지 범위 표시

---

## 현재 검증 완료 상태

### 기능 검증

- 더미 플레이어 좌우 이동 가능
- 더미 플레이어 점프 가능
- 단일 노트 A 입력 가능
- 롱 노트 D 누름 / 뗌 입력 가능
- 적 WANDER 상태 동작 가능
- 적 CHASE 상태 동작 가능
- 적 ATTACK READY 상태 표시 가능
- 적 HP 감소 가능
- 적 HP Bar / HP 숫자 갱신 가능
- 적 HP 0 도달 시 사망 처리 가능
- 적 사망 후 2초 뒤 비활성화 가능

### A 키 접촉 데미지 검증

- A 키 입력 시 접촉 중인 적에게 25 데미지 적용
- 방향키 입력만으로는 데미지 발생하지 않음
- A 키 입력이 다음 프레임에 남아 뒤늦게 데미지를 발생시키지 않음
- `DummyPlayerAttack.contactNoteHitKey = A`
- `NoteDebugInput.singleNoteKey = A`

### 빌드 / Unity 확인

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj`
  - 경고 0개
  - 오류 0개

- Unity Refresh / Compile 완료
- 활성 씬 확인
  - `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

---

## 다른 파트와의 연결 상태

현재 구현은 다른 파트 코드와 직접 연결하지 않았다.

### 연결하지 않은 항목

- 실제 플레이어 파트 코드
- 실제 UI 파트 코드
- 실제 리듬 판정 시스템
- 실제 보스 시스템
- 실제 NPC 시스템

### 이후 연결 시 참고

- 실제 플레이어가 준비되면 `DummyPlayer`를 교체하면 된다.
- 실제 리듬 판정 시스템이 준비되면 `NoteDebugInput`의 임시 키 입력 이벤트를 대체하면 된다.
- 실제 UI가 준비되면 `EnemyDebugOverlay`의 임시 HP / STATE 표시를 대체하거나 비활성화하면 된다.
- 실제 공격 시스템이 준비되면 `DummyPlayerAttack`의 A/J 임시 공격을 제거하거나 브릿지 형태로 교체하면 된다.

---

## 현재 결론

2026-07-17 기준, 1주차 Enemy 독립 테스트 환경 구현은 완료 상태다.

현재 씬만 실행해도 다른 팀 코드 없이 다음 흐름을 확인할 수 있다.

1. 더미 플레이어 이동
2. 적 배회
3. 적 플레이어 감지
4. 적 추격
5. A 키 접촉 데미지
6. HP 감소
7. 사망 처리
8. 사망 후 2초 뒤 비활성화
