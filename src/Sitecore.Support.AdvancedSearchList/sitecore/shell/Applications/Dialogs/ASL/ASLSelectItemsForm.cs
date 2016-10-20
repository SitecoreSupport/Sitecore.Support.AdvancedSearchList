
namespace Sitecore.Support.ASL
{
  using System;
  using Sitecore.Data;
  using Sitecore.Diagnostics;
  using Sitecore.Support.ASL.ParamsStorage;
  using Sitecore.Text;
  using Sitecore.Web.UI.HtmlControls;

  /// <summary>
  /// Logic to show select items dialog with search. Used by <see cref="AdvancedSearchListControl"/>
  /// </summary>
  public class ASLSelectItemsForm : Sitecore.Web.UI.Pages.DialogForm
  {
    protected Edit selectedItems;

    protected Frame search;

    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");

      base.OnLoad(e);

      var storage = ASLSpecificStorage.GetStorage();
      var itemId = storage.IndexResolveItem;

      if (itemId != (ID)null)
      {
        // It is required to pass the id parameter for index resolution through the Referrer header
        this.search.SourceUri = this.AppendQueryStringParameter(this.search.SourceUri, "id", itemId.ToString());
      }
    }

    protected override void OnOK(object sender, EventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");

      Context.ClientPage.ClientResponse.SetDialogValue(this.selectedItems.Value);
      base.OnOK(sender, args);
    }

    private string AppendQueryStringParameter(string urlStr, string key, string value)
    {
      Assert.ArgumentNotNull(urlStr, "urlStr");
      Assert.ArgumentNotNull(key, "key");
      Assert.ArgumentNotNull(value, "value");

      var url = new UrlString(urlStr);
      var pV = url.Parameters[key];

      if (pV != null && !string.IsNullOrWhiteSpace(pV))
      {
        return url.ToString();
      }

      url[key] = value;

      return url.ToString();
    }
  }
}
