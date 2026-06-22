import type { Ref } from 'vue';
import { ref, unref, watch } from 'vue';
import { useDebounceFn, useThrottleFn } from '@vueuse/core';

export type RemoveEventFn = () => void;
type EventTargetLike = Pick<typeof window, 'addEventListener' | 'removeEventListener'>;
type EventHandler = Extract<Parameters<typeof window.addEventListener>[1], (...args: never[]) => void>;
type EventOptions = Parameters<typeof window.addEventListener>[2];

export interface UseEventParams {
  el?: EventTargetLike | Ref<EventTargetLike | undefined>;
  name: string;
  listener: EventHandler;
  options?: EventOptions;
  autoRemove?: boolean;
  isDebounce?: boolean;
  wait?: number;
}
export function useEventListener ({
  el = window,
  name,
  listener,
  options,
  autoRemove = true,
  isDebounce = true,
  wait = 80,
}: UseEventParams): { removeEvent: RemoveEventFn } {

  let remove: RemoveEventFn = () => {};
  const isAddRef = ref(false);

  if (el) {
    const element = ref(unref(el)) as Ref<EventTargetLike>;

    const handler = isDebounce ? useDebounceFn(listener, wait) : useThrottleFn(listener, wait);
    const realHandler = wait ? handler : listener;
    const removeEventListener = (e: EventTargetLike) => {
      e.removeEventListener(name, realHandler, options);
      isAddRef.value = false;
    };
    const addEventListener = (e: EventTargetLike) => {
      e.addEventListener(name, realHandler, options);
      isAddRef.value = true;
    };

    const removeWatch = watch(
      element,
      (v, _ov, cleanUp) => {
        if (v) {
          if (!unref(isAddRef)) {
            addEventListener(v);
          }
          cleanUp(() => {
            if (autoRemove) {
              removeEventListener(v);
            }
          });
        }
      },
      { immediate: true },
    );

    remove = () => {
      removeEventListener(element.value);
      removeWatch();
    };
  }
  return { removeEvent: remove };
}
