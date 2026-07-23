// Cliente de API — cobre todas as rotas do BarberBooking.Api
// Base URL vem de NEXT_PUBLIC_API_URL (definida no .env.local)

// Por padrão, a aplicação local do backend roda em http://localhost:5000
const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export type UserRole = "SuperAdmin" | "TenantAdmin" | "Barber" | "Customer";

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  tenantId: string | null;
  mustChangePassword: boolean;
}

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  phone: string | null;
  address: string | null;
  timeZoneId: string;
  cancellationLimitMinutes: number;
  isActive?: boolean;
  createdAtUtc?: string;
}

export interface Service {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  price: number;
  durationMinutes: number;
  isActive: boolean;
}

export interface BarberSummary {
  id: string;
  name: string;
  bio: string | null;
}

export interface WorkingHour {
  id?: string;
  dayOfWeek: number; // 0=Domingo ... 6=Sábado
  start: string; // "HH:mm:ss"
  end: string; // "HH:mm:ss"
}

// Status: 0=Confirmed, 1=Cancelled, 2=Completed, 3=NoShow
export type AppointmentStatus = 0 | 1 | 2 | 3;

export interface Appointment {
  id: string;
  tenantId: string;
  customerId: string;
  customer?: { id: string; name: string; email: string };
  barberId: string;
  barber?: { id: string; user?: { name: string } };
  serviceId: string;
  service?: { id: string; name: string; price: number; durationMinutes: number };
  startAtUtc: string;
  endAtUtc: string;
  status: AppointmentStatus;
  notes: string | null;
  createdAtUtc: string;
}

export const DAYS_OF_WEEK = [
  { value: 0, label: "Domingo" },
  { value: 1, label: "Segunda-feira" },
  { value: 2, label: "Terça-feira" },
  { value: 3, label: "Quarta-feira" },
  { value: 4, label: "Quinta-feira" },
  { value: 5, label: "Sexta-feira" },
  { value: 6, label: "Sábado" },
];

export const APPOINTMENT_STATUS_LABELS: Record<number, string> = {
  0: "Confirmado",
  1: "Cancelado",
  2: "Concluído",
  3: "Não compareceu",
};

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("token");
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    "Accept": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers ?? {}),
  };

  let res: Response;
  try {
    res = await fetch(`${API_URL}${path}`, { ...options, headers });
  } catch (e) {
    // Falha de rede ou CORS
    throw new ApiError(
      "Não foi possível conectar à API. Verifique se o backend está rodando e se o CORS está configurado.",
      0
    );
  }

  if (res.status === 204) return undefined as T;

  const contentType = res.headers.get("content-type") ?? "";
  const data = contentType.includes("application/json") ? await res.json() : null;

  if (!res.ok) {
    const message = (data && (data.message || data.error || data.title)) ?? `Erro ${res.status}`;
    throw new ApiError(message, res.status);
  }

  return data as T;
}

export const api = {
  auth: {
    login: (email: string, password: string) =>
      request<LoginResponse>("/api/auth/login", {
        method: "POST",
        credentials: "include",
        body: JSON.stringify({ email, password }),
      }),
    changePassword: (currentPassword: string, newPassword: string) =>
      request<void>("/api/auth/change-password", {
        method: "POST",
        body: JSON.stringify({ currentPassword, newPassword }),
      }),
  },

  superAdmin: {
    listTenants: () =>
      request<
        { id: string; name: string; slug: string; isActive: boolean; createdAtUtc: string }[]
      >("/api/super-admin/tenants"),
    createTenant: (input: {
      name: string;
      slug: string;
      adminName: string;
      adminEmail: string;
      phone?: string;
      address?: string;
    }) =>
      request<{
        id: string;
        name: string;
        slug: string;
        adminEmail: string;
        temporaryPassword: string;
        adminPanelUrl: string;
        publicUrl: string;
      }>("/api/super-admin/tenants", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    deleteTenant: (tenantId: string) =>
      request<void>(`/api/super-admin/tenants/${tenantId}`, { method: "DELETE" }),
  },

  admin: {
    updateSettings: (input: {
      name: string;
      phone?: string;
      address?: string;
      timeZoneId: string;
      cancellationLimitMinutes: number;
    }) =>
      request<void>("/api/admin/settings", {
        method: "PUT",
        body: JSON.stringify(input),
      }),
    listServices: () => request<Service[]>("/api/admin/services"),
    getSettings: () => request<{
      name: string;
      phone: string | null;
      address: string | null;
      timeZoneId: string;
      cancellationLimitMinutes: number;
      slotIntervalMinutes?: number | null;
    }>("/api/admin/settings"),
    createService: (input: {
      name: string;
      description?: string;
      price: number;
      durationMinutes: number;
    }) =>
      request<Service>("/api/admin/services", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    createBarber: (input: { name: string; email: string; password: string; bio?: string }) =>
      request<{ id: string; email: string }>("/api/admin/barbers", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    listBarbers: () => request<{ id: string; name: string; email: string; bio: string | null; isActive: boolean }[]>("/api/admin/barbers"),
    deleteBarber: (barberId: string) =>
      request<void>(`/api/admin/barbers/${barberId}`, { method: "DELETE" }),
    setBarberServices: (barberId: string, serviceIds: string[]) =>
      request<void>(`/api/admin/barbers/${barberId}/services`, {
        method: "PUT",
        body: JSON.stringify({ serviceIds }),
      }),
    setBarberWorkingHours: (barberId: string, hours: WorkingHour[]) =>
      request<void>(`/api/admin/barbers/${barberId}/working-hours`, {
        method: "PUT",
        body: JSON.stringify({ hours }),
      }),
    listAppointments: (date?: string) =>
      request<Appointment[]>(`/api/admin/appointments${date ? `?date=${date}` : ""}`),
  },

  public: {
    getTenant: (slug: string) => request<Tenant>(`/api/public/${slug}`),
    listServices: (slug: string) => request<Service[]>(`/api/public/${slug}/services`),
    listBarbers: (slug: string) => request<BarberSummary[]>(`/api/public/${slug}/barbers`),
    getAvailability: (slug: string, barberId: string, serviceId: string, date: string) =>
      request<string[]>(
        `/api/public/${slug}/availability?barberId=${barberId}&serviceId=${serviceId}&date=${date}`
      ),
    register: (slug: string, input: { name: string; email: string; password: string }) =>
      request<{ accessToken: string }>(`/api/public/${slug}/register`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    createAppointment: (
      slug: string,
      input: { barberId: string; serviceId: string; startAt: string; notes?: string }
    ) =>
      request<Appointment>(`/api/public/${slug}/appointments`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    myAppointments: (slug: string) =>
      request<Appointment[]>(`/api/public/${slug}/my-appointments`),
    cancelAppointment: (slug: string, id: string) =>
      request<void>(`/api/public/${slug}/appointments/${id}/cancel`, { method: "POST" }),
    rescheduleAppointment: (
      slug: string,
      id: string,
      input: { barberId: string; startAt: string }
    ) =>
      request<Appointment>(`/api/public/${slug}/appointments/${id}/reschedule`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
  },
};
