import axios from '@/plugins/axios'

/** 从 /download 拉取 JSON，自动处理 BOM 与字符串响应 */
export async function downloadJson(fileId: string): Promise<unknown> {
  const res = await axios.get('/download', { params: { fileid: fileId } })
  let data: unknown = res.data
  if (typeof data === 'string') {
    const trimmed = data.replace(/^\uFEFF/, '').trim()
    return JSON.parse(trimmed)
  }
  return data
}
