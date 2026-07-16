# Documentação de Rotas - BarberBooking.Api

## Visão Geral
A API BarberBooking foi estruturada com grupos de rotas organizadas por função (roles):
- **Auth**: Autenticação
- **SuperAdmin**: Super Administrador
- **Admin**: Administrador
- **PublicApi**: Acesso Público
- **Customer**: Cliente

---

## 1. ROTAS SWAGGER (Documentação)

### **GET /**
- **Descrição**: Redireciona a raiz da aplicação para o Swagger
- **Acesso**: Público
- **Resposta**: HTTP 301 (Redirect) → `/swagger/index.html`
- **Uso**: Acessar a documentação interativa da API

### **GET /swagger/v1/swagger.json**
- **Descrição**: Fornece o JSON da especificação OpenAPI
- **Acesso**: Público
- **Resposta**: Arquivo JSON com definição de todas as rotas
- **Uso**: Ferramentas como Postman, clientes HTTP, etc.

### **GET /swagger/index.html**
- **Descrição**: Interface web do Swagger
- **Acesso**: Público
- **Resposta**: HTML da UI Swagger
- **Uso**: Visualizar e testar rotas via interface gráfica

---

## 2. ROTAS DE AUTENTICAÇÃO (`/auth`)

**Base URL**: `https://localhost:56186/auth` ou `http://localhost:56187/auth`

### **POST /auth/register**
- **Descrição**: Registra um novo usuário no sistema
- **Acesso**: Público (sem autenticação)
- **Autenticação**: Não requerida
- **Corpo da Requisição**:
```json
{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "SenhaSegura123!"
}
```
- **Respostas**:
  - `201 Created`: Usuário registrado com sucesso
  - `400 Bad Request`: Validação falhou (email duplicado, senha fraca, etc.)
- **Efeitos Colaterais**:
  - Cria novo registro na tabela `Users`
  - Hash da senha armazenado (nunca a senha em texto plano)
  - Usuário criado com role `Customer` por padrão

### **POST /auth/login**
- **Descrição**: Autentica o usuário e retorna JWT token
- **Acesso**: Público (sem autenticação)
- **Autenticação**: Não requerida
- **Corpo da Requisição**:
```json
{
  "email": "joao@example.com",
  "password": "SenhaSegura123!"
}
```
- **Respostas**:
  - `200 OK`: Login bem-sucedido, retorna token JWT
  - `401 Unauthorized`: Credenciais inválidas
- **Retorno Sucesso**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```
- **Efeitos Colaterais**:
  - Valida credenciais contra tabela `Users`
  - Gera token JWT assinado (válido por 1 hora)
  - Token contém claims de identificação do usuário

---

## 3. ROTAS DE SUPER ADMINISTRADOR (`/super-admin`)

**Base URL**: `https://localhost:56186/super-admin`

**Autenticação**: ✅ Requerida (JWT Token válido)
**Autorização**: 🔒 Apenas usuários com role `SuperAdmin`

### **POST /super-admin/users**
- **Descrição**: Cria um novo usuário (Super Admin)
- **Headers**:
```
Authorization: Bearer {JWT_TOKEN}
```
- **Corpo da Requisição**:
```json
{
  "name": "Admin Sistema",
  "email": "admin@barber.com",
  "password": "SenhaForte123!",
  "role": "Admin"
}
```
- **Respostas**:
  - `201 Created`: Usuário criado
  - `403 Forbidden`: Sem permissão de Super Admin
  - `401 Unauthorized`: Token inválido/expirado
- **Efeitos**:
  - Novo usuário criado com a role especificada
  - Super Admin pode criar qualquer tipo de usuário

### **PUT /super-admin/users/{userId}**
- **Descrição**: Edita informações de qualquer usuário
- **Parâmetros URL**:
  - `{userId}`: ID do usuário (UUID)
- **Corpo da Requisição**:
```json
{
  "name": "Novo Nome",
  "email": "novo@email.com",
  "role": "Admin"
}
```
- **Respostas**:
  - `200 OK`: Usuário atualizado
  - `403 Forbidden`: Sem permissão
  - `404 Not Found`: Usuário não existe

### **DELETE /super-admin/users/{userId}**
- **Descrição**: Remove um usuário do sistema
- **Respostas**:
  - `204 No Content`: Usuário deletado
  - `403 Forbidden`: Sem permissão
- **Efeitos**:
  - Usuário marcado como inativo ou deletado
  - Pode afetar agendamentos relacionados

### **GET /super-admin/users**
- **Descrição**: Lista todos os usuários do sistema
- **Query Parameters**:
  - `page`: Número da página (padrão: 1)
  - `pageSize`: Quantidade por página (padrão: 10)
  - `role`: Filtrar por role (SuperAdmin, Admin, Customer, Barber)
