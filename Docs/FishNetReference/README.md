# Fish-Net prediction reference

Copied out of `Assets/FishNet/Demos/` before that folder was deleted
(2026-08-08). Saved as `.txt` and kept **outside `Assets/`** so Unity does not
try to compile them — they reference demo-only types (`MovingPlatform`,
`NetworkTrigger` setups, demo prefabs) that no longer exist in this project.

## Why these were kept

`FISHNET_MIGRATION.md` Phase 4 records an open decision on `PlayerMovement`:

- **Chosen for now:** `NetworkTransform` — movement stays in `Update()` gated on
  `IsOwner`, position replicated by a component. Keeps current feel, no rewrite.
- **Deferred:** client-side prediction with `[Replicate]`/`[Reconcile]`. This is
  the reason Fish-Net was picked over Mirror, and these files are the canonical
  worked examples for it.

## The files

| File | Shows |
|---|---|
| `CharacterControllerPrediction.cs.txt` | `TickNetworkBehaviour` + `CharacterController`. Closest match to `PlayerMovement`, which also uses a `CharacterController`. |
| `RigidbodyPrediction.cs.txt` | Same pattern for rigidbody physics. |

## Key points to re-read when picking this up

- Inherit `TickNetworkBehaviour`, not `NetworkBehaviour`.
- `SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick)` in `Awake`.
- `ReplicateData : IReplicateData` carries **inputs**;
  `ReconcileData : IReconcileData` carries **state**. Both need `GetTick`/`SetTick`.
- **All** movement logic lives inside the `[Replicate]` method. It runs on the
  owner (predicted), on the server (authoritative), and again during replay
  after a correction — so it must be deterministic.
- Use `(float)TimeManager.TickDelta` as the delta, **never `Time.deltaTime`**.
  This is the single biggest porting change for `PlayerMovement`, which uses
  `Time.deltaTime` throughout.
- `CharacterController` must be **disabled before** setting `transform.position`
  in `[Reconcile]`, then re-enabled — otherwise physics stays at the old
  position (see the comment in the reconcile method).

Upstream docs: https://fish-networking.gitbook.io/docs/manual/guides/prediction
