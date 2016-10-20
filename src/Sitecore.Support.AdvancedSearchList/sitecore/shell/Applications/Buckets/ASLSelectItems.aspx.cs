
namespace Sitecore.Support.ASL
{
  using System;
  using System.Text;
  using System.Web.UI;
  using Sitecore.Buckets.Client.sitecore_modules.Shell.Sitecore.Buckets;
  using Sitecore.Buckets.Pipelines.UI.ExpandIdBasedSearchFilters;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.SecurityModel;
  using Sitecore.StringExtensions;
  using Sitecore.Support.ASL.ParamsStorage;
  using Sitecore.Web;

  public class ASLSelectItems : Sitecore.sitecore.admin.AdminPage
  {
    private static readonly char[] aslValueSeparator = new char[] {'|'}; 

    protected override void OnInit(EventArgs arguments)
    {
      Assert.ArgumentNotNull(arguments, "arguments");
      if (!Sitecore.Context.User.IsAuthenticated)
      {
        this.CheckSecurity(true);
      }

      base.OnInit(arguments);
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      if (this.IsPostBack)
      {
        // Required since if Enter is pressed in the dialogue, a post back is sent 
        return;
      }

      var storage = ASLSpecificStorage.GetStorage();

      var index = this.PrepareIndexParameter(storage.IndexName);

      string defaultFilters, persistFilters;
      using (new SecurityDisabler())
      {
        var contentDatabase = Sitecore.Context.ContentDatabase;

        persistFilters = storage.Filters;
        persistFilters = this.PrepareClauses(contentDatabase, persistFilters);

        defaultFilters = this.GetDefaultClauses(storage.FieldId);
        defaultFilters = this.PrepareClauses(contentDatabase, defaultFilters);
      }

      var ids = this.PrepareSelectedIds(storage.Value);

      // TODO Check this
      storage.InnerStorage.Clear();

      string content = string.Format("<script type='text/javascript' language='javascript'>window.SC = window.SC || {{}}; SC.baseItemIconPath = ''; var filterForSearch='{0}';var filterForAllSearch='{1}'; var selectedIds = [{2}]; {3}</script>",
        defaultFilters, persistFilters, ids, index);

      this.AddToHead(content);
    }

    protected void AddToHead(string content)
    {
      ((ItemBucketsSearchResult)base.Master).DynamicHeadPlaceholder.Controls.Add(new LiteralControl(content));
    }

    protected string ExtractDefaultFilter(Item fieldItem)
    {
      Assert.ArgumentNotNull(fieldItem, "Field must exist...");

      var contentDatabase = Sitecore.Context.ContentDatabase;
      Assert.IsNotNull(contentDatabase, "Content Database must exist...");
      
      Field field = fieldItem.Fields[Sitecore.Buckets.Util.Constants.DefaultQuery];
      if (field == null)
      {
        return string.Empty;
      }

      var args = new ExpandIdBasedSearchFiltersArgs(field.Value, contentDatabase);
      ExpandIdBasedSearchFiltersPipeline.Run(args);
      return args.ExpandedFilters;
    }

    protected string PrepareClauses(Database db, string value)
    {
      Assert.ArgumentNotNull(db, "db");

      var args = new ExpandIdBasedSearchFiltersArgs(value, db);
      ExpandIdBasedSearchFiltersPipeline.Run(args);
      return WebUtil.EscapeJavascriptString(args.ExpandedFilters);
    }

   protected string GetDefaultClauses(ID fieldId)
   {
     // TODO Check if content db must be used here 
     var db = Sitecore.Context.ContentDatabase;
     Assert.IsNotNull(db, "Can't get the content database");

     var fieldItem = db.GetItem(fieldId);
     Field field = fieldItem.Fields[Sitecore.Buckets.Util.Constants.DefaultQuery];

     Assert.IsNotNull(field, "Can't get the DefaultQuery field...");
     return field.Value;
   }

   protected string PrepareSelectedIds(string value)
   {
     if (value.IsNullOrEmpty())
     {
       return string.Empty;
     }

     var ids = value.Split(aslValueSeparator, StringSplitOptions.RemoveEmptyEntries);

     if (ids.Length == 0)
     {
       return string.Empty;
     }

     var sb = new StringBuilder(43 * ids.Length);
     foreach (var id in ids)
     {
       if (Sitecore.Data.ID.IsID(id))
       {
         sb.AppendFormat("\"{0}\",", WebUtil.EscapeJavascriptString(id));
       }
       else
       {
         CrawlingLog.Log.Warn(string.Format("ASL Can't parse ID '{0}' from the field value. It will be skipped", id));
       }
     }

     // sb.Length - 1 to remove the ending comma
     return sb.ToString(0, sb.Length - 1);
   }

   protected string PrepareIndexParameter(string value)
   {
     return value.IsNullOrEmpty() ? string.Empty : string.Format("var aslIndex = '{0}';", WebUtil.EscapeJavascriptString(value));
   }
  }

}
