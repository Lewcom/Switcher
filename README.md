# Switcher Project OS

Цей репозиторій організований за стилем:
`North Star + Scope -> Spec-first -> Plan -> Tasks -> Implementation -> Verification -> Review -> Learning loop`.

## Structure
- `project-charter.md` - ціль, метрики, non-goals.
- `project-rules/` - workflow і quality gates.
- `skills/` - ізольовані ролі агентів.
- `specs/` - специфікації фіч.
- `plans/` - архітектурні плани та задачі.
- `verify/` - результати перевірок по фічах.
- `evals/` - чеклісти оцінки якості PR.
- `postmortems/` - щотижневе навчання процесу.

## First Feature Workflow
1. Створи фічу:
   - `specs/feature-hotkey-convert/spec.md` (копія з `specs/_template/spec.md`)
2. Після approval:
   - `plans/feature-hotkey-convert/plan.md` (копія з `plans/_template/plan.md`)
   - `plans/feature-hotkey-convert/tasks.md` (копія з `plans/_template/tasks.md`)
3. Після реалізації:
   - `verify/feature-hotkey-convert/verify.md` (копія з `verify/_template/verify.md`)
4. Перед merge:
   - пройти `evals/pr-checklist.md`

## Golden Rule
Код пишемо тільки після затверджених `spec + plan`.

