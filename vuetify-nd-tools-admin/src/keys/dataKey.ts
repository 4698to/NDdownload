import type { InjectionKey, Ref } from 'vue'

export type RequestDataKeyFn = (onVerified: () => void) => void

export const requestDataKeyKey: InjectionKey<RequestDataKeyFn> = Symbol('requestDataKey')
export const isAuthorizedKey: InjectionKey<Ref<boolean>> = Symbol('isAuthorized')
