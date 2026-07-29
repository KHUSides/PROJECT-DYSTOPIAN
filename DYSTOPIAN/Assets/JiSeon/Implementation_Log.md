# Enemy / Boss / NPC 구현 기록

이 문서는 `Assets/JiSeon` 파트의 Enemy / Boss / NPC 구현 현황을 정리한 기록입니다.

다른 파트 작업자나 다른 AI가 현재 구조를 바로 파악할 수 있도록, 시행착오 기록이 아니라 최종 구현 상태 기준으로만 작성합니다.

---

## 작업 기준

- 작업 루트: `Assets/JiSeon`
- 스크립트 위치: `Assets/JiSeon/Scripts`
- 프리팹 위치: `Assets/JiSeon/Prefabs`
- 씬 위치: `Assets/JiSeon/Scenes`
- 애니메이션 컨트롤러 위치: `Assets/JiSeon/Animations`
- 외부 원본 에셋은 직접 수정하지 않고, `Assets/JiSeon` 안에 복제본 또는 신규 에셋을 만들어 사용합니다.
- 게임은 3D 오브젝트 기반 사이드뷰 테스트 환경으로 구성합니다.

---

## 현재 메인 테스트 씬

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

현재 씬은 3D 사이드뷰 Enemy 테스트 씬입니다. `PlayerTest` 기반 리듬 테스트를 위해 다음 오브젝트를 포함합니다.

- `Player`
  - `PlayerHealthDamageAdapter`
  - `PlayerAttackEnemyDamageAdapter`
- `Rhythm System`
- `HealthCanvas`
- `UpgradeCanvas`
- `EventSystem`
- `ArmoredGuard_Melee`
- `ArmoredGuard_Ranged`

현재 2주차 테스트 기준으로는 더미 플레이어보다 실제 `PlayerTest` 기반 `Player`와 `Rhythm System` 연동 확인을 우선합니다.

---

## 2026-07-17 - 1주차 독립 테스트 환경 구현 완료

### 완료 범위

1주차 목표였던 Enemy 독립 테스트 환경을 구현했습니다.

완료 기준은 “다른 팀 코드 없이 적이 플레이어를 추격하고 피격·사망한다”이며, 현재 해당 흐름을 테스트할 수 있습니다.

### 구현 기능

- Enemy/Boss/NPC 테스트 씬 제작
- 더미 플레이어 제작
- 임시 공격 기능 제작
- 단일 / 롱 노트 디버그 이벤트 발생기 제작
- 적 체력 / 피격 / 사망 구현
- 적 배회 / 추격 구현
- 적 상태 UI 표시
- 적 체력바 표시
- 적 감지 범위 기즈모 표시

### 주요 스크립트

- `Assets/JiSeon/Scripts/Combat/DamageInfo.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyHealth.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyController.cs`
- `Assets/JiSeon/Scripts/Debug/EnemyDebugOverlay.cs`
- `Assets/JiSeon/Scripts/Debug/NoteDebugInput.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerController.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerAttack.cs`

### 1주차 조작

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 더미 플레이어 좌우 이동 |
| UpArrow | 더미 플레이어 점프 |
| A | 단일 노트 디버그 이벤트 및 접촉 중인 적 피격 테스트 |
| D 누름 | 롱 노트 디버그 시작 이벤트 |
| D 뗌 | 롱 노트 디버그 종료 이벤트 |
| J | 플레이어 전방 임시 공격 테스트 |

---

## 2026-07-23 - 2주차 적 전투 시스템 구현 진행

### 2주차 목표

기획서의 “부패한 쥐” 대신 현재 사용 가능한 사람형 Synty 에셋을 활용하여 갑옷 경비병 적을 먼저 구현합니다.

현재 목표는 다음과 같습니다.

- 단일 노트 공격
- 롱 노트 차징 공격
- 근거리 히트박스
- 원거리 투사체
- 공격 사거리 및 방향 처리
- 플랫폼 이탈 방지
- 갑옷 경비병 근거리 대표 적 제작
- 갑옷 경비병 원거리 대표 적 기본 베이스 제작

---

## 현재 사용 에셋

### Synty 원본 에셋

원본은 직접 수정하지 않습니다.

