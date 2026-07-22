# BarberBooking — Frontend (Next.js)

Frontend completo integrado com a API `BarberBooking.Api`, com 3 áreas:

- `/login` + `/super-admin` — Super Admin (cadastra barbearias)
- `/login` + `/admin` — Admin da barbearia (serviços, barbeiros, agenda, configurações)
- `/barbearias/[slug]` — Site público (cliente agenda, cancela, remarca)

## 1. Rodar o projeto

```bash
npm install
cp .env.local.example .env.local
npm run dev
```

Abra http://localhost:3000

## 2. ⚠️ Passo obrigatório no backend: configurar CORS

Sem isso, o navegador vai bloquear todas as chamadas do Next.js para sua API .NET.
Adicione isto no `Program.cs`, **antes** de `var app = builder.Build();`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

E logo após `var app = builder.Build();`, antes de `app.UseHttpsRedirection();`:

```csharp
app.UseCors("Frontend");
```

## 3. Ajuste a URL da API

Edite `.env.local`:

```
NEXT_PUBLIC_API_URL=http://localhost:56187
```

Use a porta **HTTP** (não HTTPS) para evitar o erro de certificado de desenvolvimento não confiável.
Se preferir HTTPS, rode antes: `dotnet dev-certs https --trust`.

## 4. Limitações conhecidas do backend atual

Percebi 3 lacunas nas rotas que valem a pena resolver:

1. **Não existe `GET /api/admin/barbers`** — só dá pra criar barbeiro, não listar os já cadastrados.
   Por isso, a página `/admin/barbers` só mostra barbeiros criados na sessão atual do navegador.
2. **Não existe `GET /api/admin/settings`** — o formulário de configurações começa em branco, já que
   não há como carregar os dados atuais da barbearia antes de editar.
3. **Formato de `/api/public/{slug}/availability` incerto** — não tive acesso ao `BookingService.cs`,
   então não sei se a lista de horários retornada vem como texto ISO completo (`2026-07-23T09:00:00Z`)
   ou só a hora. Fiz a integração assumindo ISO completo. Se os horários aparecerem em branco ou com
   erro na tela de agendamento, me manda o `BookingService.cs` que eu ajusto rapidinho.

Me avise que eu já te ajudo a criar essas 2 rotas que faltam no backend — são bem simples de adicionar.

## 5. Estrutura

```
app/
  login/              staff login (SuperAdmin, TenantAdmin, Barber)
  trocar-senha/        troca de senha obrigatória no primeiro acesso
  super-admin/         painel do Super Admin
  admin/                painel do Admin da barbearia
    services/
    barbers/
    settings/
  barbearias/[slug]/   site público da barbearia (cliente)
lib/
  api.ts               cliente de API — todas as chamadas ao backend
  auth-context.tsx      estado de autenticação (token + usuário)
  format.ts             helpers de formatação (moeda, data, duração)
components/
  ui.tsx                Button, Input, Card, Badge etc.
  RequireRole.tsx        guarda de rota por papel (role)
```
