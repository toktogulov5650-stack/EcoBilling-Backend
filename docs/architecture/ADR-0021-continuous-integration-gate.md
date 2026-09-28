# ADR-0021: обязательный CI-gate

- Статус: принято
- Дата: 2026-09-28

## Контекст

Репозиторий уже содержал минимальный GitHub Actions workflow, но интеграционные тесты запускались без PostgreSQL и поэтому пропускали все сценарии persistence. Версия SDK задавалась плавающим диапазоном, Docker-конфигурация не проверялась, а release workflow повторял тот же неполный набор проверок.

Архитектурная документация требует, чтобы restore, build и test проходили в Release-конфигурации без ошибок и проигнорированных предупреждений. Один и тот же обязательный минимум должен действовать для pull request, `main` и release tag.

## Решение

- CI запускается для pull request и push в `main`, а также вручную через `workflow_dispatch`.
- Workflow использует точную версию .NET SDK из `global.json`: `10.0.401`.
- Restore выполняется с `-warnaserror`; общая настройка `TreatWarningsAsErrors` продолжает действовать при build.
- Release build и Release test являются обязательными последовательными шагами без `continue-on-error`.
- GitHub Actions service `postgres:17-alpine` предоставляет изолированную временную PostgreSQL. `ECOBILLING_TEST_POSTGRES_CONNECTION` включает все PostgreSQL integration/contract tests вместо их пропуска.
- Пароль PostgreSQL в workflow относится только к удаляемому CI service и не является production-секретом. Production credentials в workflow отсутствуют.
- CI валидирует итоговый Docker Compose и собирает targets API, Worker и migrations.
- Workflow получает только разрешение `contents: read`; checkout не сохраняет Git credentials.
- Повторный запуск CI для той же ветки отменяет устаревший незавершённый запуск.
- Release workflow выполняет тот же restore/format/build/test gate до публикации API и Worker artifacts.
- Архитектурные тесты фиксируют обязательные элементы обоих workflow.

## Последствия

- Предупреждение, сбой PostgreSQL-теста, неверный Compose или неработающий Dockerfile блокируют CI.
- Полная проверка `dotnet format --verify-no-changes` пока не включена: существующая кодовая база содержит исторически смешанные окончания строк и кодировки. Новые и изменяемые C#-файлы форматируются на соответствующем этапе; отдельная нормализация должна выполняться изолированным механическим изменением.
- Полный CI требует GitHub-hosted Linux runner с Docker и занимает больше времени, чем прежний прогон с пропущенными PostgreSQL-тестами.
- Реальные end-to-end сценарии этапа 19 автоматически входят в общий `dotnet test` и используют тот же PostgreSQL service.
- Защита ветки и обязательность статуса CI на стороне GitHub настраиваются владельцем репозитория и не могут быть гарантированы содержимым workflow.
