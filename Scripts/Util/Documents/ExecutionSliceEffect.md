# ExecutionSliceEffect 설명

이 클래스는 **"적을 칼로 베면 두 조각으로 갈라져서 날아가고, 서서히 사라진다"**를 구현한다.

---

## 전체 흐름 (3단계)

```
1. 수집   →   2. 자르기 + 날리기   →   3. 페이드 아웃 + 파괴
```

---

## 1단계: 수집 — CollectSliceSources()

적 오브젝트에 붙어있는 **모든 메시(3D 모양 데이터)**를 모은다.

- **SkinnedMeshRenderer** — 캐릭터 모델용. `BakeMesh()`로 현재 애니메이션 포즈를 정적 메시로 "스냅샷" 찍음
- **MeshFilter** — 플레이스홀더(큐브, 스피어)용

적의 몸통이 큐브, 머리가 스피어면 → 2개의 SliceSource가 수집됨

---

## 2단계: 자르기 + 날리기 — Slice()

각 메시 파트마다:

1. **절단 평면을 로컬 좌표로 변환** — "월드에서 여기를 이 방향으로 자른다"를 메시 내부 좌표로 바꿈
2. **MeshSlicer.Slice()** — 실제로 메시를 두 조각으로 자름 (upper/lower)
3. **CreateSliceHalf()** — 잘린 각 조각을 새 GameObject로 만듦:
   - `MeshFilter + MeshRenderer` — 보이게
   - `MeshCollider (convex)` — 바닥에 부딪히게
   - `Rigidbody` — 물리 적용
   - `AddForce` — 양쪽으로 **밀어내기** (separationForce) + 위로 **살짝 띄우기** (upwardForce)
   - `AddTorque` — 랜덤 방향으로 **빙글빙글 회전**

머티리얼은 2개 사용:
- **submesh 0** = 원래 적의 겉면 (원본 머티리얼)
- **submesh 1** = 잘린 단면 (어두운 빨간색 — 살점 느낌)

---

## 3단계: 페이드 아웃 + 파괴 — FadeAndDestroy()

```
[0초]────────[1.5초]────────[3초]
 물리로 날아감   페이드 시작    완전 투명 → 파괴
```

1. **1.5초 동안** 조각이 물리로 날아가며 바닥에 굴러다님
2. **1.5~3초** 사이에 알파값을 1→0으로 서서히 줄여서 투명하게
3. 완전 투명해지면 `Destroy()`로 정리

---

## EnableTransparency()

URP에서 머티리얼을 **불투명 → 투명**으로 런타임에 전환하는 코드.
Inspector에서 Surface Type을 Transparent로 바꾸는 것과 같은 동작을 코드로 하는 것.
`DashAfterimageEffect`에서 이미 쓰고 있는 패턴과 동일하다.

---

## Inspector 파라미터 요약

| 필드 | 의미 | 기본값 |
|---|---|---|
| `separationForce` | 두 조각이 벌어지는 힘 | 8 |
| `upwardForce` | 위로 튀어오르는 힘 | 3 |
| `torqueForce` | 회전 세기 | 5 |
| `crossSectionMaterial` | 잘린 단면 색 (미지정 시 어두운 빨간색) | null |
| `destroyDelay` | 조각이 사라지기까지 총 시간 | 3초 |
| `fadeStartTime` | 페이드 시작 시점 | 1.5초 |
