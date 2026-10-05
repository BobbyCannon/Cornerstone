using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Overlays;

namespace Cornerstone.Presentation.Headless;

/// <summary>
/// Headless unit test session that needs to be used by the actual testing framework.
/// All UI tests are supposed to be executed from one of the <see cref="Dispatch"/> methods to keep execution flow on the UI thread.
/// Disposing unit test session stops internal dispatcher loop.
/// </summary>
public sealed class HeadlessUnitTestSession : IDisposable, IAsyncDisposable
{
    private static readonly Dictionary<Assembly, HeadlessUnitTestSession> s_session = new();

    private readonly AppBuilder _appBuilder;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly BlockingCollection<(Action, ExecutionContext?)> _queue;
    private readonly Task _dispatchTask;
    private readonly bool _isolated;
    // Only set and used with PerAssembly isolation
    private SynchronizationContext? _sharedContext;

    internal const DynamicallyAccessedMemberTypes DynamicallyAccessed =
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

    private HeadlessUnitTestSession(
        AppBuilder appBuilder, CancellationTokenSource cancellationTokenSource,
        BlockingCollection<(Action, ExecutionContext?)> queue, Task dispatchTask,
        bool isolated)
    {
        _appBuilder = appBuilder;
        _cancellationTokenSource = cancellationTokenSource;
        _queue = queue;
        _dispatchTask = dispatchTask;
        _isolated = isolated;
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task Dispatch(Action action, CancellationToken cancellationToken)
    {
        return DispatchCore(() =>
        {
            action();
            return Task.FromResult(0);
        }, !_isolated, cancellationToken);
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task<TResult> Dispatch<TResult>(Func<TResult> action, CancellationToken cancellationToken)
    {
        return DispatchCore(() => Task.FromResult(action()), !_isolated, cancellationToken);
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task<TResult> Dispatch<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
    {
        return DispatchCore(action, !_isolated, cancellationToken);
    }

    /// <summary>
    /// Dispatch method queues an async operation on the dispatcher thread, creates a new application instance,
    /// setting app cornerstone services, and runs <paramref name="action"/> parameter.
    /// </summary>
    /// <param name="action">Action to execute on the dispatcher thread with cornerstone services.</param>
    /// <param name="captureExecutionContext">Whether dispatch should capture ExecutionContext.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <exception cref="ObjectDisposedException">
    /// If global session was already cancelled and thread killed, it's not possible to dispatch any actions again
    /// </exception>
    internal Task<TResult> DispatchCore<TResult>(Func<Task<TResult>> action, bool captureExecutionContext, CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            throw new ObjectDisposedException("Session was already disposed.");
        }

        var token = _cancellationTokenSource.Token;
        var executionContext = captureExecutionContext ? ExecutionContext.Capture() : null;

        var tcs = new TaskCompletionSource<TResult>();
        _queue.Add((() =>
        {
            var cts = new CancellationTokenSource();
            using var globalCts = token.Register(s => ((CancellationTokenSource)s!).Cancel(), cts, true);
            using var localCts = cancellationToken.Register(s => ((CancellationTokenSource)s!).Cancel(), cts, true);

            IDisposable application = null!;
            try
            {
                application = _isolated
                    ? EnsureIsolatedApplication()
                    : EnsureSharedApplication();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                return; // Exit the dispatcher action if application initialization fails
            }

            bool shouldCancel = false;
            Exception? caught = null;
            TResult result = default!;

            try
            {
                var task = action();
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    task.ContinueWith((_, s) =>
                            ((CancellationTokenSource)s!).Cancel(), cts,
                        TaskScheduler.FromCurrentSynchronizationContext());

                    if (cts.IsCancellationRequested)
                    {
                        shouldCancel = true;
                    }
                    else
                    {
                        var frame = new DispatcherFrame();
                        using var innerCts = cts.Token.Register(() => frame.Continue = false, true);
                        Dispatcher.UIThread.PushFrame(frame);
                        result = task.GetAwaiter().GetResult();
                    }
                }
                else
                {
                    result = task.GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                try
                {
                    application.Dispose();
                }
                catch (Exception ex)
                {
                    // Cleanup runs before the TCS is completed, so its failure must be
                    // reported by this work item instead of escaping the consumer loop.
                    caught = ex;
                }
            }

            if (caught != null)
                tcs.TrySetException(caught);
            else if (shouldCancel)
                tcs.TrySetCanceled(cts.Token);
            else
                tcs.TrySetResult(result);
        }, executionContext));
        return tcs.Task;
    }

    private IDisposable EnsureSharedApplication()
    {
        var oldContext = SynchronizationContext.Current;
        if (Application.Current is null)
        {
            _appBuilder.SetupUnsafe();
            _sharedContext = SynchronizationContext.Current;
        }
        else
        {
            SynchronizationContext.SetSynchronizationContext(_sharedContext);
        }

        return Disposable.Create(() =>
        {
            try
            {
                Dispatcher.UIThread.RunJobs();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        });
    }

    private IDisposable EnsureIsolatedApplication()
    {
        var scope = PresentationLocator.EnterScope();
        var oldContext = SynchronizationContext.Current is PresentationSynchronizationContext
            ? null
            : SynchronizationContext.Current;
        try
        {
            Dispatcher.ResetBeforeUnitTests();
            _appBuilder.SetupUnsafe();
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        return Disposable.Create(() =>
        {
            try
            {
                Application.Current?.DetachLifetimeHandlers();
                ((ToolTipService?)PresentationLocator.Current.GetService<IToolTipService>())?.Dispose();
                (PresentationLocator.Current.GetService<FontManager>() as IDisposable)?.Dispose();
                (PresentationLocator.Current.GetService<IInputManager>() as IDisposable)?.Dispose();
                PresentationHeadlessPlatform.ResetForUnitTests();
                Dispatcher.ResetForUnitTests();
                _appBuilder.Instance = null;
            }
            finally
            {
                // Cleanup jobs can throw, but the ambient state still belongs to this dispatch.
                try
                {
                    scope.Dispose();
                }
                finally
                {
                    Dispatcher.ResetBeforeUnitTests();
                    SynchronizationContext.SetSynchronizationContext(oldContext);
                }
            }
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _dispatchTask.Wait();
        _queue.CompleteAdding();
        _cancellationTokenSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await _dispatchTask.ConfigureAwait(false);
        _queue.CompleteAdding();
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Creates instance of <see cref="HeadlessUnitTestSession"/>.
    /// </summary>
    /// <param name="entryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildCornerstoneApp -> AppBuilder method or inherit Application.
    /// </param>
    public static HeadlessUnitTestSession StartNew(
        [DynamicallyAccessedMembers(DynamicallyAccessed)]
        Type entryPointType)
    {
        // Cannot be optional parameter for ABI stability
        // ReSharper disable once IntroduceOptionalParameters.Global
        return StartNew(entryPointType, PresentationTestIsolationLevel.PerTest);
    }

    /// <summary>
    /// Creates instance of <see cref="HeadlessUnitTestSession"/>.
    /// </summary>
    /// <param name="entryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildCornerstoneApp -> AppBuilder method or inherit Application.
    /// </param>
    /// <param name="isolationLevel">Defines the isolation level for headless unit tests</param>
    public static HeadlessUnitTestSession StartNew(
        [DynamicallyAccessedMembers(DynamicallyAccessed)]
        Type entryPointType,
        PresentationTestIsolationLevel isolationLevel)
    {
        var tcs = new TaskCompletionSource<HeadlessUnitTestSession>();
        var cancellationTokenSource = new CancellationTokenSource();
        var queue = new BlockingCollection<(Action, ExecutionContext?)>();

        Task? task = null;
        task = new Task(() =>
        {
            try
            {
                var appBuilder = AppBuilder.Configure(entryPointType);
                var runIsolated = isolationLevel == PresentationTestIsolationLevel.PerTest;

                // If windowing subsystem wasn't initialized by user, force headless with default parameters.
                if (appBuilder.WindowingSubsystemName != "Headless")
                {
                    appBuilder = appBuilder.UseHeadless(new PresentationHeadlessPlatformOptions());
                }

                if (appBuilder.TextShapingSubsystemInitializer is null)
                {
                    appBuilder = appBuilder.UseHarfBuzz();
                }

                // ReSharper disable once AccessToModifiedClosure
                tcs.SetResult(new HeadlessUnitTestSession(appBuilder, cancellationTokenSource, queue, task!, runIsolated));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var (action, executionContext) = queue.Take(cancellationTokenSource.Token);
                    if (executionContext is not null)
                    {
                        ExecutionContext.Run(executionContext, a => ((Action)a!).Invoke(), action);
                    }
                    else
                    {
                        action();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        }, TaskCreationOptions.DenyChildAttach);

        task.Start(TaskScheduler.Default);
        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a session from PresentationTestApplicationAttribute attribute or reuses any existing.
    /// If PresentationTestApplicationAttribute doesn't exist, empty application is used.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "PresentationTestApplicationAttribute attribute should preserve type information.")]
    public static HeadlessUnitTestSession GetOrStartForAssembly(Assembly? assembly)
    {
        assembly ??= typeof(HeadlessUnitTestSession).Assembly;

        lock (s_session)
        {
            if (!s_session.TryGetValue(assembly, out var session))
            {
                var appBuilderEntryPointType = assembly.GetCustomAttribute<PresentationTestApplicationAttribute>()
                    ?.AppBuilderEntryPointType;

                var isolationLevel = assembly.GetCustomAttribute<PresentationTestIsolationAttribute>()
                    ?.IsolationLevel ?? PresentationTestIsolationLevel.PerTest;

                session = appBuilderEntryPointType is not null ?
                    StartNew(appBuilderEntryPointType, isolationLevel) :
                    StartNew(typeof(Application), isolationLevel);

                s_session.Add(assembly, session);
            }

            return session;
        }
    }
}
