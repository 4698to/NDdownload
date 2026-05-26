export type NDNode = {
  Name?: string
  IsGrouping?: boolean
  Children?: NDNode[] | null
  StartupFolder?: string | null
  ExtensionType?: string | null
  SubPath?: string | null
  Path?: string | null
  RootType?: string | null
  ext?: string | null
  message?: string | null
  hasHelp?: boolean
  HelpUrl?: string | null
  [key: string]: unknown
}

export type FlatRow = {
  node: NDNode
  path: string
  indexPath: number[]
  rowId: string
}

export type LoadedNDData = {
  roots: NDNode[]
  format: 'object' | 'array'
}

export function parseLoadedData(data: unknown): LoadedNDData {
  if (Array.isArray(data)) {
    return { roots: data as NDNode[], format: 'array' }
  }
  if (data && typeof data === 'object') {
    return { roots: [data as NDNode], format: 'object' }
  }
  return { roots: [], format: 'array' }
}

export function syncHasHelpFromUrl(node: NDNode): void {
  const url = node.HelpUrl
  node.hasHelp = typeof url === 'string' && url.trim().length > 0
}

export function normalizeHasHelp(nodes: NDNode[]): void {
  for (const item of nodes) {
    syncHasHelpFromUrl(item)
    if (Array.isArray(item.Children)) {
      normalizeHasHelp(item.Children)
    }
  }
}

export function serializeRoots(roots: NDNode[], format: 'object' | 'array'): unknown {
  normalizeHasHelp(roots)
  normalizeChildren(roots)
  return format === 'object' ? (roots[0] ?? {}) : roots
}

export function normalizeChildren(nodes: NDNode[]): void {
  for (const item of nodes) {
    if (Array.isArray(item.Children)) {
      normalizeChildren(item.Children)
    } else if (!item.IsGrouping) {
      item.Children = []
    } else {
      item.Children = item.Children ?? []
    }
  }
}

export function flattenTree(
  nodes: NDNode[],
  parentPath = '',
  parentIndexPath: number[] = [],
): FlatRow[] {
  const rows: FlatRow[] = []
  nodes.forEach((node, index) => {
    const indexPath = [...parentIndexPath, index]
    const label = node.Name || node.SubPath || '未命名'
    const path = parentPath ? `${parentPath} / ${label}` : label
    const rowId = indexPath.join('-')
    rows.push({ node, path, indexPath, rowId })
    const children = Array.isArray(node.Children) ? node.Children : []
    if (children.length > 0) {
      rows.push(...flattenTree(children, path, indexPath))
    }
  })
  return rows
}

export function filterRows(rows: FlatRow[], query: string): FlatRow[] {
  const q = query.trim().toLowerCase()
  if (!q) return rows
  return rows.filter(row => {
    const n = row.node
    return [
      row.path,
      n.Name,
      n.SubPath,
      n.StartupFolder,
      n.ExtensionType,
      n.message,
      n.HelpUrl,
    ].some(v => typeof v === 'string' && v.toLowerCase().includes(q))
  })
}

export function getNodeByIndexPath(nodes: NDNode[], indexPath: number[]): NDNode | null {
  let current: NDNode[] = nodes
  let node: NDNode | null = null
  for (const idx of indexPath) {
    node = current[idx] ?? null
    if (!node) return null
    current = Array.isArray(node.Children) ? node.Children : []
  }
  return node
}

export function removeNodeByIndexPath(nodes: NDNode[], indexPath: number[]): boolean {
  if (indexPath.length === 0) return false
  const parentPath = indexPath.slice(0, -1)
  const index = indexPath[indexPath.length - 1]
  const parent = parentPath.length === 0 ? null : getNodeByIndexPath(nodes, parentPath)
  const siblings = parentPath.length === 0 ? nodes : (parent?.Children as NDNode[] | undefined)
  if (!siblings || index < 0 || index >= siblings.length) return false
  siblings.splice(index, 1)
  return true
}

export function createDefaultNode(isGrouping = false): NDNode {
  return {
    Name: isGrouping ? '新分组' : '新工具',
    StartupFolder: isGrouping ? null : 'scripts\\NDTools',
    ExtensionType: isGrouping ? null : '.ms',
    SubPath: isGrouping ? null : 'new_tool.ms',
    IsGrouping: isGrouping,
    Children: isGrouping ? [] : [],
    Path: null,
    RootType: isGrouping ? null : 'MaxRoot',
    ext: null,
    message: null,
    hasHelp: false,
    HelpUrl: null,
  }
}

export function cloneNode(node: NDNode): NDNode {
  return JSON.parse(JSON.stringify(node)) as NDNode
}

export function applyNodeFields(target: NDNode, source: NDNode): void {
  const keys = [
    'Name', 'IsGrouping', 'StartupFolder', 'ExtensionType', 'SubPath',
    'Path', 'RootType', 'ext', 'message', 'HelpUrl',
  ] as const
  for (const key of keys) {
    target[key] = source[key] as never
  }
  syncHasHelpFromUrl(target)
  if (target.IsGrouping) {
    if (!Array.isArray(target.Children)) target.Children = []
  } else {
    target.Children = []
  }
}

export function addChildNode(nodes: NDNode[], parentIndexPath: number[], node: NDNode): boolean {
  if (parentIndexPath.length === 0) {
    nodes.push(node)
    return true
  }
  const parent = getNodeByIndexPath(nodes, parentIndexPath)
  if (!parent) return false
  if (!Array.isArray(parent.Children)) parent.Children = []
  parent.Children.push(node)
  parent.IsGrouping = true
  return true
}

export function findRowById(rows: FlatRow[], rowId: string): FlatRow | undefined {
  return rows.find(r => r.rowId === rowId)
}
