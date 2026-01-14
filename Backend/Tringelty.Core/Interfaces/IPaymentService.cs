using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IPaymentService
{
    Task<string> GeneratePaymentLinkAsync(CreatePaymentDto request);
}