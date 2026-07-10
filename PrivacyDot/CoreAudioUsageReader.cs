using System.Runtime.InteropServices;

namespace PrivacyDot;

internal sealed class CoreAudioUsageReader
{
    private const int ClsctxAll = 23;

    public IReadOnlyList<DeviceUsageEntry> GetActiveMicrophoneApps()
    {
        var entries = new List<DeviceUsageEntry>();
        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? devices = null;

        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

            if (enumerator.EnumAudioEndpoints(EDataFlow.Capture, DeviceState.Active, out devices) < 0 || devices is null)
            {
                return entries;
            }

            if (devices.GetCount(out var deviceCount) < 0)
            {
                return entries;
            }

            for (var i = 0; i < deviceCount; i++)
            {
                ReadDeviceSessions(devices, i, entries);
            }
        }
        catch
        {
            // Core Audio is an enhancement; registry detection still works if COM is unavailable.
        }
        finally
        {
            ReleaseComObject(devices);
            ReleaseComObject(enumerator);
        }

        return entries;
    }

    private static void ReadDeviceSessions(IMMDeviceCollection devices, int deviceIndex, List<DeviceUsageEntry> entries)
    {
        IMMDevice? device = null;
        object? managerObject = null;
        IAudioSessionEnumerator? sessions = null;

        try
        {
            if (devices.Item(deviceIndex, out device) < 0 || device is null)
            {
                return;
            }

            var sessionManagerId = typeof(IAudioSessionManager2).GUID;

            if (device.Activate(ref sessionManagerId, ClsctxAll, IntPtr.Zero, out managerObject) < 0
                || managerObject is not IAudioSessionManager2 manager)
            {
                return;
            }

            if (manager.GetSessionEnumerator(out sessions) < 0 || sessions is null)
            {
                return;
            }

            if (sessions.GetCount(out var sessionCount) < 0)
            {
                return;
            }

            for (var i = 0; i < sessionCount; i++)
            {
                ReadSession(sessions, i, entries);
            }
        }
        finally
        {
            ReleaseComObject(sessions);
            ReleaseComObject(managerObject);
            ReleaseComObject(device);
        }
    }

    private static void ReadSession(IAudioSessionEnumerator sessions, int sessionIndex, List<DeviceUsageEntry> entries)
    {
        IAudioSessionControl? control = null;

        try
        {
            if (sessions.GetSession(sessionIndex, out control) < 0 || control is null)
            {
                return;
            }

            if (control.GetState(out var state) < 0 || state != AudioSessionState.Active)
            {
                return;
            }

            if (control is not IAudioSessionControl2 control2)
            {
                return;
            }

            if (control2.GetProcessId(out var processId) < 0 || processId == 0)
            {
                return;
            }

            var entry = ProcessUsageResolver.TryCreateFromProcessId(
                (int)processId,
                DeviceKind.Microphone,
                UsageSource.CoreAudio);

            if (entry is not null)
            {
                entries.Add(entry);
            }
        }
        finally
        {
            ReleaseComObject(control);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    [Flags]
    private enum DeviceState
    {
        Active = 0x00000001
    }

    private enum AudioSessionState
    {
        Inactive = 0,
        Active = 1,
        Expired = 2
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState dwStateMask, out IMMDeviceCollection ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, int role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-C0A9FC3DF9B8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out int pcDevices);

        [PreserveSig]
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(
            ref Guid iid,
            int dwClsCtx,
            IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(int stgmAccess, IntPtr ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out DeviceState pdwState);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        [PreserveSig]
        int GetAudioSessionControl(IntPtr audioSessionGuid, uint streamFlags, out IntPtr sessionControl);

        [PreserveSig]
        int GetSimpleAudioVolume(IntPtr audioSessionGuid, uint streamFlags, out IntPtr audioVolume);

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);

        [PreserveSig]
        int RegisterSessionNotification(IntPtr sessionNotification);

        [PreserveSig]
        int UnregisterSessionNotification(IntPtr sessionNotification);

        [PreserveSig]
        int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string? sessionId, IntPtr duckNotification);

        [PreserveSig]
        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int sessionCount);

        [PreserveSig]
        int GetSession(int sessionCount, out IAudioSessionControl session);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl
    {
        [PreserveSig]
        int GetState(out AudioSessionState retVal);

        [PreserveSig]
        int GetDisplayName(out IntPtr retVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

        [PreserveSig]
        int GetIconPath(out IntPtr retVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid retVal);

        [PreserveSig]
        int SetGroupingParam(ref Guid overrideValue, ref Guid eventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr newNotifications);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr newNotifications);
    }

    [ComImport]
    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        [PreserveSig]
        int GetState(out AudioSessionState retVal);

        [PreserveSig]
        int GetDisplayName(out IntPtr retVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

        [PreserveSig]
        int GetIconPath(out IntPtr retVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid retVal);

        [PreserveSig]
        int SetGroupingParam(ref Guid overrideValue, ref Guid eventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr newNotifications);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr newNotifications);

        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);

        [PreserveSig]
        int GetProcessId(out uint retVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
    }
}
