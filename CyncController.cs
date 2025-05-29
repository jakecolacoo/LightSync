using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class CyncController
{
    private readonly HttpClient _httpClient;
    private readonly string _cyncLanBaseUrl; // e.g., "http://localhost:8080"
    private readonly string _deviceIp;       // IP of the Cync device

    public CyncController(string cyncLanBaseUrl, string deviceIp)
    {
        _httpClient = new HttpClient();
        _cyncLanBaseUrl = cyncLanBaseUrl.TrimEnd('/');
        _deviceIp = deviceIp;
    }

    public async Task SetColorAsync(int r, int g, int b)
    {
        if (string.IsNullOrWhiteSpace(_cyncLanBaseUrl) || string.IsNullOrWhiteSpace(_deviceIp))
        {
            Console.WriteLine("Cync LAN server URL or Device IP is not configured.");
            return;
        }

        string apiUrl = $"{_cyncLanBaseUrl}/api/devices/{_deviceIp}";

        // The cync-lan API expects color and saturation (S).
        // R, G, B are 0-255. Saturation is 0 (most saturated) to 255 (pure white).
        // For simplicity, we'll set saturation to 0 for the most vibrant color.
        var payload = new
        {
            status = 1, // Turn on
            color = new { r, g, b, s = 0 }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully set Cync device {_deviceIp} color to R:{r}, G:{g}, B:{b}");
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to set Cync device color. Status: {response.StatusCode}, Response: {responseBody}");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error sending request to Cync LAN server: {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred in SetColorAsync: {ex.Message}");
        }
    }

    // Optional: Add other control methods as needed (e.g., TurnOn, TurnOff, SetBrightness)
    public async Task TurnOnAsync()
    {
        await SendCommandAsync(new { status = 1 });
    }

    public async Task TurnOffAsync()
    {
        await SendCommandAsync(new { status = 0 });
    }

    public async Task SetBrightnessAsync(int brightness) // 0-100
    {
        if (brightness < 0) brightness = 0;
        if (brightness > 100) brightness = 100;
        await SendCommandAsync(new { status = 1, brightness });
    }

    private async Task SendCommandAsync(object payload)
    {
        if (string.IsNullOrWhiteSpace(_cyncLanBaseUrl) || string.IsNullOrWhiteSpace(_deviceIp))
        {
            Console.WriteLine("Cync LAN server URL or Device IP is not configured.");
            return;
        }

        string apiUrl = $"{_cyncLanBaseUrl}/api/devices/{_deviceIp}";
        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully sent command to Cync device {_deviceIp}: {jsonPayload}");
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to send command to Cync device. Status: {response.StatusCode}, Response: {responseBody}");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error sending request to Cync LAN server: {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred in SendCommandAsync: {ex.Message}");
        }
    }
}