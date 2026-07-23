"use client";

import { useEffect, useState } from "react";
import { api, Appointment, ApiError, APPOINTMENT_STATUS_LABELS } from "@/lib/api";
import { formatCurrency, formatTime, todayIsoDate } from "@/lib/format";
import { Card, Badge, Alert, Spinner, Input, Label } from "@/components/ui";

const STATUS_TONE: Record<number, "confirmed" | "cancelled" | "completed" | "noshow"> = {
  0: "confirmed",
  1: "cancelled",
  2: "completed",
  3: "noshow",
};

export default function AdminAppointmentsPage() {
  const [date, setDate] = useState(todayIsoDate());
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    api.admin
      .listAppointments(date)
      .then((data) => !cancelled && setAppointments(data))
      .catch((err) => !cancelled && setError(err instanceof ApiError ? err.message : "Erro ao carregar agenda."))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [date]);

  return (
    <div>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h2 className="font-display text-lg font-semibold text-ink">Agenda do dia</h2>
          <p className="text-sm text-steel">Todos os agendamentos da barbearia nessa data.</p>
        </div>
        <div className="w-48">
          <Label htmlFor="date">Data</Label>
          <Input id="date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>
      </div>

      {loading ? (
        <Spinner className="h-5 w-5 text-brass" />
      ) : error ? (
        <Alert>{error}</Alert>
      ) : appointments.length === 0 ? (
        <Card>
          <p className="text-sm text-steel">Nenhum agendamento para essa data.</p>
        </Card>
      ) : (
        <Card className="overflow-hidden p-0">
          <table className="w-full text-sm">
            <thead className="border-b border-ink/10 bg-ink/[0.03] text-left text-xs uppercase tracking-wide text-steel">
              <tr>
                <th className="px-4 py-3">Horário</th>
                <th className="px-4 py-3">Cliente</th>
                <th className="px-4 py-3">Barbeiro</th>
                <th className="px-4 py-3">Serviço</th>
                <th className="px-4 py-3">Valor</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {appointments.map((a) => (
                <tr key={a.id} className="border-b border-ink/5 last:border-0">
                  <td className="px-4 py-3 font-medium text-ink">
                    {formatTime(a.startAtUtc)}–{formatTime(a.endAtUtc)}
                  </td>
                  <td className="px-4 py-3 text-ink">{a.customer?.name ?? "—"}</td>
                  <td className="px-4 py-3 text-steel">{a.barber?.user?.name ?? "—"}</td>
                  <td className="px-4 py-3 text-steel">{a.service?.name ?? "—"}</td>
                  <td className="px-4 py-3 text-steel">
                    {a.service ? formatCurrency(a.service.price) : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <Badge tone={STATUS_TONE[a.status]}>{APPOINTMENT_STATUS_LABELS[a.status]}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}
