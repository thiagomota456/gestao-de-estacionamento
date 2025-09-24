
# Teste Técnico – Desenvolvedor(a) Pleno (Gestão de Estacionamento)

## ⏱️ Tempo Máximo
Este desafio foi desenhado para ser realizado em **até 4 horas**.

Recomendação de divisão do tempo:
- 1h: CRUD de Veículos (editar/deletar + troca de cliente)  
- 1.5h: Correção do faturamento por competência (snapshot)  
- 0.5h: Sanitização/validação de placa Mercosul  
- 1h: Melhorias no relatório do CSV e pequenos ajustes

## 🎯 Objetivo
Construir uma aplicação full-stack simples para gerenciar **Clientes**, **Veículos** e **Faturamento mensal**, com suporte a **importação CSV**.

## 🛠️ Stack de Referência
- Backend: .NET 8 Web API + EF Core + PostgreSQL
- Frontend: React (Vite) + React Router + React Query
- **Sem containers**: conexão local no `appsettings.json`

> Você pode trocar React por Angular/Vue, e/ou o ORM, mas mantenha o escopo e explique no README as escolhas.

## 🚀 Como Rodar (local)
### 1) Banco PostgreSQL
- Crie um banco local (ex.: `parking_test`) e ajuste `appsettings.json` se necessário.
- Rode o seed via prompt (bash/WSL):
  ```bash
  psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
  ```
- Caso esteja no Windows e não possua WSL pode abrir um gerenciador de banco de dados (DBeaver por ex.) e execute o arquivo de seed.

### 2) Backend
```bash
cd src/backend
dotnet restore
dotnet run
```
A API sobe (por padrão) em `http://localhost:5000` (ou conforme configurado). Swagger ativado.

### 3) Frontend
```bash
cd src/frontend
npm install
npm run dev
```
Acesse `http://localhost:5173`. Configure `VITE_API_URL` se precisar apontar para outra porta.

## 📚 Escopo & Regras
### Clientes
- CRUD completo, filtro por mensalista (`true|false|all`), paginação simples.
- Evitar duplicidade por **Nome + Telefone** (telefone normalizado apenas dígitos).

### Veículos
- Associados a um cliente (1:N).
- **Já implementado**: listar e criar.
- **Tarefa**: **editar e deletar**, inclusive **trocar o cliente** do veículo na edição.
- **Validações**:
  - Placa única (case-insensitive).
  - Ano entre 1900 e o ano atual (quando informado).
  - Placa sanitizada e validada no padrão Mercosul.

### Faturamento
- `POST /api/faturas/gerar` com `{ "competencia": "yyyy-MM" }` gera faturas para mensalistas.
- Evitar duplicidade por (cliente, competência).
- Associar veículos faturados em `fatura_veiculo`.
- **BUG proposital**: a lógica atual usa o **dono ATUAL** do veículo, não o **dono na data de corte** (último dia do mês). Corrija para respeitar o snapshot por competência.

### Importação CSV
- Endpoint: `POST /api/import/csv` (campo `file`).
- Formato exemplo em `scripts/exemplo.csv`.
- Retornar relatório `{ processados, inseridos, erros }`.
- **Tarefa**: melhorar mensagens de erro (linha e motivo), e opcionalmente transação por lote.

## 📂 Pastas
```
/src/backend        -> API .NET 8
/src/frontend       -> React (Vite)
/scripts/seed.sql   -> Criação e seed do banco
/scripts/exemplo.csv-> CSV de exemplo
```

## 🧪 O que será avaliado
- Modelagem e regras (placa única, troca de cliente refletida, faturamento por competência).
- Qualidade do código e separação de camadas.
- Uso consciente do PostgreSQL/ORM.
- Robustez do CSV.
- Front funcional (estado consistente pós PUT/DELETE).
- Documentação (explicar decisões e limitações).

## 📝 Observações
- O uso de IA é permitido, **desde que** você domine o que foi entregue.
- O front pode ser simples; priorize funcionalidade e clareza.
- Explique decisões importantes no README final.
