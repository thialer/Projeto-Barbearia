# 📅 Rota PUT - Estabelecer Horários de Trabalho do Barbeiro

## 🎯 Endpoint

```
PUT /api/admin/barbers/{barberId}/working-hours
```

---

## 📋 Detalhes

| Propriedade | Valor |
|-------------|-------|
| **Método HTTP** | PUT |
| **URL** | `/api/admin/barbers/{barberId}/working-hours` |
| **Base URL** | `http://localhost:5000` (desenvolvimento) |
| **Autenticação** | Requerida (JWT Bearer Token) |
| **Role Permitido** | TenantAdmin |
| **Status Sucesso** | 204 No Content |
| **Taxa Limite** | Não tem limite específico |

---

## 🔐 Autenticação

Você **PRECISA** enviar o token JWT no header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Como obter o token:**
1. Fazer login em `POST /api/auth/login`
2. Guardar o `accessToken` da resposta
3. Enviar em todas as requisições autenticadas

---

## 📤 Request Body

```json
{
  "hours": [
	{
	  "dayOfWeek": 1,
	  "start": "09:00:00",
	  "end": "12:00:00"
	},
	{
	  "dayOfWeek": 1,
	  "start": "13:00:00",
	  "end": "18:00:00"
	},
	{
	  "dayOfWeek": 2,
	  "start": "09:00:00",
	  "end": "18:00:00"
	},
	{
	  "dayOfWeek": 3,
	  "start": "09:00:00",
	  "end": "18:00:00"
	},
	{
	  "dayOfWeek": 4,
	  "start": "09:00:00",
	  "end": "18:00:00"
	},
	{
	  "dayOfWeek": 5,
	  "start": "09:00:00",
	  "end": "18:00:00"
	},
	{
	  "dayOfWeek": 6,
	  "start": "09:00:00",
	  "end": "17:00:00"
	}
  ]
}
```

---

## 📖 Explicação dos Campos

### `dayOfWeek` - Dia da Semana

| Valor | Dia |
|-------|-----|
| **0** | Domingo |
| **1** | Segunda-feira |
| **2** | Terça-feira |
| **3** | Quarta-feira |
| **4** | Quinta-feira |
| **5** | Sexta-feira |
| **6** | Sábado |

**Exemplo:** `1` = Segunda-feira

### `start` - Hora de Início

- **Formato**: `HH:mm:ss` (ISO 8601 TimeOnly)
- **Exemplo**: `"09:00:00"` = 9 da manhã
- **Validação**: Deve ser válido e menor que `end`

### `end` - Hora de Término

- **Formato**: `HH:mm:ss` (ISO 8601 TimeOnly)
- **Exemplo**: `"18:00:00"` = 6 da tarde
- **Validação**: Deve ser maior que `start`

---

## ✅ Validações

A API valida automaticamente:

1. ✅ **DayOfWeek válido** - Deve ser 0-6
2. ✅ **Horário válido** - `end` > `start`
3. ✅ **Sem sobreposição** - Dois períodos no mesmo dia não podem se cruzar
4. ✅ **Barbeiro existe** - O ID deve ser um barbeiro válido
5. ✅ **Permissão** - Só admin da barbearia pode editar seus barbeiros

### ❌ Erros Possíveis

```json
// Horários inválidos ou sobrepostos
{
  "message": "Intervalos de trabalho inválidos ou sobrepostos."
}

// Barbeiro não encontrado
{
  "status": 404,
  "message": "Not Found"
}

// Sem autenticação
{
  "status": 401,
  "message": "Unauthorized"
}

// Não é admin desta barbearia
{
  "status": 403,
  "message": "Forbidden"
}
```

---

## 📝 Exemplos Completos

### Exemplo 1: Segunda a Sexta (Normal)

```bash
curl -X PUT http://localhost:5000/api/admin/barbers/550e8400-e29b-41d4-a716-446655440000/working-hours \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{
	"hours": [
	  { "dayOfWeek": 1, "start": "09:00:00", "end": "18:00:00" },
	  { "dayOfWeek": 2, "start": "09:00:00", "end": "18:00:00" },
	  { "dayOfWeek": 3, "start": "09:00:00", "end": "18:00:00" },
	  { "dayOfWeek": 4, "start": "09:00:00", "end": "18:00:00" },
	  { "dayOfWeek": 5, "start": "09:00:00", "end": "18:00:00" }
	]
  }'
```

