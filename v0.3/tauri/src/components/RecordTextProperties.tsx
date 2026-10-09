type RecordTextProperty = {
  name: string
  type: 'boolean' | 'number' | 'text'
  defaultValue: string
  options?: readonly string[]
}

export const recordTextProperties: readonly RecordTextProperty[] = [
  { name: 'splittable', type: 'boolean', defaultValue: 'false' },
  { name: 'expandable', type: 'boolean', defaultValue: 'false' },
  { name: 'height', type: 'number', defaultValue: '20' },
  { name: 'width', type: 'number', defaultValue: '140' },
  { name: 'x', type: 'number', defaultValue: '20' },
  { name: 'y', type: 'number', defaultValue: '20' },
  { name: 'angle', type: 'number', defaultValue: '0' },
  { name: 'id', type: 'text', defaultValue: '' },
  { name: 'field', type: 'text', defaultValue: 'FieldName' },
  { name: 'vAlign', type: 'text', defaultValue: 'top', options: ['bottom', 'center', 'top'] },
  { name: 'underline', type: 'boolean', defaultValue: 'false' },
  { name: 'textColor', type: 'text', defaultValue: 'Black' },
  { name: 'leading', type: 'number', defaultValue: '12' },
  { name: 'fontSize', type: 'number', defaultValue: '10' },
  { name: 'font', type: 'text', defaultValue: 'Helvetica' },
  { name: 'align', type: 'text', defaultValue: 'left', options: ['center', 'fulljustify', 'justify', 'left', 'right'] },
  { name: 'autoLeading', type: 'boolean', defaultValue: 'true' },
  { name: 'cleanParagraphBreaks', type: 'boolean', defaultValue: 'false' },
  { name: 'paragraphIndent', type: 'number', defaultValue: '0' },
  { name: 'paragraphSpacing', type: 'number', defaultValue: '0' },
]

export function RecordTextProperties({ element, onChange }: {
  element: Element
  onChange: (name: string, value: string) => void
}) {
  const attributes = Array.from(element.attributes)
  const knownNames = new Set(recordTextProperties.map(({ name }) => name.toLowerCase()))
  return <>
    {recordTextProperties.map((property) => {
      const attribute = attributes.find(({ name }) => name.toLowerCase() === property.name.toLowerCase())
      const value = attribute?.value ?? property.defaultValue
      const options = property.type === 'boolean' ? ['false', 'true'] : property.options
      const update = (nextValue: string) => onChange(attribute?.name ?? property.name, nextValue)
      return <label key={property.name}>
        <span>{property.name}</span>
        {options ? <select value={value} onChange={(event) => update(event.target.value)}>
          {options.map((option) => <option key={option} value={option}>{option}</option>)}
        </select> : <input type={property.type === 'number' ? 'number' : 'text'} step={property.type === 'number' ? 'any' : undefined} value={value} onChange={(event) => update(event.target.value)} />}
      </label>
    })}
    {attributes.filter(({ name }) => !knownNames.has(name.toLowerCase())).map(({ name, value }) =>
      <label key={name}><span>{name}</span><input value={value} onChange={(event) => onChange(name, event.target.value)} /></label>,
    )}
  </>
}
