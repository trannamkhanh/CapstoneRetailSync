using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DemoPos.Models;

namespace DemoPos.Services;

public interface IPosBackend
{
	Task<PosResult> SendAsync(PosEvent posEvent, CancellationToken cancellationToken = default(CancellationToken));

	IReadOnlyList<Product> GetProducts();

	void Reset();
}