### Exemplo 2: Com Intervalo de Almoço

```json
{
  "hours": [
	{ "dayOfWeek": 1, "start": "09:00:00", "end": "12:00:00" },
	{ "dayOfWeek": 1, "start": "13:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 2, "start": "09:00:00", "end": "12:00:00" },
	{ "dayOfWeek": 2, "start": "13:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 3, "start": "09:00:00", "end": "12:00:00" },
	{ "dayOfWeek": 3, "start": "13:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 4, "start": "09:00:00", "end": "12:00:00" },
	{ "dayOfWeek": 4, "start": "13:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 5, "start": "09:00:00", "end": "12:00:00" },
	{ "dayOfWeek": 5, "start": "13:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 6, "start": "09:00:00", "end": "12:00:00" }
  ]
}
```

### Exemplo 3: Sábado Reduzido

```json
{
  "hours": [
	{ "dayOfWeek": 1, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 2, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 3, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 4, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 5, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 6, "start": "10:00:00", "end": "14:00:00" }
  ]
}
```

### Exemplo 4: Apenas Quinta e Sexta

```json
{
  "hours": [
	{ "dayOfWeek": 4, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 5, "start": "09:00:00", "end": "18:00:00" }
  ]
}
```

---

## 🔄 Response

### ✅ Sucesso (204 No Content)

Quando funciona, **NÃO retorna nada**:

```
HTTP/1.1 204 No Content
```

Isso significa que os horários foram salvos com sucesso!

---

## 🧪 Testando no Postman/Insomnia

### Passo 1: Fazer Login

```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "admin@barbearia.com",
  "password": "Senha123!"
}
```

**Copie o `accessToken` da resposta.**

### Passo 2: Usar o Token

Ir para a aba **Headers** e adicionar:
```
Authorization: Bearer [COLE_O_TOKEN_AQUI]
Content-Type: application/json
```

### Passo 3: Fazer a Requisição

```http
PUT http://localhost:5000/api/admin/barbers/550e8400-e29b-41d4-a716-446655440000/working-hours
```

**Body (raw JSON):**
```json
{
  "hours": [
	{ "dayOfWeek": 1, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 2, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 3, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 4, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 5, "start": "09:00:00", "end": "18:00:00" },
	{ "dayOfWeek": 6, "start": "09:00:00", "end": "17:00:00" }
  ]
}
```

---

## 📚 Outras Rotas Relacionadas

### GET - Listar Barbeiros
```
GET /api/admin/barbers
```
Retorna todos os barbeiros com seus dados.

### POST - Criar Barbeiro
```
POST /api/admin/barbers
```
Cria um novo barbeiro.

### DELETE - Deletar Barbeiro
```
DELETE /api/admin/barbers/{barberId}
```
Desativa um barbeiro.

### PUT - Definir Serviços do Barbeiro
```
PUT /api/admin/barbers/{barberId}/services
```
Define quais serviços o barbeiro oferece.

---

## 💡 Dicas

1. **Sempre valide no frontend** antes de enviar para economizar requisições
2. **Use intervalo de almoço** se o barbeiro não trabalha durante o dia todo
3. **Sunday (0) raramente é usado**, maioria das barbearias fecha domingo
4. **Horários em UTC** - O sistema armazena em UTC mas converte para o timezone da barbearia
5. **Sem overlapping** - Não pode ter dois períodos sobrepostos no mesmo dia

---

## 🔗 Referência Rápida

| Campo | Tipo | Obrigatório | Exemplo |
|-------|------|-------------|---------|
| `dayOfWeek` | Integer (0-6) | ✅ | `1` |
| `start` | TimeOnly (HH:mm:ss) | ✅ | `"09:00:00"` |
| `end` | TimeOnly (HH:mm:ss) | ✅ | `"18:00:00"` |

---

## ⚙️ Como Isso Funciona Internamente

1. Frontend envia JSON com horários
2. Backend recebe e valida cada intervalo
3. Verifica se não há sobreposição
4. Remove **TODOS** os horários antigos do barbeiro
5. Insere os novos horários
6. Salva no banco de dados
7. Retorna 204 (sucesso silencioso)

Próxima vez que um cliente tenta agendar, o sistema só mostra slots que respeitam esses horários! 📅
