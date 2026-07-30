"use client";

import { useEffect, useState } from "react";
import { api, ApiError, Tenant } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { Spinner, Button } from "@/components/ui";
import { BookingWizard } from "./BookingWizard";
import { MyAppointments } from "./MyAppointments";
import { AuthPanel } from "./AuthPanel";

export default function TenantPublicPage({ params }: { params: { slug: string } }) {
  const { slug } = params;
  const { user, logout } = useAuth();
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [tab, setTab] = useState<"booking" | "my-appointments" | "auth">("booking");

  useEffect(() => {
    api.public
      .getTenant(slug)
      .then(setTenant)
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
      })
      .finally(() => setLoading(false));
  }, [slug]);

  const isCustomerHere = user?.role === "Customer";

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-cream">
        <Spinner className="h-6 w-6 text-brass" />
      </div>
    );
  }

  if (notFound || !tenant) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-cream px-4 text-center">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink">Barbearia não encontrada</h1>
          <p className="mt-2 text-sm text-steel">Verifique se o link está correto.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-cream">
      <header className="border-b border-white/10 bg-surface/95 shadow-lg shadow-black/10 backdrop-blur">
        <div className="barber-stripe" />
        <div className="mx-auto max-w-3xl px-6 py-7">
          <div className="flex items-start justify-between">
            <div className="flex items-start gap-3">
              <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-brass/60 bg-brass/10 text-xl text-brass">✂</div>
              <div>
              <p className="mb-1 text-[10px] font-semibold uppercase tracking-[0.2em] text-brass">Seu próximo visual começa aqui</p>
              <h1 className="font-display text-2xl font-semibold text-ink sm:text-3xl">{tenant.name}</h1>
              <p className="mt-1 text-sm text-steel">
                {[tenant.address, tenant.phone].filter(Boolean).join(" · ")}
              </p>
              </div>
            </div>
            {isCustomerHere ? (
              <Button variant="ghost" onClick={logout}>
                Sair
              </Button>
            ) : (
              tab !== "auth" && (
                <Button variant="secondary" onClick={() => setTab("auth")}>
                  Entrar / Criar conta
                </Button>
              )
            )}
          </div>
          <nav className="mt-6 flex gap-2 border-b border-white/10">
            <button
              onClick={() => setTab("booking")}
              className={`border-b-2 px-3 py-2.5 text-sm font-medium transition-colors ${
                tab === "booking" ? "border-brass text-brass-light" : "border-transparent text-steel hover:text-ink"
              }`}
            >
              Agendar horário
            </button>
            {isCustomerHere && (
              <button
                onClick={() => setTab("my-appointments")}
                className={`border-b-2 px-3 py-2.5 text-sm font-medium transition-colors ${
                  tab === "my-appointments" ? "border-brass text-brass-light" : "border-transparent text-steel hover:text-ink"
                }`}
              >
                Minhas reservas
              </button>
            )}
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-6 py-10">
        {tab === "auth" && (
          <div>
            <p className="mb-4 text-center text-sm text-steel">Entre ou crie sua conta de cliente</p>
            <AuthPanel tenant={tenant} onDone={() => setTab("booking")} />
          </div>
        )}
        {tab === "booking" && <BookingWizard tenant={tenant} />}
        {tab === "my-appointments" && isCustomerHere && <MyAppointments tenant={tenant} />}
      </main>
    </div>
  );
}
