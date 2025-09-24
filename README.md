
# Teste Técnico ADN2001 – Desenvolvedor(a) (Gestão de Estacionamento)

## ⏱️ Tempo Máximo
Este desafio foi desenhado para ser realizado em **até 4 horas**.


## 🎯 Objetivo

Este teste é uma aplicação full-stack completa com Frontend em React e backend em .NET C# Web.Api com as seguintes telas

Composto pelas Telas:

  - Cliente
    Permitido apenas listagem, cadastro e exclusão. 
  - Veículo 
    Permite apenas a edição do Modelo
  - Faturamento
    Gera as faturas para os mensalistas.

## Proposta do Teste

  É desejado que você configure a máquina em seu ambiente, compreenda como o código funcione e consiga realizar a correção das tarefas 
abaixo. As regras de execução da os estão na seção *Escopo & Regras* mais abaixo neste documento.
  
  ### Tarefa 1 - Completar a Tela de Cliente
  
  Desejamos ter a opção de Editar os clientes Permitindo Trocar Nome, Telefone, Endereço, VAlor da mensalidade e se o cliente é mensalista
ou não. Devemos garantir unicidade na base para os campos (Nome + Telefone). 
  Exibir mensagens de erro corretamente que orientem o usuário do erro que o mesmo cometeu.
  Se atente a seção 'Escopo & Regras'

  ### Tarefa 2 - Completar a Tela de Veículos

  A edição de veículos foi feita de forma bem simplificada, desejamos permitir editar o Modelo e Ano do veículo, e permitir a troca do cliente
inclusive no meio de um período de faturamento. Neste caso a fatura deverá ser parcial pela data de corte tanto para o primeiro cliente quanto para o segundo cliente 
proporcional ao número de dias.

  ### Tarefa 3 - Upload CSV
  Temos uma demanda de melhoria nas mensagens de erro de forma que seja possível compreender melhor os erros que ocorreram durante o processo de importação. A proposta é que haja o detalhamento dos erros por linha.


--
## Informações para configuração do Projeto

### 🛠️ Stack de Referência
- Backend: .NET 8 Web API + EF Core + PostgreSQL
- Frontend: React (Vite) + React Router + React Query
- **Sem containers**: conexão local no `appsettings.json`

> Você pode trocar React por Angular/Vue, e/ou o ORM, mas mantenha o escopo e explique no README as escolhas. O boilerplate criado está em React com javascript.

### 🚀 Como Rodar (local)
#### 1) Banco PostgreSQL
- Crie um banco local (ex.: `parking_test`) e ajuste `appsettings.json` se necessário.
- Rode o seed via prompt (bash/WSL):
  ```bash
  psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
  ```
- Caso esteja no Windows e não possua WSL pode abrir um gerenciador de banco de dados (DBeaver por ex.) e execute o arquivo de seed.

#### 2) Backend
```bash
cd src/backend
dotnet restore
dotnet run
```
A API sobe (por padrão) em `http://localhost:5000` (ou conforme configurado). Swagger ativado.

#### 3) Frontend
```bash
cd src/frontend
npm install
npm run dev
```
Acesse `http://localhost:5173`. Configure `VITE_API_URL` se precisar apontar para outra porta.

### 📂 Pastas
```
/src/backend        -> API .NET 8
/src/frontend       -> React (Vite)
/scripts/seed.sql   -> Criação e seed do banco
/scripts/exemplo.csv-> CSV de exemplo
```

## 📚 Escopo & Regras
### Clientes
- Filtro por mensalista (`true|false|all`), paginação simples.
- Chave composta por **Nome + Telefone** (telefone normalizado apenas dígitos).
  - **Validações**:
  - Telefone sanitizado e somente númerico 

### Veículos
- Associados a um cliente (1:N).
- **Validações**:
  - Placa única (case-insensitive).
  - Ano entre 1900 e o ano atual (quando informado).
  - Placa sanitizada e validada no padrão Mercosul.

### Faturamento
- "competencia": "yyyy-MM". 
- Gera faturas apenas para mensalistas com veículos.
- Evitar duplicidade por (cliente, competência).
- Associar veículos faturados em `fatura_veiculo`.

### Importação CSV
- Endpoint: `POST /api/import/csv` (campo `file`).
- Formato exemplo em `scripts/exemplo.csv`.
- Retornar relatório `{ processados, inseridos, erros }`.


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


## Entrega
Espera-se que no prazo acordado seja publicado uma URL pública do Git com o projeto modificado e que na reunião de alinhamento seja realizada a apresentação funcional do código entregue, explicando-se as decisões, limitações e o código das tarefas propostas.
