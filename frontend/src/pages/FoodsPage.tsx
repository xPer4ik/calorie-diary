import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../api/client'

type FoodItem = {
  id: number
  userId: number | null
  name: string
  caloriesPer100g: number
  proteinPer100g: number
  fatPer100g: number
  carbsPer100g: number
  isSeed: boolean
}

type FoodForm = {
  name: string
  caloriesPer100g: number
  proteinPer100g: number
  fatPer100g: number
  carbsPer100g: number
}

const emptyFood: FoodForm = {
  name: '',
  caloriesPer100g: 0,
  proteinPer100g: 0,
  fatPer100g: 0,
  carbsPer100g: 0,
}

export function FoodsPage() {
  const [foods, setFoods] = useState<FoodItem[]>([])
  const [form, setForm] = useState(emptyFood)
  const [error, setError] = useState('')
  const [status, setStatus] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deletingFoodId, setDeletingFoodId] = useState<number | null>(null)

  async function loadFoods() {
    try {
      const response = await apiRequest<FoodItem[]>('/api/foods')
      setFoods(response)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadFoods().catch((err) =>
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить продукты'),
    )
  }, [])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setStatus('')
    const validationError = validateFood(form)

    if (validationError) {
      setError(validationError)
      return
    }

    setIsSubmitting(true)

    try {
      await apiRequest<FoodItem>('/api/foods', {
        method: 'POST',
        body: {
          ...form,
          name: form.name.trim(),
        },
      })
      setForm(emptyFood)
      setStatus('Продукт добавлен')
      await loadFoods()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить продукт')
    } finally {
      setIsSubmitting(false)
    }
  }

  async function deleteFood(id: number) {
    setError('')
    setStatus('')
    setDeletingFoodId(id)

    try {
      await apiRequest<void>(`/api/foods/${id}`, { method: 'DELETE' })
      setStatus('Продукт удален')
      await loadFoods()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить продукт')
    } finally {
      setDeletingFoodId(null)
    }
  }

  function updateNumber(key: Exclude<keyof FoodForm, 'name'>, value: string) {
    setForm((current) => ({ ...current, [key]: Number(value) }))
  }

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <h1>Продукты</h1>
          <p className="muted">Базовые продукты и ваши собственные продукты.</p>
        </div>
      </div>

      <div className="two-column">
        <form className="panel form-grid" onSubmit={handleSubmit} aria-busy={isSubmitting}>
          <h2>Добавить продукт</h2>
          <label>
            Название
            <input
              value={form.name}
              onChange={(event) =>
                setForm((current) => ({ ...current, name: event.target.value }))
              }
              maxLength={150}
              required
            />
          </label>
          <label>
            Калории на 100 г
            <input
              type="number"
              min={0}
              step="0.1"
              required
              value={form.caloriesPer100g}
              onChange={(event) =>
                updateNumber('caloriesPer100g', event.target.value)
              }
            />
          </label>
          <label>
            Белки на 100 г
            <input
              type="number"
              min={0}
              step="0.1"
              required
              value={form.proteinPer100g}
              onChange={(event) =>
                updateNumber('proteinPer100g', event.target.value)
              }
            />
          </label>
          <label>
            Жиры на 100 г
            <input
              type="number"
              min={0}
              step="0.1"
              required
              value={form.fatPer100g}
              onChange={(event) => updateNumber('fatPer100g', event.target.value)}
            />
          </label>
          <label>
            Углеводы на 100 г
            <input
              type="number"
              min={0}
              step="0.1"
              required
              value={form.carbsPer100g}
              onChange={(event) =>
                updateNumber('carbsPer100g', event.target.value)
              }
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

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Добавляем...' : 'Добавить продукт'}
          </button>
        </form>

        <div className="panel">
          <h2>Список продуктов</h2>
          <div className="list-stack">
            {isLoading && <p className="muted">Загружаем продукты...</p>}
            {!isLoading && foods.length === 0 && (
              <p className="muted">Продукты пока не найдены.</p>
            )}
            {foods.map((food) => (
              <article className="list-item" key={food.id}>
                <div>
                  <strong>{food.name}</strong>
                  <p className="muted">
                    {food.caloriesPer100g} ккал, Б {food.proteinPer100g} г, Ж{' '}
                    {food.fatPer100g} г, У {food.carbsPer100g} г
                  </p>
                </div>
                {food.isSeed ? (
                  <span className="pill">Базовый</span>
                ) : (
                  <button
                    type="button"
                    className="ghost-button"
                    disabled={deletingFoodId === food.id}
                    onClick={() => void deleteFood(food.id)}
                  >
                    {deletingFoodId === food.id ? 'Удаляем...' : 'Удалить'}
                  </button>
                )}
              </article>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

function validateFood(form: FoodForm) {
  if (!form.name.trim()) {
    return 'Укажите название продукта.'
  }

  if (form.name.trim().length > 150) {
    return 'Название продукта должно быть не длиннее 150 символов.'
  }

  if (
    !isNonNegative(form.caloriesPer100g) ||
    !isNonNegative(form.proteinPer100g) ||
    !isNonNegative(form.fatPer100g) ||
    !isNonNegative(form.carbsPer100g)
  ) {
    return 'Калории, белки, жиры и углеводы должны быть не меньше 0.'
  }

  return ''
}

function isNonNegative(value: number) {
  return Number.isFinite(value) && value >= 0
}
