using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using Mpv.NET.Player;

namespace Clipple.Types;

public partial class HostedMediaPlayer : HwndHost
{
    public HostedMediaPlayer()
    {
        MediaPlayer = new(Path.Combine(App.LibPath, "mpv-2.dll"))
        {
            KeepOpen = KeepOpen.Always,
            Volume   = 100
        };
    }

    #region Methods
    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var window = CreateWindowEx(0, "static", "",
            WS_CHILD | WS_VISIBLE,
            0, 0,
            0, 0,
            hwndParent.Handle,
            HOST_ID,
            nint.Zero, 
            0);
        
        var handleRef = new HandleRef(this, window);
        MediaPlayer.Handle = handleRef.Handle;

        HostWindowCreated = true;
        if (waitingLoad is { } path)
            Load(path);

        return handleRef;
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    protected override nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        return nint.Zero;
    }
    #endregion
    
    #region Win32
    [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, 
        int style, int x,  int y, int width, int height, nint hwndParent, nint hMenu, nint hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam);
    
    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(nint hwnd);

    private const int WS_CHILD   = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int HOST_ID    = 0x00000002;
    #endregion

    #region Members
    private string? waitingLoad;
    #endregion
    
    #region Properties
    public MpvPlayer MediaPlayer { get; private set; }
    
    public bool HostWindowCreated { get; private set; }
    #endregion
    
        
    #region Forwarded properties
    public TimeSpan Position
    {
        get => MediaPlayer.Position;
        set => MediaPlayer.Position = value;
    }

    public bool IsMuted
    {
        get => MediaPlayer.IsMuted;
        set => MediaPlayer.IsMuted = value;
    }

    public double Speed
    {
        get => MediaPlayer.Speed;
        set => MediaPlayer.Speed = value;
    }

    public int Volume
    {
        get => MediaPlayer.Volume;
        set => MediaPlayer.Volume = value;
    }
    
    public TimeSpan Duration => MediaPlayer.Duration;

    public bool     EndReached => MediaPlayer.EndReached;
    public bool     IsPlaying  => MediaPlayer.IsPlaying;
    #endregion
    
    #region Forwarded methods
    public void Load(string path)
    {
        if (HostWindowCreated)
        {
            MediaPlayer.Load(path);
        }
        else
            waitingLoad = path;
    }
    
    public void SetFilter(string filterString)
    {
        MediaPlayer.API.SetPropertyString("lavfi-complex", filterString);
    }

    public void Pause() => MediaPlayer.Pause();
    
    public void Resume() => MediaPlayer.Resume();
    
    public void Stop() => MediaPlayer.Stop();
    
    public void NextFrame() => MediaPlayer.NextFrame();
    
    public void PreviousFrame() => MediaPlayer.PreviousFrame();
    
    public async Task SeekAsync(TimeSpan time) => await MediaPlayer.SeekAsync(time);

    #endregion
    
    #region Forwarded events
    public event EventHandler<MpvPlayerPositionChangedEventArgs> PositionChanged
    {
        add => MediaPlayer.PositionChanged += value;
        remove => MediaPlayer.PositionChanged -= value;
    }
    
    public event EventHandler MediaPaused
    {
        add => MediaPlayer.MediaPaused += value;
        remove => MediaPlayer.MediaPaused -= value;
    }
    
    public event EventHandler MediaResumed
    {
        add => MediaPlayer.MediaResumed += value;
        remove => MediaPlayer.MediaResumed -= value;
    }
    
    public event EventHandler MediaFinished
    {
        add => MediaPlayer.MediaFinished += value;
        remove => MediaPlayer.MediaFinished -= value;
    }
    
    public event EventHandler MediaError
    {
        add => MediaPlayer.MediaError += value;
        remove => MediaPlayer.MediaError -= value;
    }
    
    public event EventHandler MediaLoaded
    {
        add => MediaPlayer.MediaLoaded += value;
        remove => MediaPlayer.MediaLoaded -= value;
    }
    #endregion
}