- 캐릭터 원본: `Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab`
- 근거리 무기 원본: `Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Greatsword_Round_01.prefab`
- 원거리 무기 원본: `Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Staff_Gem_01.prefab`

### JiSeon 폴더 내 복제 / 제작 에셋

- 근거리 시각 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_KnightVisual.prefab`
- 원거리 시각 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_RangedVisual.prefab`
- 근거리 무기 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_Greatsword_Round.prefab`
- 원거리 무기 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_Staff.prefab`
- 근거리 적 프리팹: `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Melee.prefab`
- 원거리 적 프리팹: `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Ranged.prefab`
- 원거리 투사체 프리팹: `Assets/JiSeon/Prefabs/Projectiles/EnemyMagicProjectile.prefab`

### Kevin Iglesias 애니메이션 에셋

사용 위치:

- `Assets/Kevin Iglesias/Human Animations`

근거리 갑옷 경비병은 Female 2H 계열 휴머노이드 애니메이션을 사용합니다. Synty 캐릭터는 Humanoid 리깅 기반으로 해당 애니메이션을 재생할 수 있습니다.

---

## ArmoredGuard_Melee 현재 구현 상태

### 역할

근거리 갑옷 경비병입니다.

리듬 시스템의 Enemy 노트 이벤트를 받고, 플레이어를 추격 중일 때만 근거리 공격을 실행합니다.

### 구성 컴포넌트

- `CharacterController`
- `EnemyHealth`
- `EnemyController`
- `EnemyDebugOverlay`
- `EnemyMeleeCombat`
- `EnemyRhythmCombatBridge`
- `EnemyAnimationController`

### 시각 구성

- `VisualRoot`
  - `ArmoredGuard_KnightVisual`
    - `Root/Hips/.../Hand_R`
      - `WeaponSocket_R`
        - `ArmoredGuard_Greatsword_Round`

`ArmoredGuard_Greatsword_Round`는 Synty 캐릭터의 오른손 본 `Hand_R`에 바로 붙이지 않고, `WeaponSocket_R` 하위에 배치되어 있습니다. `WeaponSocket_R`는 Kevin Iglesias 2H 대검 애니메이션의 무기 소켓 방향을 참고해 설정했으며, Idle / Move / SingleNoteAttack 포즈에서 검날이 기사 몸 안쪽으로 눕지 않도록 보정되어 있습니다.

### 단일 노트 공격

- 조건: Enemy 단일 노트가 판정선에 도달했고, 적이 플레이어를 `Chase` 중일 때
- 방향: 공격 직전 플레이어 방향을 바라봄
- 방식: 전방 근거리 히트박스
- 현재 데미지: `12`
- Hitbox Offset: `(1.15, 1, 0)`
- Hitbox Size: `(1.8, 1.8, 1.2)`
- 애니메이션: `HumanF@Attack2H01`
- 애니메이션 시작 지점: normalized time `0.22`
- 애니메이션 속도: `1.5`
- 공격 이동 잠금 시간: `1.10초`

### 롱 노트 차징 공격

- 조건: Enemy 롱 노트가 판정선에 도달했고, 적이 플레이어를 `Chase` 중일 때
- 롱 노트 시작: 차징 시작 자세를 재생하고 제자리에서 대기
- 롱 노트 종료: 종료 순간 플레이어 방향을 바라보고 차징 공격 실행
- 방식: 전방 강화 근거리 히트박스
- 현재 최소 데미지: `20`
- 현재 최대 데미지: `40`
- Full Charge Time: `1.5초`
- Charged Hitbox Offset: `(1.45, 1, 0)`
- Charged Hitbox Size: `(2.6, 2, 1.4)`
- 차징 시작 애니메이션: `HumanF@Attack2H01`
- 차징 시작 애니메이션 시작 지점: normalized time `0.08`
- 차징 시작 애니메이션 속도: `0`
- 롱 노트 종료 공격 애니메이션: `HumanF@Attack2H01`
- 롱 노트 종료 공격 시작 지점: normalized time `0.18`
- 롱 노트 종료 공격 속도: `1.5`
- 롱 노트 종료 공격 이동 잠금 시간: `1.20초`

차징 시작 상태는 공격 클립의 초반 준비 자세에서 멈추고, 롱 노트 종료 시점에 같은 2H 공격 클립의 휘두르기 구간부터 바로 재생되도록 구성되어 있습니다.

