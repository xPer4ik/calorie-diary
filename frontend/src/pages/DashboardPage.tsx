import { useEffect, useMemo, useState } from 'react'
import { ApiError, apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'

type DiarySummary = {
  date: string
  calories: number
  protein: number
  fat: number
  carbs: number
  dailyCaloriesTarget: number | null
  proteinTarget: number | null
  fatTarget: number | null
  carbsTarget: number | null
  caloriesRemaining: number | null
  proteinRemaining: number | null
  fatRemaining: number | null
  carbsRemaining: number | null
}

export function DashboardPage() {
  const { user } = useAuth()
  const [summary, setSummary] = useState<DiarySummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const todayIso = useMemo(() => getLocalDateIso(), [])
  const todayLabel = useMemo(() => formatDisplayDate(todayIso), [todayIso])
  const calorieTarget = summary?.dailyCaloriesTarget ?? null
  const caloriesEaten = summary?.calories ?? 0
  const progress =
    calorieTarget && calorieTarget > 0
      ? Math.min(100, Math.round((caloriesEaten / calorieTarget) * 100))
      : 0

  useEffect(() => {
    async function loadSummary() {
      setIsLoading(true)
      setError('')

      try {
        const response = await apiRequest<DiarySummary>(
          `/api/diary/summary?date=${todayIso}`,
        )
        setSummary(response)
      } catch (err) {
        setError(
          err instanceof ApiError
            ? err.message
            : 'Не удалось загрузить итоги за сегодня',
        )
      } finally {
        setIsLoading(false)
      }
    }

    void loadSummary()
  }, [todayIso])

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <h1>Главная</h1>
          <p className="muted">
            Здравствуйте, {user?.displayName}. Сегодня {todayLabel}
          </p>
        </div>
      </div>

      {error && (
        <p className="error-message" role="alert" aria-live="polite">
          {error}
        </p>
      )}

      <div className="dashboard-grid">
        <article className="panel dashboard-card highlight-card">
          <div>
            <p className="card-label">Дневная норма</p>
            <h2>
              {isLoading
                ? 'Загрузка...'
                : calorieTarget
                  ? `${caloriesEaten} / ${calorieTarget} ккал`
                  : `${caloriesEaten} ккал`}
            </h2>
          </div>

          <div className="progress-block">
            <div
              className="progress-bar"
              aria-label={`Прогресс по калориям ${progress}%`}
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={progress}
            >
              <span style={{ width: `${progress}%` }} />
            </div>
            <p className="muted">
              {calorieTarget
                ? `${progress}% от дневной цели`
                : 'Сохраните профиль, чтобы сравнивать питание с целью.'}
            </p>
          </div>
        </article>

        <StatCard
          label="Съедено сегодня"
          value={isLoading ? '...' : `${caloriesEaten} ккал`}
          hint={
            caloriesEaten > 0
              ? 'Посчитано по записям дневника'
              : 'Сегодня приемы пищи еще не добавлены.'
          }
        />
        <StatCard
          label="Цель"
          value={calorieTarget ? `${calorieTarget} ккал` : 'Не задана'}
          hint={calorieTarget ? 'Загружено из профиля' : 'Заполните профиль'}
        />
      </div>

      <div className="two-column">
        <section className="panel">
          <h2>БЖУ за сегодня</h2>
          {isLoading ? (
            <p className="muted">Загружаем БЖУ...</p>
          ) : (
            <div className="metric-list">
              <MacroRow
                label="Белки"
                value={summary?.protein ?? 0}
                target={summary?.proteinTarget ?? null}
                remaining={summary?.proteinRemaining ?? null}
              />
              <MacroRow
                label="Жиры"
                value={summary?.fat ?? 0}
                target={summary?.fatTarget ?? null}
                remaining={summary?.fatRemaining ?? null}
              />
              <MacroRow
                label="Углеводы"
                value={summary?.carbs ?? 0}
                target={summary?.carbsTarget ?? null}
                remaining={summary?.carbsRemaining ?? null}
              />
            </div>
          )}
        </section>

        <section className="panel guidance-card">
          <h2>Как пользоваться</h2>
          <p className="muted">
            Питайтесь осознанно: сначала заполните профиль и сохраните дневную
            норму, затем добавляйте продукты и записывайте приемы пищи в дневник.
          </p>
          <p className="muted">
            На этой странице видно, сколько калорий уже съедено за день и
            насколько вы приблизились к цели. Если записей нет, начните с
            дневника питания.
          </p>
        </section>
      </div>

      {!isLoading && caloriesEaten === 0 && (
        <div className="empty-state">
          <strong>Сегодня еще нет записей.</strong>
          <p className="muted">
            Откройте дневник, выберите продукт, укажите граммы, и дневной итог
            обновится автоматически.
          </p>
        </div>
      )}
    </section>
  )
}

function StatCard({
  label,
  value,
  hint,
}: {
  label: string
  value: string
  hint: string
}) {
  return (
    <article className="panel dashboard-card">
      <p className="card-label">{label}</p>
      <strong>{value}</strong>
      <p className="muted">{hint}</p>
    </article>
  )
}

function MacroRow({
  label,
  value,
  target,
  remaining,
}: {
  label: string
  value: number
  target: number | null
  remaining: number | null
}) {
  return (
    <div className="metric-row">
      <span>{label}</span>
      <strong>{value} г</strong>
      {target !== null && remaining !== null ? (
        <small>{formatTargetNote(target, remaining, 'г')}</small>
      ) : (
        <small>Сохраните профиль, чтобы сравнивать с целью.</small>
      )}
    </div>
  )
}

function getLocalDateIso() {
  const date = new Date()
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

function formatDisplayDate(dateIso: string) {
  const [year, month, day] = dateIso.split('-').map(Number)
  const date = new Date(year, month - 1, day)

  return new Intl.DateTimeFormat('ru-RU', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date)
}

function formatTargetNote(target: number, remaining: number, suffix: string) {
  if (remaining < 0) {
    return `цель ${target} ${suffix}, выше цели на ${Math.abs(remaining)} ${suffix}`
  }

  if (remaining === 0) {
    return `цель ${target} ${suffix}, цель выполнена`
  }

  return `цель ${target} ${suffix}, осталось ${remaining} ${suffix}`
}
