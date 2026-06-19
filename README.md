# Calorie Diary

Учебное full-stack веб-приложение «Калькулятор калорий и дневник питания».

Проект позволяет пользователю зарегистрироваться, рассчитать дневную норму калорий и БЖУ, сохранить профиль, вести список продуктов и добавлять записи питания в дневник.

## Стек

- Backend: ASP.NET Core Web API, C#
- Database: SQLite, Entity Framework Core
- Frontend: React, TypeScript, Vite
- Styles: CSS
- Auth: JWT
- API: REST/JSON
- Tests: xUnit, PowerShell smoke-test

## Возможности

- регистрация и вход пользователя;
- JWT-аутентификация и защищенные страницы;
- расчет BMR, TDEE, дневной нормы калорий и БЖУ;
- сохранение профиля пользователя;
- базовые seed-продукты и пользовательские продукты;
- дневник питания по датам;
- дневной итог по калориям, белкам, жирам и углеводам;
- Swagger/OpenAPI для проверки backend API.

## Структура проекта

```text
backend/        ASP.NET Core Web API
backend.Tests/  unit-тесты backend
frontend/       React + TypeScript + Vite
scripts/        вспомогательные скрипты проверки
```

## Запуск backend

```powershell
cd backend
dotnet run
```

Backend по умолчанию запускается на:

```text
http://localhost:5010
```

Swagger доступен по адресу:

```text
http://localhost:5010/swagger
```

## Запуск frontend

После клонирования нужно установить зависимости:

```powershell
cd frontend
npm install
npm run dev
```

Frontend запускается на адресе, который покажет Vite, обычно:

```text
http://localhost:5173
```

Переменная API находится в `frontend/.env.example`:

```text
VITE_API_URL=http://localhost:5010
```

## Проверка

Backend:

```powershell
dotnet build .\backend\backend.csproj
dotnet test .\backend.Tests\backend.Tests.csproj
```

Frontend:

```powershell
Push-Location .\frontend
npm install
npm run build
Pop-Location
```

Smoke-test API:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1
```

## Примечание

Локальные файлы базы данных SQLite, папки `bin`, `obj`, `node_modules` и `dist` не должны попадать в репозиторий. Они создаются автоматически при запуске, сборке или установке зависимостей.
