# Enemy / Boss / NPC 구현 기록

이 문서는 `Assets/JiSeon` 파트의 Enemy / Boss / NPC 구현 현황을 정리한 기록입니다.

다른 파트 작업자나 다른 AI가 현재 구조를 빠르게 파악할 수 있도록, 시행착오 과정이 아니라 최종 구현 상태 기준으로 작성합니다.

---

## 작업 기준

- 작업 루트: `Assets/JiSeon`
- 스크립트 위치: `Assets/JiSeon/Scripts`
- 프리팹 위치: `Assets/JiSeon/Prefabs`
- 씬 위치: `Assets/JiSeon/Scenes`
- 애니메이션 컨트롤러 위치: `Assets/JiSeon/Animations`
- 원본 에셋은 직접 수정하지 않고, `Assets/JiSeon` 안에 복제본 또는 신규 에셋을 만들어 사용합니다.
- 게임은 3D 오브젝트 기반 사이드뷰 테스트 환경으로 구성합니다.

---

## 현재 테스트 씬

### Enemy 테스트 씬

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

포함 요소:

- `PlayerTest` 기반 Player
- `Rhythm System`
- `HealthCanvas`
- `UpgradeCanvas`
- `EventSystem`
- `ArmoredGuard_Melee`
- `ArmoredGuard_Ranged`

### Boss 테스트 씬

- `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

포함 요소:

- `PlayerTest` 기반 Player
- `Rhythm System`
- `HealthCanvas`
- `UpgradeCanvas`
- `EventSystem`
- `MerlinBoss`
- `BossHudCanvas`

보스 테스트 씬은 일반 Enemy와 분리된 보스 전용 테스트 씬입니다. 이후 보스별로 별도 씬을 추가하는 방식을 기준으로 합니다.

---

## 2026-07-17 - 1주차 독립 테스트 환경 구현

### 구현 완료 범위

1주차 목표였던 Enemy 독립 테스트 환경을 구현했습니다.

완료 기준:

- 다른 팀 코드 없이도 테스트 씬에서 적이 플레이어를 감지합니다.
- 적이 플레이어를 추격합니다.
- 플레이어가 임시 공격 입력을 통해 적을 피격시킬 수 있습니다.
- 적 HP가 감소하고, HP 0 도달 시 사망 처리됩니다.

### 주요 구현 기능

- Enemy / Boss / NPC 테스트용 사이드뷰 씬 구성
- 더미 플레이어 이동 / 점프 / 공격 기능
- 단일 노트 / 롱 노트 디버그 이벤트 발생 기능
- 적 HP / 피격 / 사망 처리
- 적 Idle / Wander / Chase / Dead 상태 처리
- 적 감지 범위 Gizmo 표시
- 적 머리 위 STATE 표시
- 적 머리 위 HP 바 / 현재 HP 숫자 표시

### 주요 스크립트

- `Assets/JiSeon/Scripts/Combat/DamageInfo.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyHealth.cs`
- `Assets/JiSeon/Scripts/Enemy/EnemyController.cs`
- `Assets/JiSeon/Scripts/Debug/EnemyDebugOverlay.cs`
- `Assets/JiSeon/Scripts/Debug/NoteDebugInput.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerController.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerAttack.cs`
- `Assets/JiSeon/Scripts/Test/DummyPlayerHealth.cs`

### 1주차 테스트 입력

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 더미 플레이어 좌우 이동 |
| UpArrow | 더미 플레이어 점프 |
| A | 단일 노트 디버그 이벤트 / 접촉 중인 적 피격 테스트 |
| D 누름 | 롱 노트 디버그 시작 |
| D 뗌 | 롱 노트 디버그 종료 |
| J | 더미 플레이어 임시 공격 테스트 |

---

## 2026-07-23 - 2주차 적 전투 시스템 구현

### 구현 완료 범위

2주차 목표였던 적 전투 시스템의 기본 구조를 구현했습니다.

기획서의 “부패한 쥐”는 현재 사용 가능한 적 에셋 상황에 맞춰 “갑옷 경비병”으로 대체했습니다.

완료 기준:

- 테스트 씬에서 Rhythm System의 Enemy 노트 이벤트에 맞춰 적들이 공격합니다.
- 근거리 대표 적과 원거리 대표 적 기본 베이스가 존재합니다.
- Enemy는 노트 이벤트만으로 공격하며, 자동 공격하지 않습니다.

### 사용 원본 에셋

원본 에셋은 직접 수정하지 않았습니다.

- 캐릭터 원본: `Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab`
- 근거리 무기 원본: `Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Greatsword_Round_01.prefab`
- 원거리 무기 원본: `Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Staff_Gem_01.prefab`
- 애니메이션 원본: `Assets/Kevin Iglesias/Human Animations`

### JiSeon 폴더 내 복제 / 제작 에셋

- 근거리 시각 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_KnightVisual.prefab`
- 원거리 시각 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_RangedVisual.prefab`
- 근거리 무기 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_Greatsword_Round.prefab`
- 원거리 무기 프리팹: `Assets/JiSeon/Prefabs/SyntyCopies/ArmoredGuard_Staff.prefab`
- 근거리 적 프리팹: `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Melee.prefab`
- 원거리 적 프리팹: `Assets/JiSeon/Prefabs/Enemies/ArmoredGuard_Ranged.prefab`
- 원거리 투사체 프리팹: `Assets/JiSeon/Prefabs/Projectiles/EnemyMagicProjectile.prefab`
- 원거리 투사체 임시 머티리얼: `Assets/JiSeon/Etc_/EnemyMagicProjectile.mat`

### ArmoredGuard_Melee 현재 구현 상태

역할:

- 근거리 갑옷 경비병입니다.
- 플레이어를 감지하면 Chase 상태가 됩니다.
- Enemy 단일 노트 / 롱 노트 이벤트가 판정선에 도달했을 때, Chase 중이면 공격합니다.

구성 컴포넌트:

- `CharacterController`
- `EnemyHealth`
- `EnemyController`
- `EnemyDebugOverlay`
- `EnemyMeleeCombat`
- `EnemyRhythmCombatBridge`
- `EnemyAnimationController`

단일 노트 공격:

- 조건: Enemy 단일 노트 도달 + 적이 Chase 상태
- 방식: 전방 근거리 히트박스
- 현재 데미지: `12`
- Hitbox Offset: `(1.15, 1, 0)`
- Hitbox Size: `(1.8, 1.8, 1.2)`
- 애니메이션: `HumanF@Attack2H01`
- 공격 애니메이션 속도: `1.5`
- 공격 중 이동 잠금 적용

롱 노트 차징 공격:

