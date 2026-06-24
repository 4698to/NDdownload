export type InstallBoxItem = {
  zipname?: string
  abouttext?: string | null
  targetpath?: string | null
  savepath?: string | null
  dirpath?: string | null
  type?: number
  SeriesMin?: number
  SeriesMax?: number
  helplink?: string | null
  selected?: boolean
  IsEnabled?: boolean
  quick?: boolean
  isParent?: boolean
  child?: InstallBoxItem[] | null
  version?: number
  sha?: string | null
  ischange?: boolean
  LastWriteTime?: string
  LastPackTime?: string
  /** 浏览页预计算展示字段（仅 UI，不写入发布 JSON） */
  _displayPath?: string
  _displayDate?: string
  _displaySeries?: string
  [key: string]: unknown
}

export type InstallBoxMeta = Record<string, unknown>

export const DIR_TYPE_OPTIONS = [
  { title: 'MaxRoot（Max 安装目录子路径）', value: 0 },
  { title: 'Application（ApplicationPlugins）', value: 1 },
] as const

export const DIR_TYPE_LABELS: Record<number, string> = {
  0: 'MaxRoot',
  1: 'Application',
}

export function formatDirType(type: unknown): string {
  if (type === null || type === undefined || type === '') return 'MaxRoot'
  const num = Number(type)
  return DIR_TYPE_LABELS[num] ?? String(type)
}

export function getInstallBoxItemPath(item: InstallBoxItem): string {
  if (item.type === 0) {
    return item.dirpath ?? ''
  }
  if (item.dirpath) {
    return `ApplicationPlugins/${item.dirpath}`
  }
  return item.targetpath ?? ''
}

/** 一次性预计算树节点展示字段，避免模板内重复格式化 */
export function enrichInstallBoxDisplay(
  items: InstallBoxItem[],
  seriesMin: number,
  seriesMax: number,
): void {
  for (const item of items) {
    if (item.dirpath) {
      item._displayPath = getInstallBoxItemPath(item)
    }
    item._displayDate = formatMsDate(item.LastPackTime)
    const min = item.SeriesMin ?? 0
    const max = item.SeriesMax ?? 0
    item._displaySeries =
      min !== 0 && max !== 0 ? `${min} - ${max}` : `${seriesMin} - ${seriesMax}`
    if (Array.isArray(item.child)) {
      enrichInstallBoxDisplay(item.child, seriesMin, seriesMax)
    }
  }
}

export function stripDevFields(item: InstallBoxItem): InstallBoxItem {
  const result = cloneItem(item)
  delete result.targetpath
  delete result.savepath
  if (Array.isArray(result.child)) {
    result.child = result.child.map(stripDevFields)
  } else if (!result.isParent) {
    result.child = null
  }
  return result
}

export function buildParentFromScan(parent: InstallBoxItem, children: InstallBoxItem[]): InstallBoxItem {
  return {
    ...cloneItem(parent),
    isParent: true,
    child: children.map(c => cloneItem(c)),
  }
}

export function syncVersionMeta(meta: InstallBoxMeta, version: string, lastPack?: string): InstallBoxMeta {
  const next: InstallBoxMeta = { ...meta, Version: version, _version: version }
  if (lastPack) {
    next.LastPack = lastPack
    next._lastPack = lastPack
  }
  return next
}

export function getVersionRaise(meta: InstallBoxMeta): boolean {
  if (typeof meta.is_version_raise === 'boolean') return meta.is_version_raise
  if (typeof meta.version_raise === 'boolean') return meta.version_raise
  return true
}

export function setVersionRaise(meta: InstallBoxMeta, value: boolean): void {
  meta.is_version_raise = value
  meta.version_raise = value
}

export function previewNextVersion(meta: InstallBoxMeta, versionRaise: boolean): string {
  const raw = String(meta.Version ?? meta._version ?? '1.0')
  const num = Number.parseFloat(raw)
  const base = Number.isFinite(num) ? num : 1.0
  if (!versionRaise) return raw
  return (Math.round((base + 0.01) * 100) / 100).toString()
}

export function countPackLeaves(items: InstallBoxItem[]): number {
  let count = 0
  function walk(nodes: InstallBoxItem[]) {
    for (const node of nodes) {
      if (!node || typeof node !== 'object') continue
      const children = Array.isArray(node.child) ? node.child : []
      if (node.isParent && children.length > 0) {
        for (const child of children) {
          if (child && !child.isParent && needsPackFileCheck(child)) count += 1
        }
      } else if (needsPackFileCheck(node)) {
        count += 1
      }
    }
  }
  walk(items)
  return count
}

export function truncateSha(sha: string | null | undefined, max = 12): string {
  if (!sha) return '—'
  return sha.length <= max ? sha : `${sha.slice(0, max)}…`
}

