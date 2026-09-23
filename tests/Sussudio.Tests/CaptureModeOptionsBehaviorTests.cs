using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class CaptureModeOptionsBehaviorTests
{
    public CaptureModeOptionsBehaviorTests()
        => global::Program.EnsureTargetAssemblyLoadedForXUnit();

    [Fact]
    public void AutomaticResolutionPreservesIntentUntilAUserChangesResolution()
    {
        var owner = new SelectionFixture();
        owner.SetField("_hasUserOverriddenFrameRateForCurrentMode", true);
        owner.SetField("_forceSourceAutoRetarget", true);
        owner.ApplyModeOptions(() => owner.ApplyModeOptions(() => owner.Set("SelectedResolution", "1920x1080")));

        Assert.Equal("1920x1080", owner.Get<string>("SelectedResolution"));
        Assert.False(owner.Flag("_hasUserOverriddenResolutionForCurrentMode"));
        Assert.True(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
        Assert.True(owner.Flag("_forceSourceAutoRetarget"));
        Assert.True(owner.Flag("_pendingSdrAutoSelectionForDeviceChange"));
        Assert.Equal(60, owner.Field("_pendingSdrAutoFriendlyFrameRateBucket"));

        owner.Set("SelectedResolution", "3840x2160");

        Assert.True(owner.Flag("_hasUserOverriddenResolutionForCurrentMode"));
        Assert.False(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
        Assert.False(owner.Flag("_forceSourceAutoRetarget"));
        Assert.False(owner.Flag("_pendingSdrAutoSelectionForDeviceChange"));
        Assert.Null(owner.Field("_pendingSdrAutoFriendlyFrameRateBucket"));
        Assert.False(owner.Rebuilding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticFrameRatePreservesIncomingStateAndExactTiming(bool alreadyAutomatic)
    {
        var owner = new SelectionFixture();
        var option = owner.AddFrameRate(60, 60000d / 1001d, "60000/1001");
        owner.SetField("_isApplyingAutomaticFrameRateSelection", alreadyAutomatic);

        owner.ApplyFrameRate(option, 60);

        Assert.Equal(alreadyAutomatic, owner.AutomaticFrameRate);
        Assert.False(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
        Assert.True(owner.Get<bool>("IsAutoFrameRateSelected"));
        Assert.True(owner.Flag("_pendingSdrAutoSelectionForDeviceChange"));
        Assert.Equal(60000d / 1001d, owner.Get<double>("SelectedFrameRate"));
        Assert.Equal(60d, owner.Get<double?>("SelectedFriendlyFrameRate"));
        Assert.Equal(60000d / 1001d, owner.Get<double?>("SelectedExactFrameRate"));
        Assert.Equal("60000/1001", owner.Get<string>("SelectedExactFrameRateArg"));

        owner.SetField("_isApplyingAutomaticFrameRateSelection", false);
        owner.Set("SelectedFrameRate", 120d);
        Assert.True(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
        Assert.False(owner.Get<bool>("IsAutoFrameRateSelected"));
        Assert.False(owner.Flag("_pendingSdrAutoSelectionForDeviceChange"));
        Assert.Null(owner.Field("_pendingSdrAutoFriendlyFrameRateBucket"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NestedFrameRateNotificationsRestoreOuterAutomaticSelection(bool alreadyAutomatic, bool failInner)
    {
        var owner = new SelectionFixture();
        owner.SetField("_isApplyingAutomaticFrameRateSelection", alreadyAutomatic);
        var failure = new InvalidOperationException("nested selection observer failed");
        var nested = false;
        var outerNotificationObserved = false;
        ((INotifyPropertyChanged)owner.ViewModel).PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != "SelectedFrameRate")
            {
                return;
            }

            Assert.True(owner.AutomaticFrameRate);
            if (nested)
            {
                if (failInner && owner.Get<double>("SelectedFrameRate") == 120)
                {
                    throw failure;
                }
                return;
            }

            nested = true;
            outerNotificationObserved = true;
            var observed = Record.Exception(() => owner.ApplyFrameRate(null, 120));
            Assert.Same(failInner ? failure : null, observed);
            Assert.True(owner.AutomaticFrameRate);
            owner.ApplyFrameRate(null, 144);
            Assert.True(owner.AutomaticFrameRate);
        };

        owner.ApplyFrameRate(null, 30);

        Assert.True(outerNotificationObserved);
        Assert.Equal(alreadyAutomatic, owner.AutomaticFrameRate);
        Assert.False(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
        Assert.True(owner.Get<bool>("IsAutoFrameRateSelected"));
    }

    [Fact]
    public void ResolutionRebuildFinishesItsScopeBeforeRebuildingFrameRates()
    {
        var owner = new SelectionFixture();
        owner.AddFormat(1280, 720, 60, "NV12");
        owner.AddFormat(1920, 1080, 60, "NV12");
        owner.SetField("_isApplyingAutomaticFrameRateSelection", true);
        var events = new List<string>();
        ((INotifyCollectionChanged)owner.Get<object>("AvailableResolutions")).CollectionChanged += (_, _) =>
        {
            events.Add("resolutions");
            Assert.True(owner.Rebuilding);
            owner.ApplyModeOptions(() => Assert.True(owner.Rebuilding));
            Assert.True(owner.Rebuilding);
        };
        ((INotifyCollectionChanged)owner.Get<object>("AvailableFrameRates")).CollectionChanged += (_, _) =>
        {
            events.Add("rates");
            Assert.True(owner.Rebuilding);
        };
        var original = (Action<double?>)SelectionFixture.Property(owner.OptionContext, "SetDetectedSourceFrameRate").GetValue(owner.OptionContext)!;
        SelectionFixture.Property(owner.OptionContext, "SetDetectedSourceFrameRate").SetValue(owner.OptionContext,
            new Action<double?>(value =>
            {
                events.Add("rate-source");
                Assert.False(owner.Rebuilding);
                original(value);
            }));

        owner.RebuildResolutions();

        Assert.Contains("resolutions", events);
        Assert.Contains("rates", events);
        Assert.True(events.LastIndexOf("resolutions") < events.IndexOf("rate-source"));
        Assert.True(events.IndexOf("rate-source") < events.IndexOf("rates"));
        Assert.False(owner.Rebuilding);
        Assert.True(owner.AutomaticFrameRate);
        Assert.False(owner.Flag("_hasUserOverriddenResolutionForCurrentMode"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolutionCollectionFailureRestoresIncomingScopeAndStopsDependentRebuild(bool alreadyRebuilding)
    {
        var owner = new SelectionFixture();
        owner.AddFormat(1920, 1080, 60, "NV12");
        owner.SetField("_isRebuildingModeOptions", alreadyRebuilding);
        var failure = new InvalidOperationException("resolution collection observer failed");
        var dependentChanges = 0;
        ((INotifyCollectionChanged)owner.Get<object>("AvailableResolutions")).CollectionChanged += (_, _) =>
        {
            Assert.True(owner.Rebuilding);
            throw failure;
        };
        ((INotifyCollectionChanged)owner.Get<object>("AvailableFrameRates")).CollectionChanged += (_, _) => dependentChanges++;

        var observed = Assert.Throws<InvalidOperationException>(owner.RebuildResolutions);

        Assert.Same(failure, observed);
        Assert.Equal(alreadyRebuilding, owner.Rebuilding);
        Assert.Equal(0, dependentChanges);
        Assert.False(owner.Flag("_suppressFormatChangeReinitialize"));
    }

    [Fact]
    public void SdrRetargetAppliesResolutionThenSuppressedRatesThenOneRenegotiation()
    {
        var owner = new SelectionFixture();
        var mjpg = owner.AddFormat(1280, 720, 60, "MJPG");
        var nv12 = owner.AddFormat(1920, 1080, 60, "NV12");
        owner.Set("SelectedFormat", mjpg);
        var target = SelectionFixture.Create("Sussudio.Models.CaptureDevice");
        var supported = (IList)SelectionFixture.Property(target, "SupportedFormats").GetValue(target)!;
        supported.Add(mjpg);
        supported.Add(nv12);
        var applier = owner.CreateRetargetApplier();
        var context = SelectionFixture.InstanceField(applier, "_context").GetValue(applier)!;
        var events = new List<string>();
        ((INotifyPropertyChanged)owner.ViewModel).PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "SelectedResolution")
            {
                events.Add("resolution");
                Assert.True(owner.Rebuilding);
                Assert.False(owner.Flag("_suppressFormatChangeReinitialize"));
            }
        };
        var rebuild = (Action)SelectionFixture.Property(context, "RebuildFrameRateOptions").GetValue(context)!;
        SelectionFixture.Property(context, "RebuildFrameRateOptions").SetValue(context, new Action(() =>
        {
            events.Add("rates");
            Assert.False(owner.Rebuilding);
            Assert.True(owner.Flag("_suppressFormatChangeReinitialize"));
            rebuild();
        }));
        SelectionFixture.Property(context, "EnqueueUiOperation").SetValue(context,
            new Func<Func<Task>, string, bool>((_, name) =>
            {
                events.Add(name);
                Assert.False(owner.Rebuilding);
                Assert.False(owner.Flag("_suppressFormatChangeReinitialize"));
                return true;
            }));

        var applied = SelectionFixture.Call(applier.GetType().GetMethod("TryApplyDeviceFormatProbeRetarget")!, applier,
            target, true, true, "1280x720", 60d, false);

        Assert.Equal(true, applied);
        Assert.Equal(new[] { "resolution", "rates", "format probe sdr retarget" }, events);
        Assert.Equal("1920x1080", owner.Get<string>("SelectedResolution"));
        Assert.Equal(60d, owner.Get<double>("SelectedFrameRate"));
        Assert.False(owner.Flag("_hasUserOverriddenResolutionForCurrentMode"));
        Assert.False(owner.Flag("_hasUserOverriddenFrameRateForCurrentMode"));
    }

    private sealed class SelectionFixture
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly object _optionController;
        private readonly Type _graph;
        private readonly IDictionary _formatsByResolution;

        public SelectionFixture()
        {
            var type = Require("Sussudio.ViewModels.MainViewModel");
            ViewModel = RuntimeHelpers.GetUninitializedObject(type);
            foreach (var name in new[] { "AvailableFormats", "AvailableFrameRates", "AvailableResolutions", "AvailableVideoFormats" })
            {
                Set(name, Activator.CreateInstance(Property(ViewModel, name).PropertyType));
            }
            var formatsField = InstanceField(ViewModel, "_resolutionToFormats");
            _formatsByResolution = (IDictionary)Activator.CreateInstance(formatsField.FieldType)!;
            formatsField.SetValue(ViewModel, _formatsByResolution);
            var telemetry = Require("Sussudio.Models.SourceSignalTelemetrySnapshot").GetMethod("CreateUnavailable")!
                .Invoke(null, new object?[] { "selection-test", null })!;
            SetField("_latestSourceTelemetry", telemetry);

            var timingContext = Create("Sussudio.Controllers.MainViewModelFrameRateTimingResolverContext");
            var runtime = Create("Sussudio.Models.CaptureRuntimeSnapshot");
            SetGetter(timingContext, "GetResolutionToFormats", () => _formatsByResolution);
            SetGetter(timingContext, "GetRuntimeSnapshot", () => runtime);
            SetGetter(timingContext, "GetLatestSourceTelemetry", () => telemetry);
            SetGetter(timingContext, "GetSelectedFormat", () => Get<object?>("SelectedFormat"));
            Property(timingContext, "AvailableFrameRates").SetValue(timingContext, Get<object>("AvailableFrameRates"));
            SetField("_frameRateTimingResolver", Activator.CreateInstance(
                Require("Sussudio.Controllers.MainViewModelFrameRateTimingResolver"), timingContext)!);
            _graph = type.GetNestedType("MainViewModelControllerGraph", BindingFlags.NonPublic)!;
            _optionController = Call(_graph.GetMethod("CreateCaptureModeOptionRebuildController", BindingFlags.Static | BindingFlags.NonPublic)!, null, ViewModel)!;
            SetField("_captureModeOptionRebuildController", _optionController);
            ApplyModeOptions = type.GetMethod("ApplyCaptureModeOptions", Instance)!.CreateDelegate<Action<Action>>(ViewModel);
            Set("SelectedVideoFormat", "Auto");
            ApplyModeOptions(() => Set("SelectedResolution", "1280x720"));
            ApplyFrameRate(null, 60);
            SetField("_isAutoFrameRateSelected", true);
            SetField("_hasUserOverriddenFrameRateForCurrentMode", false);
            SetField("_pendingSdrAutoSelectionForDeviceChange", true);
            SetField("_pendingSdrAutoFriendlyFrameRateBucket", 60);
        }

        public object ViewModel { get; }
        public Action<Action> ApplyModeOptions { get; }
        public bool Rebuilding => Flag("_isRebuildingModeOptions");
        public bool AutomaticFrameRate => Flag("_isApplyingAutomaticFrameRateSelection");
        public object OptionContext => InstanceField(_optionController, "_context").GetValue(_optionController)!;
        public T Get<T>(string name) => (T)Property(ViewModel, name).GetValue(ViewModel)!;
        public void Set(string name, object? value) => Call(Property(ViewModel, name).GetSetMethod(true)!, ViewModel, value);
        public object? Field(string name) => InstanceField(ViewModel, name).GetValue(ViewModel);
        public bool Flag(string name) => (bool)Field(name)!;
        public void SetField(string name, object? value) => InstanceField(ViewModel, name).SetValue(ViewModel, value);
        public void ApplyFrameRate(object? option, double fallback)
            => Call(ViewModel.GetType().GetMethod("ApplyResolvedFrameRateSelection", Instance)!, ViewModel, option, fallback);
        public void RebuildResolutions()
            => Call(_optionController.GetType().GetMethod("RebuildResolutionOptions")!, _optionController);

        public object AddFormat(uint width, uint height, double rate, string pixelFormat)
        {
            var format = Create("Sussudio.Models.MediaFormat");
            foreach (var (name, value) in new (string, object)[] { ("Width", width), ("Height", height), ("FrameRate", rate), ("PixelFormat", pixelFormat) })
            {
                Property(format, name).SetValue(format, value);
            }
            var key = $"{width}x{height}";
            if (!_formatsByResolution.Contains(key))
            {
                _formatsByResolution.Add(key, Activator.CreateInstance(typeof(List<>).MakeGenericType(format.GetType()))!);
            }
            ((IList)_formatsByResolution[key]!).Add(format);
            Get<IList>("AvailableFormats").Add(format);
            return format;
        }

        public object AddFrameRate(double friendly, double exact, string rational)
        {
            var option = Create("Sussudio.Models.FrameRateOption");
            Property(option, "FriendlyValue").SetValue(option, friendly);
            Property(option, "Value").SetValue(option, exact);
            Property(option, "Rational").SetValue(option, rational);
            Property(option, "IsEnabled").SetValue(option, true);
            Get<IList>("AvailableFrameRates").Add(option);
            return option;
        }

        public object CreateRetargetApplier()
        {
            var probe = Call(_graph.GetMethod("CreateDeviceFormatProbeController", BindingFlags.Static | BindingFlags.NonPublic)!, null, ViewModel)!;
            return InstanceField(probe, "_retargetApplier").GetValue(probe)!;
        }

        public static object Create(string name) => Activator.CreateInstance(Require(name))!;
        private static Type Require(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
        public static PropertyInfo Property(object owner, string name) => owner.GetType().GetProperty(name, Instance)!;
        public static FieldInfo InstanceField(object owner, string name) => owner.GetType().GetField(name, Instance)!;

        private static void SetGetter(object context, string name, Func<object?> getter)
        {
            var property = Property(context, name);
            var resultType = property.PropertyType.GetMethod("Invoke")!.ReturnType;
            property.SetValue(context, Expression.Lambda(property.PropertyType,
                Expression.Convert(Expression.Invoke(Expression.Constant(getter)), resultType)).Compile());
        }

        public static object? Call(MethodInfo method, object? owner, params object?[] arguments)
        {
            try
            {
                return method.Invoke(owner, arguments);
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
