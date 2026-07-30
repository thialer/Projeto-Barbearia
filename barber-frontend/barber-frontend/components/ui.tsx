"use client";

import { ButtonHTMLAttributes, InputHTMLAttributes, LabelHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes, forwardRef } from "react";

export function Button({
  variant = "primary",
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "danger" | "ghost" }) {
  const base =
    "inline-flex items-center justify-center gap-2 rounded-md px-4 py-2.5 text-sm font-semibold transition-all duration-200 disabled:cursor-not-allowed disabled:opacity-50";
  const variants: Record<string, string> = {
    primary: "bg-brass text-[#17120e] shadow-[0_8px_22px_rgba(217,167,102,.18)] hover:bg-brass-light hover:-translate-y-0.5",
    secondary: "border border-brass/50 bg-brass/5 text-brass-light hover:border-brass hover:bg-brass/15",
    danger: "bg-cancelled text-[#281414] hover:brightness-110",
    ghost: "bg-transparent text-ink hover:bg-white/5",
  };
  return <button className={`${base} ${variants[variant]} ${className}`} {...props} />;
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className = "", ...props }, ref) => (
    <input
      ref={ref}
      className={`w-full rounded-md border border-white/10 bg-surface-input px-3 py-2.5 text-sm text-ink shadow-inner shadow-black/10 placeholder:text-steel-light transition-colors focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  )
);
Input.displayName = "Input";

export function Select({ className = "", ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select
      className={`w-full rounded-md border border-white/10 bg-surface-input px-3 py-2.5 text-sm text-ink shadow-inner shadow-black/10 transition-colors focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  );
}

export function Textarea({ className = "", ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={`w-full rounded-md border border-white/10 bg-surface-input px-3 py-2.5 text-sm text-ink shadow-inner shadow-black/10 placeholder:text-steel-light transition-colors focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  );
}

export function Label({ className = "", ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return <label className={`mb-1.5 block text-[11px] font-semibold uppercase tracking-[0.14em] text-steel ${className}`} {...props} />;
}

export function Card({ className = "", children }: { className?: string; children: React.ReactNode }) {
  return (
    <div className={`rounded-lg border border-white/10 bg-surface p-6 shadow-[0_16px_40px_rgba(0,0,0,.2)] ${className}`}>
      {children}
    </div>
  );
}

export function Badge({ children, tone = "neutral" }: { children: React.ReactNode; tone?: "neutral" | "confirmed" | "cancelled" | "completed" | "noshow" }) {
  const tones: Record<string, string> = {
    neutral: "bg-white/10 text-ink",
    confirmed: "bg-confirmed/15 text-confirmed",
    cancelled: "bg-cancelled/15 text-cancelled",
    completed: "bg-completed/15 text-completed",
    noshow: "bg-noshow/15 text-noshow",
  };
  return (
    <span className={`inline-block rounded-full px-2.5 py-1 text-xs font-semibold ${tones[tone]}`}>
      {children}
    </span>
  );
}

export function Alert({
  children,
  tone = "error",
  className = "",
}: {
  children: React.ReactNode;
  tone?: "error" | "success";
  className?: string;
}) {
  const tones = {
    error: "bg-cancelled/10 text-cancelled border-cancelled/30",
    success: "bg-confirmed/10 text-confirmed border-confirmed/30",
  };
  return <div className={`rounded-md border px-4 py-3 text-sm ${tones[tone]} ${className}`}>{children}</div>;
}

export function Spinner({ className = "" }: { className?: string }) {
  return (
    <svg className={`animate-spin ${className}`} viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );
}
