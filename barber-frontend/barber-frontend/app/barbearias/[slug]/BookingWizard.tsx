"use client";

import { useEffect, useState } from "react";
import { api, ApiError, BarberSummary, Service, Tenant } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { formatCurrency, formatDurationMinutes, todayIsoDate } from "@/lib/format";
import { Button, Card, Alert, Spinner, Input, Label, Textarea } from "@/components/ui";
import { AuthPanel } from "./AuthPanel";

function formatSlot(slot: string): string {
  const d = new Date(slot);
  if (isNaN(d.getTime())) return slot;
  return d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

export function BookingWizard({ tenant }: { tenant: Tenant }) {
  const { user } = useAuth();
  const isCustomerOfTenant = user?.role === "Customer";

  const [step, setStep] = useState(1);
  const [services, setServices] = useState<Service[]>([]);
  const [barbers, setBarbers] = useState<BarberSummary[]>([]);
  const [loadingCatalog, setLoadingCatalog] = useState(true);
  const [catalogError, setCatalogError] = useState<string | null>(null);

  const [serviceId, setServiceId] = useState<string | null>(null);
  const [barberId, setBarberId] = useState<string | null>(null);
  const [date, setDate] = useState(todayIsoDate());
  const [slots, setSlots] = useState<string[]>([]);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [slotsError, setSlotsError] = useState<string | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [notes, setNotes] = useState("");

  const [booking, setBooking] = useState(false);
  const [bookingError, setBookingError] = useState<string | null>(null);
  const [bookingSuccess, setBookingSuccess] = useState(false);

  useEffect(() => {
    Promise.all([api.public.listServices(tenant.slug), api.public.listBarbers(tenant.slug)])
      .then(([s, b]) => {
        setServices(s);
        setBarbers(b);
      })
      .catch((err) => setCatalogError(err instanceof ApiError ? err.message : "Erro ao carregar catálogo."))
      .finally(() => setLoadingCatalog(false));
  }, [tenant.slug]);

  useEffect(() => {
    if (step !== 3 || !serviceId || !barberId) return;
    setLoadingSlots(true);
    setSlotsError(null);
    setSelectedSlot(null);
    api.public
      .getAvailability(tenant.slug, barberId, serviceId, date)
      .then(setSlots)
      .catch((err) => setSlotsError(err instanceof ApiError ? err.message : "Erro ao carregar horários."))
      .finally(() => setLoadingSlots(false));
  }, [step, serviceId, barberId, date, tenant.slug]);

  const selectedService = services.find((s) => s.id === serviceId);
  const selectedBarber = barbers.find((b) => b.id === barberId);

  async function confirmBooking() {
    if (!serviceId || !barberId || !selectedSlot) return;
    setBooking(true);
    setBookingError(null);
    try {
      await api.public.createAppointment(tenant.slug, {
        barberId,
        serviceId,
        startAt: selectedSlot,
        notes: notes || undefined,
      });
      setBookingSuccess(true);
    } catch (err) {
      setBookingError(err instanceof ApiError ? err.message : "Não foi possível concluir o agendamento.");
    } finally {
      setBooking(false);
    }
  }

  if (bookingSuccess) {
    return (
      <Card className="mx-auto max-w-md text-center">
        <h3 className="mb-2 font-display text-lg font-semibold text-confirmed">Agendamento confirmado!</h3>
        <p className="mb-4 text-sm text-steel">
          {selectedService?.name} com {selectedBarber?.name} em{" "}
          {selectedSlot && new Date(selectedSlot).toLocaleString("pt-BR")}.
        </p>
        <Button
          variant="secondary"
          onClick={() => {
            setBookingSuccess(false);
            setStep(1);
            setServiceId(null);
            setBarberId(null);
            setSelectedSlot(null);
            setNotes("");
          }}
        >
          Fazer outro agendamento
        </Button>
      </Card>
    );
  }

  if (loadingCatalog) return <Spinner className="mx-auto h-5 w-5 text-brass" />;
  if (catalogError) return <Alert>{catalogError}</Alert>;

  return (
    <div className="mx-auto max-w-2xl">
      <ol className="mb-6 flex justify-between text-xs font-semibold text-steel">
        {["Serviço", "Barbeiro", "Data e horário", "Confirmação"].map((label, i) => (
          <li key={label} className={`flex items-center gap-1.5 ${step === i + 1 ? "text-brass-dark" : ""}`}>
            <span
              className={`flex h-5 w-5 items-center justify-center rounded-full text-[11px] ${
                step > i + 1 ? "bg-confirmed text-white" : step === i + 1 ? "bg-brass text-white" : "bg-ink/10"
              }`}
            >
              {i + 1}
            </span>
            {label}
          </li>
        ))}
      </ol>

      {step === 1 && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {services.map((s) => (
            <button
              key={s.id}
              onClick={() => {
                setServiceId(s.id);
                setStep(2);
              }}
              className="rounded-lg border border-ink/10 bg-white p-4 text-left shadow-sm transition-colors hover:border-brass"
            >
              <p className="font-semibold text-ink">{s.name}</p>
              {s.description && <p className="mt-1 text-sm text-steel">{s.description}</p>}
              <div className="mt-2 flex justify-between text-sm">
                <span className="font-semibold text-brass-dark">{formatCurrency(s.price)}</span>
                <span className="text-steel">{formatDurationMinutes(s.durationMinutes)}</span>
              </div>
            </button>
          ))}
        </div>
      )}

      {step === 2 && (
        <div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {barbers.map((b) => (
              <button
                key={b.id}
                onClick={() => {
                  setBarberId(b.id);
                  setStep(3);
                }}
                className="rounded-lg border border-ink/10 bg-white p-4 text-left shadow-sm transition-colors hover:border-brass"
              >
                <p className="font-semibold text-ink">{b.name}</p>
                {b.bio && <p className="mt-1 text-sm text-steel">{b.bio}</p>}
              </button>
            ))}
          </div>
          <Button variant="ghost" className="mt-4" onClick={() => setStep(1)}>
            ← Voltar
          </Button>
        </div>
      )}

      {step === 3 && (
        <div>
          <div className="mb-4 w-48">
            <Label htmlFor="date">Data</Label>
            <Input id="date" type="date" min={todayIsoDate()} value={date} onChange={(e) => setDate(e.target.value)} />
          </div>
          {loadingSlots ? (
            <Spinner className="h-5 w-5 text-brass" />
          ) : slotsError ? (
            <Alert>{slotsError}</Alert>
          ) : slots.length === 0 ? (
            <p className="text-sm text-steel">Nenhum horário disponível nessa data.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {slots.map((slot) => (
                <button
                  key={slot}
                  onClick={() => setSelectedSlot(slot)}
                  className={`rounded-md border px-3 py-2 text-sm font-medium transition-colors ${
                    selectedSlot === slot ? "border-brass bg-brass/10 text-brass-dark" : "border-ink/15 text-ink hover:border-brass"
                  }`}
                >
                  {formatSlot(slot)}
                </button>
              ))}
            </div>
          )}
          <div className="mt-4 flex gap-3">
            <Button variant="ghost" onClick={() => setStep(2)}>
              ← Voltar
            </Button>
            <Button disabled={!selectedSlot} onClick={() => setStep(4)}>
              Continuar
            </Button>
          </div>
        </div>
      )}

      {step === 4 && (
        <div>
          <Card className="mb-5">
            <p className="text-sm text-steel">Resumo do agendamento</p>
            <p className="mt-1 font-semibold text-ink">{selectedService?.name}</p>
            <p className="text-sm text-steel">
              com {selectedBarber?.name} · {selectedSlot && new Date(selectedSlot).toLocaleString("pt-BR")}
            </p>
            <p className="mt-1 text-sm font-semibold text-brass-dark">
              {selectedService && formatCurrency(selectedService.price)}
            </p>
          </Card>

          {!isCustomerOfTenant ? (
            <div>
              <p className="mb-3 text-center text-sm text-steel">Entre ou crie sua conta para confirmar</p>
              <AuthPanel tenant={tenant} />
            </div>
          ) : (
            <div className="space-y-3">
              <div>
                <Label htmlFor="notes">Observações (opcional)</Label>
                <Textarea id="notes" rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
              </div>
              {bookingError && <Alert>{bookingError}</Alert>}
              <div className="flex gap-3">
                <Button variant="ghost" onClick={() => setStep(3)}>
                  ← Voltar
                </Button>
                <Button onClick={confirmBooking} disabled={booking}>
                  {booking ? "Confirmando..." : "Confirmar agendamento"}
                </Button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
