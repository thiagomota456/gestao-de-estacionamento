import React, { useState } from 'react'

export default function CsvUploadPage(){
  const [log, setLog] = useState(null)

  async function handleUpload(e){
    e.preventDefault()
    const file = e.target.file.files[0]
    const fd = new FormData()
    fd.append('file', file)
    const r = await fetch((import.meta.env.VITE_API_URL || 'http://localhost:5000') + '/api/import/csv', {
      method: 'POST',
      body: fd
    })
    const j = await r.json()
    setLog(j)
  }

  return (
    <div>
      <h2>Importar CSV</h2>
      <div className="section">
        <form onSubmit={handleUpload} style={{display:'flex', gap:10, alignItems:'center'}}>
          <input type="file" name="file" accept=".csv" />
          <button type="submit">Enviar</button>
        </form>
      </div>

      <h3 style={{marginTop:16}}>Relatório</h3>
      <div className="section">
        <pre style={{background:'#0b0c0e', color:'#c7d2fe', padding:12, margin:0, borderRadius:10}}>
{log? JSON.stringify(log, null, 2) : 'Aguardando upload...'}
        </pre>
        <p className="note">Tarefa: melhorar o relatório de erros (linhas e motivos mais claros; opcional transação por lote).</p>
      </div>
    </div>
  )
}
