"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { api, ApiError } from "@/lib/api";
import { Button, Input, Label, Alert, Spinner } from "@/components/ui";

const ROLE_HOME: Record<string, string> = {
  SuperAdmin: "/super-admin",
  TenantAdmin: "/admin",
  Barber: "/login",
  Customer: "/login",
};

export default function TrocarSenhaPage() {
  const { user, loading: authLoading, setSession, logout } = useAuth();
  const router = useRouter();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!authLoading && !user) {
      router.replace("/login");
    }
  }, [authLoading, user, router]);

  if (authLoading || !user) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-cream">
        <Spinner className="h-6 w-6 text-brass" />
      </div>
    );
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (newPassword.length < 8) {
      setError("A nova senha deve ter pelo menos 8 caracteres.");
      return;
    }
    setLoading(true);
    try {
      await api.auth.changePassword(currentPassword, newPassword);
      setSession(localStorage.getItem("token")!, { ...user!, mustChangePassword: false });
      router.push(ROLE_HOME[user!.role] ?? "/login");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível trocar a senha.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-cream px-4">
      <div className="w-full max-w-sm">
        <h1 className="mb-1 font-display text-2xl font-semibold text-ink">Defina uma nova senha</h1>
        <p className="mb-6 text-sm text-steel">
          Por segurança, troque a senha provisória antes de continuar.
        </p>
        <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-ink/10 bg-white p-6 shadow-sm">
          {error && <Alert>{error}</Alert>}
          <div>
            <Label htmlFor="current">Senha atual (provisória)</Label>
            <Input id="current" type="password" required value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="new">Nova senha</Label>
            <Input id="new" type="password" required minLength={8} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
          </div>
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Salvando..." : "Salvar nova senha"}
          </Button>
          <button type="button" onClick={logout} className="w-full text-center text-xs text-steel hover:underline">
            Sair
          </button>
        </form>
      </div>
    </div>
  );
}
