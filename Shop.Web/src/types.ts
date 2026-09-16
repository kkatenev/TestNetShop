export type Product = {
  id: number
  name: string
  description: string
  price: number
  imageUrl: string
}

export type CartItem = {
  product: Product
  quantity: number
}

export type OrderItem = {
  id: number
  productId: number
  productName: string
  unitPrice: number
  quantity: number
}

export type Order = {
  id: number
  userName: string
  createdAt: string
  total: number
  status: string
  items: OrderItem[]
}

export type ActivityLog = {
  id: number
  createdAt: string
  taskId: number | null
  message: string
}