- 조건: Enemy 롱 노트 시작 / 종료 이벤트 + 적이 Chase 상태
- 롱 노트 시작: 차징 준비 자세 유지
- 롱 노트 종료: 플레이어 방향을 보고 강화 근거리 히트박스 공격
- 현재 최소 데미지: `20`
- 현재 최대 데미지: `40`
- Full Charge Time: `1.5초`
- Charged Hitbox Offset: `(1.45, 1, 0)`
- Charged Hitbox Size: `(2.6, 2, 1.4)`
- 공격 애니메이션: `HumanF@Attack2H01`
- 공격 애니메이션 속도: `1.5`
- 차징 / 공격 중 이동 잠금 적용

### ArmoredGuard_Ranged 현재 구현 상태

역할:

- 원거리 갑옷 경비병 기본 베이스입니다.
- 플레이어를 감지하면 Chase 상태가 됩니다.
- Enemy 단일 노트 / 롱 노트 이벤트가 판정선에 도달했을 때, Chase 중이면 투사체 공격을 실행합니다.

구성 컴포넌트:

- `CharacterController`
- `EnemyHealth`
- `EnemyController`
- `EnemyDebugOverlay`
- `EnemyRangedCombat`
- `EnemyRhythmCombatBridge`
- `EnemyAnimationController`

단일 노트 공격:

- 방식: 전방 원거리 투사체 발사
- 현재 데미지: `8`
- Fire Range: `8`
- Projectile Speed: `8`
- Projectile Hit Radius: `0.45`
- Projectile Life Time: `3초`

롱 노트 차징 공격:

- 롱 노트 시작: 차징 시작
- 롱 노트 종료: 플레이어 방향을 보고 강화 투사체 발사
- 현재 최소 데미지: `16`
- 현재 최대 데미지: `32`
- Full Charge Time: `1.5초`
- Charged Projectile Speed: `10`
- Charged Projectile Hit Radius: `0.6`

### Enemy 공통 공격 조건

Enemy 공격은 아래 조건을 모두 만족할 때만 실행됩니다.

1. `RhythmSystem`에서 Enemy 노트 이벤트가 발생해야 합니다.
2. 적이 플레이어를 Chase 중이어야 합니다.
3. 해당 적이 해당 노트 타입에 반응하도록 설정되어 있어야 합니다.
4. 적이 공격 가능한 상태여야 합니다.

### Enemy 관련 주요 스크립트

- `Assets/JiSeon/Scripts/Enemy/EnemyController.cs`
  - Idle / Wander / Chase / Dead 상태 관리
  - 플레이어 감지
  - 추격 / 배회
  - 플랫폼 이탈 방지
  - 공격 / 차징 중 이동 잠금

- `Assets/JiSeon/Scripts/Enemy/EnemyHealth.cs`
  - HP 감소
  - 피격 이벤트
  - 사망 이벤트
  - 사망 후 Collider 비활성화
  - 사망 후 2초 뒤 GameObject 비활성화

- `Assets/JiSeon/Scripts/Enemy/EnemyMeleeCombat.cs`
  - 단일 노트 근거리 히트박스 공격
  - 롱 노트 차징 시작
  - 롱 노트 종료 시 강화 근거리 히트박스 공격
  - 공격 직전 플레이어 방향 갱신

- `Assets/JiSeon/Scripts/Enemy/EnemyRangedCombat.cs`
  - 단일 노트 투사체 발사
  - 롱 노트 차징 시작
  - 롱 노트 종료 시 강화 투사체 발사
  - 공격 직전 플레이어 방향 갱신

- `Assets/JiSeon/Scripts/Enemy/EnemyProjectile.cs`
  - X축 방향 이동
  - 수명 시간 처리
  - Player 레이어 충돌 판정
  - `IDamageable` 대상에게 데미지 전달

- `Assets/JiSeon/Scripts/Enemy/EnemyRhythmCombatBridge.cs`
  - `RhythmSystem` Enemy 노트 이벤트와 Enemy 전투 스크립트 연결
  - 적마다 단일 노트 / 롱 노트 반응 여부 설정 가능
  - 적마다 근거리 / 원거리 전투 방식 선택 가능

- `Assets/JiSeon/Scripts/Enemy/EnemyAnimationController.cs`
  - 이동 / 정지 / 회전 / 차징 / 공격 / 피격 / 사망 애니메이션 전환
  - 공격 애니메이션 즉시 재생
  - 공격 요청이 연속으로 들어오면 이전 공격 모션 종료를 기다리지 않고 새 공격 모션 재생
  - Root Motion 비활성화 기준

### Player 연동 어댑터

- `Assets/JiSeon/Scripts/Test/PlayerHealthDamageAdapter.cs`
  - `PlayerTest`의 `PlayerHealth`를 `IDamageable` 방식으로 받을 수 있게 연결합니다.
  - 원본 Player 스크립트는 직접 수정하지 않습니다.

- `Assets/JiSeon/Scripts/Test/PlayerAttackEnemyDamageAdapter.cs`
  - `PlayerController.NormalAttackPerformed`
  - `PlayerController.ChargedAttackPerformed`
  - 플레이어 공격 범위 안의 `EnemyHealth` 또는 `BossHealth`에 데미지를 전달합니다.
  - 원본 Player 스크립트는 직접 수정하지 않습니다.

---

## 2026-07-27 - 3주차 멀린 보스 시스템 1차 구현

### 구현 완료 범위

3주차 목표인 `Merlin, The Indomitable Gatekeeper` 보스전 기본 구조를 구현했습니다.

멀린은 일반 Enemy처럼 배회하거나 추격하지 않습니다. 기획 기준에 맞춰 공격하지 않을 때는 이동하지 않고, Rhythm System의 Boss 노트 이벤트에 반응할 때만 패턴을 실행합니다.

### 보스 프리팹

- `Assets/JiSeon/Prefabs/Bosses/MerlinBoss.prefab`

구성 컴포넌트:

- `CharacterController`
- `BossHealth`
- `BossController`
- `MerlinBossCombat`
- `BossRhythmCombatBridge`
- `BossAnimationController`
- `MerlinHeldSpearPoseController`

### 임시 시각 프리팹

- `Assets/JiSeon/Prefabs/SyntyCopies/Merlin_TempVisual.prefab`

사용 원본:

- `Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Male_01.prefab`

멀린 전용 실제 프리팹이 완성되기 전까지 임시 보스 외형으로 사용합니다.

### 창 프리팹

- 손에 들고 있는 창: `Assets/JiSeon/Prefabs/SyntyCopies/Merlin_Spear.prefab`
- 창 투사체: `Assets/JiSeon/Prefabs/Projectiles/MerlinSpearProjectile.prefab`

사용 원본:

- `Assets/Synty/PolygonDungeon/Prefabs/Weapons/SM_Wep_Spear_01.prefab`

