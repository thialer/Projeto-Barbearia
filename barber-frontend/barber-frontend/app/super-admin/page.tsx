"use client";

import { FormEvent, useEffect, useState } from "react";
import { RequireRole } from "@/components/RequireRole";
import { useAuth } from "@/lib/auth-context";
import { api, ApiError } from "@/lib/api";
import { Button, Input, Label, Card, Alert, Badge, Spinner } from "@/components/ui";

interface TenantRow {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAtUtc: string;
}

interface CreatedTenantInfo {
  name: string;
  slug: string;
  adminEmail: string;
  temporaryPassword: string;
  adminPanelUrl: string;
  publicUrl: string;
}

function SuperAdminDashboard() {
  const { logout, user } = useAuth();
  const [tenants, setTenants] = useState<TenantRow[]>([]);
  const [loadingList, setLoadingList] = useState(true);
  const [listError, setListError] = useState<string | null>(null);

  const [form, setForm] = useState({ name: "", slug: "", adminName: "", adminEmail: "", phone: "", address: "" });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatedTenantInfo | null>(null);

  async function loadTenants() {
    setLoadingList(true);
    setListError(null);
    try {
      setTenants(await api.superAdmin.listTenants());
    } catch (err) {
      setListError(err instanceof ApiError ? err.message : "Não foi possível carregar as barbearias.");
    } finally {
      setLoadingList(false);
    }
  }

  useEffect(() => {
    loadTenants();
  }, []);

  function slugify(text: string) {
    return text
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/(^-|-$)/g, "");
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError(null);
    setCreating(true);
    try {
      const result = await api.superAdmin.createTenant(form);
      setCreated(result);
      setForm({ name: "", slug: "", adminName: "", adminEmail: "", phone: "", address: "" });
      loadTenants();
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Não foi possível criar a barbearia.");
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="min-h-screen bg-cream">
      <header className="border-b border-ink/10 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="font-display text-xl font-semibold text-ink">BarberBooking · Super Admin</h1>
            <p className="text-xs text-steel">{user?.name}</p>
          </div>
          <Button variant="ghost" onClick={logout}>
            Sair
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-5xl space-y-8 px-6 py-8">
        <section>
          <h2 className="mb-4 font-display text-lg font-semibold text-ink">Cadastrar nova barbearia</h2>
          <Card>
            {created && (
              <div className="mb-5 rounded-md border border-confirmed/30 bg-confirmed/5 p-4 text-sm">
                <p className="mb-2 font-semibold text-confirmed">
                  Barbearia "{created.name}" criada com sucesso! Envie estes dados ao proprietário:
                </p>
                <ul className="space-y-1 text-ink">
                  <li><strong>Login:</strong> {created.adminEmail}</li>
                  <li><strong>Senha provisória:</strong> {created.temporaryPassword}</li>
                  <li><strong>Painel administrativo:</strong> {created.adminPanelUrl}</li>
                  <li><strong>Site público:</strong> {created.publicUrl}</li>
                </ul>
              </div>
            )}
            {createError && <Alert className="mb-4">{createError}</Alert>}
            <form onSubmit={handleCreate} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <Label htmlFor="name">Nome da barbearia</Label>
                <Input
                  id="name"
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value, slug: form.slug || slugify(e.target.value) })}
                />
              </div>
              <div>
                <Label htmlFor="slug">Slug (usado na URL pública)</Label>
                <Input id="slug" required pattern="[a-z0-9-]{3,60}" value={form.slug} onChange={(e) => setForm({ ...form, slug: e.target.value })} />
              </div>
              <div>
                <Label htmlFor="adminName">Nome do administrador</Label>
                <Input id="adminName" required value={form.adminName} onChange={(e) => setForm({ ...form, adminName: e.target.value })} />
              </div>
              <div>
                <Label htmlFor="adminEmail">E-mail do administrador</Label>
                <Input id="adminEmail" type="email" required value={form.adminEmail} onChange={(e) => setForm({ ...form, adminEmail: e.target.value })} />
              </div>
              <div>
                <Label htmlFor="phone">Telefone (opcional)</Label>
                <Input id="phone" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              <div>
                <Label htmlFor="address">Endereço (opcional)</Label>
                <Input id="address" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
              </div>
              <div className="sm:col-span-2">
                <Button type="submit" disabled={creating}>
                  {creating ? "Criando..." : "Criar barbearia"}
                </Button>
              </div>
            </form>
          </Card>
        </section>

        <section>
          <h2 className="mb-4 font-display text-lg font-semibold text-ink">Barbearias cadastradas</h2>
          {loadingList ? (
            <Spinner className="h-5 w-5 text-brass" />
          ) : listError ? (
            <Alert>{listError}</Alert>
          ) : tenants.length === 0 ? (
            <p className="text-sm text-steel">Nenhuma barbearia cadastrada ainda.</p>
          ) : (
            <Card className="overflow-hidden p-0">
              <table className="w-full text-sm">
                <thead className="border-b border-ink/10 bg-ink/[0.03] text-left text-xs uppercase tracking-wide text-steel">
                  <tr>
                    <th className="px-4 py-3">Nome</th>
                    <th className="px-4 py-3">Slug</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3">Criada em</th>
                  </tr>
                </thead>
                <tbody>
                  {tenants.map((t) => (
                    <tr key={t.id} className="border-b border-ink/5 last:border-0">
                      <td className="px-4 py-3 font-medium text-ink">{t.name}</td>
                      <td className="px-4 py-3 text-steel">/{t.slug}</td>
                      <td className="px-4 py-3">
                        <Badge tone={t.isActive ? "confirmed" : "cancelled"}>{t.isActive ? "Ativa" : "Inativa"}</Badge>
                      </td>
                      <td className="px-4 py-3 text-steel">{new Date(t.createdAtUtc).toLocaleDateString("pt-BR")}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Card>
          )}
        </section>
      </main>
    </div>
  );
}

export default function SuperAdminPage() {
  return (
    <RequireRole role="SuperAdmin">
      <SuperAdminDashboard />
    </RequireRole>
  );
}
