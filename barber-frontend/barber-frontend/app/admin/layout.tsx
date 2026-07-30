"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { RequireRole } from "@/components/RequireRole";
import { useAuth } from "@/lib/auth-context";
import { Button } from "@/components/ui";

const NAV = [
  { href: "/admin", label: "Agenda" },
  { href: "/admin/services", label: "Serviços" },
  { href: "/admin/barbers", label: "Barbeiros" },
  { href: "/admin/settings", label: "Configurações" },
];

function AdminShell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  return (
    <div className="min-h-screen bg-cream">
      <header className="border-b border-white/10 bg-surface/95 shadow-lg shadow-black/10 backdrop-blur">
        <div className="barber-stripe" />
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-brass">Área administrativa</p>
            <h1 className="font-display text-xl font-semibold text-ink">BarberBooking</h1>
            <p className="text-xs text-steel">{user?.name}</p>
          </div>
          <Button variant="ghost" onClick={logout}>
            Sair
          </Button>
        </div>
        <nav className="mx-auto flex max-w-5xl gap-1 overflow-x-auto px-6">
          {NAV.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={`border-b-2 px-3 py-2.5 text-sm font-medium transition-colors ${
                pathname === item.href
                  ? "border-brass text-brass-light"
                  : "border-transparent text-steel hover:text-ink"
              }`}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </header>
      <main className="mx-auto max-w-5xl px-6 py-8">{children}</main>
    </div>
  );
}

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireRole role="TenantAdmin">
      <AdminShell>{children}</AdminShell>
    </RequireRole>
  );
}
