"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, Service, ApiError } from "@/lib/api";
import { formatCurrency, formatDurationMinutes } from "@/lib/format";
import { Button, Input, Label, Textarea, Card, Alert, Spinner } from "@/components/ui";

export default function ServicesPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);

  const [form, setForm] = useState({ name: "", description: "", price: "", durationMinutes: "" });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  function load() {
    setLoading(true);
    api.admin
      .listServices()
      .then(setServices)
      .catch((err) => setListError(err instanceof ApiError ? err.message : "Erro ao carregar serviços."))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError(null);
    const price = parseFloat(form.price.replace(",", "."));
    const durationMinutes = parseInt(form.durationMinutes, 10);
    if (isNaN(price) || price < 0) {
      setCreateError("Informe um preço válido.");
      return;
    }
    if (isNaN(durationMinutes) || durationMinutes < 5 || durationMinutes > 480) {
      setCreateError("A duração deve ser entre 5 e 480 minutos.");
      return;
    }
    setCreating(true);
    try {
      await api.admin.createService({
        name: form.name,
        description: form.description || undefined,
        price,
        durationMinutes,
      });
      setForm({ name: "", description: "", price: "", durationMinutes: "" });
      load();
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Não foi possível criar o serviço.");
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="space-y-8">
      <section>
        <h2 className="mb-4 font-display text-lg font-semibold text-ink">Novo serviço</h2>
        <Card>
          {createError && <Alert className="mb-4">{createError}</Alert>}
          <form onSubmit={handleCreate} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="name">Nome</Label>
              <Input id="name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="description">Descrição (opcional)</Label>
              <Textarea id="description" rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="price">Preço (R$)</Label>
              <Input id="price" required inputMode="decimal" placeholder="35.00" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} />
            </div>
            <div>
              <Label htmlFor="duration">Duração (minutos)</Label>
              <Input id="duration" required type="number" min={5} max={480} value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: e.target.value })} />
            </div>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={creating}>
                {creating ? "Salvando..." : "Adicionar serviço"}
              </Button>
            </div>
          </form>
        </Card>
      </section>

      <section>
        <h2 className="mb-4 font-display text-lg font-semibold text-ink">Serviços cadastrados</h2>
        {loading ? (
          <Spinner className="h-5 w-5 text-brass" />
        ) : listError ? (
          <Alert>{listError}</Alert>
        ) : services.length === 0 ? (
          <p className="text-sm text-steel">Nenhum serviço cadastrado ainda.</p>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {services.map((s) => (
              <Card key={s.id}>
                <h3 className="font-semibold text-ink">{s.name}</h3>
                {s.description && <p className="mt-1 text-sm text-steel">{s.description}</p>}
                <div className="mt-3 flex items-center justify-between text-sm">
                  <span className="font-semibold text-brass-dark">{formatCurrency(s.price)}</span>
                  <span className="text-steel">{formatDurationMinutes(s.durationMinutes)}</span>
                </div>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