현재 구조:

- `MerlinBoss`
  - `VisualRoot`
    - `Merlin_TempVisual`
      - `Root/Hips/.../Hand_R`
        - `WeaponSocket_R`
          - `Merlin_Spear`

### 멀린 보스 공통 상태

관련 스크립트:

- `Assets/JiSeon/Scripts/Boss/BossController.cs`
- `Assets/JiSeon/Scripts/Boss/BossHealth.cs`

상태:

| State | 역할 |
|---|---|
| `Intro` | 등장 직후 짧은 대기 |
| `Idle` | 기본 대기, 이동하지 않음 |
| `Charging` | 롱 노트 시작 후 창 투척 준비 |
| `Attacking` | 단일 노트 공격 또는 롱 노트 종료 공격 |
| `Recovering` | 창 회수 후 짧은 복귀 |
| `Dead` | 사망 상태 |

보스 이동 규칙:

- 멀린은 Wander / Chase를 하지 않습니다.
- 공격하지 않을 때는 제자리에서 대기합니다.
- 패턴 실행 직전 플레이어 방향을 바라봅니다.
- Root Motion은 비활성화되어 있습니다.

### 보스 체력 / HUD

관련 스크립트:

- `Assets/JiSeon/Scripts/Boss/BossHealth.cs`
- `Assets/JiSeon/Scripts/Boss/BossHud.cs`

현재 임시 체력:

- Max HP: `500`
- Current HP: `500`

HUD 표시:

- 화면 중앙 상단: 보스 이름
- 화면 중앙 상단: 보스 HP 바
- 화면 중앙 상단: 현재 HP / 최대 HP
- 화면 우하단: 단일 노트 카운터
- 화면 우하단: 다음 단일 노트 패턴

현재 표시 예시:

- Boss Name: `Merlin, The Indomitable Gatekeeper`
- HP: `500/500`
- Single Counter: `0`
- Next: `Thrust`

### Rhythm System 연결

관련 스크립트:

- `Assets/JiSeon/Scripts/Boss/BossRhythmCombatBridge.cs`
- `Assets/DongJun/Scripts/Rhythm Scripts/RhythmSystem.cs`

`MerlinBoss_Test.unity`의 `RhythmSystem` 기준:

- `opponentActor`: `Boss`
- `R`: 리듬 UI 표시 / 숨김
- `T`: 차트 시작 / 정지

브리지 동작:

- Boss 단일 노트 이벤트 수신
- Boss 롱 노트 시작 이벤트 수신
- Boss 롱 노트 종료 이벤트 수신
- 테스트 편의를 위해 Enemy 이벤트도 수신 가능하도록 설정 가능

### 멀린 단일 노트 패턴

관련 스크립트:

- `Assets/JiSeon/Scripts/Boss/MerlinBossCombat.cs`
- `Assets/JiSeon/Scripts/Boss/BossAnimationController.cs`

규칙:

| 단일 노트 카운트 | 패턴 |
|---:|---|
| 홀수 | 창 찌르기 |
| 짝수 | 창 휘두르기 |

현재 임시 값:

- 공격력: `18`
- 찌르기 Hitbox Offset: `(1.35, 1.05, 0)`
- 찌르기 Hitbox Size: `(2.2, 1.25, 1.2)`
- 휘두르기 Hitbox Offset: `(1.15, 1.1, 0)`
- 휘두르기 Hitbox Size: `(2.6, 1.65, 1.4)`

동작:

- Boss 단일 노트가 판정선에 도달하면 실행됩니다.
- 공격 직전 플레이어 방향을 바라봅니다.
- 1회차 단일 노트는 창 찌르기입니다.
- 2회차 단일 노트는 창 휘두르기입니다.
- 이후 같은 규칙으로 반복됩니다.

### 멀린 롱 노트 패턴

관련 스크립트:

- `Assets/JiSeon/Scripts/Boss/MerlinBossCombat.cs`
- `Assets/JiSeon/Scripts/Boss/MerlinSpearProjectile.cs`
- `Assets/JiSeon/Scripts/Boss/MerlinHeldSpearPoseController.cs`

현재 임시 값:

- 창 투척 공격력: `28`
- 창 투척 속도: `26`
- 창 회수 속도: `75`
- 창 최대 거리: `14`
- 창 Hit Radius: `0.45`
- 창 투척 release delay: `0.45초`
- Spear blocking mask: `SpearBlocker` 레이어

동작:

- 롱 노트 시작 시 `Charging` 상태로 전환합니다.
- 롱 노트 시작 동안 창을 플레이어 방향으로 가로로 눕힌 투척 준비 자세를 유지합니다.
- 롱 노트 종료 시 플레이어 방향을 바라보고 `SpearThrow` 애니메이션을 재생합니다.
- 손을 놓는 타이밍에 맞춰 `0.45초` 뒤에 손에 든 창을 숨기고, 같은 위치 / 회전에서 창 투사체를 생성합니다.
- 창 투사체는 손 애니메이션 위치가 낮아져도 최소 높이 `1.5` 이상에서 생성되며, Z는 `0`으로 고정됩니다.
- 창 투사체는 발사 순간 높이의 Y를 유지한 채 멀린이 바라보는 좌우 방향으로 수평 직선 비행합니다.
- 창 투사체는 플레이어를 관통하며, 플레이어에게 피해를 입혀도 즉시 회수되지 않습니다.
- 창 투사체는 `SpearBlocker` 레이어의 벽에 닿으면 해당 지점에 꽂힌 상태로 잠시 멈춥니다.
- 일반 `Environment` 플랫폼 / 발판은 창 투사체 blocker로 사용하지 않습니다.
- 창 투사체가 환경에 꽂히거나 최대 거리까지 날아가면 일정 시간 뒤 멀린 손 쪽으로 빠르게 회수됩니다.
- 창 회수 완료 후 손에 든 창을 다시 표시합니다.

---

## 2026-07-30 - 멀린 창 위치 / 투척 캔슬 보정

### 구현 목적

멀린이 들고 있는 창이 Kevin Iglesias 데모 씬의 창 사용 방식처럼 손 위치와 모션 흐름에 더 맞게 보이도록 보정했습니다.

특히 아래 기준을 반영했습니다.

- Idle / Charge / Thrust / Swing 중에는 두 손으로 창을 쥐는 느낌이 나야 합니다.
- Throw 중에는 손을 놓기 전까지 창이 손에 붙어 있어야 합니다.
- Throw release 시점에 손에 든 창을 숨기고, 그 위치에서 투사체 창이 나가야 합니다.
- 롱노트 투척 중 가까운 단일노트가 들어와도 기존 창던지기 모션 / 투사체가 캔슬되지 않아야 합니다.

