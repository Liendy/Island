using System.Diagnostics;

namespace AuraIsland.Services;

public static class NetworkService
{
    public static Task<string> GetWifiStateAsync() =>
        RunAsync("powershell.exe", "-NoProfile -Command \"$a=Get-NetAdapter -Name 'Wi-Fi' -ErrorAction SilentlyContinue; if(!$a){'NotFound'} elseif($a.Status -eq 'Disabled'){'Disabled'} else {'Enabled'}\"", false);

    public static Task<string> SetWifiAsync(bool enabled)
    {
        var command = enabled ? "Enable-NetAdapter" : "Disable-NetAdapter";
        return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command} -Name 'Wi-Fi' -Confirm:$false -ErrorAction Stop\"", true);
    }

    public static Task<string> GetBluetoothStateAsync() =>
        RunAsync("powershell.exe", "-NoProfile -Command \"$d=Get-PnpDevice -Class Bluetooth -ErrorAction SilentlyContinue; if($d | Where-Object {$_.Status -eq 'OK'}) {'Enabled'} else {'Disabled'}\"", false);

    public static Task<string> SetBluetoothAsync(bool enabled)
    {
        var action = enabled ? "Enable-PnpDevice" : "Disable-PnpDevice";
        var script = $"$d=Get-PnpDevice -Class Bluetooth -ErrorAction SilentlyContinue; if($d){{$d | ForEach-Object {{ {action} -InstanceId $_.InstanceId -Confirm:$false -ErrorAction Stop }}}} else {{Write-Output 'No Bluetooth devices found'}}";
        return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"", true);
    }

    private static async Task<string> RunAsync(string file, string args, bool elevate)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = elevate,
                Verb = elevate ? "runas" : "",
                CreateNoWindow = !elevate,
                RedirectStandardOutput = !elevate,
                RedirectStandardError = !elevate
            };
            using var p = Process.Start(psi);
            if (p is null) return "Не удалось запустить команду Windows.";
            if (elevate) { await p.WaitForExitAsync(); return p.ExitCode == 0 ? "Команда выполнена." : $"Код Windows: {p.ExitCode}."; }
            var output = await p.StandardOutput.ReadToEndAsync();
            var error = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return string.IsNullOrWhiteSpace(output) ? error : output.Trim();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { return "Запрос прав администратора отменён."; }
        catch (Exception ex) { return ex.Message; }
    }
}
