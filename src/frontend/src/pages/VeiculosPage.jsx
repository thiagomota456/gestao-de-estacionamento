
import React, { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'

export default function VeiculosPage(){
  const qc = useQueryClient()
  const [clienteId, setClienteId] = useState('')
  const clientes = useQuery({ queryKey:['clientes-mini'], queryFn:() => apiGet('/api/clientes?pagina=1&tamanho=100') })
  const veiculos = useQuery({ queryKey:['veiculos', clienteId], queryFn:() => apiGet(`/api/veiculos${clienteId?`?clienteId=${clienteId}`:''}`) })

  const [form, setForm] = useState({ placa:'', modelo:'', ano:'', clienteId:'' })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/veiculos', data),
    onSuccess: () => qc.invalidateQueries({ queryKey:['veiculos'] })
  })

  // TODO (candidato): implementar edição e deleção com troca de cliente
  // BUG atual: após editar, a lista pode não atualizar (React Query: invalidar corretamente as keys)
  const update = useMutation({
    mutationFn: ({id, data}) => apiPut(`/api/veiculos/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey:['veiculos'] })
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/veiculos/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey:['veiculos'] })
  })

  useEffect(()=>{
    if(clientes.data?.itens?.length && !clienteId){
      setClienteId(clientes.data.itens[0].id)
      setForm(f => ({...f, clienteId: clientes.data.itens[0].id}))
    }
  }, [clientes.data])

  return (
    <div>
      <h2>Veículos</h2>

      <div style={{display:'flex', gap:8, alignItems:'center'}}>
        <label>Cliente: </label>
        <select value={clienteId} onChange={e=>{ setClienteId(e.target.value); setForm(f=>({...f, clienteId:e.target.value}))}}>
          {clientes.data?.itens?.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
      </div>

      <h3>Novo veículo</h3>
      <div style={{display:'grid', gap:6, gridTemplateColumns:'repeat(4, 1fr)'}}>
        <input placeholder="Placa" value={form.placa} onChange={e=>setForm({...form, placa:e.target.value})}/>
        <input placeholder="Modelo" value={form.modelo} onChange={e=>setForm({...form, modelo:e.target.value})}/>
        <input placeholder="Ano" value={form.ano} onChange={e=>setForm({...form, ano:e.target.value})}/>
        <button onClick={()=>create.mutate({
          placa: form.placa, modelo: form.modelo, ano: form.ano? Number(form.ano): null, clienteId: form.clienteId || clienteId
        })}>Salvar</button>
      </div>

      <h3 style={{marginTop:16}}>Lista</h3>
      {veiculos.isLoading? <p>Carregando...</p> : (
        <table border="1" cellPadding="6">
          <thead><tr><th>Placa</th><th>Modelo</th><th>Ano</th><th>ClienteId</th><th>Ações</th></tr></thead>
          <tbody>
            {veiculos.data?.map(v=>(
              <tr key={v.id}>
                <td>{v.placa}</td>
                <td>{v.modelo}</td>
                <td>{v.ano ?? '-'}</td>
                <td>{v.clienteId}</td>
                <td>
                  <button onClick={()=>{
                    const novoModelo = prompt('Novo modelo', v.modelo || '')
                    if(novoModelo===null) return
                    update.mutate({ id: v.id, data:{ placa: v.placa, modelo: novoModelo, ano: v.ano, clienteId } })
                  }}>Editar</button>
                  <button onClick={()=>remover.mutate(v.id)}>Excluir</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p style={{marginTop:8, color:'#666'}}>TODO: permitir troca de cliente na edição, e garantir atualização de lista sem precisar recarregar a página.</p>
    </div>
  )
}
