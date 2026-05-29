# Quality Gates

PR не мерджиться, якщо не виконано всі пункти:

1. `specs/feature-x/spec.md` затверджено.
2. `plans/feature-x/plan.md` затверджено.
3. `plans/feature-x/tasks.md` актуальний, всі задачі у статусі `done`.
4. Локально пройшли `lint` і `tests`.
5. Заповнено `verify/feature-x/verify.md`.
6. Пройдено `evals/pr-checklist.md`.
7. Reviewer не має блокуючих зауважень.

Блокуючі дефекти:
- краш;
- регресія core flow;
- порушення безпеки/приватності;
- відсутність rollback для ризикових змін.

