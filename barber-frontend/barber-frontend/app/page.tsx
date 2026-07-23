"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Spinner } from "@/components/ui";

const ROLE_HOME: Record<string, string> = {
  SuperAdmin: "/super-admin",
  TenantAdmin: "/admin",
  Barber: "/login",
  Customer: "/login",
};

export default function Home() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) return;
    router.replace(user ? ROLE_HOME[user.role] ?? "/login" : "/login");
  }, [user, loading, router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-cream">
      <Spinner className="h-6 w-6 text-brass" />
    </div>
  );
}
