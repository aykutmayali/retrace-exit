using System;
using System.Diagnostics;
using StackifyLib;

namespace Sample.LegacyIis
{
    public class CheckoutService
    {
        public void PlaceOrder(string orderId)
        {
            var tracer = ProfileTracer.CreateAsOperation("PlaceOrder", orderId);
            tracer.Exec(() =>
            {
                Metrics.Count("Orders", "Placed");
                Metrics.Time("Orders", "PlaceOrderDuration", TimeSpan.FromMilliseconds(40));
            });
        }
    }
}
