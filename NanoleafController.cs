// NanoleafController.cs

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class NanoleafStateEventArgs : EventArgs
{
    public int R { get; }
    public int G { get; }
    public int B { get; }

    public NanoleafStateEventArgs(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}

public class NanoleafController
{
    private readonly string _host;
    private readonly string _authToken;
    private readonly HttpClient _httpClient;

    public event EventHandler<NanoleafStateEventArgs> StateChanged;

    private int? _currentHue;
    private int? _currentSaturation;
    private int? _currentBrightness;

    public NanoleafController(string host, string authToken)
    {
        _host = host;
        _authToken = authToken;
        _httpClient = new HttpClient();
        // For long-lived SSE connections, HttpClient's default timeout might be an issue.
        // However, with HttpCompletionOption.ResponseHeadersRead and cancellation token,
        // it should be manageable. If issues arise, consider:
        // _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    private string BaseUrl => $"http://{_host}:16021/api/v1/{_authToken}";

    public async Task SetColorAsync(int r, int g, int b)
    {
        // Note: Nanoleaf API usually prefers HSB for state.
        // This method sends an RGB command, which some devices might support directly or interpret.
        // For screen mirroring like 4D, direct color setting might be overridden by the mirroring itself.
        var url = $"{BaseUrl}/state";
        // The Nanoleaf OpenAPI docs suggest "write" commands for HSB or CT.
        // Example for HSB: {"write": {"hue": H, "sat": S, "bri": B}}
        // Using a direct RGB approach here as per the original stub's intention,
        // but its effectiveness may vary depending on the Nanoleaf device and its current mode.
        // A common way to set RGB-like color is often through HSB.
        var hsb = RgbToHsb(r, g, b); // Convert RGB to HSB to align with typical Nanoleaf state representation
        var json = $"{{\"hue\":{{\"value\":{hsb.h}}}, \"sat\":{{\"value\":{hsb.s}}}, \"bri\":{{\"value\":{hsb.b}}}}}";
        // Simpler HSB payload:
        json = $"{{\"hue\": {hsb.h}, \"sat\": {hsb.s}, \"bri\": {hsb.b}}}";
        // The original code had: var json = $"{{\"color\":{{\"r\":{r},\"g\":{g},\"b\":{b}}}}}";
        // This is not standard. Let's use the HSB approach for /state.
        // The most reliable command for setting color is often PUT to /api/v1/TOKEN/state with body {"hue": {"value": H}, "sat": {"value": S}, "bri": {"value": B}} or similar for effects.
        // For simplicity, let's revert to the user's original placeholder intent if this is just a command method
        // and not the primary sync mechanism. The sync will come from events.
        // The example was: var json = $"{{\"color\":{{\"r\":{r},\"g\":{g},\"b\":{b}}}}}"; which isn't standard.
        // A valid write command for color using HSB values (which are the primary state attributes for color):
        // json = $"{{\"write\": {{ \"hue\": {hsb.h}, \"sat\": {hsb.s}, \"brightness\": {hsb.b} }} }}";
        // Or simply update state:
        json = $"{{\"brightness\": {{ \"value\": {hsb.b} }}, \"hue\": {{ \"value\": {hsb.h} }}, \"sat\": {{ \"value\": {hsb.s} }} }}";


        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await _httpClient.PutAsync(url, content);
    }

    public async Task SetEffectAsync(string effectName)
    {
        var url = $"{BaseUrl}/effects";
        var json = $"{{\"select\":\"{effectName}\"}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await _httpClient.PutAsync(url, content);
    }

    public async Task StartListeningToEventsAsync(CancellationToken cancellationToken)
    {
        // Event IDs: 1=State, 2=Layout, 3=Effects, 4=Touch (for Shapes/Elements)
        // We are interested in State events (id=1) for color changes.
        var eventsUrl = $"{BaseUrl}/events?id=1";
        var request = new HttpRequestMessage(HttpMethod.Get, eventsUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        try
        {
            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken)) // Pass CancellationToken
                using (var reader = new StreamReader(stream))
                {
                    string currentEventName = null; // SSE "event:" field (if used)
                    string currentEventData = "";

                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(); // This honors CancellationToken in .NET 7+
                                                                // For older .NET, Task.Run with loop and token check might be needed if ReadLineAsync doesn't cancel promptly.

                        if (cancellationToken.IsCancellationRequested) break;

                        if (string.IsNullOrWhiteSpace(line)) // Empty line signifies end of an event
                        {
                            if (!string.IsNullOrEmpty(currentEventData)) // We don't care about currentEventName (Nanoleaf uses 'id' for type)
                            {
                                // Nanoleaf uses the "id:" field for event type, not "event:"
                                // So we pass null for eventName and let ProcessEvent use its internal logic
                                ProcessEvent(null, currentEventData);
                                currentEventData = ""; // Reset for next event
                            }
                        }
                        else if (line.StartsWith("event:")) // Though Nanoleaf docs say they use "id:" for type
                        {
                            currentEventName = line.Substring("event:".Length).Trim();
                        }
                        else if (line.StartsWith("id:")) // This is what Nanoleaf uses for event type
                        {
                             // This is the event *type* id (1 for state, 2 for layout etc), not a unique event instance id.
                             // We'll parse this inside ProcessEvent if needed, or assume it's state if we only sub to id=1
                             // For now, ProcessEvent implicitly handles id=1 type by parsing specific attributes.
                             // Let's store the event type and pass it.
                             currentEventName = line.Substring("id:".Length).Trim(); // Store the type (e.g., "1")
                        }
                        else if (line.StartsWith("data:"))
                        {
                            currentEventData += line.Substring("data:".Length).Trim();
                        }
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error connecting to Nanoleaf event stream: {ex.Message}");
            // Optionally re-throw or handle reconnection logic
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Nanoleaf event listening canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading Nanoleaf event stream: {ex.Message}");
        }
    }

    private void ProcessEvent(string eventType, string eventData)
    {
        // We only subscribed to id=1 (state events), so we assume eventType will be "1" or related if Nanoleaf sends it.
        // The primary check is the content of eventData for HSB attributes.
        try
        {
            // Nanoleaf state event data (id=1) format:
            // data: {"events": [{"attr": <id>, "value": <val>}, ...]}
            // Attribute IDs: 2=brightness (0-100), 3=hue (0-360), 4=saturation (0-100)
            using (JsonDocument doc = JsonDocument.Parse(eventData))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("events", out JsonElement eventsArray))
                {
                    bool stateUpdatedByThisEventBatch = false;
                    foreach (JsonElement ev in eventsArray.EnumerateArray())
                    {
                        if (ev.TryGetProperty("attr", out JsonElement attrElement) &&
                            ev.TryGetProperty("value", out JsonElement valueElement))
                        {
                            int attr = attrElement.GetInt32();
                            // Ensure value is treated as int, could be bool for "on" state (attr 1)
                            if (valueElement.ValueKind == JsonValueKind.Number)
                            {
                                int value = valueElement.GetInt32();
                                switch (attr)
                                {
                                    case 2: // brightness
                                        if (_currentBrightness != value) { _currentBrightness = value; stateUpdatedByThisEventBatch = true; }
                                        break;
                                    case 3: // hue
                                        if (_currentHue != value) { _currentHue = value; stateUpdatedByThisEventBatch = true; }
                                        break;
                                    case 4: // saturation
                                        if (_currentSaturation != value) { _currentSaturation = value; stateUpdatedByThisEventBatch = true; }
                                        break;
                                }
                            }
                            else if (attr == 1 && valueElement.ValueKind == JsonValueKind.False || valueElement.ValueKind == JsonValueKind.True) // "on" state
                            {
                                bool isOn = valueElement.GetBoolean();
                                // You might want to handle the "on" state (e.g., if off, don't send color updates or send black)
                                // For now, we only update color if H,S,B change.
                            }
                        }
                    }

                    if (stateUpdatedByThisEventBatch && _currentHue.HasValue && _currentSaturation.HasValue && _currentBrightness.HasValue)
                    {
                        var (r, g, b) = HsbToRgb(_currentHue.Value, _currentSaturation.Value / 100.0, _currentBrightness.Value / 100.0);
                        OnStateChanged(new NanoleafStateEventArgs(r, g, b));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing Nanoleaf event data: {ex.Message} - Data: \"{eventData}\"");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing Nanoleaf event: {ex.Message}");
        }
    }

    protected virtual void OnStateChanged(NanoleafStateEventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }

    // Helper method to convert HSB to RGB
    // h is 0-360, s is 0-1.0, b is 0-1.0
    // returns (r, g, b) each 0-255
    private (int R, int G, int B) HsbToRgb(double h, double s, double b)
    {
        s = Math.Max(0, Math.Min(1, s)); // Clamp s to [0, 1]
        b = Math.Max(0, Math.Min(1, b)); // Clamp b to [0, 1]
        h = h % 360;
        if (h < 0) h += 360;

        double r = 0, g = 0, bl = 0;
        if (s == 0) // Achromatic (grey)
        {
            r = g = bl = b;
        }
        else
        {
            int i = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);
            double p = b * (1 - s);
            double q = b * (1 - f * s);
            double t = b * (1 - (1 - f) * s);

            switch (i)
            {
                case 0: r = b; g = t; bl = p; break;
                case 1: r = q; g = b; bl = p; break;
                case 2: r = p; g = b; bl = t; break;
                case 3: r = p; g = q; bl = b; break;
                case 4: r = t; g = p; bl = b; break;
                case 5: r = b; g = p; bl = q; break;
            }
        }
        return ((int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(bl * 255));
    }
    
    // Helper method to convert RGB to HSB for the SetColorAsync method
    // r, g, b are 0-255
    // returns (h 0-360, s 0-100, b 0-100)
    private (int h, int s, int b) RgbToHsb(int r, int g, int bl)
    {
        double R = r / 255.0;
        double G = g / 255.0;
        double B = bl / 255.0;

        double max = Math.Max(R, Math.Max(G, B));
        double min = Math.Min(R, Math.Min(G, B));
        double delta = max - min;

        double hue = 0;
        if (delta != 0)
        {
            if (max == R) hue = ((G - B) / delta) % 6;
            else if (max == G) hue = (B - R) / delta + 2;
            else hue = (R - G) / delta + 4;
        }
        hue = Math.Round(hue * 60);
        if (hue < 0) hue += 360;

        double saturation = (max == 0) ? 0 : delta / max;
        double brightness = max;

        return ((int)hue, (int)Math.Round(saturation * 100), (int)Math.Round(brightness * 100));
    }
}
