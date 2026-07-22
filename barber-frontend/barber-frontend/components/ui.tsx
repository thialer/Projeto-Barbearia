"use client";

import { ButtonHTMLAttributes, InputHTMLAttributes, LabelHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes, forwardRef } from "react";

export function Button({
  variant = "primary",
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "danger" | "ghost" }) {
  const base =
    "inline-flex items-center justify-center gap-2 rounded-md px-4 py-2.5 text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed";
  const variants: Record<string, string> = {
    primary: "bg-ink text-cream hover:bg-brass-dark",
    secondary: "bg-transparent border border-ink/20 text-ink hover:border-ink/50",
    danger: "bg-cancelled text-cream hover:opacity-90",
    ghost: "bg-transparent text-ink hover:bg-ink/5",
  };
  return <button className={`${base} ${variants[variant]} ${className}`} {...props} />;
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className = "", ...props }, ref) => (
    <input
      ref={ref}
      className={`w-full rounded-md border border-ink/15 bg-white px-3 py-2.5 text-sm text-ink placeholder:text-steel-light focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  )
);
Input.displayName = "Input";

export function Select({ className = "", ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select
      className={`w-full rounded-md border border-ink/15 bg-white px-3 py-2.5 text-sm text-ink focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  );
}

export function Textarea({ className = "", ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={`w-full rounded-md border border-ink/15 bg-white px-3 py-2.5 text-sm text-ink placeholder:text-steel-light focus:border-brass focus:outline-none ${className}`}
      {...props}
    />
  );
}

export function Label({ className = "", ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return <label className={`mb-1.5 block text-xs font-semibold uppercase tracking-wide text-steel ${className}`} {...props} />;
}

export function Card({ className = "", children }: { className?: string; children: React.ReactNode }) {
  return (
    <div className={`rounded-lg border border-ink/10 bg-white p-6 shadow-sm ${className}`}>
      {children}
    </div>
  );
}

export function Badge({ children, tone = "neutral" }: { children: React.ReactNode; tone?: "neutral" | "confirmed" | "cancelled" | "completed" | "noshow" }) {
  const tones: Record<string, string> = {
    neutral: "bg-ink/10 text-ink",
    confirmed: "bg-confirmed/10 text-confirmed",
    cancelled: "bg-cancelled/10 text-cancelled",
    completed: "bg-completed/10 text-completed",
    noshow: "bg-noshow/10 text-noshow",
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
    error: "bg-cancelled/10 text-cancelled border-cancelled/20",
    success: "bg-confirmed/10 text-confirmed border-confirmed/20",
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
