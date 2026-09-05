# Lumio Lobby Motion Design Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development to implement this plan task-by-task (hosts without subagents: its Inline Fallback section). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the player lobby with a scene-driven hero, animated game-card covers, live-status cues, and reusable motion tokens while preserving existing routes and GameTech visual language.

**Architecture:** Keep `LobbyPage.tsx` responsible for lobby content and state, use `lobby.module.css` for all page-specific scene layers and interaction motion, and add only shared timing/keyframe tokens to `styles/tokens.css` and `styles/deco.css`. Existing `.ui-*` primitives remain the styling base; no animation dependency or API contract changes are introduced.

**Tech Stack:** React 19, TypeScript strict, React Router 7, CSS Modules, Vite, Vitest + Testing Library.

## Global Constraints

- Use existing GameTech colors and `.ui-*` primitives from `web/src/styles/`; do not add a second palette.
- Keep all user-facing copy in Chinese except established product names, route names, and protocol identifiers.
- Animate only `transform`, `opacity`, and `box-shadow`; preserve layout dimensions during interaction.
- Respect `prefers-reduced-motion: reduce` and keep semantic online/status text available without animation.
- Do not change API calls, routes, authentication behavior, or non-lobby pages.
- Before claiming completion run `pnpm -C web verify` and `node .spec/tools/spec-lint.mjs`.

### Task 1: Shared Motion Tokens And Lobby Contract Tests

**Files:**
- Modify: `web/src/styles/tokens.css`
- Modify: `web/src/styles/deco.css`
- Modify: `web/src/app/App.test.tsx`

**Interfaces:**
- Produces shared custom properties `--ui-motion-enter`, `--ui-motion-stagger`, `--ui-motion-float`, `--ui-motion-hover`, and `--ui-motion-slow`.
- Produces keyframes `lm-float`, `lm-online-pulse`, and `lm-in` usable by CSS Modules.

- [ ] **Step 1: Add failing lobby assertions**

Extend the existing `renders the lobby route` test with:

```ts
expect(screen.getByText('12 人在线')).toBeInTheDocument();
expect(screen.getByText('最近更新')).toBeInTheDocument();
expect(screen.getByLabelText('体素炸弹人在线状态')).toHaveTextContent('5/8');
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run `pnpm -C web test -- App.test.tsx -t "renders the lobby route"`.
Expected: FAIL because the new status elements do not exist yet.

- [ ] **Step 3: Add shared motion values**

Add the five `--ui-motion-*` values beside the existing duration tokens in `tokens.css`. Add `lm-float`, `lm-online-pulse`, and `lm-in` keyframes to `deco.css`, using only `transform` and `opacity` for movement.

- [ ] **Step 4: Run style and focused test checks**

Run `pnpm -C web typecheck && pnpm -C web test -- App.test.tsx -t "renders the lobby route"`.
Expected: typecheck passes; the focused test remains red until Task 2 supplies the markup.

- [ ] **Step 5: Commit**

```bash
git add web/src/styles/tokens.css web/src/styles/deco.css web/src/app/App.test.tsx
git commit -m "feat(web): add lobby motion tokens"
```

### Task 2: Scene-Driven Lobby Markup And Status Model

**Files:**
- Modify: `web/src/features/lobby/LobbyPage.tsx`

**Interfaces:**
- Consumes the motion tokens and keyframes from Task 1.
- Produces accessible elements with `aria-label="体素炸弹人在线状态"`, visible `12 人在线`, and visible `最近更新`.

- [ ] **Step 1: Add the lobby status and scene data to the component**

Extend the local game data with `online`, `capacity`, `updated`, and a `scene` tone. Keep the existing `slug`, launch navigation, and share behavior unchanged.

- [ ] **Step 2: Replace the static Hero art with layered decorative markup**

Render one main `.heroCore` voxel and four `.heroShard` spans inside the existing `heroArt`; mark the wrapper `aria-hidden="true"`. Keep the existing `.ui-voxel` primitive for the main cube.

- [ ] **Step 3: Add the status rail and card metadata**

Render a status rail beside the catalog heading with a green status square, `12 人在线`, and `最近更新`. On the published card render a cover online pill containing the accessible label and `5/8`; keep the existing visible launch and share buttons.

- [ ] **Step 4: Run the focused tests**

Run `pnpm -C web test -- App.test.tsx -t "renders the lobby route|published lobby game exposes a launch action"`.
Expected: PASS, including the new status assertions and existing launch action.

- [ ] **Step 5: Commit**

```bash
git add web/src/features/lobby/LobbyPage.tsx
git commit -m "feat(web): add animated lobby scene markup"
```

### Task 3: High-Fidelity Lobby Styling And Responsive Motion

**Files:**
- Modify: `web/src/features/lobby/lobby.module.css`
- Modify: `web/src/styles/primitives.css`

**Interfaces:**
- Consumes the markup and shared motion tokens from Tasks 1–2.
- Preserves `.ui-card--game`, `.ui-cover`, `.ui-btn`, and `.ui-grid` behavior for all existing consumers.

- [ ] **Step 1: Add failing style-level expectations through build validation**

Run `pnpm -C web lint` against the new class names after Task 2. Expected: the markup builds, but the visual classes have no required declarations yet; use this run to capture the baseline before styling.

- [ ] **Step 2: Implement the desktop hero scene**

Set the hero to a 360px minimum scene with layered positioning, staggered `lm-in` entry, four shard colors from existing tokens, and a 72px main voxel. Add a low-opacity diagonal line layer without changing the current grid background contract.

- [ ] **Step 3: Implement catalog status and card cover scenes**

Add a flex status rail, cover grid lines, online pill, and three geometric cover layers per card. Add `ui-card--pop` to the published card and `transform`/`box-shadow` hover and `:focus-within` states capped at 4px lift and 6px scene offset.

- [ ] **Step 4: Add reusable primitive motion helpers**

Add only the shared `.ui-motion-enter` and `.ui-online-dot` helpers to `primitives.css`, both using the tokenized durations and existing reduced-motion media query.

- [ ] **Step 5: Add mobile and reduced-motion rules**

At `max-width: 640px`, switch the hero to one column, keep two small shards, move the status rail under the catalog heading, and disable pointer-driven scene transforms. Add a reduced-motion override that sets animation and transition duration to `0.01ms` for the new classes.

- [ ] **Step 6: Run the full web verification**

Run `pnpm -C web verify`.
Expected: lint, typecheck, Vitest, and OpenAPI check all pass with no generated-file diff.

- [ ] **Step 7: Commit**

```bash
git add web/src/features/lobby/lobby.module.css web/src/styles/primitives.css
git commit -m "feat(web): polish lobby with game motion"
```

## Plan Self-Review

- Coverage: hero layers, catalog status, card scenes, motion tokens, reduced motion, mobile layout, accessibility text, and existing route behavior are covered by Tasks 1–3.
- Placeholder scan: no TODO, TBD, or unspecified implementation step is required.
- Type consistency: class names and accessible labels produced by Task 2 are the selectors consumed by Task 3; token names produced by Task 1 are used verbatim by Tasks 2–3.

