const STORAGE_KEY = 'debug-log'
const MAX_ENTRIES = 100

const LEVEL_ORDER = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
} as const

const DEFAULT_CONFIG = {
  minimumLevel: 'debug',
  sinks: {
    console: true,
    localStorage: true,
    applicationInsights: true,
  },
} as const

const RESTRICTED_DATA_KEY = /password|passcode|token|authorization|cookie|email|display.?name/i

export type DebugLogLevel = keyof typeof LEVEL_ORDER

export interface DebugLogSinks {
  console: boolean
  localStorage: boolean
  applicationInsights: boolean
}

export interface DebugLogEntry {
  timestamp: string
  source: string
  level?: DebugLogLevel
  message: string
  data?: Record<string, unknown>
}

export interface DebugLogOptions {
  sinks?: Partial<DebugLogSinks>
}

export interface DebugLogSourceOptions {
  minimumLevel?: DebugLogLevel
}

export interface DebugLogger {
  debug: (message: string, data?: Record<string, unknown>, options?: DebugLogOptions) => void
  info: (message: string, data?: Record<string, unknown>, options?: DebugLogOptions) => void
  warn: (message: string, data?: Record<string, unknown>, options?: DebugLogOptions) => void
  error: (message: string, data?: Record<string, unknown>, options?: DebugLogOptions) => void
}

interface DebugLogInput {
  source: string
  level: DebugLogLevel
  message: string
  data?: Record<string, unknown>
  sinks?: Partial<DebugLogSinks>
}

interface DebugLoggingConfig {
  minimumLevel: DebugLogLevel
  sinks: DebugLogSinks
}

function getStricterMinimumLevel(a: DebugLogLevel, b: DebugLogLevel): DebugLogLevel {
  return LEVEL_ORDER[a] >= LEVEL_ORDER[b] ? a : b
}

function isDebugLogLevel(value: unknown): value is DebugLogLevel {
  return typeof value === 'string' && value in LEVEL_ORDER
}

function normalizeConfig(value: unknown): DebugLoggingConfig {
  if (!value || typeof value !== 'object') {
    return {
      minimumLevel: DEFAULT_CONFIG.minimumLevel,
      sinks: { ...DEFAULT_CONFIG.sinks },
    }
  }

  const config = value as { minimumLevel?: unknown; sinks?: unknown }
  const sinks = config.sinks as Partial<DebugLogSinks> | undefined

  return {
    minimumLevel: isDebugLogLevel(config.minimumLevel)
      ? config.minimumLevel
      : DEFAULT_CONFIG.minimumLevel,
    sinks: {
      console: typeof sinks?.console === 'boolean' ? sinks.console : DEFAULT_CONFIG.sinks.console,
      localStorage:
        typeof sinks?.localStorage === 'boolean'
          ? sinks.localStorage
          : DEFAULT_CONFIG.sinks.localStorage,
      applicationInsights:
        typeof sinks?.applicationInsights === 'boolean'
          ? sinks.applicationInsights
          : DEFAULT_CONFIG.sinks.applicationInsights,
    },
  }
}

function filterSafeData(
  data: Record<string, unknown> | undefined,
): Record<string, unknown> | undefined {
  if (!data) {
    return undefined
  }

  return Object.fromEntries(Object.entries(data).filter(([key]) => !RESTRICTED_DATA_KEY.test(key)))
}

function formatConsoleTimestamp(timestamp: string): string {
  const date = new Date(timestamp)
  const pad = (value: number) => value.toString().padStart(2, '0')

  return `${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(
    date.getMinutes(),
  )}:${pad(date.getSeconds())}`
}

function getLogs(): DebugLogEntry[] {
  if (!import.meta.client) {
    return []
  }

  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return []
    }

    const entries: unknown = JSON.parse(raw)
    return Array.isArray(entries) ? (entries as DebugLogEntry[]) : []
  } catch {
    return []
  }
}

function clear(): void {
  if (!import.meta.client) {
    return
  }

  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Local storage is optional and must not disrupt the viewer.
  }
}