const MS_DATE_EPOCH = -62135596800000

function toYmdHm(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  const h = String(date.getHours()).padStart(2, '0')
  const min = String(date.getMinutes()).padStart(2, '0')
  return `${y}-${m}-${d} ${h}:${min}`
}

/** 将 Newtonsoft `/Date(ms)/` 或毫秒时间戳格式化为本地时间 */
export function formatMsDate(
  val: string | number | null | undefined,
  emptyLabel = '—',
): string {
  if (val === null || val === undefined || val === '') return emptyLabel

  if (typeof val === 'string' && val.startsWith('/Date(')) {
    const match = val.match(/\/Date\((-?\d+)(?:[+-]\d+)?\)\//)
    if (match) {
      const ms = Number.parseInt(match[1], 10)
      if (ms <= MS_DATE_EPOCH) return emptyLabel
      const date = new Date(ms)
      if (date.getFullYear() < 1970) return emptyLabel
      return toYmdHm(date)
    }
    return emptyLabel
  }

  const num = typeof val === 'string' ? Number.parseInt(val, 10) : val
  if (Number.isFinite(num) && num > MS_DATE_EPOCH) {
    const date = new Date(num)
    if (date.getFullYear() < 1970) return emptyLabel
    return toYmdHm(date)
  }

  return emptyLabel
}

export type LoadedInstallBoxData = {
  meta: InstallBoxMeta
  items: InstallBoxItem[]
}

export type FlatRow = {
  node: InstallBoxItem
  path: string
  indexPath: number[]
  rowId: string
}

const META_ITEM_KEY = 'item'

export function parseLoadedData(data: unknown): LoadedInstallBoxData {
  if (!data || typeof data !== 'object' || Array.isArray(data)) {
    return { meta: {}, items: [] }
  }
  const record = data as Record<string, unknown>
  const items = Array.isArray(record[META_ITEM_KEY])
    ? (record[META_ITEM_KEY] as InstallBoxItem[])
    : []
  const meta: InstallBoxMeta = { ...record }
  delete meta[META_ITEM_KEY]
  return { meta, items }
}

export function serializeData(meta: InstallBoxMeta, items: InstallBoxItem[]): Record<string, unknown> {
  normalizeChildren(items)
  return { ...meta, [META_ITEM_KEY]: items }
}

export function normalizeChildren(items: InstallBoxItem[]): void {
  for (const item of items) {
    if (!item || typeof item !== 'object') continue
    if (Array.isArray(item.child)) {
      normalizeChildren(item.child)
      if (item.child.length === 0 && !item.isParent) {
        item.child = null
      }
    } else if (item.isParent) {
      item.child = item.child ?? []
    } else {
      item.child = null
    }
  }
}

export function flattenTree(
  nodes: InstallBoxItem[],
  parentPath = '',
  parentIndexPath: number[] = [],
): FlatRow[] {
  const rows: FlatRow[] = []
  nodes.forEach((node, index) => {
    if (!node || typeof node !== 'object') return
    const indexPath = [...parentIndexPath, index]
    const label = node.zipname || node.abouttext || '未命名'
    const path = parentPath ? `${parentPath} / ${label}` : label
    const rowId = indexPath.join('-')
    rows.push({ node, path, indexPath, rowId })
    const children = Array.isArray(node.child) ? node.child : []
    if (children.length > 0) {
      rows.push(...flattenTree(children, path, indexPath))
    }
  })
  return rows
}

export function rowMatchesQuery(row: FlatRow, query: string): boolean {
  const q = query.trim().toLowerCase()
  if (!q) return true
  const n = row.node
  return [
    row.path,
    n.zipname,
    n.abouttext,
    n.targetpath,
    n.dirpath,
    n.helplink,
    n.savepath,
  ].some(v => typeof v === 'string' && v.toLowerCase().includes(q))
}

export function getAncestorRowIds(indexPath: number[]): string[] {
  const ids: string[] = []
  for (let i = 1; i < indexPath.length; i++) {
    ids.push(indexPath.slice(0, i).join('-'))
  }
  return ids
}

export function rowHasChildren(row: FlatRow): boolean {
  return Array.isArray(row.node.child) && row.node.child.length > 0
}

export function isRowVisible(row: FlatRow, collapsedIds: Set<string>): boolean {
  return !getAncestorRowIds(row.indexPath).some(id => collapsedIds.has(id))
}

export function filterVisibleRows(rows: FlatRow[], collapsedIds: Set<string>): FlatRow[] {
  return rows.filter(row => isRowVisible(row, collapsedIds))
}

export function getSearchVisibleRowIds(rows: FlatRow[], query: string): Set<string> {
  const visible = new Set<string>()
  for (const row of rows) {
    if (!rowMatchesQuery(row, query)) continue
    visible.add(row.rowId)
    for (const id of getAncestorRowIds(row.indexPath)) {
      visible.add(id)
    }
  }
  return visible
}

export function filterSearchVisibleRows(rows: FlatRow[], query: string): FlatRow[] {
  const visibleIds = getSearchVisibleRowIds(rows, query)
  return rows.filter(row => visibleIds.has(row.rowId))
}

export function getNodeByIndexPath(nodes: InstallBoxItem[], indexPath: number[]): InstallBoxItem | null {
  let current: InstallBoxItem[] = nodes
  let node: InstallBoxItem | null = null
  for (const idx of indexPath) {
    node = current[idx] ?? null
    if (!node) return null
    current = Array.isArray(node.child) ? node.child : []
  }
  return node
}

export function removeNodeByIndexPath(nodes: InstallBoxItem[], indexPath: number[]): boolean {
  if (indexPath.length === 0) return false
  const parentPath = indexPath.slice(0, -1)
  const index = indexPath[indexPath.length - 1]
  const parent = parentPath.length === 0 ? null : getNodeByIndexPath(nodes, parentPath)
  const siblings = parentPath.length === 0 ? nodes : parent?.child
  if (!siblings || index < 0 || index >= siblings.length) return false
  siblings.splice(index, 1)
  return true
}

export function createDefaultItem(isParent = false): InstallBoxItem {
  return {
    zipname: isParent ? '新分组' : '新资源',
    abouttext: '',
    targetpath: null,
    savepath: null,
    dirpath: 'scripts',
    type: 0,
    SeriesMin: 2015,
    SeriesMax: 2025,
    helplink: null,
    selected: true,
    IsEnabled: true,
    quick: false,
    isParent,
    child: isParent ? [] : null,
    version: 1,
    sha: null,
    ischange: true,
    LastWriteTime: '/Date(-62135596800000)/',
    LastPackTime: '/Date(-62135596800000)/',
  }
}

export function cloneItem(node: InstallBoxItem): InstallBoxItem {
  return JSON.parse(JSON.stringify(node)) as InstallBoxItem
}

const EDITABLE_ITEM_KEYS = [
  'zipname', 'abouttext', 'targetpath', 'savepath', 'dirpath', 'type',
  'SeriesMin', 'SeriesMax', 'helplink', 'selected', 'IsEnabled', 'quick',
  'isParent', 'version', 'sha', 'ischange',
] as const

export function applyItemFields(target: InstallBoxItem, source: InstallBoxItem): void {
  for (const key of EDITABLE_ITEM_KEYS) {
    target[key] = source[key] as never
  }
  if (target.isParent) {
    if (!Array.isArray(target.child)) target.child = []
  } else {
    target.child = null
  }
}

export function addChildItem(nodes: InstallBoxItem[], parentIndexPath: number[], item: InstallBoxItem): boolean {
  if (parentIndexPath.length === 0) {
    nodes.push(item)
    return true
  }
  const parent = getNodeByIndexPath(nodes, parentIndexPath)
  if (!parent) return false
  if (!Array.isArray(parent.child)) parent.child = []
  parent.child.push(item)
  parent.isParent = true
  return true
}

export function findRowById(rows: FlatRow[], rowId: string): FlatRow | undefined {
  return rows.find(r => r.rowId === rowId)
}

export function formatSeriesRange(item: InstallBoxItem): string {
  const min = item.SeriesMin ?? 0
  const max = item.SeriesMax ?? 0
  if (min === 0 && max === 0) return '全部'
  return `${min} - ${max}`
}

export type PackFileCheckResult = {
  exists: boolean
  source: 'packpath' | null
  filename: string | null
  path?: string | null
  error?: string | null
}

export function needsPackFileCheck(item: InstallBoxItem): boolean {
  if (!item.zipname?.trim()) return false
  if (item.isParent && Array.isArray(item.child) && item.child.length > 0) return false
  return true
}

export function collectPackFileNames(items: InstallBoxItem[]): string[] {
  const names = new Set<string>()
  function walk(nodes: InstallBoxItem[]) {
    for (const node of nodes) {
      if (!node || typeof node !== 'object') continue
      if (needsPackFileCheck(node)) {
        names.add(node.zipname!.trim())
      }
      const children = Array.isArray(node.child) ? node.child : []
      if (children.length > 0) walk(children)
    }
  }
  walk(items)
  return [...names]
}

export function getRowPackFileName(row: FlatRow): string | null {
  return needsPackFileCheck(row.node) ? row.node.zipname!.trim() : null
}
