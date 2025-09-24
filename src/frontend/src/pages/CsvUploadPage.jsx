
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
      <form onSubmit={handleUpload}>
        <input type="file" name="file" accept=".csv" />
        <button type="submit">Enviar</button>
      </form>
      <pre style={{background:'#111', color:'#0f0', padding:8, marginTop:12}}>{log? JSON.stringify(log, null, 2) : 'Aguardando upload...'}</pre>
      <p style={{marginTop:8, color:'#666'}}>Tarefa: melhorar relatório de erros (linhas e motivos mais claros; opcional transação por lote).</p>
    </div>
  )
}
