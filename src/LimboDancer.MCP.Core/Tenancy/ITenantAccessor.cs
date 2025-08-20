using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimboDancer.MCP.Core.Tenancy;

public interface ITenantAccessor
{
    Guid TenantId { get; }
}
