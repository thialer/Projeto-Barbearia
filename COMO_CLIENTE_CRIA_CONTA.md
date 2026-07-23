# 🔐 Como o Cliente Cria Conta

## 📍 Localização

O cliente pode criar conta em duas ocasiões:

1. **Na aba de Login/Registro** (antes de agendar)
   - URL: `http://localhost:3000/barbearias/[slug]`
   - Clica em "Entrar / Criar conta"

2. **Durante o Passo 4 do agendamento** (se não estiver logado)
   - Após selecionar serviço, barbeiro, data e horário
   - O sistema pede para criar conta antes de confirmar

## 📂 Componentes Envolvidos

### Frontend
```
barber-frontend/barber-frontend/
├── app/barbearias/[slug]/
│   ├── page.tsx              ← Página principal
│   ├── AuthPanel.tsx         ← ⭐ COMPONENTE DE LOGIN/REGISTRO
│   └── BookingWizard.tsx     ← Usa AuthPanel no Step 4
├── lib/
│   ├── api.ts                ← Cliente API
│   └── auth-context.tsx      ← Gerencia autenticação
```

### Backend
```
Program.cs
├── POST /api/auth/login      ← Faz login (retorna JWT)
├── POST /api/public/{slug}/register  ← ⭐ CRIA CONTA DO CLIENTE
```

---

## 🎨 Interface (AuthPanel.tsx)

### Visual:
```
┌─────────────────────────────────┐
│ [Criar conta]  [Já tenho conta] │  ← Abas para alternar
├─────────────────────────────────┤
│ Nome:     [_______________]      │  (só aparece em "Criar conta")
│ E-mail:   [_______________]      │
│ Senha:    [_______________]      │  (mínimo 8 caracteres em "Criar")
│                                   │
│ [Criar conta e continuar] 🔘     │
└─────────────────────────────────┘
```

### Código:
```typescript
export function AuthPanel({ tenant, onDone }: { tenant: Tenant; onDone?: () => void }) {
  const [mode, setMode] = useState<"login" | "register">("register");  // Inicia em "register"
  const [form, setForm] = useState({ name: "", email: "", password: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
	e.preventDefault();
	setError(null);
	setLoading(true);
	try {
	  if (mode === "register") {
		// 1️⃣ Chamar API de registro
		const res = await api.public.register(tenant.slug, form);

		// 2️⃣ Armazenar token JWT
		setSession(res.accessToken, {
		  id: "",
		  name: form.name,
		  email: form.email,
		  role: "Customer",
		  tenantId: null,
		  mustChangePassword: false,
		});

		// 3️⃣ Fazer login para obter dados completos (id, tenantId)
		await login(form.email, form.password);
	  }
	  onDone?.();  // Callback quando conseguir criar conta
	} catch (err) {
	  setError(err instanceof ApiError ? err.message : "Erro.");
	} finally {
	  setLoading(false);
	}
  }

  return (
	<div>
	  {/* Abas */}
	  <div className="flex gap-1">
		<button onClick={() => setMode("register")}>Criar conta</button>
		<button onClick={() => setMode("login")}>Já tenho conta</button>
	  </div>

	  {/* Formulário */}
	  <form onSubmit={handleSubmit}>
		{error && <Alert>{error}</Alert>}

		{/* Campo Nome - só aparece em "Criar conta" */}
		{mode === "register" && (
		  <div>
			<Label>Nome</Label>
			<Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
		  </div>
		)}

		<div>
		  <Label>E-mail</Label>
		  <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
		</div>

		<div>
		  <Label>Senha</Label>
		  <Input 
			type="password" 
			minLength={mode === "register" ? 8 : undefined}
			value={form.password} 
			onChange={(e) => setForm({ ...form, password: e.target.value })} 
		  />
		</div>

		<Button type="submit" disabled={loading}>
		  {loading ? "Aguarde..." : mode === "register" ? "Criar conta e continuar" : "Entrar"}
		</Button>
	  </form>
	</div>
  );
}
```

---

## 🔄 Fluxo de Registro Passo a Passo

```
1. Cliente clica "Entrar / Criar conta"
					↓
2. AuthPanel abre em modo "Criar conta"
					↓
3. Cliente preenche:
   - Nome: "João Silva"
   - E-mail: "joao@example.com"
   - Senha: "Senha1234"     (min 8 caracteres)
					↓
4. Clica "Criar conta e continuar"
					↓
5. Frontend chama: api.public.register(slug, { name, email, password })
					↓
6. Backend executa:
   ├─ ✅ Valida e-mail (formato válido)
   ├─ ✅ Valida senha (mínimo 8 caracteres)
   ├─ ✅ Verifica se e-mail já existe
   ├─ ✅ Cria novo usuário:
   │   - Role: "Customer"
   │   - TenantId: ID da barbearia
   │   - Email: normalizado (lowercase)
   │   - PasswordHash: hash seguro (bcrypt)
   │   - IsActive: true
   │   - MustChangePassword: false
   └─ ✅ Retorna JWT accessToken
					↓
7. Frontend armazena token no localStorage
					↓
8. Frontend faz login automático para pegar dados completos
					↓
9. Cliente agora está AUTENTICADO ✅
   - Pode agendar horários
   - Pode ver seus agendamentos
   - Pode gerenciar sua conta
```

