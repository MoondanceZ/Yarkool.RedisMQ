import { ref, watch } from 'vue';
import { tryOnUnmounted } from '@vueuse/core';

export function useTimeoutFn (handle: (...args: unknown[]) => unknown, wait: number, native = false) {
  if (typeof handle !== 'function') {
    throw new Error('handle is not Function!');
  }

  const { readyRef, stop, start } = useTimeoutRef(wait);
  if (native) {
    handle();
  } else {
    watch(
      readyRef,
      maturity => {
        if (maturity) {
          handle();
        }
      },
      { immediate: false },
    );
  }
  return { readyRef, stop, start };
}

export function useTimeoutRef (wait: number) {
  const readyRef = ref(false);

  let timer: ReturnType<typeof setTimeout>;
  function stop (): void {
    readyRef.value = false;
    if (timer) {
      window.clearTimeout(timer);
    }
  }
  function start (): void {
    stop();
    timer = setTimeout(() => {
      readyRef.value = true;
    }, wait);
  }

  start();

  tryOnUnmounted(stop);

  return { readyRef, stop, start };
}
