import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, apiRequest } from '../api/client'

type ProfileForm = {
  gender: 'male' | 'female'
  age: number
  heightCm: number
  weightKg: number
  activityLevel: 'low' | 'light' | 'moderate' | 'high'
  goal: 'lose' | 'maintain' | 'gain'
}

type CalculationResult = {
  bmr: number
  tdee: number
  dailyCaloriesTarget: number
  proteinTarget: number
  fatTarget: number
  carbsTarget: number
}

type ProfileResponse = ProfileForm & CalculationResult

const initialForm: ProfileForm = {
  gender: 'female',
  age: 28,
  heightCm: 168,
  weightKg: 62,
  activityLevel: 'moderate',
  goal: 'maintain',
}

export function ProfilePage() {
  const [form, setForm] = useState<ProfileForm>(initialForm)
  const [result, setResult] = useState<CalculationResult | null>(null)
  const [status, setStatus] = useState('')
  const [error, setError] = useState('')
  const [isLoadingProfile, setIsLoadingProfile] = useState(true)
  const [isCalculating, setIsCalculating] = useState(false)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    async function loadProfile() {
      try {
        const profile = await apiRequest<ProfileResponse>('/api/profile')
        const profileForm = toProfileForm(profile)

        setForm(profileForm)
        setResult(await getCompleteCalculation(profile, profileForm))
      } catch {
        setResult(null)
      } finally {
        setIsLoadingProfile(false)
      }
    }

    void loadProfile()
  }, [])

  function update<K extends keyof ProfileForm>(key: K, value: ProfileForm[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  async function calculate() {
    setError('')
    setStatus('')
    const validationError = validateProfileForm(form)

    if (validationError) {
      setError(validationError)
      return
    }

    setIsCalculating(true)

    try {
      const calculation = await apiRequest<CalculationResult>('/api/calculator', {
        method: 'POST',
        auth: false,
        body: form,
      })
      setResult(calculation)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось выполнить расчет')
    } finally {
      setIsCalculating(false)
    }
  }

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setStatus('')
    const validationError = validateProfileForm(form)

    if (validationError) {
      setError(validationError)
      return
    }

    setIsSaving(true)

    try {
      const savedProfile = await apiRequest<ProfileResponse>('/api/profile', {
        method: 'PUT',
        body: form,
      })
      setResult(await getCompleteCalculation(savedProfile, form))
      setStatus('Профиль сохранен')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить профиль')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <h1>Калькулятор и профиль</h1>
          <p className="muted">Рассчитайте дневную норму и сохраните цели.</p>
        </div>
      </div>

      <div className="two-column">
        <form className="panel form-grid" onSubmit={saveProfile} aria-busy={isSaving}>
          <label>
            Пол
            <select
              value={form.gender}
              onChange={(event) =>
                update('gender', event.target.value as ProfileForm['gender'])
              }
            >
              <option value="female">Женский</option>
              <option value="male">Мужской</option>
            </select>
          </label>

          <label>
            Возраст
            <input
              type="number"
              min={10}
              max={100}
              required
              value={form.age}
              onChange={(event) => update('age', Number(event.target.value))}
            />
          </label>

          <label>
            Рост, см
            <input
              type="number"
              min={100}
              max={230}
              step="0.1"
              required
              value={form.heightCm}
              onChange={(event) => update('heightCm', Number(event.target.value))}
            />
          </label>

          <label>
            Вес, кг
            <input
              type="number"
              min={30}
              max={250}
              step="0.1"
              required
              value={form.weightKg}
              onChange={(event) => update('weightKg', Number(event.target.value))}
            />
          </label>

          <label>
            Активность
            <select
              value={form.activityLevel}
              onChange={(event) =>
                update(
                  'activityLevel',
                  event.target.value as ProfileForm['activityLevel'],
                )
              }
            >
              <option value="low">Низкая</option>
              <option value="light">Легкая</option>
              <option value="moderate">Средняя</option>
              <option value="high">Высокая</option>
            </select>
          </label>

          <label>
            Цель
            <select
              value={form.goal}
              onChange={(event) =>
                update('goal', event.target.value as ProfileForm['goal'])
              }
            >
              <option value="lose">Снижение веса</option>
              <option value="maintain">Поддержание веса</option>
              <option value="gain">Набор веса</option>
            </select>
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

          <div className="button-row">
            <button
              type="button"
              className="secondary-button"
              onClick={calculate}
              disabled={isCalculating || isSaving}
            >
              {isCalculating ? 'Считаем...' : 'Рассчитать'}
            </button>
            <button type="submit" disabled={isSaving || isCalculating}>
              {isSaving ? 'Сохраняем...' : 'Сохранить профиль'}
            </button>
          </div>
        </form>

        <div className="panel">
          <h2>Цели</h2>
          {isLoadingProfile ? (
            <p className="muted">Загружаем профиль...</p>
          ) : result ? (
            <div className="metric-list">
              <Metric label="Базовая потребность" value={result.bmr} suffix="ккал" />
              <Metric label="Суточная потребность" value={result.tdee} suffix="ккал" />
              <Metric
                label="Дневная норма"
                value={result.dailyCaloriesTarget}
                suffix="ккал"
              />
              <Metric label="Белки" value={result.proteinTarget} suffix="г" />
              <Metric label="Жиры" value={result.fatTarget} suffix="г" />
              <Metric label="Углеводы" value={result.carbsTarget} suffix="г" />
            </div>
          ) : (
            <p className="muted">Рассчитайте или сохраните профиль, чтобы увидеть цели.</p>
          )}
        </div>
      </div>
    </section>
  )
}

function toProfileForm(profile: ProfileResponse): ProfileForm {
  return {
    gender: profile.gender,
    age: profile.age,
    heightCm: profile.heightCm,
    weightKg: profile.weightKg,
    activityLevel: profile.activityLevel,
    goal: profile.goal,
  }
}

async function getCompleteCalculation(
  profile: ProfileResponse,
  form: ProfileForm,
): Promise<CalculationResult> {
  if (hasCompleteCalculation(profile)) {
    return profile
  }

  return apiRequest<CalculationResult>('/api/calculator', {
    method: 'POST',
    auth: false,
    body: form,
  })
}

function hasCompleteCalculation(profile: ProfileResponse) {
  return Number.isFinite(profile.bmr) && Number.isFinite(profile.tdee)
}

function validateProfileForm(form: ProfileForm) {
  if (!Number.isFinite(form.age) || form.age < 10 || form.age > 100) {
    return 'Возраст должен быть от 10 до 100 лет.'
  }

  if (
    !Number.isFinite(form.heightCm) ||
    form.heightCm < 100 ||
    form.heightCm > 230
  ) {
    return 'Рост должен быть от 100 до 230 см.'
  }

  if (
    !Number.isFinite(form.weightKg) ||
    form.weightKg < 30 ||
    form.weightKg > 250
  ) {
    return 'Вес должен быть от 30 до 250 кг.'
  }

  return ''
}

function Metric({
  label,
  value,
  suffix,
}: {
  label: string
  value: number
  suffix: string
}) {
  return (
    <div className="metric-row">
      <span>{label}</span>
      <strong>
        {Number.isFinite(value) ? value : '-'} {suffix}
      </strong>
    </div>
  )
}
