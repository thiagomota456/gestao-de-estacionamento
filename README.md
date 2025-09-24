
# Teste Técnico ADN2001 – Desenvolvedor(a) (Gestão de Estacionamento)

## 1. Tempo Máximo
Este desafio foi desenhado para ser realizado em **até 4 horas**.

---

## 2. Objetivo
Este teste consiste em uma aplicação full-stack, com **Frontend em React** e **Backend em .NET C# Web API**, composta pelas seguintes telas:

- **Cliente**
  Atualmente permite apenas listagem, cadastro e exclusão.  

- **Veículo**
  Atualmente permite apenas a edição simplificada do modelo.  

- **Faturamento**
  Responsável pela geração de faturas para clientes mensalistas.  

---

## 3. Proposta do Teste
O(a) candidato(a) deverá configurar o ambiente, compreender o funcionamento do código existente e realizar as correções e melhorias listadas abaixo. As regras gerais estão detalhadas na seção *Escopo & Regras*.  

### Tarefa 1 – Completar a Tela de Cliente
- Implementar a edição de clientes, permitindo alterar **Nome**, **Telefone**, **Endereço**, **Valor da mensalidade** e se o cliente é mensalista.  
- Garantir unicidade na base para os campos (**Nome + Telefone**).  
- Exibir mensagens de erro claras e úteis para orientar o usuário.  

### Tarefa 2 – Completar a Tela de Veículos
- Permitir edição do **Modelo**, **Ano** e também a **troca de cliente** associado ao veículo.  
- A troca de cliente pode ocorrer no meio de um período de faturamento. Neste caso, a fatura deverá ser proporcional, considerando os dias em que o veículo esteve associado a cada cliente.  

### Tarefa 3 – Melhorar Upload CSV
- Detalhar melhor as mensagens de erro no processo de importação.  
- O relatório deve identificar claramente a **linha** e o **motivo do erro**.  

---

## 4. Informações para Configuração do Projeto

### 4.1 Stack de Referência
- **Backend**: .NET 8 Web API + EF Core + PostgreSQL  
- **Frontend**: React (Vite) + React Router + React Query  
- **Sem containers**: a conexão é configurada diretamente em `appsettings.json`.  

> É permitido substituir React por Angular/Vue e/ou trocar o ORM, desde que o escopo seja mantido e as decisões sejam explicadas no README. O boilerplate fornecido está em React com JavaScript.  

### 4.2 Execução Local

#### Banco PostgreSQL
1. Crie um banco local (ex.: `parking_test`) e ajuste a `ConnectionString` em `appsettings.json`, se necessário.  
2. Rode o seed pelo terminal (bash/WSL):  
   ```bash
   psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
   ```  
   Caso utilize Windows sem WSL, execute o script pelo gerenciador de banco de dados de sua preferência (ex.: DBeaver).  

#### Backend
```bash
cd src/backend
dotnet restore
dotnet run
```
A API será iniciada (por padrão) em `http://localhost:5000`. Swagger ativado em `/swagger`.  

#### Frontend
```bash
cd src/frontend
npm install
npm run dev
```
A aplicação ficará disponível em `http://localhost:5173`.  
Configure `VITE_API_URL` caso seja necessário apontar para outra porta.  

### 4.3 Estrutura de Pastas
```
/src/backend        -> API .NET 8
/src/frontend       -> React (Vite)
/scripts/seed.sql   -> Criação e seed do banco
/scripts/exemplo.csv-> CSV de exemplo
```

---

## 5. Escopo & Regras

### Clientes
- Deve haver filtro por mensalista (`true|false|all`) e paginação simples.  
- Chave composta por **Nome + Telefone** (telefone normalizado apenas dígitos).  
- Validações:
  - Telefone sanitizado (apenas caracteres numéricos).  

### Veículos
- Associados a um cliente (1:N).  
- Validações:
  - Placa única (case-insensitive).  
  - Ano entre 1900 e o ano atual (quando informado).  
  - Placa sanitizada e validada no padrão Mercosul.  

### Faturamento
- Entrada no formato `"competencia": "yyyy-MM"`.  
- Geração de faturas apenas para mensalistas com veículos ativos.  
- Evitar duplicidade por (cliente, competência).  
- Associar veículos faturados em `fatura_veiculo`.  
- Ajustar a lógica de faturamento para lidar corretamente com troca de cliente durante a competência.  

### Importação CSV
- Endpoint: `POST /api/import/csv` (campo `file`).  
- Formato de exemplo: `scripts/exemplo.csv`.  
- Retorno esperado: `{ processados, inseridos, erros }`.  
- Melhorar detalhamento de erros por linha.  

---

## 6. Critérios de Avaliação
- Modelagem e regras (unicidade, troca de cliente refletida, faturamento proporcional).  
- Clareza e organização do código.  
- Boas práticas no uso do PostgreSQL e do ORM.  
- Robustez da rotina de importação CSV.  
- Funcionalidade do frontend (estado consistente sem necessidade de recarregar a página).  
- Documentação clara sobre decisões e limitações.  

---

## 7. Observações
- O uso de ferramentas de apoio (incluindo IA) é permitido, desde que o(a) candidato(a) tenha pleno domínio do código entregue.  
- O frontend pode ser simples, desde que funcione corretamente e seja claro.  
- Explique no README final todas as decisões técnicas relevantes.  

---

## 8. Entrega
- Espera-se que, no prazo acordado, o código seja publicado em um repositório Git público.  
- Na reunião de alinhamento, o(a) candidato(a) deverá apresentar o funcionamento do código entregue e explicar as decisões técnicas adotadas, as limitações e a resolução das tarefas propostas.  
