import { createApp, h, ref } from 'vue'
import { createVuetify } from 'vuetify'
import { VBtn, VIcon, VSnackbar } from 'vuetify/components'

export interface ToastOptions {
  text: string
  color?: 'success' | 'error' | 'warning' | 'info'
  timeout?: number
}

// 添加全局变量跟踪当前 toast 实例
let currentToast: {
  app: ReturnType<typeof createApp>
  container: HTMLElement
} | null = null

const destroyToast = () => {
  if (currentToast) {
    currentToast.app.unmount()
    document.body.removeChild(currentToast.container)
    currentToast = null
  }
}

const createToast = (options: ToastOptions) => {
  // 销毁已存在的 toast
  destroyToast()

  const toastApp = createApp({
    setup () {
      const show = ref(true)

      return () => h('div', { class: 'v-application' }, [
        h('div', { class: 'v-application--wrap' }, [
          h(VSnackbar, {
            modelValue: show.value,
            'onUpdate:modelValue': (value: boolean) => {
              show.value = value
              if (!value) {
                setTimeout(() => {
                  destroyToast()
                }, 300)
              }
            },
            location: 'top',
            color: options.color || 'success',
            timeout: options.timeout || 3000,
          }, {
            default: () => options.text,
            actions: () => h(VBtn, {
              icon: true,
              variant: 'text',
              size: 'small',
              onClick: () => show.value = false,
            }, () => h(VIcon, {
              size: 'small',
            }, () => 'mdi-close')),
          }),
        ]),
      ])
    },
  })

  // 使用 Vuetify
  const vuetify = createVuetify({
    components: {
      VSnackbar,
      VBtn,
      VIcon,
    },
  })
  toastApp.use(vuetify)

  // 创建容器并挂载
  const container = document.createElement('div')
  document.body.appendChild(container)
  toastApp.mount(container)

  // 保存当前 toast 实例
  currentToast = {
    app: toastApp,
    container,
  }
}

const toast = {
  show: (options: ToastOptions | string) => {
    if (typeof options === 'string') {
      createToast({ text: options })
    } else {
      createToast(options)
    }
  },
  success: (text: string, timeout?: number) => {
    createToast({ text, color: 'success', timeout })
  },
  error: (text: string, timeout?: number) => {
    createToast({ text, color: 'error', timeout })
  },
  warning: (text: string, timeout?: number) => {
    createToast({ text, color: 'warning', timeout })
  },
  info: (text: string, timeout?: number) => {
    createToast({ text, color: 'info', timeout })
  },
}
export default toast
