using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AuthGate.Auth.Hubs;

[Authorize]
public class SessionHub : Hub { }