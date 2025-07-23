import axios from 'axios'
import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios'

const instance = axios.create({
  baseURL: '/', // 可根据需要修改
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
instance.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // 可在此添加 token 等
    return config
  },
  (error: any) => Promise.reject(error)
)

// 响应拦截器
instance.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: any) => {
    // 可统一处理错误
    return Promise.reject(error)
  }
)

export default instance 