interface DebugLogStorage {
  getLogs: () => DebugLogEntry[]
  clear: () => void
}

interface SourceDebugLog extends DebugLogStorage {
  logger: DebugLogger
}

export function useDebugLog(source: string): SourceDebugLog
export function useDebugLog(source: string, options: DebugLogSourceOptions): SourceDebugLog
export function useDebugLog(): DebugLogStorage
export function useDebugLog(source?: string, options?: DebugLogSourceOptions): SourceDebugLog {
  const appConfig = useAppConfig()
  //TODO: const appInsights = useAppInsights()
  const config = normalizeConfig(appConfig.debugLogging)
  const sourceMinimumLevel =
    options?.minimumLevel && isDebugLogLevel(options.minimumLevel)
      ? options.minimumLevel
      : config.minimumLevel
  const effectiveMinimumLevel = getStricterMinimumLevel(config.minimumLevel, sourceMinimumLevel)

  function writeConsole(entry: DebugLogEntry): void {
    try {
      const method = console[entry.level ?? 'debug']
      if (typeof method !== 'function') {
        return
      }

      const abbreviations: Record<DebugLogLevel, string> = {
        debug: 'DEB',
        info: 'INF',
        warn: 'WAR',
        error: 'ERR',
      }
      const output = `${abbreviations[entry.level ?? 'debug']} ${formatConsoleTimestamp(
        entry.timestamp,
      )} ${entry.source}: ${entry.message}`

      if (entry.data) {
        method.call(console, output, entry.data)
      } else {
        method.call(console, output)
      }
    } catch {
      // Console availability must not affect other sinks.
    }
  }

  function writeLocalStorage(entry: DebugLogEntry): void {
    try {
      const entries = getLogs()
      entries.push(entry)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(entries.slice(-MAX_ENTRIES)))
    } catch {
      // Storage availability must not affect other sinks.
    }
  }

  function writeApplicationInsights(entry: DebugLogEntry): void {
    try {
      //TODO
      /*
      appInsights.trackEvent(
        { name: 'DebugLog' },
        {
          timestamp: entry.timestamp,
          source: entry.source,
          level: entry.level ?? 'debug',
          message: entry.message,
          ...(entry.data ? { data: JSON.stringify(entry.data) } : {}),
        },
      )
        */
    } catch {
      // Telemetry availability must not affect other sinks.
    }
  }

  function emit(input: DebugLogInput): void {
    if (!import.meta.client) {
      return
    }

    if (LEVEL_ORDER[input.level] < LEVEL_ORDER[config.minimumLevel]) {
      return
    }

    const entry: DebugLogEntry = {
      timestamp: new Date().toISOString(),
      source: input.source,
      level: input.level,
      message: input.message,
      data: filterSafeData(input.data),
    }
    const sinks = input.sinks ?? {}

    if (config.sinks.console && sinks.console !== false) {
      writeConsole(entry)
    }
    if (config.sinks.localStorage && sinks.localStorage !== false) {
      writeLocalStorage(entry)
    }
    if (config.sinks.applicationInsights && sinks.applicationInsights !== false) {
      writeApplicationInsights(entry)
    }
  }

  function createLevelLogger(level: DebugLogLevel) {
    if (LEVEL_ORDER[level] < LEVEL_ORDER[effectiveMinimumLevel]) {
      return (
        _message: string,
        _data?: Record<string, unknown>,
        _options?: DebugLogOptions,
      ): void => {
        // This level is below the effective source/global threshold for this logger instance.
      }
    }

    return (message: string, data?: Record<string, unknown>, options?: DebugLogOptions): void => {
      if (!source) {
        return
      }

      emit({
        source,
        level,
        message,
        data,
        sinks: options?.sinks,
      })
    }
  }

  return {
    logger: {
      debug: createLevelLogger('debug'),
      info: createLevelLogger('info'),
      warn: createLevelLogger('warn'),
      error: createLevelLogger('error'),
    },
    getLogs,
    clear,
  }
}
