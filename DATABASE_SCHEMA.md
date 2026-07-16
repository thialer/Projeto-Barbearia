# 📊 Documentação do Banco de Dados

## 1️⃣ **Tenant** (Barbearias)
Representa cada barbearia no sistema (suporte multi-tenant).

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único da barbearia |
| **Name** | String | Nome da barbearia (ex: "Barbearia XYZ") |
| **Slug** | String | URL-friendly (ex: "barbearia-xyz") - ÚNICO |
| **Phone** | String? | Telefone de contato |
| **Address** | String? | Endereço completo |
| **TimeZoneId** | String | Timezone (padrão: "America/Sao_Paulo") |
| **CancellationLimitMinutes** | Int | Prazo para cancelar agendamento (padrão: 120 min) |
| **IsActive** | Bool | Ativa ou desativada |
| **CreatedAtUtc** | DateTime | Quando foi criada |

---

## 2️⃣ **User** (Usuários)
Usuários do sistema (Admin, Barbeiro, Cliente).

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único |
| **TenantId** | GUID? | ID da barbearia (NULL para SuperAdmin) |
| **Name** | String | Nome completo |
| **Email** | String | Email - ÚNICO |
| **PasswordHash** | String | Senha criptografada (bcrypt) |
| **Role** | Enum | SuperAdmin, TenantAdmin, Barber, Customer |
| **MustChangePassword** | Bool | Se deve trocar senha no próximo login |
| **IsActive** | Bool | Ativa ou desativada |
| **CreatedAtUtc** | DateTime | Quando foi criada |

---

## 3️⃣ **Barber** (Barbeiros)
Barbeiros que atendem em uma barbearia.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único |
| **TenantId** | GUID | ID da barbearia |
| **UserId** | GUID | ID do usuário (referência) |
| **Bio** | String? | Biografia/descrição do barbeiro |
| **IsActive** | Bool | Ativo ou inativo |

---

## 4️⃣ **Service** (Serviços)
Serviços oferecidos pela barbearia (Corte, Barba, etc).

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único |
| **TenantId** | GUID | ID da barbearia |
| **Name** | String | Nome do serviço (ex: "Corte de Cabelo") |
| **Description** | String? | Descrição detalhada |
| **Price** | Decimal | Preço do serviço |
| **DurationMinutes** | Int | Duração em minutos |
| **IsActive** | Bool | Ativo ou inativo |

---

## 5️⃣ **BarberService** (Relação Barbeiro ↔ Serviço)
Define quais serviços cada barbeiro oferece.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **BarberId** | GUID | ID do barbeiro |
| **ServiceId** | GUID | ID do serviço |

**Exemplo:** Barbeiro João oferece "Corte" e "Barba", mas não oferece "Coloração"

---

## 6️⃣ **WorkingHour** (Horários de Trabalho)
Horários que cada barbeiro trabalha em cada dia da semana.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único |
| **BarberId** | GUID | ID do barbeiro |
| **DayOfWeek** | Enum | Dia da semana (Monday, Tuesday, etc) |
| **Start** | TimeOnly | Hora de início (ex: 08:00) |
| **End** | TimeOnly | Hora de término (ex: 18:00) |

**Exemplo:** João trabalha segunda a sexta das 08:00 às 18:00

---

## 7️⃣ **Appointment** (Agendamentos)
Agendamentos feitos pelos clientes.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| **Id** | GUID | Identificador único |
| **TenantId** | GUID | ID da barbearia |
| **CustomerId** | GUID | ID do cliente |
| **BarberId** | GUID | ID do barbeiro |
| **ServiceId** | GUID | ID do serviço |
| **StartAtUtc** | DateTime | Quando começa o agendamento |
| **EndAtUtc** | DateTime | Quando termina |
| **Status** | Enum | Confirmed, Cancelled, Completed, NoShow |
| **Notes** | String? | Observações do cliente |
| **CreatedAtUtc** | DateTime | Quando foi criado |

---

## 📈 Relacionamentos

```
Tenant (1) ──→ (N) User
		   ──→ (N) Barber
		   ──→ (N) Service
		   ──→ (N) Appointment

User (1) ──→ (N) Barber (via UserId)

Barber (1) ──→ (N) BarberService
		(1) ──→ (N) WorkingHour
		(1) ──→ (N) Appointment

Service (1) ──→ (N) BarberService
		 (1) ──→ (N) Appointment

BarberService é uma tabela de junção (N:N) entre Barber e Service
```

---

## 🔑 Chaves Primárias e Únicas

| Tabela | Chaves Únicas |
|--------|--------------|
| **Tenant** | Slug (único dentro do sistema) |
| **User** | Email (único no sistema) |
| **Barber** | Sem chave única adicional |
| **Service** | Sem chave única adicional |
| **BarberService** | BarberId + ServiceId (PK composta) |
| **WorkingHour** | Sem chave única adicional |
| **Appointment** | Sem chave única adicional |

---

## 💡 Exemplos de Dados

### Cenário: Cliente marca agendamento

```
1. TENANT criada: "Barbearia XYZ" (slug: "barbearia-xyz")
2. USER criada: "João Barbeiro" (role: Barber, tenantId: xyz)
3. BARBER criada: (userId: joão, tenantId: xyz)
4. SERVICE criada: "Corte de Cabelo" (price: 50.00, duration: 30min)
5. BARBERSERVICE criada: João oferece "Corte de Cabelo"
6. WORKINGHOUR criada: João trabalha Seg-Sex 08:00-18:00
7. USER criada: "Pedro Cliente" (role: Customer, tenantId: xyz)
8. APPOINTMENT criada: Pedro agendou com João para corte
```

---

**Pronto! Banco bem documentado e enxuto! 📊**