---

## ArmoredGuard_Ranged 현재 구현 상태

### 역할

원거리 갑옷 경비병 기본 베이스입니다.

리듬 시스템의 Enemy 노트 이벤트를 받고, 플레이어를 추격 중일 때만 원거리 투사체 공격을 실행합니다.

### 구성 컴포넌트

- `CharacterController`
- `EnemyHealth`
- `EnemyController`
- `EnemyDebugOverlay`
- `EnemyRangedCombat`
- `EnemyRhythmCombatBridge`
- `EnemyAnimationController`

### 시각 구성

- `VisualRoot`
  - `ArmoredGuard_RangedVisual`
  - `ArmoredGuard_Staff`

### 단일 노트 공격

- 조건: Enemy 단일 노트가 판정선에 도달했고, 적이 플레이어를 `Chase` 중일 때
- 방식: 전방 원거리 투사체 발사
- 현재 데미지: `8`
- Fire Range: `8`
- Projectile Speed: `8`
- Projectile Hit Radius: `0.45`
- Projectile Life Time: `3초`

### 롱 노트 차징 공격

- 조건: Enemy 롱 노트가 판정선에 도달했고, 적이 플레이어를 `Chase` 중일 때
- 롱 노트 시작: 차징 시작
- 롱 노트 종료: 종료 순간 플레이어 방향을 바라보고 강화 투사체 발사
- 현재 최소 데미지: `16`
- 현재 최대 데미지: `32`
- Full Charge Time: `1.5초`
- Charged Projectile Speed: `10`
- Charged Projectile Hit Radius: `0.6`

---

## 현재 Animator Controller

### 컨트롤러

- 근거리: `Assets/JiSeon/Animations/Enemy/ArmoredGuard_Melee.controller`
- 원거리: `Assets/JiSeon/Animations/Enemy/ArmoredGuard_Ranged.controller`

### 공통 Animator Parameter

| Parameter | Type | 용도 |
|---|---|---|
| `Moving` | Bool | 이동 상태 |
| `Speed` | Float | 이동 속도 |
| `Charging` | Bool | 롱 노트 차징 상태 |
| `Dead` | Bool | 사망 상태 |
| `SingleAttack` | Trigger | 단일 노트 공격 |
| `ChargedAttack` | Trigger | 롱 노트 종료 공격 |
| `Hit` | Trigger | 피격 |
| `Die` | Trigger | 사망 |

### 근거리 컨트롤러 상태

| State | Motion |
|---|---|
| `Idle` | `HumanF@CombatIdle2H01` |
| `Move` | `Move_WalkRun_2H` BlendTree |
| `LongChargeStart` | `HumanF@Attack2H01` |
| `SingleNoteAttack` | `HumanF@Attack2H01` |
| `LongNoteChargedAttack` | `HumanF@Attack2H01` |
| `Hit` | `HumanF@CombatDamage01` |
| `Death` | `HumanF@Death01` |
| `TurnLeft` | `HumanF@Turn01_Left` |
| `TurnRight` | `HumanF@Turn01_Right` |

`LongChargeStart`는 속도 `0`으로 설정되어 공격 초반 준비 자세에서 멈춥니다.

### 근거리 Move BlendTree

| Threshold | Clip |
|---:|---|
| `0.35` | `HumanF@Walk01_Forward` |
| `2.80` | `HumanF@Run01_Forward` |

---

## 현재 스크립트 역할 정리

### `EnemyController.cs`

적의 기본 AI를 담당합니다.

- Idle / Wander / Chase / Dead 상태 관리
- 플레이어 감지
- 배회
- 추격
- 공격 사거리 도달 시 정지
- 플랫폼 가장자리 / 벽 감지 기반 이동 제한
- 사망 상태 전환
- 현재 상태, 타겟, 바라보는 방향 공개
- 공격 / 차징 중 이동 잠금 처리

현재 공격 자체는 자동 공격이 아니라 리듬 이벤트를 받은 전투 스크립트가 실행합니다. `EnemyController`는 적이 추격 중인지, 어느 방향을 보고 있는지, 공격 중 이동을 멈춰야 하는지를 관리합니다.

### `EnemyHealth.cs`

