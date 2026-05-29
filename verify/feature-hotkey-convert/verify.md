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
| Select text `ghbdsn` in Notepad + `Ctrl+Alt+L` | `привіт` replaces selection | pending retest | not-run |
| Cursor after `ghbdsn` without selection + `Ctrl+Alt+L` | last word converted to `привіт` | pass (`op=c207aa3f`, `path=last_word`, `replaced=True`) | pass |
| Mixed text `ghbdsn, 123!` + convert | punctuation and digits stay unchanged | blocked in agent session | blocked |
| Hotkey conflict (occupied combo) | app does not crash, warning shown/logged | blocked in agent session | blocked |

## 4) Acceptance Criteria Check
| criterion | status | evidence |
|---|---|---|
| AC1 | pass | interactive log op `c207aa3f` shows convert+inject success for last-word flow |
| AC2 | blocked | requires interactive desktop manual run |
| AC3 | blocked | latency measurement pending interactive run |
| AC4 | pass-partial | error paths wrapped, logging added |
| AC5 | pass-partial | reversible mapping verified via unit test |
| AC6 | pass | unit test confirms punctuation preservation |

## 5) Residual Risks
- Clipboard-dependent capture may fail in protected/sandboxed inputs.
- Some applications may ignore synthetic key events.
- Clipboard restore is best-effort and may race with user clipboard changes.
- Manual e2e evidence is still required from local interactive session.

## 6) Recommendation
- request changes (run `scripts/manual-smoke-hotkey.ps1` locally and record outcomes)
