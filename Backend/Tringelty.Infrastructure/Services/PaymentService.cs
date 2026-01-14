using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IBusinessRepository _repository;
    private readonly IStripeService _stripeService;

    public PaymentService(IBusinessRepository repository, IStripeService stripeService)
    {
        _repository = repository;
        _stripeService = stripeService;
    }

    public async Task<string> GeneratePaymentLinkAsync(CreatePaymentDto request)
    {
        // 1. Get Worker from DB
        var worker = await _repository.GetWorkerByIdAsync(request.WorkerId);

        if (worker == null)
        {
            throw new KeyNotFoundException($"Worker with ID {request.WorkerId} not found.");
        }

        // 2. Validate Worker Status
        // Business Rule: We cannot accept money if the worker hasn't set up Stripe.
        if (string.IsNullOrEmpty(worker.StripeAccountId) || !worker.IsOnboarded)
        {
            throw new InvalidOperationException("This worker is not ready to accept payments yet.");
        }

        // 3. Call Stripe Service
        var paymentUrl = await _stripeService.CreateCheckoutSessionAsync(
            worker.StripeAccountId,
            request.Amount,
            request.Currency,
            worker
        );

        return paymentUrl;
    }
}