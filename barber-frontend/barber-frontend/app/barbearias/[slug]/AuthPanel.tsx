"use client";

import { FormEvent, useState } from "react";
import { api, ApiError, Tenant } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { Button, Input, Label, Alert } from "@/components/ui";

export function AuthPanel({ tenant, onDone }: { tenant: Tenant; onDone?: () => void }) {
  const { login, setSession } = useAuth();
  const [mode, setMode] = useState<"login" | "register">("register");
  const [form, setForm] = useState({ name: "", email: "", password: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      if (mode === "register") {
        const res = await api.public.register(tenant.slug, form);
        setSession(res.accessToken, {
          id: "",
          name: form.name,
          email: form.email,
          role: "Customer",
          tenantId: null,
          mustChangePassword: false,
        });
        // Faz login em seguida para obter os dados completos do usuário (id, tenantId)
        await login(form.email, form.password);
      } else {
        const user = await login(form.email, form.password);
        if (user.role !== "Customer") {
          setError("Esta conta não é de cliente. Use a área administrativa para acessar.");
          return;
        }
      }
      onDone?.();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível continuar.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-sm">
      <div className="mb-4 flex rounded-md border border-ink/10 bg-white p-1 text-sm">
        <button
          type="button"
          onClick={() => setMode("register")}
          className={`flex-1 rounded-sm py-1.5 font-medium transition-colors ${mode === "register" ? "bg-ink text-cream" : "text-steel"}`}
        >
          Criar conta
        </button>
        <button
          type="button"
          onClick={() => setMode("login")}
          className={`flex-1 rounded-sm py-1.5 font-medium transition-colors ${mode === "login" ? "bg-ink text-cream" : "text-steel"}`}
        >
          Já tenho conta
        </button>
      </div>

      <form onSubmit={handleSubmit} className="space-y-3 rounded-lg border border-ink/10 bg-white p-5 shadow-sm">
        {error && <Alert>{error}</Alert>}
        {mode === "register" && (
          <div>
            <Label htmlFor="name">Nome</Label>
            <Input id="name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
        )}
        <div>
          <Label htmlFor="email">E-mail</Label>
          <Input id="email" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div>
          <Label htmlFor="password">Senha</Label>
          <Input
            id="password"
            type="password"
            required
            minLength={mode === "register" ? 8 : undefined}
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
        </div>
        <Button type="submit" className="w-full" disabled={loading}>
          {loading ? "Aguarde..." : mode === "register" ? "Criar conta e continuar" : "Entrar"}
        </Button>
      </form>
    </div>
  );
}
