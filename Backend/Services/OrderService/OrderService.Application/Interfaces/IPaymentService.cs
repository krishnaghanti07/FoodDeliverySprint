using System;
using System.Collections.Generic;
using System.Text;
using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResultDto> SimulatePaymentAsync(SimulatePaymentDto dto);
    Task<PaymentSummaryDto?> GetPaymentByOrderIdAsync(Guid orderId);
}