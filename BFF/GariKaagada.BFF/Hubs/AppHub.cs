using Microsoft.AspNetCore.SignalR;

namespace GariKaagada.BFF.Hubs;

/// <summary>
/// Proves the SignalR hub-hosting mechanism itself works (constitution Principle V/VI).
/// Intentionally has zero hub methods — real-time features are added by whichever feature
/// needs them first.
/// </summary>
public class AppHub : Hub
{
}
