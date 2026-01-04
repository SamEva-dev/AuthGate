using System.Threading;
using System.Threading.Tasks;

namespace AuthGate.Auth.Application.Common.Security;

public interface IMachineTokenProvider
{
    Task<string> GetProvisioningTokenAsync(CancellationToken ct = default);
}
