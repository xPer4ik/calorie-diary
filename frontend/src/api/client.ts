const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5010'
const TOKEN_KEY = 'calorie-diary-token'

export class ApiError extends Error {
  status: number
  details?: unknown

  constructor(status: number, message: string, details?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

type RequestOptions = {
  method?: string
  body?: unknown
  auth?: boolean
}

export function getStoredToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function setStoredToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearStoredToken() {
  localStorage.removeItem(TOKEN_KEY)
}

export async function apiRequest<T>(
  path: string,
  { method = 'GET', body, auth = true }: RequestOptions = {},
): Promise<T> {
  const headers = new Headers()

  if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }

  if (auth) {
    const token = getStoredToken()

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
  }

  let response: Response

  try {
    response = await fetch(`${API_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(
      0,
      'Ошибка.',
    )
  }

  const text = await response.text()
  const data = parseJson(text)

  if (!response.ok) {
    throw new ApiError(
      response.status,
      getErrorMessage(data),
      isObject(data) ? data.details : undefined,
    )
  }

  return data as T
}

function parseJson(text: string) {
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

function getErrorMessage(data: unknown) {
  if (!isObject(data)) {
    return 'Запрос не выполнен.'
  }

  const baseMessage =
    typeof data.error === 'string'
      ? translateMessage(data.error)
      : 'Запрос не выполнен.'
  const details = formatDetails(data.details)

  return details ? `${baseMessage} ${details}` : baseMessage
}

function formatDetails(details: unknown) {
  if (!isObject(details)) {
    return ''
  }

  const messages = Object.entries(details).flatMap(([field, value]) => {
    if (Array.isArray(value)) {
      return value.map((item) => `${translateField(field)}: ${translateMessage(String(item))}`)
    }

    return [`${translateField(field)}: ${translateMessage(String(value))}`]
  })

  return messages.length > 0 ? messages.join(' ') : ''
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function translateField(field: string) {
  const fields: Record<string, string> = {
    displayName: 'имя',
    email: 'электронная почта',
    password: 'пароль',
    gender: 'пол',
    age: 'возраст',
    heightCm: 'рост',
    weightKg: 'вес',
    activityLevel: 'активность',
    goal: 'цель',
    name: 'название',
    caloriesPer100g: 'калории',
    proteinPer100g: 'белки',
    fatPer100g: 'жиры',
    carbsPer100g: 'углеводы',
    date: 'дата',
    mealType: 'прием пищи',
    foodItemId: 'продукт',
    grams: 'граммы',
  }

  return fields[field] ?? 'поле'
}

function translateMessage(message: string) {
  const messages: Record<string, string> = {
    'Validation failed.': 'Проверьте данные.',
    'Email is already registered.': 'Эта электронная почта уже зарегистрирована.',
    'Invalid email or password.': 'Неверная электронная почта или пароль.',
    'Authentication is required or the token is invalid.': 'Нужно войти в аккаунт.',
    'You do not have access to this resource.': 'Нет доступа к этому действию.',
    'User id claim is missing or invalid.': 'Сессия недействительна.',
    'User from token was not found.': 'Пользователь из сессии не найден.',
    'Profile is not created yet.': 'Профиль еще не создан.',
    'Food item was not found.': 'Продукт не найден.',
    'Diary entry was not found.': 'Запись дневника не найдена.',
    'Display name is required.': 'Укажите имя.',
    'Display name must be 100 characters or fewer.': 'Имя должно быть не длиннее 100 символов.',
    'Email is required.': 'Укажите электронную почту.',
    'Email must be 256 characters or fewer.': 'Электронная почта должна быть не длиннее 256 символов.',
    'Email format is invalid.': 'Неверный формат электронной почты.',
    'Password is required.': 'Укажите пароль.',
    'Password must contain at least 6 characters.': 'Пароль должен содержать минимум 6 символов.',
    'Food name is required.': 'Укажите название продукта.',
    'Food name must be 150 characters or fewer.': 'Название продукта должно быть не длиннее 150 символов.',
    'Calories per 100g must be greater than or equal to 0.': 'Калории на 100 г должны быть не меньше 0.',
    'Protein per 100g must be greater than or equal to 0.': 'Белки на 100 г должны быть не меньше 0.',
    'Fat per 100g must be greater than or equal to 0.': 'Жиры на 100 г должны быть не меньше 0.',
    'Carbs per 100g must be greater than or equal to 0.': 'Углеводы на 100 г должны быть не меньше 0.',
    'Date must use YYYY-MM-DD format.': 'Дата должна быть в формате год-месяц-день.',
    'Food item id is required.': 'Выберите продукт.',
    'Grams must be greater than 0.': 'Количество граммов должно быть больше 0.',
    'Meal type must be breakfast, lunch, dinner, or snack.': 'Выберите корректный прием пищи.',
  }

  if (messages[message]) {
    return messages[message]
  }

  return /[A-Za-z]/.test(message) ? 'Проверьте данные.' : message
}
