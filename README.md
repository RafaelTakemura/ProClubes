# ProClubes — Sistema Integrado de Gestão de Clubes Associativos (v1.2)

O **ProClubes** é uma plataforma multiplataforma para gestão administrativa, financeira, operacional e de relacionamento com associados de redes de clubes sociais e recreativos. O ecossistema é composto por três aplicações integradas (**API REST**, **Portal Web da Diretoria** e **Aplicativo Mobile do Associado/Dependente**) acompanhadas de suítes automatizadas de testes unitários e de integração.

---

## Arquitetura da Solução

A solução ([`ProClubes.slnx`](./ProClubes.slnx)) está organizada nos seguintes projetos:

| Projeto | Tecnologia | Descrição |
| :--- | :--- | :--- |
| **`ProClubesApi`** | ASP.NET Core 10 Web API + EF Core + MySQL | Backend central, regras de negócio, autenticação JWT com rotação de Refresh Token, controle de acesso por unidade/papel, auditoria e rotinas automáticas em background. |
| **`ProClubesWeb`** | Blazor Web App (.NET 10 Interactive Server) | Painel administrativo e financeiro voltado à Diretoria (`SuperAdmin`, `Admin` de Unidade e `Tesoureiro`). |
| **`ProClubesApp`** | .NET MAUI 10 (Android, iOS, MacCatalyst, Windows) | Aplicativo móvel para candidatos, associados titulares e dependentes, com cache offline em SQLite (`sqlite-net-pcl`). |
| **`tests/ProClubes.UnitTests`** | xUnit | Testes unitários de regras financeiras, cálculo de encargos (multa e juros *pro rata die*), elegibilidade eleitoral e validações de domínio. |
| **`tests/ProClubes.IntegrationTests`** | xUnit + `WebApplicationFactory` | Testes de integração ponta a ponta cobrindo os 16 cenários prioritários da especificação v1.2. |

---

## Perfis de Acesso e Governança

### 1. Papéis Administrativos (Portal Web)
* **`SuperAdmin` (Escopo Global):**
  * Cadastra e gerencia as **Unidades** da rede (`ProClubes Foz`, `ProClubes Resort Foz`, `ProClubes Maringá`, etc.).
  * Designa e substitui **Admins** e **Tesoureiros** por unidade (garantindo a invariante de no máximo 1 `Admin` ativo por unidade).
  * Parametriza a **Configuração Financeira Global** (dias de geração, aviso e vencimento, percentuais de multa e juros mensais) e a tabela de **Valores de Mensalidade** por unidade e categoria.
  * Consulta a **Trilha de Auditoria** consolidada de todas as unidades.
* **`Admin` (Escopo da Unidade):**
  * Analisa solicitações de cadastro de novos associados (aprovação com definição de categoria, solicitação de correção ou rejeição fundamentada).
  * Gerencia o quadro de associados e dependentes da sua unidade.
  * Cria e administra **Eventos**, **Espaços Reserváveis** e **Reservas**.
  * Responde às manifestações da **Ouvidoria** e conduz **Votações Eletrônicas** (abertura, encerramento e apuração).
* **`Tesoureiro` (Escopo Financeiro da Unidade):**
  * Visualiza o painel financeiro da unidade, consulta mensalidades por competência/status e registra baixas manuais de pagamento.

### 2. Perfis Associativos (Aplicativo Mobile)
* **Visitante / Candidato (`RestritaCadastro`):**
  * Visualiza a vitrine pública de eventos futuros, envia solicitação de cadastro escolhendo a unidade desejada, acompanha o status da análise e reenvia dados quando solicitada correção.
* **Associado Titular (`Ativo`):**
  * Categorias suportadas:
    * **`FUNDADOR`**: Isento de mensalidade e com direito a voto nas assembleias da sua unidade.
    * **`ASSOCIADO`**: Contribuinte mensal e com direito a voto quando adimplente.
    * **`CONVIDADO`**: Contribuinte mensal, sem direito a voto.
    * **`SIMPATIZANTE`**: Isento de mensalidade, sem direito a voto.
  * Acesso à **Carteirinha Digital** (com QR Code de validação), **Mensalidades** (com detalhamento de encargos e pagamento no app), **Eventos** (confirmação de presença), **Reservas de Espaços** (solicitação e cancelamento), **Votações** e **Ouvidoria** (abertura de manifestações e consulta às respostas da Diretoria).
* **Dependente (`Ativo`):**
  * Possui acesso individual ao aplicativo para emissão de **Carteirinha Digital** vinculada ao titular, consulta/confirmação em **Eventos** e envio de manifestações na **Ouvidoria**.

---

## Principais Módulos e Regras de Negócio

1. **Segurança e Sessão (`api/v1/auth`):**
   * Senhas armazenadas com hash seguro (`PasswordHasher`), Access Token JWT de curta duração e Refresh Token opaco com rotação automática e detecção de reuso.
   * Incremento automático de `VersaoSeguranca` ao alterar senha, bloquear conta ou revogar papéis, invalidando sessões ativas imediatamente.
