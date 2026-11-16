
namespace SnackCounter.Application.Exceptions;

public sealed class DuplicateActiveRefillRequestException : Exception
{
    public DuplicateActiveRefillRequestException()
        : base("An active refill request already exists for this office and product.")
    {
    }
}
