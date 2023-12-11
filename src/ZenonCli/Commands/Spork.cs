using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Spork
    {
        [Verb("spork.list", HelpText = "List all sporks.")]
        public class List : ConnectionCommand
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "pageSize")]
            public int? PageSize { get; set; }

            protected override async Task ProcessAsync()
            {
                if (!this.PageIndex.HasValue)
                    this.PageIndex = 0;

                if (!this.PageSize.HasValue)
                    this.PageSize = 25;

                AssertPageRange(PageIndex.Value, PageSize.Value);

                var result = await Zdk!.Embedded.Spork
                    .GetAll((uint)PageIndex.Value, (uint)PageSize.Value);

                if (result == null || result.Count == 0)
                {
                    WriteInfo("No sporks found");
                    return;
                }

                WriteInfo("Sporks:");

                foreach (var spork in result.List)
                {
                    WriteInfo($"Name: {spork.Name}");
                    WriteInfo($"  Description: {spork.Description}");
                    WriteInfo($"  Activated: {spork.Activated}");
                    if (spork.Activated)
                        WriteInfo($"  EnforcementHeight: {spork.EnforcementHeight}");
                    WriteInfo($"  Hash: {spork.Id}");
                }
            }
        }

        [Verb("spork.create", HelpText = "Create a new spork.")]
        public class Create : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            [Value(1, Required = true, MetaName = "description")]
            public string? Description { get; set; }

            protected override async Task ProcessAsync()
            {
                var name = this.Name!;
                var description = this.Description!;

                if (name.Length < Constants.SporkNameMinLength ||
                    name.Length > Constants.SporkNameMaxLength)
                {
                    WriteInfo($"Spork name must be {Constants.SporkNameMinLength} to {Constants.SporkNameMaxLength} characters in length");
                    return;
                }

                if (String.IsNullOrEmpty(description))
                {
                    WriteInfo($"Spork description cannot be empty");
                    return;
                }

                if (description.Length > Constants.SporkDescriptionMaxLength)
                {
                    WriteInfo($"Spork description cannot exceed {Constants.SporkDescriptionMaxLength} characters in length");
                    return;
                }

                WriteInfo("Creating spork ...");
                await SendAsync(Zdk!.Embedded.Spork.CreateSpork(name, description));
                WriteInfo("Done");
            }
        }

        [Verb("spork.activate", HelpText = "Activate a spork.")]
        public class Activate : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the spork to activate.")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var id = ParseHash(Id, "id");

                WriteInfo("Activating spork ...");
                await SendAsync(Zdk!.Embedded.Spork.ActivateSpork(id));
                WriteInfo("Done");
            }
        }
    }
}
