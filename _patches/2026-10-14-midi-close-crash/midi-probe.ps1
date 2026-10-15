$src = @'
using System;
using System.Runtime.InteropServices;

public static class WinMM
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MIDIINCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint   vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint   dwSupport;
    }

    [DllImport("winmm.dll")] public static extern uint midiInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    public static extern uint midiInGetDevCapsW(IntPtr id, ref MIDIINCAPS caps, uint size);
    [DllImport("winmm.dll")]
    public static extern uint midiInOpen(out IntPtr handle, uint deviceId, IntPtr cb, IntPtr inst, uint flags);
    [DllImport("winmm.dll")] public static extern uint midiInClose(IntPtr handle);
}
'@

Add-Type -TypeDefinition $src -Language CSharp | Out-Null

function Decode($code) {
    switch ($code) {
        0  { "OK" }
        2  { "MMSYSERR_BADDEVICEID (identifiant invalide)" }
        4  { "MMSYSERR_ALLOCATED (deja ouvert par un autre processus)" }
        7  { "MMSYSERR_NOMEM" }
        11 { "MMSYSERR_INVALPARAM" }
        default { "code $code" }
    }
}

$n = [WinMM]::midiInGetNumDevs()
Write-Output "Entrees MIDI vues par Windows : $n"
Write-Output ""

for ($i = 0; $i -lt $n; $i++) {
    $caps = New-Object WinMM+MIDIINCAPS
    $size = [System.Runtime.InteropServices.Marshal]::SizeOf([type]'WinMM+MIDIINCAPS')
    [void][WinMM]::midiInGetDevCapsW([IntPtr]$i, [ref]$caps, $size)

    $h = [IntPtr]::Zero
    $rc = [WinMM]::midiInOpen([ref]$h, $i, [IntPtr]::Zero, [IntPtr]::Zero, 0)
    if ($rc -eq 0) { [void][WinMM]::midiInClose($h) }

    Write-Output ("[{0}] {1,-28} ouverture : {2}" -f $i, $caps.szPname, (Decode $rc))
}