- **Respostas**:
  - `200 OK`: Lista de usuários com paginação
```json
{
  "items": [
	{
	  "id": "uuid",
	  "name": "João Silva",
	  "email": "joao@example.com",
	  "role": "Customer",
	  "isActive": true,
	  "createdAtUtc": "2025-01-15T10:30:00Z"
	}
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 10
}
```

---

## 4. ROTAS DE ADMINISTRADOR (`/admin`)

**Base URL**: `https://localhost:56186/admin`

**Autenticação**: ✅ Requerida (JWT Token válido)
**Autorização**: 🔒 Apenas usuários com role `Admin` (ou superior)

### **POST /admin/barbers**
- **Descrição**: Cria novo barbeiro no sistema
- **Corpo da Requisição**:
```json
{
  "name": "Carlos Barbeiro",
  "email": "carlos@barber.com",
  "specialties": ["Corte Masculino", "Barba", "Degradê"]
}
```
- **Respostas**:
  - `201 Created`: Barbeiro criado
  - `400 Bad Request`: Dados inválidos

### **PUT /admin/barbers/{barberId}**
- **Descrição**: Edita informações do barbeiro
- **Efeitos**:
  - Atualiza nome, especialidades, disponibilidade

### **DELETE /admin/barbers/{barberId}**
- **Descrição**: Remove barbeiro do sistema
- **Efeitos**:
  - Barbeiro marcado como inativo
  - Agendamentos futuros podem ser cancelados

### **GET /admin/barbers**
- **Descrição**: Lista todos os barbeiros
- **Respostas**:
  - `200 OK`: Lista de barbeiros
```json
{
  "items": [
	{
	  "id": "uuid",
	  "name": "Carlos",
	  "email": "carlos@barber.com",
	  "specialties": ["Corte", "Barba"],
	  "averageRating": 4.8,
	  "totalReviews": 45
	}
  ]
}
```

### **POST /admin/schedules**
- **Descrição**: Configura horários de atendimento para os barbeiros
- **Corpo**:
```json
{
  "barberId": "uuid",
  "dayOfWeek": "Monday",
  "startTime": "08:00",
  "endTime": "18:00",
  "breakStartTime": "12:00",
  "breakEndTime": "13:00"
}
```
- **Efeitos**:
  - Define disponibilidade de um barbeiro
  - Afeta quais horários aparecem para agendamento

### **GET /admin/reports/revenue**
- **Descrição**: Relatório de receita
- **Query Parameters**:
  - `startDate`: Data inicial (YYYY-MM-DD)
  - `endDate`: Data final (YYYY-MM-DD)
- **Respostas**:
  - `200 OK`: Dados de receita no período

### **GET /admin/reports/bookings**
- **Descrição**: Relatório de agendamentos
- **Respostas**:
  - `200 OK`: Estatísticas de agendamentos

---

## 5. ROTAS PÚBLICAS (`/public`)

**Base URL**: `https://localhost:56186/api/public` ou `http://localhost:56187/api/public`

**Autenticação**: ❌ Não requerida
**Autorização**: Acesso aberto

### **GET /api/public/barbers**
- **Descrição**: Lista barbeiros disponíveis (para clientes não autenticados)
- **Query Parameters**:
  - `specialty`: Filtrar por especialidade
  - `rating`: Filtrar por avaliação mínima
- **Respostas**:
  - `200 OK`: Lista de barbeiros públicos
```json
{
  "items": [
	{
	  "id": "uuid",
	  "name": "Carlos",
	  "specialties": ["Corte", "Barba"],
	  "averageRating": 4.8,
	  "imageUrl": "https://..."
	}
  ]
}
```

### **GET /api/public/barbers/{barberId}/availability**
- **Descrição**: Consulta disponibilidade de um barbeiro
- **Query Parameters**:
  - `date`: Data desejada (YYYY-MM-DD)
- **Respostas**:
  - `200 OK`: Horários disponíveis
```json
{
  "date": "2025-01-20",
  "availableSlots": [
	{
	  "startTime": "08:00",
	  "endTime": "08:30",
	  "isAvailable": true
	},
	{
	  "startTime": "08:30",
	  "endTime": "09:00",
	  "isAvailable": false
	}
  ]
}
```

### **GET /api/public/services**
- **Descrição**: Lista serviços disponíveis
- **Respostas**:
  - `200 OK`: Todos os serviços com preços
```json
{
  "items": [
	{
	  "id": "uuid",
	  "name": "Corte Simples",
	  "description": "Corte de cabelo básico",
	  "durationMinutes": 30,
	  "price": 50.00
	}
  ]
}
```

---

## 6. ROTAS DE CLIENTE (`/customers`)

**Base URL**: `https://localhost:56186/api/customers`

**Autenticação**: ✅ Requerida (JWT Token válido)
**Autorização**: 🔒 Apenas clientes autenticados (role `Customer`)

