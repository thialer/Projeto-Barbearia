"use client";

import { useEffect, useState } from "react";
import { api, Appointment, ApiError, APPOINTMENT_STATUS_LABELS, BarberSummary, Tenant } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { Button, Card, Badge, Alert, Spinner, Input, Label, Select } from "@/components/ui";

const STATUS_TONE: Record<number, "confirmed" | "cancelled" | "completed" | "noshow"> = {
  0: "confirmed",
  1: "cancelled",
  2: "completed",
  3: "noshow",
};

function RescheduleForm({
  tenant,
  appointment,
  barbers,
  onDone,
}: {
  tenant: Tenant;
  appointment: Appointment;
  barbers: BarberSummary[];
  onDone: () => void;
}) {
  const [barberId, setBarberId] = useState(appointment.barberId);
  const [startAt, setStartAt] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    if (!startAt) {
      setError("Escolha a nova data e horário.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await api.public.rescheduleAppointment(tenant.slug, appointment.id, {
        barberId,
        startAt: new Date(startAt).toISOString(),
      });
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível remarcar.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mt-3 space-y-2 rounded-md border border-ink/10 bg-ink/[0.02] p-3">
      {error && <Alert>{error}</Alert>}
      <div>
        <Label htmlFor={`barber-${appointment.id}`}>Barbeiro</Label>
        <Select id={`barber-${appointment.id}`} value={barberId} onChange={(e) => setBarberId(e.target.value)}>
          {barbers.map((b) => (
            <option key={b.id} value={b.id}>
              {b.name}
            </option>
          ))}
        </Select>
      </div>
      <div>
        <Label htmlFor={`when-${appointment.id}`}>Nova data e horário</Label>
        <Input id={`when-${appointment.id}`} type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} />
      </div>
      <Button className="text-xs" disabled={saving} onClick={submit}>
        {saving ? "Enviando..." : "Confirmar nova data"}
      </Button>
    </div>
  );
}

export function MyAppointments({ tenant }: { tenant: Tenant }) {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [barbers, setBarbers] = useState<BarberSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reschedulingId, setReschedulingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  function load() {
    setLoading(true);
    Promise.all([api.public.myAppointments(tenant.slug), api.public.listBarbers(tenant.slug)])
      .then(([a, b]) => {
        setAppointments(a);
        setBarbers(b);
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "Erro ao carregar seus agendamentos."))
      .finally(() => setLoading(false));
  }

  useEffect(load, [tenant.slug]);

  async function cancel(id: string) {
    setActionError(null);
    try {
      await api.public.cancelAppointment(tenant.slug, id);
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Não foi possível cancelar.");
    }
  }

  if (loading) return <Spinner className="mx-auto h-5 w-5 text-brass" />;
  if (error) return <Alert>{error}</Alert>;
  if (appointments.length === 0) return <p className="text-center text-sm text-steel">Você ainda não tem agendamentos.</p>;

  return (
    <div className="mx-auto max-w-2xl space-y-3">
      {actionError && <Alert>{actionError}</Alert>}
      {appointments.map((a) => (
        <Card key={a.id}>
          <div className="flex items-start justify-between">
            <div>
              <p className="font-semibold text-ink">{a.service?.name ?? "Serviço"}</p>
              <p className="text-sm text-steel">
                com {a.barber?.user?.name ?? "barbeiro"} · {formatDateTime(a.startAtUtc)}
              </p>
            </div>
            <Badge tone={STATUS_TONE[a.status]}>{APPOINTMENT_STATUS_LABELS[a.status]}</Badge>
          </div>
          {a.status === 0 && (
            <div className="mt-3 flex gap-3">
              <Button variant="secondary" className="text-xs" onClick={() => setReschedulingId(reschedulingId === a.id ? null : a.id)}>
                Remarcar
              </Button>
              <Button variant="danger" className="text-xs" onClick={() => cancel(a.id)}>
                Cancelar
              </Button>
            </div>
          )}
          {reschedulingId === a.id && (
            <RescheduleForm
              tenant={tenant}
              appointment={a}
              barbers={barbers}
              onDone={() => {
                setReschedulingId(null);
                load();
              }}
            />
          )}
        </Card>
      ))}
    </div>
  );
}
