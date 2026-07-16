# BarberBooking.Api 💈

Sistema de gerenciamento de agendamentos para barbearias com suporte multi-tenant, autenticação JWT e API REST completa.

## 🚀 Quick Start

### 1. Requisitos
- **.NET 10 SDK**
- **PostgreSQL 12+** (ou Docker)

### 2. Configurar User Secrets (Uma Vez)

```bash
# Instalar ferramentas (primeira vez)
dotnet tool install -g dotnet-user-secrets

# Configurar 6 secrets
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=barber_booking;Username=postgres;Password=sua_senha"
dotnet user-secrets set "Jwt:Key" "sua-chave-jwt-super-longa-com-32-caracteres-aleatorios"
dotnet user-secrets set "Jwt:Issuer" "BarberBooking"
dotnet user-secrets set "Jwt:Audience" "BarberBooking.Client"
dotnet user-secrets set "Seed:SuperAdminEmail" "admin@plataforma.local"
dotnet user-secrets set "Seed:SuperAdminPassword" "SenhaSegura123!"

# Verificar
dotnet user-secrets list
```

### 3. Preparar Banco de Dados

```bash
dotnet restore
dotnet ef database update
```

### 4. Executar

```bash
dotnet run
```

Acesse: **http://localhost:5000/swagger**

---

## 📖 Instalação Detalhada

### PostgreSQL

**Windows/macOS/Linux:**
```bash
# Docker (recomendado)
docker run --name barber-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=sua_senha -e POSTGRES_DB=barber_booking -p 5432:5432 -d postgres:15
```

**Sem Docker:**
- Windows: https://www.postgresql.org/download/windows/
- macOS: `brew install postgresql@15 && brew services start postgresql@15`
- Linux: `sudo apt-get install postgresql`

---

## 🔐 Segurança

### Credenciais Seguras

✅ **Desenvolvimento:** User Secrets (`dotnet user-secrets`)
✅ **Produção:** Variáveis de ambiente

**Nunca commitar:**
- `appsettings.Development.json`
- `.env` files
- Connection strings reais

---

## 📚 Endpoints

Swagger em execução: `https://localhost:5000/swagger`

### Principais Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/login` | Fazer login |
| POST | `/api/super-admin/tenants` | Criar barbearia |
| POST | `/api/admin/services` | Adicionar serviço |
| POST | `/api/admin/barbers` | Registrar barbeiro |
| POST | `/api/public/{slug}/appointments` | Criar agendamento |

---

## 🐛 Troubleshooting

| Erro | Solução |
|------|---------|
| Connection refused | Verifique se PostgreSQL está rodando |
| JWT:Key não configurado | `dotnet user-secrets set "Jwt:Key" "..."`  |
| Port já em uso | Mude a porta em `Properties/launchSettings.json` |
| Migration failed | `dotnet ef database drop` depois `update` |

---

## 📁 Estrutura

```
BarberBooking.Api/
├── Contracts/          # DTOs
├── Domain/             # Entidades
├── Infrastructure/     # DbContext
├── Services/           # Lógica de negócio
├── Migrations/         # Entity Framework
├── Program.cs          # Configuração
├── appsettings.json    # Config (sem credenciais)
└── .gitignore          # Git seguro
```

---

## 🌍 Produção

Configure variáveis de ambiente:
```bash
export ConnectionStrings__Postgres="prod_connection"
export Jwt__Key="prod_jwt_secret"
export ASPNETCORE_ENVIRONMENT="Production"
```

---

## 📝 Primeira Execução

1. **Fazer login** no Swagger com:
   - Email: `admin@plataforma.local`
   - Senha: `SenhaSegura123!`

2. **Criar barbearia:**
   ```json
   POST /api/super-admin/tenants
   {
	 "name": "Minha Barbearia",
	 "slug": "minha-barbearia",
	 "adminName": "João Silva",
	 "adminEmail": "joao@barbearia.com"
   }
   ```

3. **Adicionar serviço:**
   ```json
   POST /api/admin/services
   {
	 "name": "Corte de Cabelo",
	 "price": 50.00,
	 "durationMinutes": 30
   }
   ```

---

## 📄 Licença

MIT License

---

**🚀 Pronto para começar!**
