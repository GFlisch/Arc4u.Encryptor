﻿// Licensed to the Arc4u Foundation under one or more agreements.
// The Arc4u Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Arc4u.Encryptor;
using FluentResults;
using McMaster.Extensions.CommandLineUtils;
using Arc4u.Diagnostics;
using Arc4u.Results;
using Microsoft.Extensions.Logging;
using Arc4u.Security.Cryptography;

namespace Arc4u.Cyphertool.Helpers
{
    internal class ExtractCertificateHelper
    {
        public ExtractCertificateHelper(CertificateHelper certificateHelper, ILogger<CertificateHelper> logger)
        {
            _certificateHelper = certificateHelper;
            _logger = logger;
        }

        readonly ILogger<CertificateHelper> _logger;
        readonly CertificateHelper _certificateHelper;
        public int ExtractCertificatePems(CommandArgument<string> certifcate, CommandOption passwordOption, CommandOption folderOption, CommandOption caOption, X509Certificate2? x509Encrypt = null)
        {
            Result result = Result.Ok();
            CheckCertificateArgument(certifcate)
                .LogIfFailed()
                .OnFailed(result)
                .OnSuccessNotNull(cert =>
                {
                    _certificateHelper.GetCertificate(cert, passwordOption?.Value(), null, null, true)
                          .LogIfFailed()
                          .OnFailed(result)
                          .OnSuccessNotNull(x509 =>
                          {
                              _logger.Technical().LogInformation("The certificate '{subject}' has been loaded!", x509.Subject);

                              var folder = folderOption.Value();
                              bool saveToFolder = false;
                              if (folderOption.HasValue())
                              {
                                  if (!Directory.Exists(folder))
                                  {
                                      _logger.Technical().LogError($"The folder '{folder}' does not exist!");
                                      _logger.Technical().LogInformation("The pem files will be display to the console.");
                                  }
                                  else
                                  {
                                      saveToFolder = true;
                                  }

                                  _logger.Technical().LogInformation("The keys will be stored in the folder '{folder}'.", folder!);
                              }

                              ExtractPublicKey(x509, folder, saveToFolder);

                              ExtractPrivateKey(x509, folder, saveToFolder, x509Encrypt);

                              ExtractCertificateAuthorities(x509, caOption, folder, saveToFolder);
                          });
                });


            return result.IsSuccess ? 1 : -1;
        }

        
        private Result<string> CheckCertificateArgument(CommandArgument<string> certifcate)
        {
            if (null == certifcate?.Value)
            {
                return Result.Fail("The certificate is missing!");
            }

            if (!File.Exists(certifcate.Value))
            {
                return Result.Fail($"The certificate file '{certifcate.Value}' does not exist!");
            }

            return Result.Ok(certifcate.Value);
        }

        private void ExtractCertificateAuthorities(X509Certificate2 x509, CommandOption caOption, string? folder, bool saveToFolder)
        {
            if (caOption.HasValue())
            {
                // chain.
                var chain = new X509Chain();
                chain.Build(x509);
                int idx = 1;
                if (chain.ChainElements.Count > 1)
                {
                    _logger.Technical().LogInformation("Extract the CA certificates");

                    foreach (var element in chain.ChainElements.Skip(1))
                    {
                        _logger.Technical().LogInformation("{idx}: Certificate Subject: {subject}", idx++, element.Certificate.Subject);
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (var element in chain.ChainElements.Skip(1))
                    {

                        _certificateHelper.ConvertPublicKeyToPem(element.Certificate)
                                          .LogIfFailed()
                                          .OnSuccessNotNull((pem) =>
                                          {
                                              sb.Append(pem);
                                          });
                    }
                    if (saveToFolder)
                    {
                        var fileName = Path.Combine(folder!, $"{x509.FriendlyName}.ca.pem");
                        _logger.Technical().LogInformation("Save certificates authority public keys to folder {folder} with name {name}", folder!, fileName);
                        File.WriteAllText(Path.Combine(folder!, fileName),
                                          sb.ToString());
                    }
                    else
                    {
                        Console.Write(sb.ToString());
                    }
                }
            }
        }

        private void ExtractPrivateKey(X509Certificate2 x509, string? folder, bool saveToFolder, X509Certificate2? x509Encrypt = null)
        {
            if (!x509.HasPrivateKey)
            {
                _logger.Technical().LogWarning("The certificate doesn't have a private key.");
                return;
            }

            _certificateHelper.ConvertPrivateKeyToPem(x509)
                              .LogIfFailed()
                              .OnSuccessNotNull((pem) =>
                              {
                                  var folderInfo = "Save private key to folder {folder} with name {name}";
                                  var consoleInfo = "Extract private key.";
                                  var content = pem;
                                  if (x509Encrypt is not null)
                                  {
                                      Result.Try(() => x509Encrypt.Encrypt(pem))
                                            .LogIfFailed()
                                            .OnSuccessNotNull(encrypted =>
                                            {
                                                content = encrypted;
                                                folderInfo = "Save encrypted private key to folder {folder} with name {name}";
                                                consoleInfo = "Extract encrypted private key.";
                                            });
                                        

                                  }


                                  if (saveToFolder)
                                  {
                                      var fileName = Path.Combine(folder!, $"{x509.FriendlyName}.key.pem");
                                      _logger.Technical().LogInformation(folderInfo, folder!, fileName);
                                      File.WriteAllText(Path.Combine(folder!, fileName),
                                                        content);
                                  }
                                  else
                                  {
                                      _logger.Technical().LogInformation(consoleInfo);
                                      Console.WriteLine(content);
                                  }
                              });
        }

        private void ExtractPublicKey(X509Certificate2 x509, string? folder, bool saveToFolder) => _certificateHelper.ConvertPublicKeyToPem(x509)
                                                                  .LogIfFailed()
                                                                  .OnSuccessNotNull((pem) =>
                                                                  {
                                                                      if (saveToFolder)
                                                                      {
                                                                          var fileName = Path.Combine(folder!, $"{x509.FriendlyName}.pem");
                                                                          _logger.Technical().LogInformation("Save public key to folder {folder} with name {name}", folder!, fileName);
                                                                          File.WriteAllText(Path.Combine(folder!, fileName), pem);
                                                                      }
                                                                      else
                                                                      {
                                                                          _logger.Technical().LogInformation("Extract public key.");
                                                                          Console.WriteLine(pem);
                                                                      }
                                                                  });
    }
}