### 추가 / 수정 스크립트

#### `Assets/JiSeon/Scripts/Boss/MerlinHeldSpearPoseController.cs`

멀린이 들고 있는 창의 위치를 애니메이션 상태에 맞춰 보정하는 스크립트입니다.

현재 동작:

- `LateUpdate()`에서 매 프레임 창 위치를 갱신합니다.
- `Animator`의 현재 상태를 확인합니다.
- `Intro`, `Idle`, `SpearThrust`, `SpearSwing` 상태에서는 오른손 / 왼손 본 위치를 기준으로 창의 축을 양손 사이에 맞춥니다.
- `SpearCharge` 상태에서는 던지기 직전 세로 창에서 가로 창으로 갑자기 꺾이는 현상을 줄이기 위해, 창 축을 플레이어 방향 가로축으로 고정합니다.
- `SpearThrow` 상태에서는 데모 씬의 오른손 prop 구조처럼 `WeaponSocket_R` 기준으로 창을 붙입니다.
- 오른손 / 왼손의 손바닥 위치는 Synty 캐릭터 손가락 본 위치를 기준으로 자동 추정합니다.
- 창 투척 직전 `ForceUpdatePose()`를 호출할 수 있어, 투사체 생성 위치가 현재 손에 들린 창 위치와 맞게 갱신됩니다.

프리팹 현재 값:

- `heldSpear`: `Merlin_Spear`
- `oneHandSocket`: `WeaponSocket_R`
- `autoEstimatePalmOffsets`: `true`
- `rightHandGripOffset`: `(0.08, 0.003, -0.013)`
- `leftHandGripOffset`: `(0.08, -0.003, 0.013)`
- `spearRightGripLocal`: `(0, 0, 0)`
- `oneHandSpearLocalPosition`: `(0, 0, 0)`
- `oneHandSpearLocalEuler`: `(0, 0, 180)`
- `minimumTwoHandDistance`: `0.18`
- `forceHorizontalChargePose`: `true`
- `horizontalChargeAxis`: `(1, 0.03, 0)`
- `forceTipTowardFacingDirection`: `true`

#### `Assets/JiSeon/Scripts/Boss/MerlinBossCombat.cs`

롱노트 투척 중 공격 요청 처리와 투척 release 위치 갱신을 보정했습니다.

현재 동작:

- 창 투척 예약 중이거나 창 투사체가 활성화되어 있으면 단일 노트 공격 요청을 무시합니다.
- 창 투척 예약 중이거나 창 투사체가 활성화되어 있으면 새로운 롱 노트 차징 요청을 무시합니다.
- 이미 투척 중인 상태에서 롱 노트 release가 다시 들어와도 기존 투척을 덮어쓰지 않습니다.
- 투척 직전 `MerlinHeldSpearPoseController.ForceUpdatePose()`를 호출합니다.
- 그 직후 손에 들린 창의 월드 위치 / 회전을 캡처해 `MerlinSpearProjectile`을 생성합니다.
- 더 이상 단일 노트 입력이 롱노트 창던지기 모션이나 투사체를 취소하지 않습니다.
- `spearBlockingMask`로 창 투사체가 막히는 전용 레이어를 설정합니다.

#### `Assets/JiSeon/Scripts/Boss/MerlinSpearProjectile.cs`

멀린 롱노트 창 투사체의 비행 / 관통 / 꽂힘 / 회수를 처리합니다.

현재 동작:

- 창은 포물선이 아니라 발사 순간 높이의 Y를 유지한 채 멀린이 바라보는 좌우 방향으로 수평 직선 비행합니다.
- 창은 손 애니메이션 위치가 낮아져도 최소 높이 `1.5` 이상에서 생성되며, Z는 `0`으로 고정됩니다.
- `lockOutgoingToHorizontalLine` 기본값은 `true`이며, `MerlinSpearProjectile.prefab`에도 켜진 상태로 저장되어 있습니다.
- `lockToSideViewPlane` 기본값은 `true`이며, 투사체의 Z 위치를 `0`으로 고정해 사이드뷰 카메라 기준에서 창이 앞뒤 깊이로 빠져 보이지 않는 상황을 줄입니다.
- `useTipAsBlockingProbe` 기본값은 `true`이며, 벽 / 바닥 / 천장 충돌을 창 피벗이 아니라 창끝 기준으로 판정합니다.
- `autoCalculateTipForwardOffset` 기본값은 `true`이며, 현재 창 모델 바운드 기준 창끝 오프셋은 약 `1.929`입니다.
- `onlyStickToFacingWallsDuringHorizontalThrow` 기본값은 `true`이며, 수평 발사 중에는 진행 방향을 정면으로 막는 벽면만 꽂힘 대상으로 사용합니다.
- 수평 발사 중 바닥 윗면 / 천장 아랫면처럼 normal이 진행 방향을 막지 않는 면은 꽂힘 대상으로 사용하지 않습니다.
- 창끝이 Environment에 닿으면 창 피벗은 뒤쪽에 남고, 창끝이 벽 표면에 꽂힌 위치로 정지합니다.
- 런타임 테스트용 `TrailRenderer`를 창 피벗이 아니라 창끝 위치에 자동으로 추가해, 창이 빠르게 날아가도 Game View에서 이동 궤적을 볼 수 있게 했습니다.
- 디버그 로그를 통해 `spawned`, `pierced Player`, `stuck by ...`, `returning` 상태를 Console에서 확인할 수 있습니다.
- `minimumBlockingDistance` 기본값은 `0.75`이며, 발사 직후 너무 가까운 환경 충돌은 무시해 손 주변 플랫폼에 즉시 박히는 상황을 줄입니다.
- Player는 관통합니다.
- Player를 관통하는 동안 `IDamageable` 대상에게 한 번만 피해를 줍니다.
- Player에게 피해를 줘도 창은 즉시 회수되지 않습니다.
- `blockingMask`에 포함된 `SpearBlocker` 오브젝트는 관통하지 않습니다.
- 일반 `Environment` 플랫폼 / 발판은 관통합니다.
- `SpearBlocker` 벽에 닿으면 해당 지점에 창이 꽂힌 상태로 잠시 멈춥니다.
- 꽂힌 뒤 `1.2초`가 지나면 멀린 손 쪽으로 빠르게 회수됩니다.
- 환경에 닿지 않고 최대 거리까지 날아간 경우에도 `0.35초` 뒤 자동 회수됩니다.
- 회수 중에는 플레이어에게 추가 피해를 주지 않습니다.

### 수정 프리팹

- `Assets/JiSeon/Prefabs/Bosses/MerlinBoss.prefab`

변경된 구성:

