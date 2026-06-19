import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import logoUrl from '../assets/food-diary-logo.svg'

const navItems = [
  { to: '/dashboard', label: 'Главная' },
  { to: '/profile', label: 'Калькулятор и профиль' },
  { to: '/diary', label: 'Дневник' },
  { to: '/foods', label: 'Продукты' },
]

export function AppLayout() {
  const { logout, user } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-block">
          <img className="brand-logo" src={logoUrl} alt="" />
          <div>
            <p className="brand">Дневник калорий</p>
            <p className="muted">Учет питания и дневных целей</p>
          </div>
        </div>

        <div className="topbar-user">
          <span>{user?.displayName}</span>
          <button type="button" className="ghost-button" onClick={handleLogout}>
            Выйти
          </button>
        </div>
      </header>

      <nav className="main-nav" aria-label="Основная навигация">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) => (isActive ? 'active' : undefined)}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <main className="page-content">
        <Outlet />
      </main>

      <footer className="app-footer">
        <span>Дневник калорий</span>
        <span>Учебный проект для расчета нормы питания и ведения дневника.</span>
      </footer>
    </div>
  )
}