### **POST /api/customers/bookings**
- **Descrição**: Cria um novo agendamento
- **Corpo da Requisição**:
```json
{
  "barberId": "uuid",
  "serviceId": "uuid",
  "desiredDateTime": "2025-01-20T14:00:00Z",
  "notes": "Prefiro degradê suave"
}
```
- **Respostas**:
  - `201 Created`: Agendamento criado
  - `400 Bad Request`: Horário não disponível
  - `409 Conflict`: Conflito de agendamento
- **Retorno**:
```json
{
  "id": "uuid",
  "barberId": "uuid",
  "customerId": "uuid",
  "serviceId": "uuid",
  "scheduledTime": "2025-01-20T14:00:00Z",
  "status": "Confirmed",
  "bookingNumber": "BK-2025-00123"
}
```
- **Efeitos**:
  - Novo agendamento criado na tabela `Bookings`
  - Status inicial: "Confirmed"
  - Horário fica indisponível para outros clientes

### **GET /api/customers/bookings**
- **Descrição**: Lista agendamentos do cliente autenticado
- **Query Parameters**:
  - `status`: Filtrar por status (Confirmed, Cancelled, Completed)
  - `fromDate`: Data inicial
  - `toDate`: Data final
- **Respostas**:
  - `200 OK`: Lista de agendamentos do cliente
```json
{
  "items": [
	{
	  "id": "uuid",
	  "barberName": "Carlos",
	  "serviceName": "Corte + Barba",
	  "scheduledTime": "2025-01-20T14:00:00Z",
	  "status": "Confirmed",
	  "price": 85.00
	}
  ],
  "totalCount": 5
}
```

### **GET /api/customers/bookings/{bookingId}**
- **Descrição**: Detalhes de um agendamento específico
- **Respostas**:
  - `200 OK`: Dados completos do agendamento
  - `404 Not Found`: Agendamento não encontrado
  - `403 Forbidden`: Acesso denegado (agendamento de outro cliente)

### **PUT /api/customers/bookings/{bookingId}**
- **Descrição**: Reschedula um agendamento existente
- **Corpo**:
```json
{
  "newDateTime": "2025-01-21T15:00:00Z"
}
```
- **Respostas**:
  - `200 OK`: Agendamento atualizado
  - `400 Bad Request`: Novo horário não disponível
  - `403 Forbidden`: Não pode alterar agendamento de outro cliente
- **Efeitos**:
  - Libera slot anterior
  - Reserva novo slot
  - Pode enviar notificação para barbeiro

### **DELETE /api/customers/bookings/{bookingId}**
- **Descrição**: Cancela um agendamento
- **Query Parameters**:
  - `reason`: Motivo do cancelamento (opcional)
- **Respostas**:
  - `204 No Content`: Cancelado com sucesso
  - `403 Forbidden`: Não pode cancelar agendamento de outro cliente
  - `400 Bad Request`: Agendamento é hoje (não pode cancelar)
- **Efeitos**:
  - Status alterado para "Cancelled"
  - Horário fica novamente disponível
  - Cliente pode receber reembolso (se aplicável)

### **GET /api/customers/profile**
- **Descrição**: Retorna perfil do cliente autenticado
- **Respostas**:
  - `200 OK`: Dados do cliente
```json
{
  "id": "uuid",
  "name": "João Silva",
  "email": "joao@example.com",
  "phone": "(11) 99999-9999",
  "totalBookings": 5,
  "totalSpent": 425.00,
  "memberSince": "2024-11-15T10:30:00Z"
}
```

### **PUT /api/customers/profile**
- **Descrição**: Atualiza perfil do cliente
- **Corpo**:
```json
{
  "name": "João Silva",
  "phone": "(11) 99999-9999"
}
```
- **Respostas**:
  - `200 OK`: Perfil atualizado
  - `400 Bad Request`: Validação falhou

### **POST /api/customers/reviews**
- **Descrição**: Deixa avaliação após agendamento
- **Corpo**:
```json
{
  "bookingId": "uuid",
  "rating": 5,
  "comment": "Ótimo corte! Voltarei com certeza."
}
```
- **Respostas**:
  - `201 Created`: Avaliação criada
  - `400 Bad Request`: Agendamento não encontrado ou já avaliado
- **Efeitos**:
  - Cria registro em tabela `Reviews`
  - Afeta rating médio do barbeiro

---

## 7. FLUXO DE DADOS E RELACIONAMENTOS