- `MerlinHeldSpearPoseController` 컴포넌트 추가
- `MerlinBossCombat.heldSpearPoseController` 참조 연결
- `MerlinBossCombat.spearBlockingMask`를 `SpearBlocker` 레이어로 설정
- `spearThrowSpeed`를 `26`으로 설정
- `spearReturnSpeed`를 `75`로 설정
- `spearMaxDistance`를 `14`로 설정
- `lockThrowReleaseToHorizontalLine`을 `true`로 설정
- `minimumHorizontalThrowHeight`를 `1.5`로 설정
- `horizontalThrowLockedZ`를 `0`으로 설정
- `Merlin_Spear.localPosition`을 `(0, 0, 0)` 기준으로 정리
- `Merlin_Spear.localEulerAngles`는 `(0, 0, 180)` 기준 유지

### 수정 테스트 씬

- `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

추가 오브젝트:

- `MerlinSpearTestWall_Left`
- `MerlinSpearTestWall_Right`

현재 값:

- `MerlinSpearTestWall_Left`
  - 위치: `(-5.75, 2, 0)`
  - 크기: `(0.35, 4, 6)`
  - 레이어: `SpearBlocker`
- `MerlinSpearTestWall_Right`
  - 위치: `(6.25, 2, 0)`
  - 크기: `(0.35, 4, 6)`
  - 레이어: `SpearBlocker`

용도:

- 멀린이 플레이어 방향으로 창을 던졌을 때, 왼쪽 / 오른쪽 어느 방향에서도 창이 플레이어를 관통한 뒤 테스트 벽에 꽂히는지 확인하기 위한 테스트 벽입니다.

### 애니메이션 컨트롤러

- `Assets/JiSeon/Animations/Boss/MerlinBoss.controller`

현재 상태:

| State | Motion |
|---|---|
| `Intro` | `HumanM@CombatIdlePolearm01` |
| `Idle` | `HumanM@CombatIdlePolearm01` |
| `SpearThrust` | `HumanM@AttackPolearm01` |
| `SpearSwing` | `HumanM@Attack2H01` |
| `SpearCharge` | `HumanM@ThrowSpear01_R - Hold` |
| `SpearThrow` | `HumanM@ThrowSpear01_R` |
| `Hit` | `HumanM@CombatDamage01` |
| `Death` | `Kneeling Down` |

현재 재생 값:

- 공격 애니메이션 속도: `1.5`
- 찌르기 시작 normalized time: `0.46`
- 휘두르기 시작 normalized time: `0.22`
- 차징 hold normalized time: `0.20`
- 투척 시작 normalized time: `0.14`

Death 애니메이션:

- 사용 파일: `Assets/JiSeon/Animations/Boss/Mixamo/Kneeling Down.fbx`
- Import Rig: `Humanoid`
- Clip Name: `Kneeling Down`
- Loop Time: `false`
- Root Transform Position Y는 발 기준에 맞게 사용할 수 있도록 `heightFromFeet`를 켠 상태입니다.
- 멀린 `Death` state는 사망 후 쓰러지는 모션이 아니라 한쪽 무릎을 꿇는 Mixamo 애니메이션을 사용합니다.

Death 지면 보정:

- 관련 스크립트: `Assets/JiSeon/Scripts/Boss/BossAnimationController.cs`
- `alignDeathPoseToGround`: `true`
- `deathVisualRoot`: `VisualRoot`
- `deathGroundMask`: `Environment`
- `useDeathSupportBones`: `true`
- 사망 상태에서는 매 프레임 `LeftFoot`, `RightFoot`, `LeftLowerLeg`, `RightLowerLeg` 중 가장 낮은 bone을 지면 높이에 맞춥니다.
- 렌더러 bounds 기준이 아니라 실제 Humanoid 발 / 무릎 지지점 기준으로 보정합니다.
- 이 보정은 Death 애니메이션이 무릎 꿇은 상태에서 바닥보다 뜨거나 과하게 박히는 것을 줄이기 위한 최종 시각 보정입니다.

### 검증 상태

빌드:

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
- 결과: 경고 0개 / 오류 0개

Unity 에디터:

- `MerlinBoss_Test.unity` 기준으로 창끝 판정용 물리 경로를 확인했습니다.
- 현재 발사 루트 샘플: `releaseRoot=(4.224, 1.521, 0.000)`
- 현재 창끝 오프셋 샘플: `tipOffset=1.929`
- 현재 창끝 시작 샘플: `tipOrigin=(2.295, 1.521, 0.000)`
- 왼쪽 벽 충돌 샘플: `MerlinSpearTestWall_Left`, `wallPoint=(-5.575, 1.521, 0.000)`, `normal=(1.000, 0.000, 0.000)`, `wallDot=1.000`
- 벽에 꽂힌 상태의 루트 / 창끝 샘플: `stuckRoot=(-3.616, 1.521, 0.000)`, `stuckTip=(-5.545, 1.521, 0.000)`
- 플레이어 관통 판정 샘플: `player sweep hits=1`
- 실제 Play Mode에서 Unity Game View에 `R` / `T` 입력을 넣어 RhythmSystem 차트를 시작한 뒤 롱노트 창 투척 로그를 확인했습니다.
- 실제 R/T 플로우에서 기존 문제 로그였던 `stuck by Platform at (1.96, 0.00, 0.00)`는 더 이상 발생하지 않습니다.
- 실제 R/T 플로우 기준 현재 로그 순서: `Long note charge started -> Long note charge released -> spawned -> pierced Player -> stuck by MerlinSpearTestWall_Left -> returning`
- 실제 R/T 플로우 기준 창 생성 샘플: `spawned at (4.33, 2.19, 0.00)`, `tip (2.41, 2.19, 0.00)`, `direction (-1.00, 0.00, 0.00)`
- 실제 R/T 플로우 기준 플레이어 관통 샘플: `pierced Player for 28 damage`
- 실제 R/T 플로우 기준 벽 꽂힘 샘플: `stuck by MerlinSpearTestWall_Left at (-3.62, 2.19, 0.00)`
- Unity 콘솔 기준 스크립트 컴파일 오류는 없습니다.
- 코드 컴파일과 프리팹 직렬화 값 기준으로 `MerlinBoss` 프리팹에 창 보정 컴포넌트와 전투 코드 참조가 반영되어 있습니다.

---

## 2026-07-31 - 멀린 보스 외형 프리팹 교체

### 구현 목적

기존 임시 Synty 기사 외형을 실제 멀린 전용 모델로 교체했습니다.

전투 로직 / 체력 / HUD / 리듬 연결은 유지하고, `MerlinBoss` 프리팹의 시각 모델과 Animator / 손 무기 소켓만 새 모델 기준으로 재연결했습니다.

### 사용 에셋

- 새 멀린 모델: `Assets/JiSeon/Prefabs/Bosses/Merlin/Merlin_0722/C_Merlin.fbx`
- 멀린 텍스처:
  - `Assets/JiSeon/Prefabs/Bosses/Merlin/Merlin_0722/TX_Merlin_Basic.png`
  - `Assets/JiSeon/Prefabs/Bosses/Merlin/Merlin_0722/TX_Merlin_metallic.png`
  - `Assets/JiSeon/Prefabs/Bosses/Merlin/Merlin_0722/TX_Merlin_roughness.png`
- 새 멀린 머티리얼: `Assets/JiSeon/Prefabs/Bosses/Merlin/Merlin_0722/MAT_Merlin.mat`
- 보스 프리팹: `Assets/JiSeon/Prefabs/Bosses/Merlin/MerlinBoss.prefab`
- 테스트 씬: `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

