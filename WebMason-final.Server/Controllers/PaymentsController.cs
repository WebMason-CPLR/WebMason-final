using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System;
using System.Threading.Tasks;
using WebMason_final.Server.Data;
using WebMason_final.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            var userId = User.FindFirst("nameid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = request.Server;
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                      UnitAmount = 2000,
                      Currency = "usd",
                      ProductData = new SessionLineItemPriceDataProductDataOptions
                      {
                        Name = server.name,
                        Description = server.description,
                      },
                    },
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "https://localhost:4200/success",
                CancelUrl = "https://localhost:4200/cancel",
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Ok(new { sessionId = session.Id });
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest paymentRequest)
        {
            try
            {
                //var options = new ChargeCreateOptions
                //{
                //    Amount = paymentRequest.Amount,
                //    Currency = paymentRequest.Currency,
                //    Description = paymentRequest.Description,
                //    Source = paymentRequest.Token,
                //    Metadata = new Dictionary<string, string>
                //    {
                //        { "ServerType", paymentRequest.ServerType },
                //        { "UserId", paymentRequest.UserId }
                //    }
                //};

                //var service = new ChargeService();
                //Charge charge = await service.CreateAsync(options);
                var charge = "succeeded";

                if (charge == "succeeded")
                {
                    // Payment succeeded, proceed with server deployment
                    return Ok(new { success = true, message = "Payment successful" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Payment failed" });
                }
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class PaymentRequest
    {
        public string Token { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string ServerType { get; set; }
        public string UserId { get; set; }
    }

    public class CreateCheckoutSessionRequest
    {
        public dynamic Server { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public string Token { get; set; }
        public dynamic Server { get; set; }
    }
}
