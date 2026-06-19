import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  apiRequest,
  clearStoredToken,
  getStoredToken,
  setStoredToken,
} from '../api/client'

export type CurrentUser = {
  id: number
  email: string
  displayName: string
}

type AuthResponse = {
  token: string
  user: CurrentUser
}

type AuthContextValue = {
  user: CurrentUser | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (
    email: string,
    password: string,
    displayName: string,
  ) => Promise<void>
  logout: () => void
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const logout = useCallback(() => {
    clearStoredToken()
    setUser(null)
  }, [])

  const refreshUser = useCallback(async () => {
    if (!getStoredToken()) {
      setUser(null)
      setIsLoading(false)
      return
    }

    try {
      const currentUser = await apiRequest<CurrentUser>('/api/auth/me')
      setUser(currentUser)
    } catch {
      logout()
    } finally {
      setIsLoading(false)
    }
  }, [logout])

  useEffect(() => {
    void refreshUser()
  }, [refreshUser])

  const login = useCallback(async (email: string, password: string) => {
    const response = await apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      auth: false,
      body: { email, password },
    })

    setStoredToken(response.token)
    setUser(response.user)
  }, [])

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const response = await apiRequest<AuthResponse>('/api/auth/register', {
        method: 'POST',
        auth: false,
        body: { email, password, displayName },
      })

      setStoredToken(response.token)
      setUser(response.user)
    },
    [],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      isAuthenticated: Boolean(user),
      login,
      register,
      logout,
      refreshUser,
    }),
    [isLoading, login, logout, refreshUser, register, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)

  if (!value) {
    throw new Error('useAuth must be used inside AuthProvider')
  }

  return value
}
