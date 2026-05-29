# Verification: feature-hotkey-convert

## 1) Test Scope
- Що перевіряли:
  - Unit-тести `LayoutConverter` (UA<->EN, punctuation, reversible, empty).
  - Збірку всього solution (`Switcher.App`, `Switcher.Core`, `Switcher.Core.Tests`).
- Що не перевіряли:
  - Повноцінні ручні e2e кейси в реальних застосунках (Notepad/Browser/Messenger) невалідні в цьому неінтерактивному середовищі агента.

## 2) Automated Tests
| test | result | notes |
|---|---|---|
| `dotnet build Switcher.sln --no-restore` | pass | 0 errors |
| `dotnet test Switcher.sln` | pass | 5 passed, 0 failed |
| `LayoutConverterTests.Convert_ConvertsEnglishWordToUkrainian` | pass | `ghbdsn -> привіт` |
| `LayoutConverterTests.Convert_ConvertsUkrainianWordToEnglish` | pass | `привіт -> ghbdsn` |
| `LayoutConverterTests.Convert_PreservesPunctuationSpacesAndDigits` | pass | punctuation unchanged |
| `LayoutConverterTests.Convert_IsReversibleForSupportedLetters` | pass | double convert returns source |
| `LayoutConverterTests.Convert_HandlesEmptyString` | pass | empty -> empty |

## 3) Manual Cases
| case | expected | actual | result |
|---|---|---|---|
| Cursor after `ghbdsn` in Notepad + `Ctrl+Alt+L` | last word converted to `привіт` | pass (`copy_strategy=wm_copy_focused`, `inject_wm_paste result=ok`) | pass |
| Repeat toggle in Notepad (`привіт` -> hotkey) | converted back to `ghbdsn` | pass (`out_preview="ghbdsn"`, `replaced=True`) | pass |
| Mixed text `ghbdsn, 123!` + convert in Notepad | punctuation/digits unchanged | pass (manual confirm by user) | pass |
| Input in chat/web-like field | best-effort conversion | intermittent timeout (`path=none`) | partial |
| Hotkey conflict (occupied combo) | app does not crash, warning shown/logged | not-run | not-run |

## 4) Acceptance Criteria Check
| criterion | status | evidence |
|---|---|---|
| AC1 | pass | multiple successful Notepad runs with `copy_strategy=wm_copy_focused` and `replaced=True` |
| AC2 | pass-partial | stable in Notepad, web/chat fields are best-effort |
| AC3 | blocked | latency measurement pending dedicated timing run |
| AC4 | pass-partial | error paths wrapped, logging added |
| AC5 | pass | reversible mapping verified via unit test + manual toggle in Notepad |
| AC6 | pass | unit test confirms punctuation preservation |

## 5) Residual Risks
- Clipboard-dependent capture may fail in protected/sandboxed inputs.
- Web/chat inputs may ignore or override copy/paste message routing.
- Clipboard restore is best-effort and may race with user clipboard changes.
- Conflict scenario for occupied hotkey is not manually verified yet.

## 6) Recommendation
- approve for MVP scope with explicit support boundary:
  - Stable target: standard Windows text fields (e.g., Notepad).
  - Best effort target: web/chat custom input controls.
