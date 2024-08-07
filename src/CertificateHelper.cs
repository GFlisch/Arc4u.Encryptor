﻿// Licensed to the Arc4u Foundation under one or more agreements.
// The Arc4u Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Arc4u.Security;
using Arc4u.Security.Cryptography;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Arc4u.Encryptor
{
    internal class CertificateHelper
    {
        public CertificateHelper(ILogger<CertificateHelper> logger, IX509CertificateLoader x509CertificateLoader)
        {
            _logger = logger;
            _x509CertificateLoader = x509CertificateLoader;
        }

        readonly ILogger<CertificateHelper> _logger;
        readonly IX509CertificateLoader _x509CertificateLoader;

        public Result<X509Certificate2> GetCertificate([DisallowNull] string cert, string? password, string? storeName, string? storeLocation)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cert);

            // Certificate is coming from the store if the certOption.Value() does not contain a file name ending with .pfx
            bool fromCertStore = !cert.EndsWith(".pfx");

            if (fromCertStore)
            {
                return GetCertificateFromStore(cert, storeName, storeLocation);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    Console.Write("Password:");
                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        // Break the loop if Enter key is pressed
                        if (key.Key == ConsoleKey.Enter)
                            break;
                        password += key.KeyChar;
                        Console.Write("*");
                    }
                    Console.WriteLine("");
                }

                return GetCertificateFromFile(cert, password);
            }
        }

        private Result<X509Certificate2> GetCertificateFromFile(string cert, string? password)
        {
            if (!File.Exists(cert))
            {
                return Result.Fail($"The certificate file {cert} does not exist!");
            }

            if (File.Exists(cert))
            {
                return Result.Try(() => new X509Certificate2(cert, password));
            }

            return Result.Fail($"The certificate file {cert} does not exist!");
        }
        private Result<X509Certificate2> GetCertificateFromStore(string cert, string? storeName, string? storeLocation)
        {
            var certInfo = new CertificateInfo
            {
                Name = cert
            };

            if (!string.IsNullOrWhiteSpace(storeName))
            {
                try
                {
                    certInfo.StoreName = Enum.Parse<StoreName>(storeName, true);
                }
                catch (Exception)
                {

                    return Result.Fail($"{storeName} is not a valid store!");
                }

            }

            if (!string.IsNullOrWhiteSpace(storeLocation))
            {
                try
                {
                    certInfo.Location = Enum.Parse<StoreLocation>(storeLocation, true);
                    
                }
                catch (Exception)
                {

                    return Result.Fail($"{storeLocation} is not a valid location!");
                }
            }
            _logger.LogInformation("Certificate name is: {Name}", certInfo.Name);
            _logger.LogInformation("Store name is: {StoreName}", certInfo.StoreName);
            _logger.LogInformation("Store location is: {Location}", certInfo.Location);
            _logger.LogInformation("Certificate search is: {FindType}", certInfo.FindType);

            return Result.Try(() => _x509CertificateLoader.FindCertificate(certInfo));
        }

    }
}