### 모델 Import 설정

`C_Merlin.fbx`는 기존 `MerlinBoss.controller`의 Humanoid 애니메이션을 사용할 수 있도록 아래 기준으로 설정했습니다.

- Rig Animation Type: `Humanoid`
- Avatar Setup: `Create From This Model`
- 생성 Avatar: `C_MerlinAvatar`
- Avatar 상태: `valid`, `human`

### 현재 프리팹 구조

`MerlinBoss`의 현재 외형 구조:

- `MerlinBoss`
  - `VisualRoot`
    - `Merlin_Visual`
      - `Root_GRP/Root/Merlin_Hips/.../Merlin_R_Hand`
        - `WeaponSocket_R`
          - `Merlin_Spear`

기존 `Merlin_TempVisual`은 사용하지 않습니다.

### 재연결된 컴포넌트

`MerlinBoss.prefab` 기준:

- `BossController.visualRoot`: `VisualRoot`
- `BossAnimationController.animator`: `Merlin_Visual`
- `BossAnimationController.deathVisualRoot`: `VisualRoot`
- `MerlinBossCombat.heldSpear`: `Merlin_Spear`
- `MerlinBossCombat.spearSocket`: `WeaponSocket_R`
- `MerlinBossCombat.throwOrigin`: `WeaponSocket_R`
- `MerlinHeldSpearPoseController.animator`: `Merlin_Visual`
- `MerlinHeldSpearPoseController.heldSpear`: `Merlin_Spear`
- `MerlinHeldSpearPoseController.oneHandSocket`: `WeaponSocket_R`

### 애니메이션 연결 상태

- Runtime Animator Controller: `Assets/JiSeon/Animations/Boss/MerlinBoss.controller`
- Animator Avatar: `C_MerlinAvatar`
- `Idle` state 확인됨
- `SpearThrow` state 확인됨
- 오른손 Humanoid Bone: `Merlin_R_Hand`
- 왼손 Humanoid Bone: `Merlin_L_Hand`

### 머티리얼 / 텍스처

`M_Merlin` SkinnedMeshRenderer에 `MAT_Merlin`을 적용했습니다.

현재 머티리얼 값:

- Shader: `Universal Render Pipeline/Lit`
- BaseMap / MainTex: `TX_Merlin_Basic`
- Metallic texture 참조: `TX_Merlin_metallic`
- Metallic: `0.25`
- Smoothness: `0.35`

### 검증 상태

빌드:

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
- 결과: 경고 0개 / 오류 0개

Unity 에디터:

- `MerlinBoss_Test.unity`에서 `MerlinBoss` 씬 인스턴스가 새 `Merlin_Visual`을 사용함을 확인했습니다.
- `Merlin_TempVisual`은 씬 인스턴스에서 존재하지 않습니다.
- Play Mode에서 `Merlin_Visual` Animator가 `Idle` 상태로 정상 재생됨을 확인했습니다.
- Play Mode에서 활성 렌더러 bounds 샘플: `size=(1.767, 2.312, 2.448)`, `center=(3.568, 1.235, 0.000)`
- Play Mode에서 `MAT_Merlin`과 `TX_Merlin_Basic` 텍스처가 적용된 상태를 확인했습니다.
- 실제 Game View에 `R` / `T` 입력을 넣어 RhythmSystem 차트를 시작한 뒤, 모델 교체 후에도 멀린 패턴과 롱노트 창 투척이 정상 동작함을 확인했습니다.
- 실제 R/T 플로우 기준 창 생성 샘플: `spawned at (4.34, 2.01, 0.00)`, `tip (2.42, 2.01, 0.00)`, `direction (-1.00, 0.00, 0.00)`
- 실제 R/T 플로우 기준 현재 로그 순서: `Single note pattern -> Long note charge started -> Long note charge released -> spawned -> pierced Player -> stuck by MerlinSpearTestWall_Left -> returning`

---

## 2026-07-31 - 멀린 보스 크기 2배 조정

### 구현 목적

멀린 보스가 화면에서 더 보스답게 보이도록 외형 크기를 기존 대비 2배로 키웠습니다.

### 수정 대상

- 보스 프리팹: `Assets/JiSeon/Prefabs/Bosses/Merlin/MerlinBoss.prefab`
- 테스트 씬 인스턴스: `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`의 `MerlinBoss`

### 현재 값

- `VisualRoot.localScale`: `(2.24, 2.24, 2.24)`
- `CharacterController.height`: `4`
- `CharacterController.radius`: `0.9`
- `CharacterController.center`: `(0, 2, 0)`

### 검증 상태

- `MerlinBoss_Test.unity` 기준 활성 렌더러 bounds 샘플: `size=(3.604, 4.625, 5.020)`, `center=(3.169, 2.311, 0.062)`
- 이전 새 멀린 모델 적용 직후 bounds 높이 약 `2.31`에서 현재 약 `4.63`으로 2배 확대됨을 확인했습니다.

---

## 2026-07-31 - 멀린 롱노트 창 투척 조준 / 창날 가시성 보정

### 구현 목적

멀린 롱노트 종료 시 창이 완전 수평으로만 날아가 플레이어 몸통을 빗나가는 문제와, 벽에 꽂힌 창이 사이드뷰에서 칼날 면이 잘 보이지 않는 문제를 보정했습니다.

### 수정 대상

