"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api";
import { Button, Input, Label, Alert } from "@/components/ui";

const ROLE_HOME: Record<string, string> = {
  SuperAdmin: "/super-admin",
  TenantAdmin: "/admin",
  Barber: "/login",
  Customer: "/login",
};

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const user = await login(email, password);
      if (user.mustChangePassword) {
        router.push("/trocar-senha");
        return;
      }
      if (user.role === "Barber") {
        setError(
          "Login de barbeiro ainda não tem um painel próprio nesta versão. Peça ao administrador da barbearia para consultar sua agenda."
        );
        return;
      }
      router.push(ROLE_HOME[user.role] ?? "/login");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível entrar.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-cream px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <h1 className="font-display text-3xl font-semibold text-ink">BarberBooking</h1>
          <p className="mt-1 text-sm text-steel">Painel administrativo</p>
        </div>
        <div className="barber-stripe mb-6 rounded-full" />
        <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-ink/10 bg-white p-6 shadow-sm">
          {error && <Alert>{error}</Alert>}
          <div>
            <Label htmlFor="email">E-mail</Label>
            <Input
              id="email"
              type="email"
              required
              autoFocus
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="voce@barbearia.com"
            />
          </div>
          <div>
            <Label htmlFor="password">Senha</Label>
            <Input
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </div>
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Entrando..." : "Entrar"}
          </Button>
        </form>
        <p className="mt-6 text-center text-xs text-steel">
          É cliente e quer agendar um horário? Acesse o site da sua barbearia diretamente pelo link
          que ela te enviou.
        </p>
      </div>
    </div>
  );
}
