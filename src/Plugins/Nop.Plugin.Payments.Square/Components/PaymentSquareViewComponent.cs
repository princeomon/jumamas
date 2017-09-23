using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Plugin.Payments.Square.Models;
using Nop.Plugin.Payments.Square.Services;
using Nop.Services.Common;
using Nop.Services.Localization;

namespace Nop.Plugin.Payments.Square.Components
{
    [ViewComponent(Name = "PaymentSquare")]
    public class PaymentSquareViewComponent : ViewComponent
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly SquarePaymentManager _squarePaymentManager;

        #endregion

        #region Ctor

        public PaymentSquareViewComponent(ILocalizationService localizationService,
            IWorkContext workContext,
            SquarePaymentManager squarePaymentManager)
        {
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._squarePaymentManager = squarePaymentManager;
        }

        #endregion

        #region Methods

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();
            
            //get postal code from the billing address or from the shipping one
            model.PostalCode = _workContext.CurrentCustomer.BillingAddress?.ZipPostalCode 
                ?? _workContext.CurrentCustomer.ShippingAddress?.ZipPostalCode;

            //whether customer already has stored cards
            var customerId = _workContext.CurrentCustomer.GetAttribute<string>(SquarePaymentDefaults.CustomerIdAttribute);
            var customer = _squarePaymentManager.GetCustomer(customerId);
            if (customer?.Cards != null)
                model.StoredCards = customer.Cards.Select(card => new SelectListItem { Text = card.Last4, Value = card.Id }).ToList();

            //add special item for 'there are no cards' with value 0
            var noCardText = _localizationService.GetResource("Plugins.Payments.Square.Fields.StoredCard.NotExist");
            model.StoredCards.Insert(0, new SelectListItem { Text = noCardText, Value = "0" });

            //set postback values (we cannot access "Form" with "GET" requests)
            if (this.Request.Method != WebRequestMethods.Http.Get)
            {
                if (this.Request.Form.TryGetValue("StoredCardId", out StringValues storedCardId))
                    model.StoredCardId = storedCardId;

                if (this.Request.Form.TryGetValue("SaveCard", out StringValues saveCardValue) && bool.TryParse(saveCardValue, out bool saveCard))
                    model.SaveCard = saveCard;
            }

            return View("~/Plugins/Payments.Square/Views/PaymentInfo.cshtml", model);
        }

        #endregion
    }
}