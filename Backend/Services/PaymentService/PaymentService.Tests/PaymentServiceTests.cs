using FoodDelivery.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Application.DTOs;
using PaymentService.Application.Services;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;

namespace PaymentService.Tests;

// ══════════════════════════════════════════════════════════════════════
// PAYMENT SERVICE — UNIT TESTS
// Covers: Simulate success/failure, duplicate guard, refund rules
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class PaymentSimulationServiceTests
{
    private Mock<IPaymentTransactionRepository> _repo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<ILogger<PaymentSimulationService>> _log = null!;
    private PaymentSimulationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo      = new Mock<IPaymentTransactionRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _log       = new Mock<ILogger<PaymentSimulationService>>();
        _sut       = new PaymentSimulationService(_repo.Object, _publisher.Object, _log.Object);
    }

    // ── Simulate Success ──────────────────────────────────────────────

    [Test]
    public async Task Simulate_SuccessfulPayment_ReturnsSuccessResult()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 350m, Method = "COD", ShouldSucceed = true
        };

        var result = await _sut.SimulateAsync(dto);

        Assert.That(result.Status, Is.EqualTo("Success"));
        Assert.That(result.GatewayTxnId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public async Task Simulate_FailedPayment_ReturnsFailedResult()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 200m, Method = "CARD", ShouldSucceed = false
        };

        var result = await _sut.SimulateAsync(dto);

        Assert.That(result.Status, Is.EqualTo("Failed"));
        Assert.That(result.FailureReason, Is.Not.Null.And.Not.Empty);
        Assert.That(result.GatewayTxnId, Is.Null);
    }

    // ── Method Validation ─────────────────────────────────────────────

    [Test]
    public void Simulate_InvalidMethod_ThrowsArgumentException()
    {
        var dto = new SimulatePaymentDto
        {
            OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(),
            Amount = 100m, Method = "BITCOIN", ShouldSucceed = true
        };

        Assert.ThrowsAsync<ArgumentException>(() => _sut.SimulateAsync(dto));
    }

    [Test]
    [TestCase("COD")]
    [TestCase("CARD")]
    [TestCase("WALLET")]
    public async Task Simulate_AllValidMethods_Succeed(string method)
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 100m, Method = method, ShouldSucceed = true
        };

        var result = await _sut.SimulateAsync(dto);

        Assert.That(result.Status, Is.EqualTo("Success"));
    }

    // ── Amount Validation ─────────────────────────────────────────────

    [Test]
    public void Simulate_ZeroAmount_ThrowsArgumentException()
    {
        var dto = new SimulatePaymentDto
        {
            OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(),
            Amount = 0m, Method = "COD", ShouldSucceed = true
        };

        Assert.ThrowsAsync<ArgumentException>(() => _sut.SimulateAsync(dto));
    }

    [Test]
    public void Simulate_NegativeAmount_ThrowsArgumentException()
    {
        var dto = new SimulatePaymentDto
        {
            OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(),
            Amount = -50m, Method = "COD", ShouldSucceed = true
        };

        Assert.ThrowsAsync<ArgumentException>(() => _sut.SimulateAsync(dto));
    }

    // ── Duplicate Payment Guard ───────────────────────────────────────

    [Test]
    public void Simulate_AlreadyPaidOrder_ThrowsInvalidOperation()
    {
        var orderId = Guid.NewGuid();
        var existingTxn = new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = orderId,
            Status = PaymentStatus.Success, Amount = 300m
        };
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(existingTxn);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 300m, Method = "COD", ShouldSucceed = true
        };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SimulateAsync(dto));
    }

    // ── RabbitMQ Event Publishing ─────────────────────────────────────

    [Test]
    public async Task Simulate_SuccessfulPayment_PublishesCompletedEventTwice()
    {
        // Publishes to both PaymentCompletedOrder (→ OrderService) and PaymentCompletedAdmin (→ AdminService)
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 100m, Method = "COD", ShouldSucceed = true
        };

        await _sut.SimulateAsync(dto);

        _publisher.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public async Task Simulate_FailedPayment_PublishesFailedEventTwice()
    {
        // Publishes to both PaymentFailedOrder (→ OrderService) and PaymentFailed (general)
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 100m, Method = "CARD", ShouldSucceed = false
        };

        await _sut.SimulateAsync(dto);

        _publisher.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
    }

    // ── Transaction Persistence ───────────────────────────────────────

    [Test]
    public async Task Simulate_AnyPayment_PersistsTransactionToRepository()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((PaymentTransaction?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new SimulatePaymentDto
        {
            OrderId = orderId, CustomerId = Guid.NewGuid(),
            Amount = 500m, Method = "WALLET", ShouldSucceed = true
        };

        await _sut.SimulateAsync(dto);

        _repo.Verify(r => r.AddAsync(It.IsAny<PaymentTransaction>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}

// ══════════════════════════════════════════════════════════════════════
// REFUND SERVICE — UNIT TESTS
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class RefundServiceTests
{
    private Mock<IPaymentTransactionRepository> _repo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<ILogger<RefundService>> _log = null!;
    private RefundService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo      = new Mock<IPaymentTransactionRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _log       = new Mock<ILogger<RefundService>>();
        _sut       = new RefundService(_repo.Object, _publisher.Object, _log.Object);
    }

    [Test]
    public async Task ProcessRefund_FullRefund_SetsStatusRefunded()
    {
        var orderId = Guid.NewGuid();
        var txn = new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = orderId,
            Amount = 400m, Status = PaymentStatus.Success
        };
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(txn);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ProcessRefundAsync(
            new RefundRequestDto { OrderId = orderId, RefundAmount = 400m, Reason = "Customer request" },
            Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo("Refunded"));
        Assert.That(txn.Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public async Task ProcessRefund_PartialRefund_SetsStatusPartialRefund()
    {
        var orderId = Guid.NewGuid();
        var txn = new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = orderId,
            Amount = 400m, Status = PaymentStatus.Success
        };
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(txn);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<PaymentTransaction>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ProcessRefundAsync(
            new RefundRequestDto { OrderId = orderId, RefundAmount = 200m, Reason = "Partial" },
            Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo("PartialRefund"));
    }

    [Test]
    public void ProcessRefund_AmountExceedsPaid_ThrowsInvalidOperation()
    {
        var orderId = Guid.NewGuid();
        var txn = new PaymentTransaction
        {
            OrderId = orderId, Amount = 300m, Status = PaymentStatus.Success
        };
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(txn);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessRefundAsync(
                new RefundRequestDto { OrderId = orderId, RefundAmount = 500m, Reason = "Overpay" },
                Guid.NewGuid()));
    }

    [Test]
    public void ProcessRefund_NotSuccessfulPayment_ThrowsInvalidOperation()
    {
        var orderId = Guid.NewGuid();
        var txn = new PaymentTransaction
        {
            OrderId = orderId, Amount = 300m, Status = PaymentStatus.Failed
        };
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(txn);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessRefundAsync(
                new RefundRequestDto { OrderId = orderId, RefundAmount = 300m, Reason = "Refund" },
                Guid.NewGuid()));
    }

    [Test]
    public void ProcessRefund_NoPaymentRecord_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>())).ReturnsAsync((PaymentTransaction?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ProcessRefundAsync(
                new RefundRequestDto { OrderId = Guid.NewGuid(), RefundAmount = 100m, Reason = "Test" },
                Guid.NewGuid()));
    }
}
