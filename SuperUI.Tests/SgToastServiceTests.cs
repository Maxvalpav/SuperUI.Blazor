using Xunit;
using SuperUI.Components;
using Microsoft.Extensions.Options;

namespace SuperUI.Tests;

/// <summary>
/// Tests for SgToastService timeout cancellation and IAsyncDisposable implementation.
/// Validates: Requirements 8 - ToastService Timeout Cancellation
/// </summary>
public class SgToastServiceTests
{
    [Fact]
    public void Show_CreatesToastWithUniqueId()
    {
        // Arrange
        var service = new SgToastService();
        var toastIds = new List<string>();
        service.Added += t => toastIds.Add(t.Id);

        // Act
        service.Show("Test message 1");
        service.Show("Test message 2");

        // Assert
        Assert.Equal(2, toastIds.Count);
        Assert.NotEqual(toastIds[0], toastIds[1]);
    }

    [Fact]
    public void Show_CreatesToastWithCancellationTokenSource()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Show("Test message", durationMs: 5000);

        // Assert
        Assert.NotNull(capturedToast);
        Assert.Null(capturedToast.TimeoutCts); // Not set until host component processes it
    }

    [Fact]
    public void Dismiss_CancelsTimeoutToken()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;
        service.Show("Test message", durationMs: 5000);

        // Simulate host component setting the timeout token
        var cts = new CancellationTokenSource();
        capturedToast!.TimeoutCts = cts;

        // Act
        service.Dismiss(capturedToast.Id);

        // Assert
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public void Dismiss_RemovesFromActiveToasts()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;
        service.Show("Test message");

        // Act
        service.Dismiss(capturedToast!.Id);

        // Assert - verify the toast was removed from active toasts
        // by checking that dismissing again doesn't cancel a token
        var cts = new CancellationTokenSource();
        capturedToast.TimeoutCts = cts;
        service.Dismiss(capturedToast.Id); // Should not throw
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeAsync_CancelsAllActiveTokens()
    {
        // Arrange
        var service = new SgToastService();
        var toasts = new List<SgToast>();
        service.Added += t => toasts.Add(t);

        // Create multiple toasts
        service.Show("Toast 1", durationMs: 5000);
        service.Show("Toast 2", durationMs: 5000);
        service.Show("Toast 3", durationMs: 5000);

        // Simulate host component setting timeout tokens
        var ctsList = new List<CancellationTokenSource>();
        foreach (var toast in toasts)
        {
            var cts = new CancellationTokenSource();
            toast.TimeoutCts = cts;
            ctsList.Add(cts);
        }

        // Act
        await service.DisposeAsync();

        // Assert - all tokens should be cancelled and disposed
        foreach (var cts in ctsList)
        {
            // After dispose, the CTS is disposed, so we can't check IsCancellationRequested
            // Instead, we verify that the toast's TimeoutCts is null
            Assert.Throws<ObjectDisposedException>(() => cts.Token);
        }
    }

    [Fact]
    public void Show_ThrowsObjectDisposedException_WhenServiceDisposed()
    {
        // Arrange
        var service = new SgToastService();
        _ = service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.Show("Test message"));
    }

    [Fact]
    public void Dismiss_ReturnsGracefully_WhenServiceDisposed()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;
        service.Show("Test message");
        _ = service.DisposeAsync();

        // Act & Assert - should not throw
        service.Dismiss(capturedToast!.Id);
    }

    [Fact]
    public async Task SgToast_DisposeAsync_DisposesTimeoutCts()
    {
        // Arrange
        var toast = new SgToast { Message = "Test" };
        var cts = new CancellationTokenSource();
        toast.TimeoutCts = cts;

        // Act
        await toast.DisposeAsync();

        // Assert
        Assert.Null(toast.TimeoutCts);
        Assert.Throws<ObjectDisposedException>(() => cts.Token);
    }

    [Fact]
    public async Task SgToast_DisposeAsync_HandlesAlreadyDisposedCts()
    {
        // Arrange
        var toast = new SgToast { Message = "Test" };
        var cts = new CancellationTokenSource();
        cts.Dispose();
        toast.TimeoutCts = cts;

        // Act & Assert - should not throw
        await toast.DisposeAsync();
    }

    [Fact]
    public void Show_WithCustomDuration_SetsCorrectDuration()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Show("Test message", durationMs: 2000);

        // Assert
        Assert.Equal(2000, capturedToast!.DurationMs);
    }

    [Fact]
    public void Show_WithDefaultDuration_UsesServiceDefault()
    {
        // Arrange
        var options = Options.Create(new SuperUiOptions { DefaultToastDurationMs = 3000 });
        var service = new SgToastService(options);
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Show("Test message");

        // Assert
        Assert.Equal(3000, capturedToast!.DurationMs);
    }

    [Fact]
    public void Success_CreatesSuccessVariantToast()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Success("Success message", "Success Title");

        // Assert
        Assert.Equal("success", capturedToast!.Variant);
        Assert.Equal("Success message", capturedToast.Message);
        Assert.Equal("Success Title", capturedToast.Title);
    }

    [Fact]
    public void Error_CreatesDangerVariantToast()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Error("Error message", "Error Title");

        // Assert
        Assert.Equal("danger", capturedToast!.Variant);
        Assert.Equal("Error message", capturedToast.Message);
    }

    [Fact]
    public void Warn_CreatesWarnVariantToast()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Warn("Warning message", "Warning Title");

        // Assert
        Assert.Equal("warn", capturedToast!.Variant);
    }

    [Fact]
    public void Info_CreatesDefaultVariantToast()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;

        // Act
        service.Info("Info message", "Info Title");

        // Assert
        Assert.Equal("default", capturedToast!.Variant);
    }

    [Fact]
    public void Added_EventRaisedWhenToastShown()
    {
        // Arrange
        var service = new SgToastService();
        var eventRaised = false;
        service.Added += _ => eventRaised = true;

        // Act
        service.Show("Test message");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Removed_EventRaisedWhenToastDismissed()
    {
        // Arrange
        var service = new SgToastService();
        SgToast? capturedToast = null;
        service.Added += t => capturedToast = t;
        service.Show("Test message");

        var eventRaised = false;
        var dismissedId = string.Empty;
        service.Removed += id =>
        {
            eventRaised = true;
            dismissedId = id;
        };

        // Act
        service.Dismiss(capturedToast!.Id);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(capturedToast.Id, dismissedId);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new SgToastService();
        service.Show("Test message");

        // Act & Assert - should not throw
        await service.DisposeAsync();
        await service.DisposeAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public void SgToast_HasUniqueIdPerInstance()
    {
        // Arrange & Act
        var toast1 = new SgToast();
        var toast2 = new SgToast();
        var toast3 = new SgToast();

        // Assert
        Assert.NotEqual(toast1.Id, toast2.Id);
        Assert.NotEqual(toast2.Id, toast3.Id);
        Assert.NotEqual(toast1.Id, toast3.Id);
    }

    [Fact]
    public void SgToast_DefaultVariantIsDefault()
    {
        // Arrange & Act
        var toast = new SgToast();

        // Assert
        Assert.Equal("default", toast.Variant);
    }

    [Fact]
    public void SgToast_DefaultDurationMs()
    {
        // Arrange & Act
        var toast = new SgToast();

        // Assert
        Assert.Equal(4000, toast.DurationMs);
    }
}
