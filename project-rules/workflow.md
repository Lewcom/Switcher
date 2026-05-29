# Workflow

## Canonical Flow
1. Spec Agent готує `specs/feature-x/spec.md`.
2. Після затвердження Spec, Plan Agent готує `plans/feature-x/plan.md`.
3. Plan декомпозується в `plans/feature-x/tasks.md` (0.5-2h кожна задача).
4. Implementer виконує задачі малими інкрементами.
5. Tester заповнює `verify/feature-x/verify.md`.
6. Reviewer проходить `evals/pr-checklist.md` і приймає/блокує PR.
7. Після merge додається learning у `postmortems/`.

## Stop Rules
- Немає затвердженого spec -> код не пишемо.
- Немає затвердженого plan -> код не пишемо.
- Немає verify -> merge заборонено.

