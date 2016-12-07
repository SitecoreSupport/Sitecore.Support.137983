using System;
using System.Collections.Specialized;
using Sitecore.Configuration;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.Support.Shell.Framework.Commands
{
  [Serializable]
  public class AddMaster : Command
  {
    protected event ItemCreatedDelegate ItemCreated;

    protected void Add(ClientPipelineArgs args)
    {
      if (!SheerResponse.CheckModified()) return;
      var item = Context.ContentDatabase.GetItem(args.Parameters["Master"]);
      if (item == null)
      {
        SheerResponse.Alert(Translate.Text("Branch \"{0}\" not found.", args.Parameters["Master"]));
      }
      else if (item.TemplateID == TemplateIDs.CommandMaster)
      {
        var str = item["Command"];
        if (!string.IsNullOrEmpty(str)) Context.ClientPage.SendMessage(this, str);
      }
      else if (args.IsPostBack)
      {
        if (!args.HasResult) return;

        var str2 = StringUtil.GetString(args.Parameters["ItemID"]);
        var name = StringUtil.GetString(args.Parameters["Language"]);
        var parent = Context.ContentDatabase.Items[str2, Language.Parse(name)];

        if (parent == null)
        {
          SheerResponse.Alert("Parent item not found.");
        }
        else if (!parent.Access.CanCreate())
        {
          Context.ClientPage.ClientResponse.Alert("You do not have permission to create items here.");
        }
        else
        {
          Item item3 = null;

          try
          {
            if (item.TemplateID == TemplateIDs.BranchTemplate)
            {
              BranchItem branch = item;
              item3 = Context.Workflow.AddItem(args.Result, branch, parent);
              Log.Audit(this, "Add from branch: {0}", AuditFormatter.FormatItem(branch));
            }
            else
            {
              TemplateItem template = item;
              item3 = Context.Workflow.AddItem(args.Result, template, parent);
              Log.Audit(this, "Add from template: {0}", AuditFormatter.FormatItem(template));
            }
          }
          catch (WorkflowException exception)
          {
            Log.Error("Workflow error: could not add item from master", exception, this);
            SheerResponse.Alert(exception.Message);
          }

          if ((item3 != null)) ItemCreated?.Invoke(this, new ItemCreatedEventArgs(item3));
        }
      }
      else
      {
        SheerResponse.Input("Enter a name for the new item:", item.DisplayName, Settings.ItemNameValidation, "'$Input' is not a valid name.", Settings.MaxItemNameLength);
        args.WaitForPostBack();
      }
    }

    public override void Execute(CommandContext context)
    {
      if ((context.Items.Length != 1) || !context.Items[0].Access.CanCreate()) return;
      var item = context.Items[0];
      var parameters = new NameValueCollection
      {
        ["Master"] = context.Parameters["master"],
        ["ItemID"] = item.ID.ToString(),
        ["Language"] = item.Language.ToString(),
        ["Version"] = item.Version.ToString()
      };
      Context.ClientPage.Start(this, "Add", parameters);
    }

    public override CommandState QueryState(CommandContext context)
    {
      Error.AssertObject(context, "context");
      if (context.Items.Length != 1) return CommandState.Hidden;
      return !context.Items[0].Access.CanCreate() ? CommandState.Disabled : base.QueryState(context);
    }
  }
}