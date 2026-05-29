# Implementation Plan: feature-hotkey-convert

## 1) Goal
Після запуску tray-застосунку користувач натискає глобальний hotkey і отримує перетворення або виділеного фрагмента, або останнього слова у фокусному полі, з цільовою затримкою до 100 мс.
Дефолтний hotkey v1: `Ctrl+Alt+L`. Підтримка мапи v1: тільки `UA <-> EN`.

## 2) Architecture Decision
- Обраний підхід:
  - `C# + .NET 8` desktop процес (tray app).
  - `HotkeyService` для глобальної реєстрації клавіш.
  - `TextAccessService` для читання виділення/контексту.
  - `LayoutConverter` для `UA <-> EN` mapping.
  - `TextInjector` для заміни тексту (primary: SendInput, fallback: Clipboard).
  - `AppLogger` для помилок/діагностики.
- Альтернативи, які розглядали:
  - AutoHotkey-only скрипт (швидкий старт, але слабка масштабованість для тестів і архітектури).
  - Electron/Node desktop app (зайвий runtime overhead для low-level input задач).
- Чому обрали саме це:
  - Найкращий контроль над WinAPI і глобальними hotkeys.
  - Просте тестування mapping/правил у unit-тестах.
  - Добрий шлях росту до наступних фіч (auto-replace, settings UI).

## 3) Steps
1. Створити каркас застосунку і базові сервіси (`HotkeyService`, `LayoutConverter`, `TextInjector`).
2. Реалізувати `UA <-> EN` таблицю та reversible convert API.
3. Реалізувати flow:
   - якщо є виділення -> конвертувати виділення;
   - інакше -> знайти останнє слово біля курсора і конвертувати його.
4. Додати graceful-error обробку і логування.
5. Покрити unit-тестами mapping і edge cases.
6. Підготувати verify-репорт для ручних сценаріїв.

## 4) Dependencies
- Code dependencies:
  - .NET 8 SDK
  - WinAPI interop для hotkeys/input
  - test framework (`xUnit` або `NUnit`)
- Runtime/tooling dependencies:
  - Windows 10/11
  - доступ до глобальних клавіш у user session

## 5) Rollback Plan
- Що відкотити:
  - Відключити реєстрацію hotkey фічі за feature-flag у конфігу.
  - Повернутись до попередньої стабільної збірки.
- Як перевірити, що rollback успішний:
  - Застосунок стартує без помилок.
  - Hotkey не тригерить feature code path.
  - Жодних нових crash-записів у логах.

## 6) Impact
- Performance:
  - O(n) від довжини фрагмента, де n <= 200 у цільовому кейсі.
- Security/Privacy:
  - Текст не надсилається у мережу; усі операції локальні.
  - Clipboard fallback має мінімізувати час утримання чутливих даних.
- Compatibility:
  - Основний таргет: стандартні editable поля Windows/браузерів.
  - Можливі обмеження у sandbox/remote/privileged полях.
