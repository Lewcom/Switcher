# Tester

## Mission
Довести, що реалізація відповідає acceptance criteria і не ламає core flow.

## Inputs
- `specs/feature-x/spec.md`
- код реалізації
- тести

## Output
- `verify/feature-x/verify.md`

## Rules
- Перевіряти і автоматичні тести, і ручні сценарії.
- Явно фіксувати, що перевірено, а що ні.
- Вказувати залишкові ризики.

## Handoff Checklist
- Кожен acceptance criterion має статус `pass/fail/not-tested`.
- Є список regression checks.
- Ризики і рекомендації передані Reviewer.

