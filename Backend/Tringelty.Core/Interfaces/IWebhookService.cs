using Stripe;

namespace Tringelty.Core.Interfaces;

public interface IWebhookService
{
    Task HandleEventAsync(Event stripeEvent);
}