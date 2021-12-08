
namespace Sitecore.Support.ASL
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.Web.UI;
  using Sitecore;
  using Sitecore.Annotations;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Resources;
  using Sitecore.SecurityModel;
  using Sitecore.Shell.Applications.ContentEditor;
  using Sitecore.Support.ASL.Datasource;
  using Sitecore.Support.ASL.ParamsStorage;
  using Sitecore.Text;
  using Sitecore.Web.UI.Sheer;

  /// <summary>
  /// Custom Control which is used to render view of the field in content editor
  /// <para>Uses Search tab in order to get list of selected values.</para>
  /// </summary>
  public class AdvancedSearchListControl : Web.UI.HtmlControls.Control, IContentField
  {
    protected static IDataSourceReader sourceFieldSource = new MlsDataSource();
    
    protected static IDataSourceReader filterFieldSource = new PersistentBucketFilterField();

    public AdvancedSearchListControl()
    {
      this.Class = "scContentControlMultilist";
      this.Activation = true;
    }

    public static readonly string SelectItemsDialogPath = "/sitecore/shell/default.aspx?xmlcontrol=ASLSelectItems";

    public static readonly char[] Sep = { '|' };

    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      
      // To track changes of Up and Down actions
      this.UpdateValueFromClient();
    }

    private void RenderInitJSDoRender(HtmlTextWriter output)
    {
      output.Write(@"<script type='text/javascript'>
          (function(){
              if (!document.getElementById('ASLControlJS')) {
                var head = document.getElementsByTagName('head')[0];
                head.appendChild(new Element('script', { type: 'text/javascript', src: '/sitecore/shell/Applications/Buckets/ASL/ASLControl.js', id: 'ASLControlJs' }));
              }
          }());</script>");
    }

    private void UpdateValueFromClient()
    {
      string value = Sitecore.Context.ClientPage.ClientRequest.Form[this.ID + "_Value"];
      if (value != null && this.Value != value)
      {
        this.SetModified();
        // TODO Validation of the new value
        this.Value = value;
      }
    }

    private void UpdateValueToClient(string newValue)
    {
      var selectedIds = ExtractSelectedIdsFromString(newValue);
      this.SetModified();
      this.Value = newValue;

      var sb = new StringBuilder();
      foreach (var id in selectedIds)
      {
        this.RenderOption(id, sb);
      }

      // TODO improve
      Sitecore.Context.ClientPage.ClientResponse.SetAttribute(this.ID + "_Value", "value", newValue);
      Sitecore.Context.ClientPage.ClientResponse.SetInnerHtml(this.ID + "_selected", sb.ToString());
    }


    protected override void DoRender(HtmlTextWriter output)
    {
      this.RenderInitJSDoRender(output);

      Assert.ArgumentNotNull(output, "output");
      
      this.ServerProperties["ID"] = this.ID;
      string str = string.Empty;
      if (this.ReadOnly)
      {
        str = " disabled=\"disabled\"";
      }

      output.Write("<input id=\"" + this.ID + "_Value\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(this.Value) + "\" />");
      output.Write("<div class='scContentControlMultilistContainer'>");
      output.Write("<table" + this.GetControlAttributes() + "style=\"min-height:182px;\" >");
      output.Write("<colgroup><col style=\"width:1%\"/><col/></colgroup>");
      output.Write("<tr>");
      output.Write("<td width=\"20\">" + Images.GetSpacer(20, 1) + "</td>");
      output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">" + Translate.Text("Selected") + "</td>");
      output.Write("</tr>");
      output.Write("<tr>");
      output.Write("<td valign=\"top\">");
      this.RenderButton(output, "ASL/16x16/delete.png", "javascript:ASLC.deleteCurrent('" + this.ID + "')");
      output.Write("<br />");
      this.RenderButton(output, "ASL/16x16/navigate_up.png", "javascript:scContent.multilistMoveUp('" + this.ID + "')");
      output.Write("<br />");
      this.RenderButton(output, "ASL/16x16/navigate_down.png", "javascript:scContent.multilistMoveDown('" + this.ID + "')");
      output.Write("</td>");
      output.Write("<td valign=\"top\" height=\"100%\">");
      output.Write("<select id=\"" + this.ID + "_selected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"" + str + " ondblclick=\"javascript:ASLC.openCurrent('" + this.ID + "')\" >");

      var selectedIds = ExtractSelectedIdsFromString(this.Value);
      var sb = new StringBuilder();
      foreach (var id in selectedIds)
      {
        this.RenderOption(id, sb);
      }

      output.Write(sb);
      output.Write("</select>");
      output.Write("</td>");
      output.Write("</tr>");
      output.Write("</table>");
      output.Write("</div>");
    }

    private void RenderButton(HtmlTextWriter output, string icon, string click)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(icon, "icon");
      Assert.ArgumentNotNull(click, "click");
      var builder = new ImageBuilder
      {
        Src = icon,
        Class = "scNavButton",
        Width = 0x10,
        Height = 0x10,
        Margin = "2px"
      };
      if (!this.ReadOnly)
      {
        builder.OnClick = click;
      }

      output.Write(builder.ToString());
    }

    public static IEnumerable<ID> ExtractSelectedIdsFromString(string value)
    {
      // TODO Remove duplicates
      Assert.IsNotNull(value, "value");
      if (String.IsNullOrWhiteSpace(value))
      {
        return new List<ID>();
      }

      var strIds = value.Split(Sep, StringSplitOptions.RemoveEmptyEntries);

      var ids = new List<ID>(strIds.Length);
      foreach (var strId in strIds)
      {
        ID id;
        if (!Data.ID.TryParse(strId, out id))
        {
          Log.Debug(string.Format("AdvancedSearchListControl: Can't parse ID:'{0}'", strId));
          continue;
        }
        else
        {
          ids.Add(id);
        }
      }

      return ids;
    }


    public override void HandleMessage(Message message)
    {
      base.HandleMessage(message);

      if (message["id"] != this.ID)
      {
        return;
      }

      switch (message.Name)
      {
        case "searchlist:opensearch":
          // Handle Select Items command to open search dialog
          Sitecore.Context.ClientPage.Start(this, "SelectItems");
          return;

        case "searchlist:unselectall":
          // Handle the Unselect All command
          Sitecore.Context.ClientPage.Start(this, "UnselectAll");
          return;
      }
    }
 
    protected void RenderOption(ID id, StringBuilder sb)
    {
      Item item;
      using (new LanguageSwitcher(this.ItemLanguage))
      {
        item = Sitecore.Context.ContentDatabase.GetItem(id);
      }

      if (item != null)
      {
        sb.AppendFormat("<option value=\"{0}\">{1}</option>", item.ID, item.DisplayName);
      }
      else
      {
        sb.AppendFormat("<option value=\"{0}\">{0} {1}</option>", id, Translate.Text("[Item not found]"));
      }
    }

    protected string RetrieveFilters(out string indexName, out ID rootItemId)
    {
      var itemIdParsed = Sitecore.Data.ID.Parse(this.ItemID);
      var fieldIdParsed = Sitecore.Data.ID.Parse(this.FieldID);

      using (new SecurityDisabler())
      {
        // TODO Validate if content db must be used
        var db = Sitecore.Context.ContentDatabase;
        var item = db.GetItem(itemIdParsed);
        var fieldItem = db.GetItem(fieldIdParsed);

        // sourceFieldSource's are ordered in such way because filterFieldSource uses a token which exactly identifies the source
        if (filterFieldSource.IsUsed(item, (TemplateFieldItem)fieldItem))
        {
          return filterFieldSource.GetDatasource(item, (TemplateFieldItem)fieldItem, out indexName, out rootItemId);
        }

        if (sourceFieldSource.IsUsed(item, (TemplateFieldItem)fieldItem))
        {
          return sourceFieldSource.GetDatasource(item, (TemplateFieldItem)fieldItem, out indexName, out rootItemId);
        }
      }

      CrawlingLog.Log.Warn("SLA Can't identify source of parameters for the field");
      indexName = null;
      rootItemId = null;
      
      return null;
    }

    [UsedImplicitly]
    private void SelectItems(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack)
      {
        if (args.Result != "undefined" && args.Result != null && this.Value != args.Result)
        {
          this.UpdateValueToClient(args.Result);
        }
      }
      else
      {
        this.UpdateValueFromClient();

        string indexName;
        ID indexResolveItem;
        var filters = this.RetrieveFilters(out indexName, out indexResolveItem);
        // TODO LOG DEBUG ASL Control Parameters 
        
        var storage = ASLSpecificStorage.GetStorage();
        storage.Value = this.Value;
        storage.FieldId = Sitecore.Data.ID.Parse(this.FieldID);
        storage.IndexName = indexName;
        storage.IndexResolveItem = indexResolveItem;
        storage.Filters = filters;

        var urlString = new UrlString(AdvancedSearchListControl.SelectItemsDialogPath);
        storage.InnerStorage.Flush(ref urlString);
        
        Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), "1050", "700", string.Empty, true);
        args.WaitForPostBack();
      }
    }

    [UsedImplicitly]
    private void UnselectAll(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.IsPostBack)
      {
        if (this.Value.Length == 0)
        {
          return;
        }

        this.UpdateValueToClient(string.Empty);
      }
    }

    public bool ReadOnly { get; set; }

    public string ItemID
    {
      get
      {
        return StringUtil.GetString(this.ViewState["ItemID"]);
      }

      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["ItemID"] = value;
      }
    }

    public string ItemLanguage
    {
      get
      {
        return StringUtil.GetString(this.ViewState["ItemLanguage"]);
      }

      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["ItemLanguage"] = value;
      }
    }

    public string Source
    {
      get
      {
        return StringUtil.GetString(this.GetViewStateProperty("Source", string.Empty));
      }

      set
      {
        if (value != this.Value)
        {
          this.SetViewStateProperty("Source", value, string.Empty);
        }
      }
    }

    public string FieldID
    {
      get
      {
        return StringUtil.GetString(this.ViewState["FieldID"]);
      }

      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["FieldID"] = value;
      }
    }

    protected void SetModified()
    {
      Sitecore.Context.ClientPage.Modified = true;
    }

    public void SetValue(string value)
    {
      Assert.ArgumentNotNull(value, "value");
      this.Value = value;
    }

    public string GetValue()
    {
      return this.Value;
    }
  }
}
