static class NativeXuProbeCommands
{
    public const int CmdAudioFormat = 0x04;
    public const int CmdAudioSamplingRate = 0x06;
    public const int CmdAudioSetAdcVolumeGain = 0x0A;
    public const int CmdAudioGetAdcVolumeGain = 0x0B;
    public const int CmdAudioSetHdmiDprxVolumeGain = 0x0C;
    public const int CmdAudioGetHdmiDprxVolumeGain = 0x0D;
    public const int CmdAudioSetUacVolumeGain = 0x10;
    public const int CmdAudioGetUacVolumeGain = 0x11;
    public const int CmdAudioSetUacOut2MixerSource = 0x26;
    public const int CmdAudioGetUacOut2MixerSource = 0x27;
    public const int CmdAudioSetDacHpMixerSource = 0x28;
    public const int CmdAudioGetDacHpMixerSource = 0x29;
    public const int CmdAudioSetI2sOutMixerSource = 0x2A;
    public const int CmdAudioGetI2sOutMixerSource = 0x2B;
    public const int CmdAudioSetUacOut1Mute = 0x2C;
    public const int CmdAudioGetUacOut1Mute = 0x2D;
    public const int CmdAudioSetUacOut2Mute = 0x2E;
    public const int CmdAudioGetUacOut2Mute = 0x2F;
    public const int CmdAudioSetDacHpMute = 0x30;
    public const int CmdAudioGetDacHpMute = 0x31;
    public const int CmdAudioSetI2sOutMute = 0x32;
    public const int CmdAudioGetI2sOutMute = 0x33;
    public const int CmdSetInputSource = 0x34;
    public const int CmdInputSource = 0x35;
    public const int CmdAudioSetAdcOnOff = 0x08;
    public const int CmdAudioSetDacHpOnOff = 0x09;
    public const int CmdAudioGetAdcOnOff = 0x74;
    public const int CmdAudioGetDacHpOnOff = 0x75;
    public const int CmdGetAuxInVolume = 0x7F;
    public const int CmdSetAuxInVolume = 0x80;
    public const int CmdGetAuxOutVolume = 0x81;
    public const int CmdSetAuxOutVolume = 0x82;
}
