using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.commands;

// TODO: Don't output list of packages to console
// TODO: Update top level help document with export command
namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("export", "exports list of currently installed packages")]
    public class ChocolateyExportCommand : ICommand
    {
        private readonly INugetService _nugetService;

        public ChocolateyExportCommand(INugetService nugetService)
        {
            _nugetService = nugetService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            // TODO: Add optionset for two new arguments
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            if (unparsedArguments.Count > 0)
            {
                throw new ApplicationException("Please see the help menu for the export command");
            }

            configuration.ListCommand.LocalOnly = true;

            // TODO: Handle parsing of --output-file-name, --include-version-numbers
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            // TODO: Add actual help command
            this.Log().Info("Export Command");
        }

        public bool may_require_admin_access()
        {
            return false;
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }

        public void run(ChocolateyConfiguration configuration)
        {
            var packageResults = _nugetService.list_run(configuration).ToList();

            var settings = new XmlWriterSettings { Indent = true };

            // TODO: Use FileSystem abstraction, rather than direct
            using (var xw = XmlWriter.Create("c:/temp/packages.config", settings))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("packages");

                foreach (var packageResult in packageResults)
                {
                    xw.WriteStartElement("package");
                    xw.WriteAttributeString("id", packageResult.Package.Id);
                    // TODO: Add parameter to specify whether to include version number or not
                    //xw.WriteAttributeString("version", package.Version.ToString());
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }
        }
    }
}