적 체력과 사망 처리를 담당합니다.

- HP 감소
- 피격 이벤트 발행
- 사망 이벤트 발행
- 사망 시 Collider 비활성화
- 사망 후 2초 뒤 GameObject 비활성화

### `EnemyMeleeCombat.cs`

근거리 경비병 전투를 담당합니다.

- 단일 노트 근거리 히트박스 공격
- 롱 노트 차징 시작
- 롱 노트 종료 시 강화 근거리 히트박스 공격
- 공격 직전 플레이어 방향 갱신
- 공격 애니메이션 연동 이벤트 발행
- WANDER 중에는 마지막 플레이어 방향이 아니라 `EnemyController`의 이동 방향 기준으로 비주얼 방향 동기화

### `EnemyRangedCombat.cs`

원거리 경비병 전투를 담당합니다.

- 단일 노트 투사체 발사
- 롱 노트 차징 시작
- 롱 노트 종료 시 강화 투사체 발사
- 공격 직전 플레이어 방향 갱신
- 공격 애니메이션 연동 이벤트 발행
- WANDER 중에는 마지막 플레이어 방향이 아니라 `EnemyController`의 이동 방향 기준으로 비주얼 방향 동기화

### `EnemyProjectile.cs`

원거리 공격 투사체를 담당합니다.

- X축 방향 이동
- 수명 시간 처리
- Player 레이어 대상 충돌 판정
- `IDamageable` 대상에게 데미지 전달

### `EnemyRhythmCombatBridge.cs`

실제 `RhythmSystem`과 Enemy 전투 스크립트를 연결합니다.

- `RhythmSystem`의 Enemy 단일 노트 이벤트 수신
- `RhythmSystem`의 Enemy 롱 노트 시작 이벤트 수신
- `RhythmSystem`의 Enemy 롱 노트 종료 이벤트 수신
- 적이 플레이어를 추격 중일 때만 공격 실행
- 적마다 단일 노트 / 롱 노트 반응 여부 설정 가능
- 적마다 근거리 / 원거리 전투 방식 선택 가능

### `EnemyAnimationController.cs`

Enemy 전투 이벤트와 Animator Controller를 연결합니다.

- 이동 / 정지 / 회전 / 차징 / 공격 / 피격 / 사망 애니메이션 전환
- 단일 노트 공격 애니메이션 즉시 재생
- 롱 노트 시작 시 준비 자세를 지정 normalized time에 고정
- 롱 노트 종료 시에만 공격 애니메이션 즉시 재생
- 공격 애니메이션 중 적 이동 잠금
- 롱 노트 차징 중 적 이동 잠금
- 공격 애니메이션이 끝나기 전에 새 Enemy 공격 요청이 들어오면 기존 모션을 즉시 끊고 새 공격 모션으로 재시작
- 새 공격 요청 시 이동 잠금 시간을 현재 시점 기준으로 갱신하여, 공격 중 미끄러지지 않고 제자리에서 다시 공격
- Root Motion 비활성화

### `PlayerHealthDamageAdapter.cs`

`PlayerTest`의 `PlayerHealth`를 Enemy 전투 시스템의 `IDamageable` 방식으로 받을 수 있게 연결합니다.

다른 파트의 `PlayerHealth.cs` 원본 코드는 직접 수정하지 않습니다.

### `PlayerAttackEnemyDamageAdapter.cs`

`PlayerTest`의 `PlayerController` 공격 이벤트를 Enemy 전투 시스템의 `IDamageable` 방식으로 연결합니다.

역할:

- `PlayerController.NormalAttackPerformed` 이벤트 수신
- `PlayerController.ChargedAttackPerformed` 이벤트 수신
- 플레이어 일반 공격 콜라이더가 닿은 EnemyHealth에 데미지 전달
- 플레이어 차징 공격 히트박스가 닿은 EnemyHealth에 데미지 전달
- 다른 파트의 `PlayerController.cs` 원본 코드는 직접 수정하지 않음

현재 데미지:

| 공격 | 데미지 |
|---|---:|
| 일반 공격 | `25` |
| 차징 공격 | `40` |

---

## 현재 Enemy 공격 조건

적은 아래 조건을 모두 만족할 때만 공격합니다.

