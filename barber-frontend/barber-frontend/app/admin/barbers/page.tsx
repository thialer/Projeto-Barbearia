"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, Service, WorkingHour, ApiError, DAYS_OF_WEEK } from "@/lib/api";
import { Button, Input, Label, Textarea, Card, Alert, Spinner } from "@/components/ui";

interface SessionBarber {
  id: string;
  name: string;
  email: string;
}

function ServicesPicker({ barberId, services }: { barberId: string; services: Service[] }) {
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [savedMsg, setSavedMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  function toggle(id: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  async function save() {
    setSaving(true);
    setError(null);
    setSavedMsg(null);
    try {
      await api.admin.setBarberServices(barberId, Array.from(selected));
      setSavedMsg("Serviços atualizados.");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Erro ao salvar serviços.");
    } finally {
      setSaving(false);
    }
  }

  if (services.length === 0) return <p className="text-sm text-steel">Cadastre serviços primeiro.</p>;

  return (
    <div>
      <div className="mb-3 flex flex-wrap gap-2">
        {services.map((s) => (
          <label
            key={s.id}
            className={`cursor-pointer rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${
              selected.has(s.id) ? "border-brass bg-brass/10 text-brass-dark" : "border-ink/15 text-steel"
            }`}
          >
            <input type="checkbox" className="hidden" checked={selected.has(s.id)} onChange={() => toggle(s.id)} />
            {s.name}
          </label>
        ))}
      </div>
      {error && <Alert className="mb-2">{error}</Alert>}
      {savedMsg && <p className="mb-2 text-xs text-confirmed">{savedMsg}</p>}
      <Button variant="secondary" onClick={save} disabled={saving} className="text-xs">
        {saving ? "Salvando..." : "Salvar serviços do barbeiro"}
      </Button>
    </div>
  );
}

function WorkingHoursEditor({ barberId }: { barberId: string }) {
  const [hours, setHours] = useState<WorkingHour[]>([{ dayOfWeek: 1, start: "09:00:00", end: "18:00:00" }]);
  const [saving, setSaving] = useState(false);
  const [savedMsg, setSavedMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  function updateRow(index: number, field: keyof WorkingHour, value: string | number) {
    setHours((prev) => prev.map((h, i) => (i === index ? { ...h, [field]: value } : h)));
  }

  function addRow() {
    setHours((prev) => [...prev, { dayOfWeek: 1, start: "09:00:00", end: "18:00:00" }]);
  }

  function removeRow(index: number) {
    setHours((prev) => prev.filter((_, i) => i !== index));
  }

  async function save() {
    setSaving(true);
    setError(null);
    setSavedMsg(null);
    try {
      await api.admin.setBarberWorkingHours(barberId, hours);
      setSavedMsg("Horários atualizados.");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Erro ao salvar horários.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="mb-3 space-y-2">
        {hours.map((h, i) => (
          <div key={i} className="flex items-center gap-2">
            <select
              className="rounded-md border border-white/10 bg-surface-input px-2 py-1.5 text-xs text-ink"
              value={h.dayOfWeek}
              onChange={(e) => updateRow(i, "dayOfWeek", parseInt(e.target.value, 10))}
            >
              {DAYS_OF_WEEK.map((d) => (
                <option key={d.value} value={d.value}>
                  {d.label}
                </option>
              ))}
            </select>
            <input
              type="time"
              className="rounded-md border border-white/10 bg-surface-input px-2 py-1.5 text-xs text-ink"
              value={h.start.slice(0, 5)}
              onChange={(e) => updateRow(i, "start", `${e.target.value}:00`)}
            />
            <span className="text-xs text-steel">até</span>
            <input
              type="time"
              className="rounded-md border border-white/10 bg-surface-input px-2 py-1.5 text-xs text-ink"
              value={h.end.slice(0, 5)}
              onChange={(e) => updateRow(i, "end", `${e.target.value}:00`)}
            />
            <button type="button" onClick={() => removeRow(i)} className="text-xs text-cancelled hover:underline">
              remover
            </button>
          </div>
        ))}
      </div>
      <div className="flex items-center gap-3">
        <Button variant="secondary" onClick={addRow} className="text-xs">
          + Adicionar dia
        </Button>
        <Button variant="secondary" onClick={save} disabled={saving} className="text-xs">
          {saving ? "Salvando..." : "Salvar horários"}
        </Button>
      </div>
      {error && <Alert className="mt-2">{error}</Alert>}
      {savedMsg && <p className="mt-2 text-xs text-confirmed">{savedMsg}</p>}
    </div>
  );
}

export default function BarbersPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [barbers, setBarbers] = useState<SessionBarber[]>([]);

  const [form, setForm] = useState({ name: "", email: "", password: "", bio: "" });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [created, setCreated] = useState<{ email: string; password: string } | null>(null);

  useEffect(() => {
    api.admin.listServices().then(setServices).catch(() => {});
    api.admin.listBarbers().then(setBarbers).catch(() => {});
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError(null);
    setCreating(true);
    try {
      const result = await api.admin.createBarber(form);
      setBarbers((prev) => [...prev, { id: result.id, name: form.name, email: result.email }]);
      setCreated({ email: result.email, password: form.password });
      setForm({ name: "", email: "", password: "", bio: "" });
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Não foi possível cadastrar o barbeiro.");
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="space-y-8">
      <section>
        <h2 className="mb-4 font-display text-lg font-semibold text-ink">Novo barbeiro</h2>
        <Card>
          {created && (
            <div className="mb-4 rounded-md border border-confirmed/30 bg-confirmed/5 p-3 text-sm">
              Barbeiro criado! Login: <strong>{created.email}</strong> · Senha provisória:{" "}
              <strong>{created.password}</strong>
            </div>
          )}
          {createError && <Alert className="mb-4">{createError}</Alert>}
          <form onSubmit={handleCreate} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <Label htmlFor="bname">Nome</Label>
              <Input id="bname" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="bemail">E-mail</Label>
              <Input id="bemail" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="bpassword">Senha provisória</Label>
              <Input id="bpassword" required minLength={8} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="bbio">Bio (opcional)</Label>
              <Textarea id="bbio" rows={1} value={form.bio} onChange={(e) => setForm({ ...form, bio: e.target.value })} />
            </div>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={creating}>
                {creating ? "Cadastrando..." : "Cadastrar barbeiro"}
              </Button>
            </div>
          </form>
        </Card>
      </section>

      {barbers.length > 0 && (
        <section>
          <h2 className="mb-4 font-display text-lg font-semibold text-ink">Barbeiros desta sessão</h2>
          <div className="space-y-4">
            {barbers.map((b) => (
              <Card key={b.id}>
                <h3 className="mb-3 font-semibold text-ink">
                  {b.name} <span className="font-normal text-steel">· {b.email}</span>
                </h3>
                <div className="mb-4">
                  <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-steel">Serviços que ele realiza</p>
                  <ServicesPicker barberId={b.id} services={services} />
                </div>
                <div>
                  <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-steel">Horário de trabalho</p>
                  <WorkingHoursEditor barberId={b.id} />
                </div>
                <div className="mt-4 border-t pt-4">
                  <Button
                    variant="danger"
                    className="text-xs"
                    onClick={() => {
                      if (confirm(`Tem certeza que deseja deletar o barbeiro ${b.name}?`)) {
                        api.admin.deleteBarber(b.id)
                          .then(() => {
                            setBarbers((prev) => prev.filter((barber) => barber.id !== b.id));
                          })
                          .catch((err) => {
                            alert(err instanceof ApiError ? err.message : "Erro ao deletar barbeiro.");
                          });
                      }
                    }}
                  >
                    Deletar barbeiro
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