---

## 🌐 API - POST /api/public/{slug}/register

### Endpoint
```
POST http://localhost:5000/api/public/barbearia-xyz/register
Content-Type: application/json
```

### Request
```json
{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "Senha1234"
}
```

### Validações Backend
✅ **E-mail válido**: Deve ter formato de e-mail (contém @)
✅ **Senha forte**: Mínimo 8 caracteres
✅ **E-mail único**: Não pode ter outro usuário com esse e-mail
✅ **Barbearia existe**: O slug deve ser uma barbearia ativa

### Response (Sucesso)
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Response (Erro)
```json
// E-mail inválido
{
  "message": "E-mail inválido."
}

// Senha muito curta
{
  "message": "A senha deve ter pelo menos 8 caracteres."
}

// E-mail já cadastrado
{
  "message": "E-mail já cadastrado."
}

// Barbearia não encontrada
{
  "message": "Not Found"
}
```

---

## 💾 Banco de Dados - O que é criado

Quando o cliente se registra, é criado um novo registro em `Users`:

```csharp
var customer = new User 
{
	Id = Guid.NewGuid(),                           // ID único
	TenantId = tenant.Id,                          // ID da barbearia
	Name = "João Silva",                           // Nome do cliente
	Email = "joao@example.com",                    // E-mail (lowercase)
	PasswordHash = "2b$12$...",                    // Hash seguro da senha
	Role = UserRole.Customer,                      // Tipo: Cliente
	MustChangePassword = false,                    // Não precisa mudar senha
	IsActive = true,                               // Conta ativa
	CreatedAtUtc = DateTime.UtcNow                 // Quando foi criada
};
```

### Campo de Interesse
- **TenantId**: Liga o cliente à barbearia específica
- **Email**: Identificador único (pode ter mesmo nome em barbearias diferentes)
- **PasswordHash**: Nunca armazena senha em texto plano!
- **Role**: "Customer" (diferente de "Barber", "TenantAdmin", "SuperAdmin")

---

## 🔑 JWT Token

Após criar conta, o cliente recebe um **JWT (JSON Web Token)** que:

1. ✅ Identifica o cliente
2. ✅ Tem data de expiração
3. ✅ É armazenado no `localStorage`
4. ✅ É enviado em todas as requisições subsequentes

### Como é usado:
```typescript
// Frontend armazena
localStorage.setItem("token", accessToken);

// Frontend usa em próximas requisições
const headers = {
  "Authorization": `Bearer ${accessToken}`  // ← Enviado naquela aqui
};

// Assim consegue chamar endpoints autenticados:
- GET /api/public/{slug}/my-appointments
- POST /api/public/{slug}/appointments
- POST /api/public/{slug}/appointments/{id}/cancel
```

---

## 📱 Fluxo Completo: Novo Cliente Agendando

```
1. Cliente acessa: http://localhost:3000/barbearias/meu-barbeiro
					↓
2. Vê a aba "Agendar horário"
					↓
3. Seleciona: Serviço → Barbeiro → Data/Hora
					↓
4. Chega no PASSO 4 (Confirmação)
					↓
5. Sistema detecta que não está autenticado
					↓
6. Mostra: "Entre ou crie sua conta para confirmar"
		   [AuthPanel aparece]
					↓
7. Cliente escolhe "Criar conta"
					↓
8. Preenche: Nome, E-mail, Senha
					↓
9. Clica "Criar conta e continuar"
					↓
10. API cria conta + retorna JWT
					↓
11. Frontend faz login automático
					↓
12. Cliente volta para o PASSO 4 já autenticado
					↓
13. Pode adicionar observações
					↓
14. Clica "Confirmar agendamento"
					↓
15. ✅ Agendamento criado!
```

---

## ⚠️ Casos de Erro Comuns

| Erro | Causa | Solução |
|------|-------|---------|
| "E-mail inválido" | Não tem @ ou tem formato errado | Verificar formato do e-mail |
| "E-mail já cadastrado" | Cliente tentando registrar com e-mail que já existe | Usar "Já tenho conta" ou recuperar senha |
| "A senha deve ter pelo menos 8 caracteres" | Senha muito fraca | Digitar senha com mínimo 8 caracteres |
| "Not Found" | Barbearia com esse slug não existe | Verificar URL |
| Erro de CORS | Frontend em porta diferente do backend | Verificar CORS configurado no backend |

---

## 🔐 Segurança

### ✅ O que é feito certo:
- Senha com hash (nunca em texto plano)
- JWT com expiração
- E-mail validado
- Validações no backend (não só frontend)

### 🚀 Melhorias possíveis:
- Confirmação de e-mail antes de ativar conta
- Recuperação de senha
- 2FA (autenticação de dois fatores)
- Limitar tentativas de login
- Audit log de criação de conta

---

## 📝 Resumo

**O cliente cria conta:**
1. Preenchendo formulário no `AuthPanel.tsx`
2. Enviando para `POST /api/public/{slug}/register`
3. Recebendo um JWT em troca
4. Ficando autenticado e podendo agendar

**Arquivo principal:** `AuthPanel.tsx`
**Localização:** `barber-frontend/barber-frontend/app/barbearias/[slug]/AuthPanel.tsx`
