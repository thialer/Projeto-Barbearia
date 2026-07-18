using System.Diagnostics.Metrics;

namespace BarberBooking.Api.Services;

public sealed class BookingMetrics
{
    private static readonly Meter Meter = new("BarberBooking.Api.Booking");
    private static readonly Counter<long> Created = Meter.CreateCounter<long>("appointments.created");
    private static readonly Counter<long> Conflicts = Meter.CreateCounter<long>("appointments.conflicts");
    private static readonly Counter<long> Cancelled = Meter.CreateCounter<long>("appointments.cancelled");

    public void AppointmentCreated() => Created.Add(1);
    public void AppointmentConflict() => Conflicts.Add(1);
    public void AppointmentCancelled() => Cancelled.Add(1);
}
