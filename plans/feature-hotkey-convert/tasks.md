# Tasks: feature-hotkey-convert

## Status Legend
- `todo`
- `in_progress`
- `blocked`
- `done`

## Task List (0.5-2h each)
| id | task | owner | estimate | status | definition of done |
|---|---|---|---|---|---|
| T1 | Bootstrap .NET solution + tray app skeleton | implementer | 1.5h | done | Проєкт збирається і стартує локально без помилок |
| T2 | Додати `HotkeyService` з реєстрацією/звільненням глобального hotkey | implementer | 1h | done | Hotkey реєструється на старті, звільняється при виході |
| T3 | Реалізувати `LayoutConverter` (UA<->EN mapping + reversible tests) | implementer | 2h | done | Unit-тести на базові/edge символи зелені |
| T4 | Реалізувати `TextInjector` (SendInput + clipboard fallback) | implementer | 2h | done | Текст замінюється у 2+ типових застосунках |
| T5 | Реалізувати flow `selection-first`, інакше `last-word` | implementer | 2h | done | AC1, AC2 виконуються у ручних smoke-кейсах |
| T6 | Додати логування помилок і graceful handling | implementer | 1h | done | При невдачі немає крашу, є інформативний лог |
| T7 | Написати verify-документ і пройти ручний тест-набір | tester | 1.5h | done | `verify.md` заповнено, ризики зафіксовані |

## Notes
- Блокери:
  - Можливі обмеження доступу до деяких полів вводу.
- Рішення:
  - Пріоритезуємо стабільність у стандартних полях; спеціальні поля фіксуємо як known limitations.
  - Для setup було потрібно встановити `.NET 8 SDK` і виконати первинний restore.
  - Зафіксовано support boundary: Win32 текстові поля = stable, web/chat inputs = best effort.
