import axios from 'axios'

let baseURL = '';
switch (import.meta.env.MODE) {
  case 'development':
    baseURL = import.meta.env.VITE_API_BASE_URL;
    break;
  default:
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    baseURL = (window as any).serverUrl || '';
    break
}

const http = axios.create({
  baseURL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
http.interceptors.request.use(
  config => {
    const accessToken = localStorage.getItem('token');
    if (accessToken) {
      config.headers = Object.assign({
        Authorization: `Bearer ${accessToken}`,
      }, config.headers);
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// 响应拦截器
http.interceptors.response.use(
  response => {
    return response.data
  },
  error => {
    // 统一错误处理
    if (error.response) {
      switch (error.response.status) {
        case 401:
          // 未授权处理
          break
        case 403:
          // 禁止访问处理
          break
        case 404:
          // 资源不存在处理
          break
        default:
          // 其他错误处理
          break
      }
    }
    return Promise.reject(error)
  }
)

export default http