```
┌─────────────┐
│    Users    │ (Todas as contas)
│ ─────────── │
│ ID          │
│ Email       │
│ Name        │
│ Role        │ ──────┬──────┬──────┬────────┐
│ Password    │       │      │      │        │
└─────────────┘       │      │      │        │
					  │      │      │        │
			┌─────────▼─┐    │      │        │
			│ SuperAdmin│    │      │        │
			└───────────┘    │      │        │
					  ┌──────▼────┐ │        │
					  │   Admin   │ │        │
					  └───────────┘ │        │
					  ┌────────────┐│        │
					  │  Barber    ││        │
					  └────────────┘│        │
						   ┌────────▼──────┐
						   │   Customer    │
						   └───────────────┘
								  │
								  │
					┌─────────────────────────┐
					│      Bookings           │
					│ ──────────────────────  │
					│ ID                      │
					│ CustomerId ─────────────┼──► Customers
					│ BarberId ───────────────┼──► Users (Barber)
					│ ServiceId ──────────────┼──► Services
					│ ScheduledTime           │
					│ Status                  │
					└─────────────────────────┘
							 │
							 │
					┌────────▼─────────┐
					│     Reviews      │
					│ ────────────────  │
					│ ID               │
					│ BookingId ───────┼──► Bookings
					│ Rating           │
					│ Comment          │
					└──────────────────┘
```

---

## 8. SEGURANÇA E AUTENTICAÇÃO

### **Tipos de Autenticação**

1. **Rotas Públicas** (sem `/auth`):
   - Nenhuma autenticação requerida
   - Qualquer pessoa pode acessar

2. **Rotas Protegidas**:
   - Header obrigatório: `Authorization: Bearer {JWT_TOKEN}`
   - Token obtido via `POST /auth/login`
   - Token válido por **1 hora**

### **Fluxo de Autenticação**

```
1. Cliente POST /auth/login
   └─> Servidor valida credenciais
	   └─> Se válido: Gera JWT Token
	   └─> Se inválido: Retorna 401 Unauthorized

2. Cliente usa token em requisições protegidas
   └─> Header: Authorization: Bearer {token}
   └─> Servidor valida assinatura do token
	   └─> Se válido: Processa requisição
	   └─> Se expirado: Retorna 401 Unauthorized
	   └─> Se sem permissão: Retorna 403 Forbidden

3. Cliente faz logout
   └─> Token descartado (sem sessão no servidor)
```

### **Erros de Autenticação/Autorização**

| Código | Significado | Ação |
|--------|-------------|------|
| `401 Unauthorized` | Token ausente, inválido ou expirado | Fazer login novamente |
| `403 Forbidden` | Autenticado mas sem permissão | Contatar administrador |
| `400 Bad Request` | Dados de requisição inválidos | Verificar formato dos dados |

---

## 9. BOAS PRÁTICAS DE CONSUMO

### ✅ Sempre fazer isso:
1. Armazenar JWT token com segurança (não em localStorage se possível)
2. Incluir token em todas as requisições protegidas
3. Verificar status HTTP da resposta
4. Implementar retry com backoff exponencial
5. Validar dados antes de enviar

### ❌ Nunca fazer isso:
1. Expor senha do usuário
2. Armazenar senha em cache
3. Fazer requisições síncronas (bloquear UI)
4. Ignorar erros HTTP
5. Compartilhar tokens entre dispositivos

---

## 10. EXEMPLOS DE REQUISIÇÕES COM CURL

### Login e Obter Token
```bash
curl -X POST http://localhost:56187/auth/login \
  -H "Content-Type: application/json" \
  -d '{
	"email": "joao@example.com",
	"password": "SenhaSegura123!"
  }'
```

### Criar Agendamento (com autenticação)
```bash
curl -X POST http://localhost:56187/api/customers/bookings \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
	"barberId": "550e8400-e29b-41d4-a716-446655440000",
	"serviceId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
	"desiredDateTime": "2025-01-20T14:00:00Z",
	"notes": "Degradê suave"
  }'
```

### Listar Barbeiros (sem autenticação)
```bash
curl http://localhost:56187/api/public/barbers
```

---

## 11. VARIÁVEIS DE AMBIENTE ESSENCIAIS

```
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=barber_booking;Username=postgres;Password=1234
Jwt__Issuer=BarberBooking
Jwt__Audience=BarberBooking.Client
Jwt__Key=YOUR_JWT_SECRET_KEY_HERE_MUST_BE_LONG_AND_SECURE
```

---

## 12. PRÓXIMOS PASSOS

1. **Implementar Refresh Tokens** para renovar JWT sem fazer login
2. **Adicionar Paginação** a todas as listagens
3. **Implementar Soft Deletes** para dados históricos
4. **Rate Limiting** para proteger contra abuso
5. **Logging Detalhado** para auditorias
6. **Validação de Email** com confirmação
7. **SMS/Push Notifications** para agendamentos
8. **Payment Integration** para cobranças online

---

**Última Atualização**: 15 de Janeiro de 2025
**Versão da API**: 1.0
**Ambiente**: Development
