using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.filesystem;

// TODO: Update top level help document with export command
namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("export", "exports list of currently installed packages")]
    public class ChocolateyExportCommand : ICommand
    {
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;

        public ChocolateyExportCommand(INugetService nugetService, IFileSystem fileSystem)
        {
            _nugetService = nugetService;
            _fileSystem = fileSystem;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("o=|output-file-path=",
                     "Output File Path - the path to where the list of currently installed packages should be saved. Defaults to packages.config.",
                     option => configuration.ExportCommand.OutputFilePath = option.remove_surrounding_quotes())
                .Add("include-version-numbers",
                     "Include Version Numbers - controls whether or not version numbers for each package appear in generated file.  Defaults to false.",
                     option => configuration.ExportCommand.IncludeVersionNumbers = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            if (unparsedArguments.Count > 0)
            {
                throw new ApplicationException("Please see the help menu for the export command");
            }
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
            // Perhaps output the XML that would have been generated as a result of the operation
            // Similar to choco install fiddler --noop
            throw new NotImplementedException();
        }

        public void run(ChocolateyConfiguration configuration)
        {
            var fileExists = _fileSystem.file_exists(configuration.ExportCommand.OutputFilePath);

            if(fileExists && !configuration.Force)
            {
                throw new Exception("File already exists");
            }

            var packageResults = _nugetService.get_all_installed_packages(configuration);

            var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };

            using (var stringWriter = new StringWriter())
            {
                using (var xw = XmlWriter.Create(stringWriter, settings))
                {
                    xw.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                    xw.WriteStartElement("packages");

                    foreach (var packageResult in packageResults)
                    {
                        xw.WriteStartElement("package");
                        xw.WriteAttributeString("id", packageResult.Package.Id);

                        if (configuration.ExportCommand.IncludeVersionNumbers)
                        {
                            xw.WriteAttributeString("version", packageResult.Package.Version.ToString());
                        }

                        xw.WriteEndElement();
                    }

                    xw.WriteEndElement();
                    xw.Flush();
                }

                // TODO: Instead of deleting directly, move to backup
                if (fileExists)
                {
                    _fileSystem.delete_file(configuration.ExportCommand.OutputFilePath);
                }

                _fileSystem.write_file(
                    configuration.ExportCommand.OutputFilePath,
                    stringWriter.GetStringBuilder().ToString(),
                    new UTF8Encoding(false));
            }
        }
    }
}