1. `RhythmSystem`에서 Enemy 노트 이벤트가 발생해야 합니다.
2. 적이 플레이어를 `Chase` 중이어야 합니다.
3. 해당 적이 해당 노트 타입에 반응하도록 설정되어 있어야 합니다.
4. 해당 적의 전투 방식이 실행 가능한 상태여야 합니다.

### 단일 노트

- Enemy 단일 노트가 판정선에 도달하면 이벤트가 발생합니다.
- 적이 추격 중이면 플레이어 방향을 보고 공격합니다.
- 근거리 경비병은 전방 히트박스를 사용합니다.
- 원거리 경비병은 투사체를 발사합니다.

### 롱 노트

- Enemy 롱 노트 시작 이벤트에서 차징을 시작합니다.
- 차징 중에는 적이 제자리에서 준비 자세를 유지합니다.
- Enemy 롱 노트 종료 이벤트에서 플레이어 방향을 보고 차징 공격을 실행합니다.
- 근거리 경비병은 강화 히트박스를 사용합니다.
- 원거리 경비병은 강화 투사체를 발사합니다.

---

## 현재 테스트 방법

테스트 씬:

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

기본 흐름:

1. 씬을 실행합니다.
2. 플레이어를 방향키로 움직여 경비병 감지 범위 안에 들어갑니다.
3. 적 위의 STATE가 `Chase`로 바뀌는지 확인합니다.
4. `R`로 리듬 UI를 표시합니다.
5. `T`로 리듬 차트를 시작합니다.
6. 오른쪽 리듬 UI의 Enemy 노트가 판정선에 도달하면 적이 공격하는지 확인합니다.

PlayerTest 기반 입력:

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 플레이어 좌우 이동 |
| UpArrow | 플레이어 점프 |
| A | 플레이어 단일 노트 입력 |
| D 누름 / 뗌 | 플레이어 롱 노트 시작 / 종료 입력 |
| R | 리듬 UI 표시 / 숨김 |
| T | 리듬 차트 시작 / 정지 |

플레이어 공격 데미지 확인:

- A 입력이 리듬 판정에 성공하여 `PlayerController.NormalAttackPerformed`가 발생하면 일반 공격 범위 안의 적 HP가 감소합니다.
- D 롱 노트 종료로 `PlayerController.ChargedAttackPerformed`가 발생하면 차징 공격 범위 안의 적 HP가 감소합니다.

---

## 2026-07-23 현재 검증 상태

### 빌드

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
  - 경고 0개
  - 오류 0개

### Unity 자동 확인

