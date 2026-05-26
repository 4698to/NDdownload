import axios from 'axios'
import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios'
import { getDataKey } from '@/utils/dataKey'

const instance = axios.create({
  baseURL: '/api', // 开发环境通过 Vite 代理到目标服务器
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
instance.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const dataKey = getDataKey()
    const method = config.method?.toLowerCase()
    if (dataKey && method && ['post', 'put', 'patch', 'delete'].includes(method)) {
      config.headers.set('X-NDTools-Data-Key', dataKey)
    }
    return config
  },
  (error: any) => Promise.reject(error)
)

// 响应拦截器
instance.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: any) => {
    if (error?.response?.status === 401) {
      import('@/utils/dataKey').then(({ clearDataKey }) => clearDataKey())
    }
    return Promise.reject(error)
  }
)

export default instance 