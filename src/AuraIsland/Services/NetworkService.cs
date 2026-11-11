using System.Diagnostics;
using System.Text;

namespace AuraIsland.Services;

public static class NetworkService
{
    public static async Task<string> GetWifiStateAsync()
    {
        var output = await RunAsync("netsh.exe", "wlan show interfaces", false);
        if (output.Contains("State", StringComparison.OrdinalIgnoreCase))
        {
            var line = output.Split('\n').FirstOrDefault(x => x.Contains("State", StringComparison.OrdinalIgnoreCase));
            return line?.Split(':').LastOrDefault()?.Trim() ?? "Unknown";
        }
        return "Unknown";
    }

    public static Task<string> SetWifiAsync(bool enabled) =>
        RunAsync("netsh.exe", $"interface set interface name=\"Wi-Fi\" admin={(enabled ? "enabled" : "disabled")}", true);

    public static Task<string> ToggleBluetoothAsync(bool enabled)
    {
        var action = enabled ? "Enable-PnpDevice" : "Disable-PnpDevice";
        var script = $"$d=Get-PnpDevice -Class Bluetooth -ErrorAction SilentlyContinue | Where-Object {{$_.Status -ne 'Unknown'}}; if($d){{$d | ForEach-Object {{ {action} -InstanceId $_.InstanceId -Confirm:$false -ErrorAction Stop }}}} else {{Write-Output 'No Bluetooth devices found'}}";
        return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"", true);
    }

    private static async Task<string> RunAsync(string file, string args, bool elevate)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args) { UseShellExecute = elevate, Verb = elevate ? "runas" : "", CreateNoWindow = !elevate, RedirectStandardOutput = !elevate, RedirectStandardError = !elevate };
            using var p = Process.Start(psi);
            if (p is null) return "Could not start Windows command.";
            if (elevate) { await p.WaitForExitAsync(); return p.ExitCode == 0 ? "Command completed." : $"Windows returned code {p.ExitCode}."; }
            var output = await p.StandardOutput.ReadToEndAsync();
            var error = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return string.IsNullOrWhiteSpace(output) ? error : output;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { return "Operation cancelled in the Windows permission dialog."; }
        catch (Exception ex) { return ex.Message; }
    }
}
