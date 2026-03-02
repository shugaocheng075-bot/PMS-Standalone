export interface RetryCheck {
  when: () => boolean
  task: () => Promise<void>
}

export interface InitialLoadOptions {
  tasks: Array<() => Promise<void>>
  retryChecks?: RetryCheck[]
}

export function useResilientLoad() {
  const runInitialLoad = async (options: InitialLoadOptions) => {
    await Promise.allSettled(options.tasks.map((task) => task()))

    if (!options.retryChecks?.length) {
      return
    }

    for (const check of options.retryChecks) {
      if (check.when()) {
        await check.task()
      }
    }
  }

  return {
    runInitialLoad,
  }
}
