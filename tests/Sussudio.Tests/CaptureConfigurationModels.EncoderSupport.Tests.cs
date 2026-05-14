using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task EncoderSupport_ComputesAvailabilityAndPreferredEncoders()
    {
        var supportType = RequireType("Sussudio.Models.EncoderSupport");
        AssertDeclaredConfigProperties(
            supportType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("HasH264Nvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasHevcNvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasAv1Nvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibX264", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibX265", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibSvtAv1", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibAomAv1", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasH264", typeof(bool), ConfigSetterExpectation.None),
                ConfigProperty("HasHevc", typeof(bool), ConfigSetterExpectation.None),
                ConfigProperty("HasAv1", typeof(bool), ConfigSetterExpectation.None),
                ConfigString("PreferredAv1Encoder", ConfigSetterExpectation.None, ConfigNullability.Nullable),
                ConfigProperty("Empty", supportType, ConfigSetterExpectation.None, scope: ConfigPropertyScope.Static)
            });

        var empty = supportType.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        AssertEqual(false, GetBoolProperty(empty, "HasH264"), "EncoderSupport.Empty.HasH264");
        AssertEqual(false, GetBoolProperty(empty, "HasHevc"), "EncoderSupport.Empty.HasHevc");
        AssertEqual(false, GetBoolProperty(empty, "HasAv1"), "EncoderSupport.Empty.HasAv1");
        AssertEqual(null, GetPropertyValue(empty, "PreferredAv1Encoder"), "EncoderSupport.Empty.PreferredAv1Encoder");

        var nvencAv1 = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(nvencAv1, "HasAv1Nvenc", true);
        SetPropertyOrBackingField(nvencAv1, "HasLibSvtAv1", true);
        AssertEqual(true, GetBoolProperty(nvencAv1, "HasAv1"), "EncoderSupport.HasAv1 with NVENC");
        AssertEqual("av1_nvenc", GetStringProperty(nvencAv1, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder NVENC priority");

        var svtAv1 = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(svtAv1, "HasLibSvtAv1", true);
        SetPropertyOrBackingField(svtAv1, "HasLibAomAv1", true);
        AssertEqual("libsvtav1", GetStringProperty(svtAv1, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder SVT priority");

        var softwareFallbacks = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibX264", true);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibX265", true);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibAomAv1", true);
        AssertEqual(true, GetBoolProperty(softwareFallbacks, "HasH264"), "EncoderSupport.HasH264 software fallback");
        AssertEqual(true, GetBoolProperty(softwareFallbacks, "HasHevc"), "EncoderSupport.HasHevc software fallback");
        AssertEqual("libaom-av1", GetStringProperty(softwareFallbacks, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder AOM fallback");

        return Task.CompletedTask;
    }
}
