import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import logoUrl from '../assets/food-diary-logo.svg'

type AuthPageProps = {
  mode: 'login' | 'register'
}

export function AuthPage({ mode }: AuthPageProps) {
  const { isAuthenticated, isLoading, login, register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isRegister = mode === 'register'

  if (isLoading) {
    return <div className="page-status">Загружаем сессию...</div>
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const trimmedEmail = email.trim()
      const trimmedDisplayName = displayName.trim()

      if (isRegister) {
        await register(trimmedEmail, password, trimmedDisplayName)
      } else {
        await login(trimmedEmail, password)
      }

      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Что-то пошло не так')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-panel">
        <div className="brand-block auth-brand">
          <img className="brand-logo brand-logo-large" src={logoUrl} alt="" />
          <div>
            <p className="brand">Дневник калорий</p>
            <p className="muted">Питание, цели и дневник в одном месте</p>
          </div>
        </div>
        <h1>{isRegister ? 'Создать аккаунт' : 'Вход в аккаунт'}</h1>
        <p className="muted">
          {isRegister
            ? 'Заведите дневник, сохраняйте продукты и следите за дневной нормой.'
            : 'Войдите, чтобы продолжить учет питания.'}
        </p>

        <form className="form-grid" onSubmit={handleSubmit} aria-busy={isSubmitting}>
          {isRegister && (
            <label>
              Имя
              <input
                value={displayName}
                onChange={(event) => setDisplayName(event.target.value)}
                autoComplete="name"
                required
              />
            </label>
          )}

          <label>
            Электронная почта
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="email"
              required
            />
          </label>

          <label>
            Пароль
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete={isRegister ? 'new-password' : 'current-password'}
              minLength={6}
              required
            />
          </label>

          {error && (
            <p className="error-message" role="alert" aria-live="polite">
              {error}
            </p>
          )}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? 'Пожалуйста, подождите...'
              : isRegister
                ? 'Зарегистрироваться'
                : 'Войти'}
          </button>
        </form>

        <p className="auth-switch">
          {isRegister ? 'Уже есть аккаунт?' : 'Еще нет аккаунта?'}{' '}
          <Link to={isRegister ? '/login' : '/register'}>
            {isRegister ? 'Войти' : 'Зарегистрироваться'}
          </Link>
        </p>
      </section>

      <footer className="app-footer auth-page-footer">
        <span>Дневник калорий</span>
        <span>Учебное приложение для осознанного питания.</span>
      </footer>
    </main>
  )
}
