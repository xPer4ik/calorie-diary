import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../api/client'

type FoodItem = {
  id: number
  name: string
}

type MealEntry = {
  id: number
  date: string
  mealType: string
  foodName: string
  grams: number
  calories: number
  protein: number
  fat: number
  carbs: number
}

type DiaryDay = {
  date: string
  entries: MealEntry[]
}

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

const mealTypes = ['breakfast', 'lunch', 'dinner', 'snack'] as const
const mealTypeLabels: Record<(typeof mealTypes)[number], string> = {
  breakfast: 'Завтрак',
  lunch: 'Обед',
  dinner: 'Ужин',
  snack: 'Перекус',
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function DiaryPage() {
  const [date, setDate] = useState(today())
  const [foods, setFoods] = useState<FoodItem[]>([])
  const [entries, setEntries] = useState<MealEntry[]>([])
  const [summary, setSummary] = useState<DiarySummary | null>(null)
  const [foodItemId, setFoodItemId] = useState(0)
  const [mealType, setMealType] = useState('breakfast')
  const [grams, setGrams] = useState(100)
  const [error, setError] = useState('')
  const [status, setStatus] = useState('')
  const [isLoadingFoods, setIsLoadingFoods] = useState(true)
  const [isLoadingDiary, setIsLoadingDiary] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deletingEntryId, setDeletingEntryId] = useState<number | null>(null)

  const selectedFoodName = useMemo(
    () => foods.find((food) => food.id === foodItemId)?.name ?? '',
    [foodItemId, foods],
  )

  useEffect(() => {
    async function loadFoods() {
      try {
        const response = await apiRequest<FoodItem[]>('/api/foods')
        setFoods(response)

        if (response.length > 0) {
          setFoodItemId(response[0].id)
        }
      } finally {
        setIsLoadingFoods(false)
      }
    }

    void loadFoods().catch((err) =>
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить продукты'),
    )
  }, [])

  async function loadDiary(targetDate = date) {
    setIsLoadingDiary(true)

    try {
      const [day, daySummary] = await Promise.all([
        apiRequest<DiaryDay>(`/api/diary?date=${targetDate}`),
        apiRequest<DiarySummary>(`/api/diary/summary?date=${targetDate}`),
      ])

      setEntries(day.entries)
      setSummary(daySummary)
    } finally {
      setIsLoadingDiary(false)
    }
  }

  useEffect(() => {
    void loadDiary(date).catch(() => setError('Не удалось загрузить дневник'))
  }, [date])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setStatus('')
    const validationError = validateDiaryForm(date, mealType, foodItemId, grams)

    if (validationError) {
      setError(validationError)
      return
    }

    setIsSubmitting(true)

    try {
      await apiRequest<MealEntry>('/api/diary', {
        method: 'POST',
        body: { date, mealType, foodItemId, grams },
      })
      setStatus(`${selectedFoodName || 'Продукт'} добавлен`)
      await loadDiary()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить запись')
    } finally {
      setIsSubmitting(false)
    }
  }

  async function deleteEntry(id: number) {
    setError('')
    setStatus('')
    setDeletingEntryId(id)

    try {
      await apiRequest<void>(`/api/diary/${id}`, { method: 'DELETE' })
      setStatus('Запись удалена')
      await loadDiary()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить запись')
    } finally {
      setDeletingEntryId(null)
    }
  }

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <h1>Дневник</h1>
          <p className="muted">Записывайте приемы пищи и сравнивайте день с целями.</p>
        </div>
      </div>

      <div className="two-column">
        <form className="panel form-grid" onSubmit={handleSubmit} aria-busy={isSubmitting}>
          <h2>Добавить прием пищи</h2>
          <label>
            Дата
            <input
              type="date"
              value={date}
              required
              onChange={(event) => setDate(event.target.value)}
            />
          </label>
          <label>
            Прием пищи
            <select
              value={mealType}
              onChange={(event) => setMealType(event.target.value)}
            >
              {mealTypes.map((type) => (
                <option key={type} value={type}>
                  {mealTypeLabels[type]}
                </option>
              ))}
            </select>
          </label>
          <label>
            Продукт
            <select
              value={foodItemId}
              disabled={isLoadingFoods || foods.length === 0}
              onChange={(event) => setFoodItemId(Number(event.target.value))}
            >
              {foods.length === 0 && <option value={0}>Продукты не загружены</option>}
              {foods.map((food) => (
                <option key={food.id} value={food.id}>
                  {food.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Граммы
            <input
              type="number"
              min={1}
              step="0.1"
              required
              value={grams}
              onChange={(event) => setGrams(Number(event.target.value))}
            />
          </label>

          {error && (
            <p className="error-message" role="alert" aria-live="polite">
              {error}
            </p>
          )}
          {status && (
            <p className="success-message" role="status" aria-live="polite">
              {status}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting || isLoadingFoods || foods.length === 0}
          >
            {isSubmitting ? 'Добавляем...' : 'Добавить запись'}
          </button>
        </form>

        <div className="panel">
          <h2>Итог за день</h2>
          {isLoadingDiary ? (
            <p className="muted">Загружаем итоги дня...</p>
          ) : summary ? (
            <div className="metric-list">
              <SummaryRow
                label="Калории"
                value={summary.calories}
                target={summary.dailyCaloriesTarget}
                remaining={summary.caloriesRemaining}
                suffix="ккал"
              />
              <SummaryRow
                label="Белки"
                value={summary.protein}
                target={summary.proteinTarget}
                remaining={summary.proteinRemaining}
                suffix="г"
              />
              <SummaryRow
                label="Жиры"
                value={summary.fat}
                target={summary.fatTarget}
                remaining={summary.fatRemaining}
                suffix="г"
              />
              <SummaryRow
                label="Углеводы"
                value={summary.carbs}
                target={summary.carbsTarget}
                remaining={summary.carbsRemaining}
                suffix="г"
              />
              {summary.dailyCaloriesTarget === null && (
                <p className="muted">Сохраните профиль, чтобы сравнивать итоги с целями.</p>
              )}
            </div>
          ) : (
            <p className="muted">Итоги пока не загружены.</p>
          )}
        </div>
      </div>

      <div className="panel full-width-panel">
        <h2>Записи за {date}</h2>
        <div className="list-stack">
          {isLoadingDiary && <p className="muted">Загружаем записи...</p>}
          {!isLoadingDiary && entries.length === 0 && (
            <p className="muted">Записей пока нет.</p>
          )}
          {entries.map((entry) => (
            <article className="list-item" key={entry.id}>
              <div>
                <strong>{entry.foodName}</strong>
                <p className="muted">
                  {mealTypeLabel(entry.mealType)}, {entry.grams} г, {entry.calories} ккал
                </p>
              </div>
              <button
                type="button"
                className="ghost-button"
                disabled={deletingEntryId === entry.id}
                onClick={() => void deleteEntry(entry.id)}
              >
                {deletingEntryId === entry.id ? 'Удаляем...' : 'Удалить'}
              </button>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

function validateDiaryForm(
  date: string,
  mealType: string,
  foodItemId: number,
  grams: number,
) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) {
    return 'Выберите корректную дату.'
  }

  if (!mealTypes.includes(mealType as (typeof mealTypes)[number])) {
    return 'Выберите корректный прием пищи.'
  }

  if (!Number.isFinite(foodItemId) || foodItemId <= 0) {
    return 'Выберите продукт.'
  }

  if (!Number.isFinite(grams) || grams <= 0) {
    return 'Количество граммов должно быть больше 0.'
  }

  return ''
}

function mealTypeLabel(value: string) {
  return mealTypeLabels[value as keyof typeof mealTypeLabels] ?? 'Неизвестный прием'
}

function SummaryRow({
  label,
  value,
  target,
  remaining,
  suffix,
}: {
  label: string
  value: number
  target: number | null
  remaining: number | null
  suffix: string
}) {
  return (
    <div className="metric-row">
      <span>{label}</span>
      <strong>
        {value} {suffix}
      </strong>
      {target !== null && remaining !== null && (
        <small>{formatTargetNote(target, remaining, suffix)}</small>
      )}
    </div>
  )
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