- 스크립트: `Assets/JiSeon/Scripts/Boss/MerlinBossCombat.cs`
- 스크립트: `Assets/JiSeon/Scripts/Boss/MerlinSpearProjectile.cs`
- 보스 프리팹: `Assets/JiSeon/Prefabs/Bosses/Merlin/MerlinBoss.prefab`
- 투사체 프리팹: `Assets/JiSeon/Prefabs/Projectiles/MerlinSpearProjectile.prefab`
- 테스트 씬: `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

### 최종 구현 상태

- 멀린 창 투척은 곡선 궤적이 아니라 직선 궤적을 유지합니다.
- 기존처럼 발사 높이에 맞춘 완전 수평선으로 날아가지 않고, 창 발사 위치에서 플레이어 몸통 부근을 향하는 직선 방향으로 날아갑니다.
- `MerlinBossCombat.spearAimOffset` 값을 `(0, 1.35, 0)`으로 조정하여 플레이어 루트가 아니라 몸통 근처를 조준하도록 했습니다.
- `MerlinBossCombat.forceThrowAimToReleaseHeight` 값을 `false`로 두어 조준점의 y값을 발사 높이로 강제하지 않도록 했습니다.
- `MerlinSpearProjectile.lockOutgoingToHorizontalLine` 값을 `false`로 두어 발사체가 실제 조준 방향 벡터를 사용하도록 했습니다.
- `MerlinSpearProjectile.spearVisualRollDegrees` 값을 `90`으로 두어 창이 날아가거나 벽에 꽂혔을 때 사이드뷰에서 창날의 면이 더 보이도록 했습니다.
- 창은 플레이어를 관통하면서 피해를 주고, `SpearBlocker` 레이어의 `MerlinSpearTestWall_Left`에 닿으면 꽂힌 뒤 자동 회수됩니다.

### 검증 상태

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
- 결과: 경고 0개 / 오류 0개
- Unity `MerlinBoss_Test.unity` Play Mode에서 실제 Game View에 R/T 입력을 넣어 롱노트 창 투척을 확인했습니다.
- 실제 로그 기준 발사 방향 샘플: `direction (-0.96, -0.29, 0.00)`
- 실제 로그 기준 플레이어 관통 샘플: `pierced Player for 28 damage`
- 실제 로그 기준 벽 고정 샘플: `stuck by MerlinSpearTestWall_Left`
- Unity Play Mode에서 발사체가 `Stuck` 상태가 되는 순간을 자동 pause로 잡아 확인했으며, 해당 시점의 창 회전 샘플은 `euler=(286.53, 270.00, 180.00)`입니다. 창날 롤 보정이 적용된 상태로 벽에 꽂힘을 확인했습니다.

---

## 2026-07-31 - 멀린 창 크기 조정

### 구현 목적

멀린이 던진 창이 사이드뷰와 벽 꽂힘 상태에서 적절한 크기로 보이도록 창 시각 크기를 조정했습니다.

손에 들고 있는 창은 `VisualRoot`와 멀린 모델 부모 스케일 영향을 받아 실제 월드 크기가 더 커집니다. 투척용 창은 월드에 별도 생성되기 때문에 같은 local scale을 써도 더 작게 보였으므로, 투척용 창은 held spear의 실제 월드 스케일에 맞춰 별도로 보정했습니다.

### 수정 대상

- 보스 프리팹: `Assets/JiSeon/Prefabs/Bosses/Merlin/MerlinBoss.prefab`
- 투사체 프리팹: `Assets/JiSeon/Prefabs/Projectiles/MerlinSpearProjectile.prefab`

### 최종 구현 상태

- 손에 들고 있는 `Merlin_Spear` local scale을 `1.2`로 설정했습니다.
- 투척용 `MerlinSpearProjectile` 내부 `Merlin_Spear` local scale은 held spear의 실제 월드 스케일과 맞도록 `2.4192`로 설정했습니다.
- held spear와 projectile spear의 실제 월드 크기를 동일하게 맞춰, 던지는 순간 또는 벽에 꽂힌 순간 창 크기가 갑자기 작아 보이지 않도록 했습니다.

### 검증 상태

- Unity 씬 기준 held spear local scale: `held local=(1.20, 1.20, 1.20)`
- Unity 씬 기준 held spear 월드 스케일: `held lossy=(2.42, 2.42, 2.42)`
- Unity 프리팹 기준 projectile spear local scale: `projectile local=(2.42, 2.42, 2.42)`
- Unity 프리팹 기준 projectile spear 월드 스케일: `projectile lossy=(2.42, 2.42, 2.42)`
- held / projectile 월드 스케일 비율: `1.000`
- Unity Play Mode에서 실제 Game View에 R/T 입력을 넣어 롱노트 창 투척을 확인했습니다.
- 실제 로그 기준 플레이어 관통 샘플: `pierced Player for 28 damage`
- 실제 로그 기준 벽 고정 샘플: `stuck by MerlinSpearTestWall_Left`

---

## 2026-07-31 - 멀린 창 회수 타이밍 / 창 찌르기 가시성 보정

### 구현 목적

롱노트 창 투척 후 다음 Boss 노트가 오기 전에 창이 최대한 빠르게 회수되도록 조정하고, 단일 노트 창 찌르기 모션에서 창이 앞으로 나가는 움직임이 더 잘 보이도록 보정했습니다.

### 수정 대상

- 스크립트: `Assets/JiSeon/Scripts/Boss/MerlinBossCombat.cs`
- 스크립트: `Assets/JiSeon/Scripts/Boss/MerlinSpearProjectile.cs`
- 스크립트: `Assets/JiSeon/Scripts/Boss/MerlinHeldSpearPoseController.cs`
- 스크립트: `Assets/JiSeon/Scripts/Boss/BossAnimationController.cs`
- 보스 프리팹: `Assets/JiSeon/Prefabs/Bosses/Merlin/MerlinBoss.prefab`
- 투사체 프리팹: `Assets/JiSeon/Prefabs/Projectiles/MerlinSpearProjectile.prefab`

### 최종 구현 상태

롱노트 창 회수:

- `MerlinBossCombat.spearThrowSpeed`: `42`
- `MerlinBossCombat.spearReturnSpeed`: `180`
- `MerlinBossCombat.returnRecoverSeconds`: `0.1`
- `MerlinSpearProjectile.stuckSecondsBeforeReturn`: `0.18`
- `MerlinSpearProjectile.maxDistanceReturnDelaySeconds`: `0.12`
- `MerlinSpearProjectile.returnArriveDistance`: `0.15`
- 창 투척 중 다음 노트가 먼저 들어오면 무시하지 않고, 창 회수 직후 실행되도록 큐 처리합니다.
- 큐 로그 예시: `Queued long note charge until spear returns.`

창 찌르기 가시성:

- `BossAnimationController.thrustStartNormalizedTime`: `0.18`
- `MerlinHeldSpearPoseController.forceForwardThrustPose`: `true`
- `MerlinHeldSpearPoseController.thrustAxis`: `(1, 0.015, 0)`
- `MerlinHeldSpearPoseController.thrustForwardWorldOffset`: `0.9`
- 찌르기 상태에서는 창 축을 플레이어 방향으로 잡고, 애니메이션 진행에 따라 창을 전방으로 밀었다가 되돌립니다.

### 검증 상태

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
- 결과: 경고 0개 / 오류 0개
- Unity `MerlinBoss_Test.unity` Play Mode에서 실제 Game View에 R/T 입력을 넣어 롱노트 창 투척을 확인했습니다.
- 실제 로그 기준 창 벽 고정 대기 시간: `for 0.18s`
- 실제 로그 기준 창 회수: `returning from`
- 실제 로그 기준 다음 노트 처리: `Queued long note charge until spear returns` 이후 창 회수 직후 `Long note charge started`
- 단일 창 찌르기 수동 샘플에서 `Merlin_Spear` x 위치 변화 범위가 `1.71`로 확인되었습니다.

---

## 2026-07-31 - 멀린 보스 HP 바 Fill 동기화 수정

### 구현 목적

멀린 보스 HP 숫자는 감소하지만 중앙 상단의 빨간 HP 바가 숫자 비율에 맞춰 줄어들지 않는 문제를 수정했습니다.

### 수정 대상

- 스크립트: `Assets/JiSeon/Scripts/Boss/BossHud.cs`
- 스크립트: `Assets/JiSeon/Scripts/Boss/BossHealth.cs`
- 테스트 씬: `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