테스트 대상:

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`
- `ArmoredGuard_Melee`

확인 내용:

- 단일 노트 공격 호출 시 `SingleNoteAttack` 상태가 normalized time `0.22`에서 즉시 시작됨
- 단일 노트 공격 호출 직후 `EnemyController.IsMovementLocked == true`
- 롱 노트 시작 호출 시 `LongChargeStart` 상태가 normalized time `0.08`에서 시작됨
- 롱 노트 시작 중 `EnemyController.IsMovementLocked == true`
- 롱 노트 종료 호출 시 `LongNoteChargedAttack` 상태가 normalized time `0.18`에서 즉시 시작됨
- 롱 노트 종료 공격 중 `EnemyController.IsMovementLocked == true`
- WANDER 상태에서 플레이어가 마지막으로 있던 방향이 아니라 실제 AI 이동 방향으로 비주얼 방향이 동기화됨

### Animator Controller 확인

`Assets/JiSeon/Animations/Enemy/ArmoredGuard_Melee.controller` 기준:

- `LongChargeStart`
  - Motion: `HumanF@Attack2H01`
  - Speed: `0`
- `SingleNoteAttack`
  - Motion: `HumanF@Attack2H01`
  - Speed: `1.5`
- `LongNoteChargedAttack`
  - Motion: `HumanF@Attack2H01`
  - Speed: `1.5`

### Unity 로그

이번 Enemy 코드 / Animator 수정으로 발생한 신규 C# 컴파일 에러는 확인되지 않았습니다.

---

## 2026-07-24 현재 구현 상태

### Enemy 연속 공격 처리

Enemy 공격 요청은 더 이상 이전 공격 애니메이션 종료를 기다리다가 스킵되지 않습니다.

현재 동작:

- Enemy 단일 노트 공격 요청이 들어오면 `SingleNoteAttack` 상태를 즉시 재생합니다.
- Enemy 롱 노트 시작 요청이 들어오면 `LongChargeStart` 상태를 normalized time `0.08`에 고정합니다.
- Enemy 롱 노트 종료 요청이 들어올 때만 `LongNoteChargedAttack` 상태를 즉시 재생합니다.
- 롱 노트 차징 중에는 `ChargedAttack` Animator Trigger를 사용하지 않고, 스크립트의 직접 재생만 사용합니다.
- 이전 공격 애니메이션이 아직 끝나지 않았더라도 새 공격 요청이 들어오면 새 공격 모션으로 바로 갈아탑니다.
- 공격 중 적 이동 정지는 유지되며, 새 공격 요청 시 이동 잠금 시간은 현재 시점 기준으로 다시 갱신됩니다.
- `EnemyMeleeCombat` / `EnemyRangedCombat`의 `attackCooldownSeconds`는 `0`으로 설정되어, 리듬 노트 간격이 짧아도 전투 스크립트 쪽 쿨다운 때문에 공격이 스킵되지 않습니다.

관련 파일:

- `Assets/JiSeon/Scripts/Enemy/EnemyAnimationController.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyMeleeCombat.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyRangedCombat.cs`
- `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Melee.prefab`
- `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Ranged.prefab`

### 플레이어 공격 → 적 HP 감소 연결

`PlayerAttackEnemyDamageAdapter`를 추가하여 플레이어 공격 이벤트가 적 체력을 깎도록 연결했습니다.

관련 파일:

- `Assets/JiSeon/Scripts/Test/PlayerAttackEnemyDamageAdapter.cs`
- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

현재 `EnemyBossNPC_Test.unity`의 `Player` 오브젝트에 `PlayerAttackEnemyDamageAdapter`가 추가되어 있습니다.

### 갑옷 경비병 대검 위치 / 방향 보정

근거리 갑옷 경비병의 대검이 손에는 붙어 있지만 Idle / Move / Attack 포즈에서 검날 방향이 몸 안쪽으로 눕는 문제를 막기 위해 무기 소켓을 추가했습니다.

현재 구조:

- `ArmoredGuard_Melee`
  - `VisualRoot`
    - `ArmoredGuard_KnightVisual`
      - `Root/Hips/.../Hand_R`
        - `WeaponSocket_R`
          - `ArmoredGuard_Greatsword_Round`

현재 소켓 값:

- `WeaponSocket_R.localPosition`: `(0.0030, 0.1053, 0.0226)`
- `WeaponSocket_R.localEulerAngles`: `(291.94, 0.43, 336.99)`
- `ArmoredGuard_Greatsword_Round.localPosition`: `(0, 0, 0)`
- `ArmoredGuard_Greatsword_Round.localEulerAngles`: `(0, 0, 0)`
- `ArmoredGuard_Greatsword_Round.localScale`: `(1.1, 1.1, 1.1)`

관련 파일:

- `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Melee.prefab`
- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`
- `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_Greatsword_Round.prefab`

