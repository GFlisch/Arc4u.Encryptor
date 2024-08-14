﻿// Licensed to the Arc4u Foundation under one or more agreements.
// The Arc4u Foundation licenses this file to you under the MIT license.

using Arc4u.Cyphertool.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Arc4u.Cyphertool.Commands;

internal class ExtractWithCommand
{
    public ExtractWithCommand(ILogger<EncryptWithCommand> logger,
                              ExtractWithPfxFileCommand pfxFileCommand)
    {
        _logger = logger;
        _fromPfxFileCommand = pfxFileCommand;
    }

    readonly ExtractWithPfxFileCommand _fromPfxFileCommand;
    readonly ILogger<EncryptWithCommand> _logger;

    const string pfxFileCommand = "pfx";
    public void Configure(CommandLineApplication cmd)
    {
        cmd.FullName = nameof(ExtractWithCommand);
        cmd.Description = "ExtractCommand";
        cmd.HelpOption();

        cmd.Command(pfxFileCommand, _fromPfxFileCommand.Configure);

        cmd.OnExecute(() =>
        {
            cmd.ShowHelp();
            return 0;
        });
    }

}
