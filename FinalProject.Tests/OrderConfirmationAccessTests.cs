using System.Reflection;
using System.Security.Claims;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FinalProject.Tests;

public class OrderConfirmationAccessTests
{
    [Fact]
    public void Anonymous_viewer_cannot_view_any_order()
    {
        Assert.False(OrderConfirmationAccess.CanView("owner-1", viewer: null));
        Assert.False(OrderConfirmationAccess.CanView("owner-1", new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void Authenticated_member_cannot_view_another_users_order()
    {
        var otherUser = Principal("intruder-id", AppRoles.Member);
        Assert.False(OrderConfirmationAccess.CanView("owner-1", otherUser));
    }

    [Fact]
    public void Owner_can_view_own_order()
    {
        var owner = Principal("owner-1", AppRoles.Member);
        Assert.True(OrderConfirmationAccess.CanView("owner-1", owner));
    }

    [Fact]
    public void Admin_and_SuperAdmin_can_view_orders_they_do_not_own()
    {
        Assert.True(OrderConfirmationAccess.CanView("owner-1", Principal("admin-id", AppRoles.Admin)));
        Assert.True(OrderConfirmationAccess.CanView("owner-1", Principal("sa-id", AppRoles.SuperAdmin)));
        Assert.True(OrderConfirmationAccess.CanView(null, Principal("admin-id", AppRoles.Admin)));
    }

    [Fact]
    public void Member_cannot_view_guest_order_with_null_user_id()
    {
        Assert.False(OrderConfirmationAccess.CanView(null, Principal("member-id", AppRoles.Member)));
    }

    [Fact]
    public void Confirmation_action_requires_authorize_attribute()
    {
        var method = typeof(CheckoutController).GetMethod(nameof(CheckoutController.Confirmation));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Confirmation_returns_not_found_for_anonymous_caller_when_authorize_is_bypassed()
    {
        var order = SampleOrder(id: 42, userId: "owner-1");
        var controller = CreateController(new FakeOrders(order), anonymous: true);

        var result = await controller.Confirmation(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Confirmation_returns_not_found_when_authenticated_user_accesses_another_users_order()
    {
        var order = SampleOrder(id: 7, userId: "owner-1");
        var controller = CreateController(new FakeOrders(order), userId: "other-user", role: AppRoles.Member);

        var result = await controller.Confirmation(7, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.DoesNotContain("owner@example.com", result.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Confirmation_returns_view_for_order_owner()
    {
        var order = SampleOrder(id: 9, userId: "owner-1");
        var controller = CreateController(new FakeOrders(order), userId: "owner-1", role: AppRoles.Member);

        var result = await controller.Confirmation(9, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(order, view.Model);
    }

    [Fact]
    public async Task Confirmation_returns_not_found_for_missing_order_without_leaking_details()
    {
        var controller = CreateController(new FakeOrders(order: null), userId: "owner-1", role: AppRoles.Member);

        var result = await controller.Confirmation(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static CheckoutController CreateController(IOrderService orders, string? userId = null, string? role = null, bool anonymous = false)
    {
        var controller = new CheckoutController(orders)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = anonymous || userId == null
                        ? new ClaimsPrincipal(new ClaimsIdentity())
                        : Principal(userId, role ?? AppRoles.Member)
                }
            }
        };
        return controller;
    }

    private static ClaimsPrincipal Principal(string userId, string role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userId),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private static Order SampleOrder(int id, string? userId) => new()
    {
        Id = id,
        UserId = userId,
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "owner@example.com",
        PhoneNumber = "+994501112233",
        StreetAddress = "1 Test St",
        City = "Baku",
        District = "Narimanov",
        Subtotal = 10m,
        DiscountAmount = 0m,
        Total = 10m,
        OrderDate = DateTime.UtcNow,
        OrderStatus = "Pending"
    };

    private sealed class FakeOrders : IOrderService
    {
        private readonly Order? _order;

        public FakeOrders(Order? order) => _order = order;

        public Task<Order> PlaceOrderAsync(
            string? userId,
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string streetAddress,
            string city,
            string district,
            string? postCode,
            string? companyName,
            string? orderNotes,
            CartViewModel cart,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_order != null && _order.Id == id ? _order : null);
    }
}
