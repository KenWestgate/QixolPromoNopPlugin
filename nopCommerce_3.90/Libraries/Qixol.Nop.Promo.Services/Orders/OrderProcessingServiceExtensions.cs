﻿using System;
using System.Linq;
using global::Nop.Core;
using global::Nop.Core.Domain.Customers;
using global::Nop.Core.Domain.Orders;
using global::Nop.Services.Common;
using global::Nop.Services.Orders;
using Qixol.Nop.Promo.Core.Domain.Promo;
using Qixol.Promo.Integration.Lib.Basket;
using Qixol.Nop.Promo.Core.Domain.Orders;

namespace Qixol.Nop.Promo.Services.Orders
{
    public partial class OrderProcessingService : global::Nop.Services.Orders.OrderProcessingService, IOrderProcessingService
    {
        private int CalculateRewardPoints(Order order)
        {
            if (order == null)
                return 0;

            var promoOrder = _promoOrderService.GetPromoOrderByOrderId(order.Id);

            if (promoOrder == null)
                return 0;

            var basketResponse = BasketResponse.FromXml(promoOrder.ResponseXml);

            return (int)basketResponse.TotalIssuedPoints;
        }

        private BasketResponse PromoSaveOrderDetails(Order order)
        {
            var basketResponse = _promoService.SendConfirmedBasket(order);

            if (basketResponse == null)
            {
                throw new NopException(string.Format("Failed to create PromoOrder for order: {0}", order.Id));
            }
            else
            {
                PromoOrder promoOrder = new PromoOrder()
                {
                    RequestXml = order.Customer.GetAttribute<string>(PromoCustomerAttributeNames.PromoBasketRequest, _genericAttributeService, _storeContext.CurrentStore.Id),
                    ResponseXml = basketResponse.ToXml(),
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    DeliveryOriginalPrice = basketResponse.DeliveryOriginalPrice
                };

                _promoOrderService.InsertPromoOrder(promoOrder);

                basketResponse.Items.ForEach(bi =>
                {
                    PromoOrderItem promoOrderItem = new PromoOrderItem()
                    {
                        LineAmount = bi.LineAmount,
                        OrderId = order.Id,
                        PromoOrderId = promoOrder.Id,
                        IsDelivery = bi.IsDelivery,
                        ProductCode = bi.ProductCode,
                        VariantCode = bi.VariantCode,
                        LinePromotionDiscount = bi.LinePromotionDiscount,
                        Barcode = bi.Barcode,
                        Generated = bi.Generated,
                        ManualDiscount = bi.ManualDiscount,
                        OriginalAmount = bi.OriginalAmount,
                        OriginalPrice = bi.OriginalPrice,
                        OriginalQuantity = bi.OriginalQuantity,
                        Price = bi.Price,
                        ProductDescription = bi.ProductDescription,
                        Quantity = bi.Quantity,
                        SplitFromLineId = bi.SplitFromLineId,
                        TotalDiscount = bi.TotalDiscount,
                        TotalIssuedPoints = bi.TotalIssuedPoints
                    };

                    promoOrder.PromoOrderItems.Add(promoOrderItem);
                    _promoOrderService.UpdatePromoOrder(promoOrder);

                    bi.AppliedPromotions.ForEach(ap =>
                    {
                        string promotionTypeDisplay = string.Empty;
                        string promotionType = string.Empty;
                        string promotionName = string.Empty;
                        string promotionDisplayText = string.Empty;
                        string externalIdentifier = string.Empty;
                        string reportingGroupCode = string.Empty;

                        var appliedPromo = (from sap in basketResponse.Summary.AppliedPromotions where sap.PromotionId == ap.PromotionId select sap).FirstOrDefault();
                        if (appliedPromo != null)
                        {
                            promotionName = appliedPromo.PromotionName;
                            promotionType = appliedPromo.PromotionType;
                            promotionTypeDisplay = appliedPromo.PromotionTypeDisplay;
                            promotionDisplayText = appliedPromo.DisplayText;
                            externalIdentifier = appliedPromo.ExternalIdentifier;
                            reportingGroupCode = appliedPromo.ReportingGroupCode;
                        }

                        PromoOrderItemPromotion promoOrderItemPromotion = new PromoOrderItemPromotion()
                        {
                            BasketLevel = ap.BasketLevelPromotion,
                            DeliveryLevel = ap.DeliveryLevelPromotion,
                            DiscountAmount = ap.DiscountAmount,
                            Instance = ap.InstanceId,
                            PointsIssued = ap.PointsIssued,
                            PromotionId = ap.PromotionId,
                            DisplayText = promotionDisplayText,
                            PromotionTypeDisplay = promotionTypeDisplay,
                            PromotionName = promotionName,
                            PromotionType = promotionType,
                            ExternalIdentifier = externalIdentifier,
                            ReportingGroupCode = reportingGroupCode
                        };
                        promoOrderItem.PromoOrderItemPromotions.Add(promoOrderItemPromotion);
                        _promoOrderService.UpdatePromoOrder(promoOrder);
                    });
                });

                basketResponse.Coupons.ForEach(c =>
                {
                    PromoOrderCoupon promoOrderCoupon = new PromoOrderCoupon()
                    {
                        CouponCode = c.CouponCode,
                        Issued = c.Issued,
                        OrderId = order.Id,
                        PromoOrderId = promoOrder.Id,
                        CouponName = c.CouponName,
                        IssuedConfirmed = c.IssuedConfirmed,
                        DisplayText = c.DisplayText
                    };
                    promoOrder.PromoOrderCoupons.Add(promoOrderCoupon);
                    _promoOrderService.UpdatePromoOrder(promoOrder);
                });
            }

            #region clean up

            var customer = order.Customer;

            // basket guid
            _genericAttributeService.SaveAttribute<string>(customer, PromoCustomerAttributeNames.PromoBasketUniqueReference, null, _storeContext.CurrentStore.Id);

            // basket response
            _genericAttributeService.SaveAttribute<string>(customer, PromoCustomerAttributeNames.PromoBasketResponse, null, _storeContext.CurrentStore.Id);

            // basket request
            _genericAttributeService.SaveAttribute<string>(customer, PromoCustomerAttributeNames.PromoBasketRequest, null, _storeContext.CurrentStore.Id);

            // coupon code
            _genericAttributeService.SaveAttribute<string>(customer, SystemCustomerAttributeNames.DiscountCouponCode, null);

            #endregion

            return basketResponse;

        }
    }
}