### 최종 구현 상태

- `BossHud`가 `BossHealth.NormalizedHealth` 값을 기준으로 HP 숫자와 HP Fill을 함께 갱신합니다.
- `Image.fillAmount`만 갱신하지 않고, `BossHpFill`의 `RectTransform.anchorMax.x`도 HP 비율에 맞춰 조정합니다.
- 빨간 HP 바는 왼쪽 기준으로 줄어들도록 `pivot.x = 0` 기준으로 처리합니다.
- `BossHealth.HealthChanged` 이벤트를 추가하여 데미지 / 회복 / 리셋처럼 HP 값이 바뀌는 모든 흐름에서 HUD가 갱신될 수 있게 했습니다.

### 검증 상태

- `dotnet build DYSTOPIAN/Assembly-CSharp.csproj --no-restore`
- 결과: 경고 0개 / 오류 0개
- Unity `MerlinBoss_Test.unity` Play Mode에서 보스에게 100 데미지를 직접 적용해 확인했습니다.
- 검증 샘플: `hp=400/500`, `text=400/500`, `fillAmount=0.800`, `anchorMaxX=0.800`
- HP가 0이 되었을 때도 `fillAmount=0.000`, `anchorMaxX=0.000`으로 확인했습니다.

---

## 현재 테스트 방법

### Enemy 테스트

테스트 씬:

- `Assets/JiSeon/Scenes/EnemyBossNPC_Test.unity`

기본 흐름:

1. 씬을 실행합니다.
2. 플레이어를 방향키로 움직여 경비병 감지 범위에 들어갑니다.
3. 적 머리 위 STATE가 `Chase`로 바뀌는지 확인합니다.
4. `R`로 리듬 UI를 표시합니다.
5. `T`로 리듬 차트를 시작합니다.
6. 오른쪽 리듬 UI의 Enemy 노트가 판정선에 도달하면 적이 공격하는지 확인합니다.

### Boss 테스트

테스트 씬:

- `Assets/JiSeon/Scenes/MerlinBoss_Test.unity`

기본 흐름:

1. 씬을 실행합니다.
2. `R`로 리듬 UI를 표시합니다.
3. `T`로 리듬 차트를 시작합니다.
4. 오른쪽 리듬 UI의 Boss 노트가 판정선에 도달하면 멀린이 패턴을 실행하는지 확인합니다.
5. 단일 노트는 `창 찌르기 -> 창 휘두르기` 순서로 반복되는지 확인합니다.
6. 롱 노트 시작 시 차징 자세를 잡고, 롱 노트 종료 시 창을 던지는지 확인합니다.
7. 창이 일직선으로 날아가는지 확인합니다.
8. 창이 플레이어를 관통하면서 피해를 주고, 즉시 회수되지 않는지 확인합니다.
9. 창이 `MerlinSpearTestWall_Left`에 닿으면 벽에 꽂혔다가 자동으로 빠르게 회수되는지 확인합니다.
10. 창 투척 중 다음 단일 노트가 가까이 붙어 있어도 창 투척 모션 / 투사체가 캔슬되지 않는지 확인합니다.
11. 플레이어 공격 입력으로 보스 HP가 감소하는지 확인합니다.

### 입력

| 입력 | 동작 |
|---|---|
| LeftArrow / RightArrow | 플레이어 좌우 이동 |
| UpArrow | 플레이어 점프 |
| A | 플레이어 단일 노트 입력 |
| D 누름 / 뗌 | 플레이어 롱 노트 시작 / 종료 입력 |
| R | 리듬 UI 표시 / 숨김 |
| T | 리듬 차트 시작 / 정지 |

---

## 다른 파트와의 연결 상태

현재 테스트를 위해 `PlayerTest` 씬의 주요 오브젝트를 `EnemyBossNPC_Test.unity`와 `MerlinBoss_Test.unity`에 복사해 사용합니다.

참조 중인 다른 파트 요소:

- `Assets/DongJun/Scripts/Rhythm Scripts/RhythmSystem.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerHealth.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerController.cs`
- `Assets/DongJun/Scripts/Player Scripts/PlayerRhythmAttackBridge.cs`

연결 방식:

- 원본 Player / Rhythm 스크립트는 직접 수정하지 않습니다.
- Enemy / Boss 쪽에서 필요한 연결은 JiSeon 폴더의 Bridge / Adapter 스크립트로 처리합니다.
- 다른 파트 코드가 아직 안정화되지 않은 상황을 고려해, Enemy / Boss 자체 기능은 최대한 독립적으로 유지합니다.

---

## 이후 참고 사항

- 실제 스테이지 씬에 Enemy 또는 Boss를 배치할 때는 해당 씬에 `RhythmSystem`이 있어야 합니다.
- 실제 플레이어 오브젝트의 레이어가 `Player`가 아니면 Enemy / Boss의 `targetMask`를 프로젝트 기준에 맞게 조정해야 합니다.
- 실제 UI가 준비되면 `EnemyDebugOverlay`의 임시 STATE / HP 표시는 교체하거나 비활성화하면 됩니다.
- 실제 멀린 전용 캐릭터 프리팹이 완성되면 `MerlinBoss.prefab`의 `Merlin_TempVisual`을 교체하면 됩니다.
- 멀린 실제 캐릭터 프리팹으로 교체할 경우 `MerlinHeldSpearPoseController`의 손 위치 보정 값은 새 리그 기준으로 다시 확인해야 합니다.
- NPC 실제 기능은 아직 구현 범위에 들어가지 않았습니다.
