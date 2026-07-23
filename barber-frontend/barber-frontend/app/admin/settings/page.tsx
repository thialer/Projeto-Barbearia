"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { Button, Input, Label, Card, Alert, Spinner } from "@/components/ui";

export default function SettingsPage() {
  const [form, setForm] = useState({
    name: "",
    phone: "",
    address: "",
    timeZoneId: "America/Sao_Paulo",
    cancellationLimitMinutes: "120",
  });
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savedMsg, setSavedMsg] = useState<string | null>(null);

  useEffect(() => {
    api.admin.getSettings()
      .then((settings) => {
        setForm({
          name: settings.name,
          phone: settings.phone || "",
          address: settings.address || "",
          timeZoneId: settings.timeZoneId,
          cancellationLimitMinutes: settings.cancellationLimitMinutes.toString(),
        });
      })
      .catch(() => {
        // Se falhar ao carregar, deixa em branco mesmo
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSavedMsg(null);
    const cancellationLimitMinutes = parseInt(form.cancellationLimitMinutes, 10);
    if (isNaN(cancellationLimitMinutes) || cancellationLimitMinutes < 0 || cancellationLimitMinutes > 10080) {
      setError("O limite de cancelamento deve ser entre 0 e 10080 minutos (7 dias).");
      return;
    }
    setSaving(true);
    try {
      await api.admin.updateSettings({
        name: form.name,
        phone: form.phone || undefined,
        address: form.address || undefined,
        timeZoneId: form.timeZoneId,
        cancellationLimitMinutes,
      });
      setSavedMsg("Configurações salvas com sucesso.");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível salvar.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h2 className="mb-4 font-display text-lg font-semibold text-ink">Configurações da barbearia</h2>
      {loading ? (
        <Spinner />
      ) : (
        <>
          <Card className="mt-4">
            {error && <Alert className="mb-4">{error}</Alert>}
            {savedMsg && <Alert tone="success" className="mb-4">{savedMsg}</Alert>}
            <form onSubmit={handleSubmit} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <Label htmlFor="name">Nome da barbearia</Label>
            <Input id="name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div>
            <Label htmlFor="phone">Telefone</Label>
            <Input id="phone" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </div>
          <div>
            <Label htmlFor="timeZoneId">Fuso horário</Label>
            <Input id="timeZoneId" required value={form.timeZoneId} onChange={(e) => setForm({ ...form, timeZoneId: e.target.value })} />
          </div>
          <div className="sm:col-span-2">
            <Label htmlFor="address">Endereço</Label>
            <Input id="address" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
          </div>
          <div>
            <Label htmlFor="cancel">Limite para cancelar (minutos antes)</Label>
            <Input
              id="cancel"
              type="number"
              min={0}
              max={10080}
              required
              value={form.cancellationLimitMinutes}
              onChange={(e) => setForm({ ...form, cancellationLimitMinutes: e.target.value })}
            />
          </div>
          <div className="sm:col-span-2">
            <Button type="submit" disabled={saving}>
              {saving ? "Salvando..." : "Salvar configurações"}
            </Button>
          </div>
        </form>
      </Card>
        </>
      )}
    </div>
  );
}