### 빌드 검증

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj`
  - 경고 0개
  - 오류 0개
- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
  - 경고 0개
  - 오류 0개

### Unity Play 검증 상태

Unity Play 모드에서 `ArmoredGuard_Melee`의 Animator 상태를 직접 재생해 검 위치를 확인했습니다.

확인한 상태:

- `Idle`
- `Move`
- `Move` normalized time `0.35`
- `SingleNoteAttack` normalized time `0.18`
- `SingleNoteAttack` normalized time `0.22`
- `SingleNoteAttack` normalized time `0.48`
- `SingleNoteAttack` normalized time `0.78`

확인 결과:

- 정지 / 이동 / 공격 포즈에서 대검이 `Hand_R > WeaponSocket_R` 기준으로 유지됩니다.
- Idle 포즈에서 검날이 허리 방향으로 눕지 않고 손 기준 위쪽 / 바깥쪽으로 세워집니다.
- Move 포즈에서 대검이 기사 몸통 중앙을 가로지르지 않고 손 기준 아래 / 바깥쪽으로 빠집니다.
- `SingleNoteAttack` 중간 지점에서 검날이 플레이어 방향으로 뻗으며, 기사 몸통 안쪽에 검날을 두고 휘두르는 형태가 줄어든 상태입니다.
- Unity Console 기준 신규 C# 컴파일 오류는 없습니다.

---

## 2026-07-27 현재 구현 상태

### 갑옷 경비병 대검 소켓 재조정

근거리 갑옷 경비병의 대검이 Idle / Move / SingleNoteAttack 포즈에서 기사 몸통을 관통해 보이지 않도록 `WeaponSocket_R` 방향을 다시 조정했습니다.

현재 구조:

- `ArmoredGuard_Melee`
  - `VisualRoot`
    - `ArmoredGuard_KnightVisual`
      - `Root/Hips/.../Hand_R`
        - `WeaponSocket_R`
          - `ArmoredGuard_Greatsword_Round`

현재 소켓 값:

- `WeaponSocket_R.localPosition`: `(0.0030, 0.1053, 0.0226)`
- `WeaponSocket_R.localEulerAngles`: `(291.94, 0.43, 336.99)`
- `ArmoredGuard_Greatsword_Round.localPosition`: `(0, 0, 0)`
- `ArmoredGuard_Greatsword_Round.localEulerAngles`: `(0, 0, 0)`
- `ArmoredGuard_Greatsword_Round.localScale`: `(1.1, 1.1, 1.1)`

관련 파일:

- `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Melee.prefab`
- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

Unity Play 모드에서 확인한 상태:

- `Idle` normalized time `0.25`
- `Move` normalized time `0.35`
- `SingleNoteAttack` normalized time `0.22`
- `SingleNoteAttack` normalized time `0.48`
- `SingleNoteAttack` normalized time `0.78`

확인 결과:

- Idle 상태에서 검날이 허리 / 몸통 가로 방향으로 눕지 않고 위쪽으로 세워집니다.
- Move 상태에서 대검이 몸통 중앙을 가로지르지 않고 손 기준 아래 / 바깥쪽에 위치합니다.
- 공격 중간 프레임에서 검날이 플레이어 방향으로 뻗으며, 몸통 안쪽에 검날을 두고 휘두르는 형태가 줄어든 상태입니다.
- Unity Console 기준 신규 C# 컴파일 오류는 없습니다.

---

## 다른 파트와의 연결 상태

현재 테스트를 위해 `PlayerTest` 씬의 주요 오브젝트를 `EnemyBossNPC_Test.unity`에 복사하여 사용합니다.

참조 중인 다른 파트 요소:

- `Assets/DongJun/Scripts/Rhythm Scripts/RhythmSystem.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerHealth.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerController.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerRhythmAttackBridge.cs`

Enemy 파트에서 추가한 연결 방식:

- `EnemyRhythmCombatBridge.cs`로 리듬 Enemy 노트 이벤트 수신
- `PlayerHealthDamageAdapter.cs`로 PlayerHealth 데미지 연동
- `PlayerAttackEnemyDamageAdapter.cs`로 PlayerController 공격 이벤트를 EnemyHealth 데미지로 연동

다른 파트 원본 스크립트는 직접 수정하지 않았습니다.

---

## 이후 참고 사항

- 실제 스테이지 씬에 적을 배치할 때는 같은 씬 안에 `RhythmSystem`이 있어야 합니다.
- 실제 플레이어 오브젝트가 `Player` 레이어가 아니면 적의 `targetMask`를 맞춰야 합니다.
- 적마다 단일 노트만 반응하거나 롱 노트만 반응하도록 `EnemyRhythmCombatBridge` 설정을 조정할 수 있습니다.
- 근거리 / 원거리 공격 방식은 `EnemyMeleeCombat`, `EnemyRangedCombat` 컴포넌트 구성으로 나뉩니다.
- 실제 UI가 준비되면 `EnemyDebugOverlay`의 임시 STATE / HP 표시를 교체하거나 비활성화하면 됩니다.
- 실제 투사체 이펙트가 준비되면 `EnemyMagicProjectile.prefab`의 임시 머티리얼 / 모델을 교체하면 됩니다.
- Boss / NPC 실제 기능은 아직 구현 범위에 들어가지 않았으며, 현재 Enemy 테스트 구조를 기반으로 확장 예정입니다.
