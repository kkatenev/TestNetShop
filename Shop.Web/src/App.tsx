import { useEffect, useMemo, useState, type FormEvent } from 'react'
import * as signalR from '@microsoft/signalr'
import {
  ApiError,
  apiUrl,
  createOrder,
  getActivityLogs,
  getOrders,
  getProducts,
  login,
  register,
} from './api/shop'
import {
  clearSession,
  getToken,
  getUserName,
  isLoggedIn,
  setSession,
} from './auth'
import type { ActivityLog, CartItem, Order, Product } from './types'
import './App.css'

type Tab = 'shop' | 'mail'
type AuthMode = 'login' | 'register'

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('ru-RU')
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    maximumFractionDigits: 0,
  }).format(value)
}

function App() {
  const [loggedIn, setLoggedIn] = useState(isLoggedIn())
  const [userName, setUserName] = useState(getUserName() ?? '')
  const [authMode, setAuthMode] = useState<AuthMode>('login')
  const [loginName, setLoginName] = useState('shop')
  const [password, setPassword] = useState('shop123')
  const [passwordConfirm, setPasswordConfirm] = useState('')
  const [tab, setTab] = useState<Tab>('shop')
  const [products, setProducts] = useState<Product[]>([])
  const [cart, setCart] = useState<CartItem[]>([])
  const [orders, setOrders] = useState<Order[]>([])
  const [logs, setLogs] = useState<ActivityLog[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [ordering, setOrdering] = useState(false)

  const cartTotal = useMemo(
    () => cart.reduce((sum, item) => sum + item.product.price * item.quantity, 0),
    [cart],
  )

  async function loadShop() {
    setError(null)
    setLoading(true)
    try {
      const [productList, orderList] = await Promise.all([getProducts(), getOrders()])
      setProducts(productList)
      setOrders(orderList)
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setLoggedIn(false)
        setError('Сессия истекла. Войди снова.')
      } else {
        setError('API недоступен. Запусти ShopApi на http://localhost:5007')
      }
    } finally {
      setLoading(false)
    }
  }

  async function loadLogs() {
    try {
      setLogs(await getActivityLogs())
    } catch {
      // журнал не должен ронять витрину
    }
  }

  useEffect(() => {
    if (!loggedIn) {
      return
    }

    void loadShop()
    void loadLogs()

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/mail`, {
        accessTokenFactory: () => getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('mailReceived', (log: ActivityLog) => {
      setLogs((prev) => [log, ...prev.filter((item) => item.id !== log.id)].slice(0, 40))
    })

    void connection.start().catch(() => {
      // если хаб недоступен — остаётся первичная загрузка loadLogs
    })

    return () => {
      void connection.stop()
    }
  }, [loggedIn])

  async function handleAuth(event: FormEvent) {
    event.preventDefault()
    setError(null)

    const name = loginName.trim()
    if (!name || !password) {
      setError('Заполни логин и пароль')
      return
    }

    if (authMode === 'register') {
      if (password !== passwordConfirm) {
        setError('Пароли не совпадают')
        return
      }
    }

    try {
      const result =
        authMode === 'login'
          ? await login(name, password)
          : await register(name, password)
      setSession(result.accessToken, result.userName)
      setUserName(result.userName)
      setLoggedIn(true)
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError('Неверный логин или пароль')
      } else if (err instanceof ApiError && err.body) {
        setError(err.body)
      } else {
        setError(
          authMode === 'login'
            ? 'Не удалось войти. Проверь, что API запущен.'
            : 'Не удалось зарегистрироваться. Проверь, что API запущен.',
        )
      }
    }
  }

  function switchAuthMode(mode: AuthMode) {
    setAuthMode(mode)
    setError(null)
    if (mode === 'register') {
      setLoginName('')
      setPassword('')
      setPasswordConfirm('')
    } else {
      setLoginName('shop')
      setPassword('shop123')
      setPasswordConfirm('')
    }
  }

  function handleLogout() {
    clearSession()
    setLoggedIn(false)
    setUserName('')
    setTab('shop')
    setProducts([])
    setCart([])
    setOrders([])
    setLogs([])
    setError(null)
  }

  function addToCart(product: Product) {
    setCart((current) => {
      const existing = current.find((item) => item.product.id === product.id)
      if (existing) {
        return current.map((item) =>
          item.product.id === product.id
            ? { ...item, quantity: item.quantity + 1 }
            : item,
        )
      }

      return [...current, { product, quantity: 1 }]
    })
  }

  function changeQuantity(productId: number, delta: number) {
    setCart((current) =>
      current
        .map((item) =>
          item.product.id === productId
            ? { ...item, quantity: item.quantity + delta }
            : item,
        )
        .filter((item) => item.quantity > 0),
    )
  }

  async function handleCheckout() {
    if (cart.length === 0) {
      return
    }

    setOrdering(true)
    setError(null)

    try {
      const order = await createOrder(
        cart.map((item) => ({
          productId: item.product.id,
          quantity: item.quantity,
        })),
      )
      setCart([])
      setOrders((current) => [order, ...current])
      await loadLogs()
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setLoggedIn(false)
        setError('Сессия истекла. Войди снова.')
      } else {
        setError('Не удалось оформить заказ')
      }
    } finally {
      setOrdering(false)
    }
  }

  if (!loggedIn) {
    return (
      <main className="page login-page">
        <section className="login-card">
          <p className="brand">Shop</p>
          <h1>{authMode === 'login' ? 'Вход в магазин' : 'Регистрация'}</h1>

          <nav className="tabs auth-tabs" aria-label="Вход или регистрация">
            <button
              type="button"
              className={authMode === 'login' ? 'tab active' : 'tab'}
              onClick={() => switchAuthMode('login')}
            >
              Вход
            </button>
            <button
              type="button"
              className={authMode === 'register' ? 'tab active' : 'tab'}
              onClick={() => switchAuthMode('register')}
            >
              Регистрация
            </button>
          </nav>

          {authMode === 'login' && (
            <p className="hint">
              Демо: <code>shop</code> / <code>shop123</code>
            </p>
          )}
          {authMode === 'register' && (
            <p className="hint">Логин от 3 символов, пароль от 6.</p>
          )}

          <form className="login-form" onSubmit={handleAuth}>
            <label>
              Логин
              <input
                value={loginName}
                onChange={(event) => setLoginName(event.target.value)}
                autoComplete="username"
              />
            </label>
            <label>
              Пароль
              <input
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete={authMode === 'login' ? 'current-password' : 'new-password'}
              />
            </label>
            {authMode === 'register' && (
              <label>
                Повтор пароля
                <input
                  type="password"
                  value={passwordConfirm}
                  onChange={(event) => setPasswordConfirm(event.target.value)}
                  autoComplete="new-password"
                />
              </label>
            )}
            <button type="submit">
              {authMode === 'login' ? 'Войти' : 'Создать аккаунт'}
            </button>
          </form>
          {error && <p className="error">{error}</p>}
        </section>
      </main>
    )
  }

  return (
    <main className="shop">
      <header className="shop-header">
        <div>
          <p className="brand">Shop</p>
          <h1>{tab === 'shop' ? 'Каталог' : 'Почта'}</h1>
          <p className="hint">
            {tab === 'shop'
              ? `Оплата имитируется письмом. Вы вошли как ${userName}.`
              : 'Письма появляются здесь после обработки заказа Worker’ом.'}
          </p>
        </div>
        <button type="button" className="secondary" onClick={handleLogout}>
          Выйти
        </button>
      </header>

      <nav className="tabs" aria-label="Разделы">
        <button
          type="button"
          className={tab === 'shop' ? 'tab active' : 'tab'}
          onClick={() => setTab('shop')}
        >
          Магазин
        </button>
        <button
          type="button"
          className={tab === 'mail' ? 'tab active' : 'tab'}
          onClick={() => setTab('mail')}
        >
          Почта
          {logs.length > 0 && <span className="badge">{logs.length}</span>}
        </button>
      </nav>

      {error && <p className="error">{error}</p>}

      {tab === 'shop' && (
        <>
          {loading && <p>Загрузка витрины…</p>}

          <div className="shop-layout">
            <section>
              <div className="product-grid">
                {products.map((product) => (
                  <article key={product.id} className="product-card">
                    <img src={product.imageUrl} alt={product.name} loading="lazy" />
                    <div className="product-body">
                      <h2>{product.name}</h2>
                      <p>{product.description}</p>
                      <div className="product-footer">
                        <strong>{formatMoney(product.price)}</strong>
                        <button type="button" onClick={() => addToCart(product)}>
                          В корзину
                        </button>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            </section>

            <aside className="sidebar">
              <section className="panel">
                <h2>Корзина</h2>
                {cart.length === 0 ? (
                  <p className="empty">Пока пусто. Добавь товары с витрины.</p>
                ) : (
                  <>
                    <ul className="cart-list">
                      {cart.map((item) => (
                        <li key={item.product.id}>
                          <div>
                            <strong>{item.product.name}</strong>
                            <span>
                              {formatMoney(item.product.price)} × {item.quantity}
                            </span>
                          </div>
                          <div className="qty">
                            <button
                              type="button"
                              className="secondary"
                              onClick={() => changeQuantity(item.product.id, -1)}
                            >
                              −
                            </button>
                            <button
                              type="button"
                              className="secondary"
                              onClick={() => changeQuantity(item.product.id, 1)}
                            >
                              +
                            </button>
                          </div>
                        </li>
                      ))}
                    </ul>
                    <div className="cart-total">
                      <span>Итого</span>
                      <strong>{formatMoney(cartTotal)}</strong>
                    </div>
                    <button type="button" disabled={ordering} onClick={() => void handleCheckout()}>
                      {ordering ? 'Оформляем…' : 'Оформить заказ'}
                    </button>
                    <p className="hint small">
                      После оформления письмо появится во вкладке «Почта».
                    </p>
                  </>
                )}
              </section>

              <section className="panel">
                <h2>Мои заказы</h2>
                {orders.length === 0 ? (
                  <p className="empty">Заказов ещё нет.</p>
                ) : (
                  <ul className="order-list">
                    {orders.map((order) => (
                      <li key={order.id}>
                        <strong>#{order.id}</strong>
                        <span>{formatMoney(order.total)}</span>
                        <em>{order.status === 'AwaitingPayment' ? 'ждёт оплаты' : order.status}</em>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </aside>
          </div>
        </>
      )}

      {tab === 'mail' && (
        <section className="panel mail-panel">
          <h2>Входящие</h2>
          <p className="hint">
            Только письма об оплате. Если пусто — оформи заказ и подожди ~2 секунды (Worker должен быть запущен).
          </p>
          {logs.length === 0 ? (
            <p className="empty">Писем пока нет.</p>
          ) : (
            <ul className="mail-list">
              {logs.map((log) => (
                <li key={log.id}>
                  <time dateTime={log.createdAt}>{formatTime(log.createdAt)}</time>
                  <p>{log.message}</p>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </main>
  )
}

export default App
