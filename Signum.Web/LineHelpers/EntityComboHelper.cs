﻿#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Engine;
using System.Configuration;
using System.Web;
using Signum.Engine.DynamicQuery;
#endregion

namespace Signum.Web
{
    public static class EntityComboHelper
    {
        internal static MvcHtmlString InternalEntityCombo(this HtmlHelper helper, EntityCombo entityCombo)
        {
            if (!entityCombo.Visible || entityCombo.HideIfNull && entityCombo.UntypedValue == null)
                return MvcHtmlString.Empty;

            if (!entityCombo.Type.IsIIdentifiable() && !entityCombo.Type.IsLite())
                throw new InvalidOperationException("EntityCombo can only be done for an identifiable or a lite, not for {0}".Formato(entityCombo.Type.CleanType()));

            HtmlStringBuilder sb = new HtmlStringBuilder();
            sb.AddLine(helper.HiddenRuntimeInfo(entityCombo));

            if (EntityBaseHelper.EmbeddedOrNew((Modifiable)entityCombo.UntypedValue))
                sb.AddLine(EntityBaseHelper.RenderPopup(helper, (TypeContext)entityCombo.Parent, RenderPopupMode.PopupInDiv, entityCombo));
            else if (entityCombo.UntypedValue != null)
                sb.AddLine(helper.Div(entityCombo.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

            if (entityCombo.ReadOnly)
                sb.AddLine(helper.FormControlStatic(entityCombo.Compose(EntityBaseKeys.ToStr), entityCombo.UntypedValue.TryToString()));
            else
            {
                List<SelectListItem> items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "" });
                if (entityCombo.Preload)
                {
                    int? id = entityCombo.IdOrNull;

                    IEnumerable<Lite<IIdentifiable>> data = entityCombo.Data ?? AutocompleteUtils.FindAllLite(entityCombo.Implementations.Value);

                    items.AddRange(
                        data.Select(lite => new SelectListItem()
                            {
                                Text = lite.ToString(),
                                Value = lite.Key(),
                                Selected = lite.IdOrNull == entityCombo.IdOrNull
                            })
                        );
                }

                entityCombo.ComboHtmlProperties.AddCssClass("form-control");

                if (entityCombo.ComboHtmlProperties.ContainsKey("onchange"))
                    throw new InvalidOperationException("EntityCombo cannot have onchange html property, use onEntityChanged instead");

                entityCombo.ComboHtmlProperties.Add("onchange", entityCombo.SFControlThen("combo_selected()"));

                if (entityCombo.Size > 0)
                {
                    entityCombo.ComboHtmlProperties.AddCssClass("sf-entity-list");
                    entityCombo.ComboHtmlProperties.Add("size", Math.Min(entityCombo.Size, items.Count - 1));
                }

                sb.AddLine(helper.DropDownList(
                        entityCombo.Compose(EntityComboKeys.Combo),
                        items,
                        entityCombo.ComboHtmlProperties));
            }

            sb.AddLine(EntityBaseHelper.ViewButton(helper, entityCombo));
            sb.AddLine(EntityBaseHelper.CreateButton(helper, entityCombo));

            if (entityCombo.ShowValidationMessage)
            {
                sb.AddLine(helper.ValidationMessage(entityCombo.Prefix));
            }

            sb.AddLine(entityCombo.ConstructorScript(JsFunction.LinesModule, "EntityCombo"));

            return helper.FormGroup(entityCombo, entityCombo.Prefix, entityCombo.LabelText, sb.ToHtml());
        }

        public static MvcHtmlString EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityCombo<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityCombo ec = new EntityCombo(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(ec, ec.CleanRuntimeType ?? ec.Type.CleanType());

            Common.FireCommonTasks(ec);

            if (settingsModifier != null)
                settingsModifier(ec);

            var result = helper.InternalEntityCombo(ec);

            var vo = ec.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(ec.PropertyRoute, helper, tc, result);
        }

        public static MvcHtmlString RenderOption(this SelectListItem item)
        {
            HtmlTag builder = new HtmlTag("option").SetInnerText(item.Text);

            if (item.Value != null)
                builder.Attr("value", item.Value);

            if (item.Selected)
                builder.Attr("selected", "selected");

            return builder.ToHtml();
        }

        public static SelectListItem ToSelectListItem<T>(this Lite<T> lite, Lite<T> selected) where T : class, IIdentifiable
        {
            return new SelectListItem { Text = lite.ToString(), Value = lite.Id.ToString(), Selected = selected.Is(lite) };
        }

        public static MvcHtmlString ToOptions<T>(this IEnumerable<Lite<T>> lites, Lite<T> selectedElement) where T : class, IIdentifiable
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (selectedElement == null || !lites.Contains(selectedElement))
                list.Add(new SelectListItem { Text = "-", Value = "" });

            list.AddRange(lites.Select(l => l.ToSelectListItem(selectedElement)));

            return new HtmlStringBuilder(list.Select(RenderOption)).ToHtml();
        }
    }
}
