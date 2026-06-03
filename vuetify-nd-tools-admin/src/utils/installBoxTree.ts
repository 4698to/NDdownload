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
  [key: string]: unknown
}

export type InstallBoxMeta = Record<string, unknown>

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
