# 📅 Fluxo de Agendamento do Cliente

## 📍 Localização no Projeto

O cliente marca horário na aba **"Agendar horário"** da página pública da barbearia.

### Estrutura de Arquivos:
```
barber-frontend/barber-frontend/
├── app/barbearias/[slug]/
│   ├── page.tsx                 ← Página principal (contém abas)
│   ├── BookingWizard.tsx        ← Wizard de agendamento (4 passos)
│   ├── AuthPanel.tsx            ← Login/Registro do cliente
│   ├── MyAppointments.tsx       ← Minhas reservas
│   └── layout.tsx
```

---

## 🎯 Fluxo em 4 Passos

### **Passo 1: Selecionar Serviço**
- **Arquivo:** `BookingWizard.tsx` (linhas 127-149)
- **O que mostra:** Grade com todos os serviços disponíveis
- **Informações visíveis:**
  - Nome do serviço
  - Descrição
  - Preço
  - Duração
- **Ação:** Clicar em um serviço vai para o Passo 2

```typescript
{step === 1 && (
  <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
	{services.map((s) => (
	  <button
		onClick={() => {
		  setServiceId(s.id);
		  setStep(2);
		}}
		// ... UI do card do serviço
	  />
	))}
  </div>
)}
```

---

### **Passo 2: Selecionar Barbeiro**
- **Arquivo:** `BookingWizard.tsx` (linhas 152-175)
- **O que mostra:** Lista de barbeiros disponíveis
- **Informações visíveis:**
  - Nome do barbeiro
  - Bio (se houver)
- **Ação:** Clicar em um barbeiro vai para o Passo 3

```typescript
{step === 2 && (
  <div>
	<div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
	  {barbers.map((b) => (
		<button
		  onClick={() => {
			setBarberId(b.id);
			setStep(3);
		  }}
		  // ... UI do card do barbeiro
		/>
	  ))}
	</div>
	<Button onClick={() => setStep(1)}>← Voltar</Button>
  </div>
)}
```

---

### **Passo 3: Selecionar Data e Horário** ⏰
- **Arquivo:** `BookingWizard.tsx` (linhas 178-215)
- **O que mostra:** 
  - Seletor de data (usando input type="date")
  - Horários disponíveis para o barbeiro naquele dia
- **Como funciona:**
  1. Cliente escolhe uma data
  2. API chama `getAvailability()` que retorna slots de horário
  3. Cliente clica no horário desejado
  4. Vai para o Passo 4

```typescript
{step === 3 && (
  <div>
	<div className="mb-4 w-48">
	  <Label htmlFor="date">Data</Label>
	  <Input 
		id="date" 
		type="date" 
		min={todayIsoDate()} 
		value={date} 
		onChange={(e) => setDate(e.target.value)} 
	  />
	</div>

	{loadingSlots ? (
	  <Spinner />
	) : (
	  <div className="flex flex-wrap gap-2">
		{slots.map((slot) => (
		  <button
			onClick={() => setSelectedSlot(slot)}
			// ... UI do botão de horário
		  >
			{formatSlot(slot)}  {/* Exibe no formato HH:mm */}
		  </button>
		))}
	  </div>
	)}
  </div>
)}
```

**⚠️ IMPORTANTE:** Os horários são carregados via:
```typescript
api.public.getAvailability(tenant.slug, barberId, serviceId, date)
```
Este endpoint calcula os slots disponíveis baseado em:
- Horários de funcionamento do barbeiro
- Agendamentos existentes
- Duração do serviço

---

### **Passo 4: Confirmação**
- **Arquivo:** `BookingWizard.tsx` (linhas 218-257)
- **O que mostra:**
  - Resumo do agendamento (serviço, barbeiro, data/hora, preço)
  - Campo de observações (opcional)
  - Se não está logado: painel de login/registro
  - Se está logado: botão de confirmar
- **Ação:** Clica em "Confirmar agendamento"

```typescript
{step === 4 && (
  <div>
	<Card>
	  {/* Resumo do agendamento */}
	  <p>{selectedService?.name}</p>
	  <p>com {selectedBarber?.name} · {selectedSlot}</p>
	  <p>{formatCurrency(selectedService?.price)}</p>
	</Card>

	{!isCustomerOfTenant ? (
	  <AuthPanel tenant={tenant} />  {/* Mostra login se não autenticado */}
	) : (
	  <div>
		<Textarea 
		  value={notes} 
		  onChange={(e) => setNotes(e.target.value)} 
		/>
		<Button onClick={confirmBooking}>
		  Confirmar agendamento
		</Button>
	  </div>
	)}
  </div>
)}
```

---

## 🔗 Endpoints da API Chamados

```typescript
// 1. Carregar serviços e barbeiros
api.public.listServices(slug)
api.public.listBarbers(slug)

// 2. Carregar horários disponíveis (Step 3)
api.public.getAvailability(slug, barberId, serviceId, date)

// 3. Criar agendamento (Step 4)
api.public.createAppointment(slug, {
  barberId,
  serviceId,
  startAt: selectedSlot,  // ISO string do horário
  notes: "..."            // opcional
})
```

---

## 🎨 Interface Visual

```
┌─────────────────────────────────────────┐
│ BARBEARIA XYZ                      [Entrar/Sair]
├──────────── [Agendar] [Minhas Reservas] ────────┤
│                                                   │
│  PASSO 1: Selecionar Serviço                     │
│  ┌─────────────────┬──────────────────┐          │
│  │ Corte Simples   │ Barba            │          │
│  │ R$ 35,00 · 30m  │ R$ 25,00 · 20m   │          │
│  └─────────────────┴──────────────────┘          │
│                                                   │
│  PASSO 2: Selecionar Barbeiro                    │
│  ┌─────────────────┬──────────────────┐          │
│  │ João Silva      │ Maria Santos     │          │
│  │ 5 anos exp.     │ Especialista     │          │
│  └─────────────────┴──────────────────┘          │
│                                                   │
│  PASSO 3: Selecionar Horário                     │
│  Data: [15/01/2025          ⏷]                  │
│  [09:00] [09:30] [10:00] [10:30] [11:00] ...    │
│                                                   │
│  PASSO 4: Confirmação                            │
│  📋 Corte Simples                                │
│  👤 com João Silva                               │
│  📅 15/01/2025 às 10:00                          │
│  💰 R$ 35,00                                     │
│                                                   │
│  Observações: [________________]                 │
│                                                   │
│  [← Voltar]  [Confirmar Agendamento]             │
│                                                   │
│  ✅ Agendamento confirmado!                      │
└─────────────────────────────────────────┘
```

---

## 🔄 Estados de Carregamento

- **`loadingCatalog`**: Carregando serviços e barbeiros
- **`loadingSlots`**: Carregando horários disponíveis
- **`booking`**: Confirmando agendamento

---

## 🎯 Resumo

**Usuário precisa fazer:**
1. ✅ Escolher um **SERVIÇO**
2. ✅ Escolher um **BARBEIRO**
3. ✅ Escolher uma **DATA** (mínimo hoje)
4. ✅ Escolher um **HORÁRIO** (do barbeiro naquele dia)
5. ✅ Adicionar **OBSERVAÇÕES** (opcional)
6. ✅ Fazer **LOGIN** ou **REGISTRAR** (se não estiver logado)
7. ✅ **CONFIRMAR** agendamento

**Arquivo principal:** `BookingWizard.tsx` 📄
**Localização:** `barber-frontend/barber-frontend/app/barbearias/[slug]/BookingWizard.tsx`
