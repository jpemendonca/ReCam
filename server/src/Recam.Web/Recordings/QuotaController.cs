using Recam.Web.Api;

namespace Recam.Web.Recordings;

public enum QuotaState
{
    Loading,
    Ready,
    Failed,
}

/// <summary>The "Recordings space" slider: how much disk all recordings may take. Same rules as the app.</summary>
public sealed class QuotaController(IRecamApi api)
{
    public const int MinimumMegabytes = 100;

    /// <summary>A camera sends at most 700 kbps: about 300 MB per hour of recording.</summary>
    public const int MegabytesPerCameraHour = 300;

    public QuotaState State { get; private set; } = QuotaState.Loading;

    public QuotaInfo? Quota { get; private set; }

    /// <summary>Where the slider is; saved only by <see cref="SaveAsync"/>.</summary>
    public int SelectedMegabytes { get; private set; }

    /// <summary>The slider's top: what the disk allows, never below the minimum.</summary>
    public int MaxMegabytes => Math.Max(Quota?.MaxMegabytes ?? MinimumMegabytes, MinimumMegabytes);

    public bool? Saved { get; private set; }

    public bool Saving { get; private set; }

    public event Action? Changed;

    /// <summary>Hours the space holds while <paramref name="cameras"/> record at once (at least one).</summary>
    public static double HoursFor(int megabytes, int cameras) =>
        (double)megabytes / MegabytesPerCameraHour / Math.Max(cameras, 1);

    public async Task LoadAsync()
    {
        State = QuotaState.Loading;
        Changed?.Invoke();
        try
        {
            Quota = await api.GetQuotaAsync(CancellationToken.None);
            SelectedMegabytes = Quota.QuotaMb;
            State = QuotaState.Ready;
        }
        catch (HttpRequestException)
        {
            State = QuotaState.Failed;
        }

        Changed?.Invoke();
    }

    /// <summary>Moves the slider, kept between the minimum and what the disk allows.</summary>
    public void Select(int megabytes)
    {
        SelectedMegabytes = Math.Clamp(megabytes, MinimumMegabytes, MaxMegabytes);
        Saved = null;
        Changed?.Invoke();
    }

    public async Task SaveAsync()
    {
        Saving = true;
        Changed?.Invoke();
        try
        {
            await api.SetQuotaAsync(SelectedMegabytes, CancellationToken.None);
            Quota = Quota! with { QuotaMb = SelectedMegabytes };
            Saved = true;
        }
        catch (HttpRequestException)
        {
            Saved = false;
        }
        finally
        {
            Saving = false;
            Changed?.Invoke();
        }
    }
}
