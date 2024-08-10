﻿// Licensed to the Arc4u Foundation under one or more agreements.
// The Arc4u Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Arc4u.Diagnostics;
using Arc4u.Encryptor;
using Arc4u.Results;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Arc4u.Cyphertool.Commands
{
    internal class ExtractFromPfxFileCommand
    {
        public ExtractFromPfxFileCommand(ILogger<EncryptFromPfxFileCommand> logger,
                                         CertificateHelper certificateHelper)
        {
            _logger = logger;
            _certificateHelper = certificateHelper;
        }

        readonly CertificateHelper _certificateHelper;
        readonly ILogger<EncryptFromPfxFileCommand> _logger;


        public void Configure(CommandLineApplication cmd)
        {
            cmd.FullName = "ExtractFromPfxHelper";
            cmd.HelpOption();

            // Argument
            var certifcate = cmd.Argument<string>("certificate", "The pfx certificate file.");

            // Options
            var passwordOption = cmd.Option("-p | --password", "The password to use for the file pfx certificate", CommandOptionType.SingleValue);
            var folderOption = cmd.Option("-f | --folder", "The folder to store the keys.", CommandOptionType.SingleValue);

            // Display id the certificate exist!
            cmd.OnExecute(() =>
            {
                if (null == certifcate?.Value)
                {
                    _logger.LogError("The certificate is missing!");
                    return -1;
                }

                if (!File.Exists(certifcate.Value))
                {
                    _logger.LogError($"The certificate file '{certifcate.Value}' does not exist!");
                    return -1;
                }

                _certificateHelper.GetCertificate(certifcate.Value, passwordOption?.Value(), null, null, true)
                                  .LogIfFailed()
                                  .OnSuccessNotNull(x509 =>
                                    {
                                        _logger.Technical().LogInformation($"The certificate '{x509.Subject}' has been loaded!");
                                        
                                        var publicKeyPem = Convert.ToBase64String(x509.GetPublicKey());

                                        Console.WriteLine(_certificateHelper.ConvertToPem(publicKeyPem, "PUBLIC KEY", true));

                                        if (!x509.HasPrivateKey)
                                        {
                                            _logger.Technical().LogWarning("The certificate doesn't have a private key.");
                                            return;
                                        }

                                        var privateKey = x509.GetRSAPrivateKey();
                                        if (privateKey is null)
                                        {
                                            _logger.Technical().LogWarning("The certificate doesn't have a RSA private key.");
                                            return;
                                        }
                                        var privateKeyPem = privateKey.ExportRSAPrivateKeyPem();
                                        Console.WriteLine(_certificateHelper.ConvertToPem(privateKeyPem));
                                    });
           
                return 0;
            });
        }

 
    }
}