2. **Fluxo de Admissão e Quadro Social (`api/v1/cadastros`, `api/v1/associados`):**
   * Máquina de estados completa (`Pendente` → `AguardandoCorrecao` → `Pendente` → `Aprovada` / `Rejeitada`).
   * Aprovação exige escolha explícita da categoria pelo administrador da unidade.
3. **Motor Financeiro e Rotinas Automáticas (`api/v1/mensalidades`):**
   * Geração idempotente de mensalidades por competência (`YYYY-MM-01`) apenas para categorias pagantes (`ASSOCIADO` e `CONVIDADO`).
   * Transição automática de `Pendente` para `Vencida` após a data de vencimento (considerando o fuso `America/Sao_Paulo`).
   * Cálculo em tempo real de multa fixa sobre o valor original e juros de mora *pro rata die* ($\text{jurosDiario} = \text{jurosMensal} / 30$).
4. **Eventos, Espaços e Reservas (`api/v1/eventos`, `api/v1/espacos`, `api/v1/reservas`):**
   * Validação de conflito de horário no mesmo espaço, respeito à janela de funcionamento, duração máxima, antecedência máxima e bloqueio de reservas para associados com mensalidades vencidas.
5. **Governança Eletrônica (`api/v1/votacoes`):**
   * Sigilo absoluto do voto: a tabela de presença eleitoral (`EleitorVotacao`) é desacoplada das cédulas (`CedulaVotacao`), impedindo rastreamento da opção escolhida pelo associado e garantindo unicidade de voto.
   * Resultados parciais permanecem ocultos até o encerramento formal da votação.
6. **Ouvidoria (`api/v1/manifestacoes`):**
   * Registro de sugestões, elogios, reclamações e denúncias com histórico completo de respostas da Diretoria visível tanto no portal web quanto em aba dedicada no aplicativo mobile.
7. **Sincronização Offline no App (`api/v1/me/sincronizacao`):**
   * Persistência local em SQLite no dispositivo móvel para consulta offline de perfil, carteirinha, eventos, mensalidades, reservas e notificações quando a API estiver indisponível.

---

## Configuração e Execução Local

### Pré-requisitos
* **.NET 10 SDK** (com workloads `maui-windows` / `maui-android` instalados para o projeto mobile)
* **MySQL 8.0 / 8.4** em execução local (ou container Docker)

### 1. Configurar Credenciais de Desenvolvimento da API
O arquivo [`ProClubesApi/appsettings.json`](./ProClubesApi/appsettings.json) versionado no Git não contém senhas nem chaves secretas. Para rodar localmente:

1. Copie o arquivo de exemplo [`ProClubesApi/appsettings.Development.Example.json`](./ProClubesApi/appsettings.Development.Example.json) para `ProClubesApi/appsettings.Development.json` (este arquivo é ignorado pelo [`.gitignore`](./.gitignore)).
2. Preencha no seu `ProClubesApi/appsettings.Development.json` (ou via variáveis de ambiente `PROCLUBES_MYSQL_CONNECTION`, `PROCLUBES_JWT_SIGNING_KEY`, `PROCLUBES_SUPERADMIN_EMAIL`, `PROCLUBES_SUPERADMIN_PASSWORD`, `PROCLUBES_SUPERADMIN_CPF`):
   * `ConnectionStrings:DefaultConnection`: string de conexão do seu MySQL local.
   * `Jwt:SigningKey`: chave secreta com no mínimo 32 caracteres.
   * `BootstrapSuperAdmin`: e-mail, CPF e senha para o provisionamento automático do Super Admin inicial no primeiro boot.

### 2. Executar os Sistemas

* **API (`ProClubesApi` — porta padrão `http://localhost:5180`):**
  ```powershell
  dotnet run --project ProClubesApi/ProClubesApi.csproj
  ```
  *(Na inicialização, a API aplica automaticamente as migrations pendentes e executa o seed inicial das categorias, unidades e Super Admin configurado).*

* **Sistema Web (`ProClubesWeb` — porta padrão `http://localhost:5210`):**
  ```powershell
  dotnet run --project ProClubesWeb/ProClubesWeb.csproj
  ```

* **Aplicativo Mobile (`ProClubesApp` — Windows Desktop em formato smartphone `410x860`):**
  ```powershell
  dotnet build ProClubesApp/ProClubesApp.csproj -f net10.0-windows10.0.19041.0
  .\ProClubesApp\bin\Debug\net10.0-windows10.0.19041.0\win-x64\ProClubesApp.exe
  ```

---

## Executando os Testes Automatizados

Os testes de integração utilizam bancos SQLite isolados e efêmeros por execução, não exigindo instância do MySQL ativa para rodar:

```powershell
# Executar testes unitários
dotnet test tests/ProClubes.UnitTests/ProClubes.UnitTests.csproj

# Executar testes de integração (16 cenários ponta a ponta)
dotnet test tests/ProClubes.IntegrationTests/ProClubes.IntegrationTests.csproj
```
