import { useEffect, useMemo, useRef, useState } from 'react'

export function DplxXmlEditor({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [search, setSearch] = useState('')
  const [matchCase, setMatchCase] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)
  const editor = useRef<HTMLTextAreaElement>(null)
  const overlay = useRef<HTMLPreElement>(null)
  const searchInput = useRef<HTMLInputElement>(null)
  const matches = useMemo(() => {
    if (!search) return []
    const escaped = search.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    return Array.from(value.matchAll(new RegExp(escaped, matchCase ? 'g' : 'gi')), (match) => ({ start: match.index!, end: match.index! + match[0].length }))
  }, [value, search, matchCase])
  const current = matches.length ? Math.min(activeIndex, matches.length - 1) : 0
  useEffect(() => { setActiveIndex(0) }, [search, matchCase])
  const highlighted = useMemo(() => {
    const tokens = Array.from(value.matchAll(/<!--[\s\S]*?-->|<!\[CDATA\[[\s\S]*?\]\]>|<\?[\s\S]*?\?>|<\/?[\w:.-]+|\/?>|"[^"]*"|'[^']*'|[\w:.-]+(?=\s*=)/g), (match) => ({
      start: match.index!, end: match.index! + match[0].length,
      kind: match[0].startsWith('<!--') ? 'comment' : /^["']/.test(match[0]) ? 'string' : match[0].startsWith('<') || match[0].endsWith('>') ? 'tag' : 'attribute',
    }))
    const boundaries = [...new Set([0, value.length, ...tokens.flatMap(({ start, end }) => [start, end]), ...matches.flatMap(({ start, end }) => [start, end])])].sort((a, b) => a - b)
    let tokenIndex = 0
    let matchIndex = 0
    return boundaries.slice(0, -1).map((start, index) => {
      const end = boundaries[index + 1]
      while (tokens[tokenIndex] && tokens[tokenIndex].end <= start) tokenIndex++
      while (matches[matchIndex] && matches[matchIndex].end <= start) matchIndex++
      const token = tokens[tokenIndex]
      const isMatch = matches[matchIndex] && matches[matchIndex].start <= start && end <= matches[matchIndex].end
      const text = <span className={token && token.start <= start ? `xml-${token.kind}` : undefined}>{value.slice(start, end)}</span>
      return isMatch ? <mark key={start} className={matchIndex === current ? 'xml-active-match' : undefined}>{text}</mark> : <span key={start}>{text}</span>
    })
  }, [value, matches, current])

  function syncScroll() {
    if (editor.current && overlay.current) {
      overlay.current.scrollTop = editor.current.scrollTop
      overlay.current.scrollLeft = editor.current.scrollLeft
    }
  }
  useEffect(() => {
    const input = editor.current
    const first = matches[0]
    if (!input || !first) return
    const lines = value.slice(0, first.start).split('\n')
    input.scrollTop = Math.max(0, (lines.length - 1) * 18.6 - input.clientHeight / 2)
    input.scrollLeft = Math.max(0, lines[lines.length - 1].length * 7.23 - input.clientWidth / 2)
    syncScroll()
  }, [search, matchCase])

  function navigate(direction: number) {
    if (!matches.length || !editor.current) return
    const index = (current + direction + matches.length) % matches.length
    setActiveIndex(index)
    const match = matches[index]
    const input = editor.current
    if (document.activeElement !== searchInput.current) input.focus()
    input.setSelectionRange(match.start, match.end)
    const preceding = value.slice(0, match.start)
    const lines = preceding.split('\n')
    input.scrollTop = Math.max(0, (lines.length - 1) * 18.6 - input.clientHeight / 2)
    input.scrollLeft = Math.max(0, lines[lines.length - 1].length * 7.23 - input.clientWidth / 2)
    syncScroll()
  }

  return <div className="dplx-xml-workspace" onKeyDown={(event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'f') {
      event.preventDefault()
      searchInput.current?.focus()
      searchInput.current?.select()
    }
    if (event.key === 'F3') { event.preventDefault(); navigate(event.shiftKey ? -1 : 1) }
  }}>
    <div className="xml-search-toolbar">
      <input ref={searchInput} type="search" aria-label="Search XML" placeholder="Search XML…" value={search} onChange={(event) => setSearch(event.target.value)} onKeyDown={(event) => {
        if (event.key === 'Enter') { event.preventDefault(); navigate(event.shiftKey ? -1 : 1) }
        if (event.key === 'Escape') { setSearch(''); editor.current?.focus() }
      }} />
      <label><input type="checkbox" checked={matchCase} onChange={(event) => setMatchCase(event.target.checked)} />Match case</label>
      <span role="status">{search ? matches.length ? `${current + 1} of ${matches.length}` : 'No matches' : 'Search XML'}</span>
      <button type="button" disabled={!matches.length} onClick={() => navigate(-1)}>Previous</button>
      <button type="button" disabled={!matches.length} onClick={() => navigate(1)}>Next</button>
      <button type="button" disabled={!search} onClick={() => { setSearch(''); searchInput.current?.focus() }}>Clear</button>
    </div>
    <div className="xml-editor-stack">
      <pre ref={overlay} className="xml-highlight-layer" aria-hidden="true">{highlighted}{'\n'}</pre>
      <textarea ref={editor} className="report-xml-editor" aria-label="DPLX XML" wrap="off" spellCheck={false} value={value} onChange={(event) => onChange(event.target.value)} onScroll={syncScroll} />
    </div>
  </div>
}
