﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticodeExaminer;

namespace NuGetPackageExplorer.Types
{
    public class AuthenticodeSignature
    {
        private readonly ISignature _signature;

        public static IReadOnlyList<AuthenticodeSignature> FromSignatures(IReadOnlyList<ISignature> signatures)
        {
            var sigs = new List<AuthenticodeSignature>();

            foreach (var signature in signatures)
            {
                if (signature.Kind == SignatureKind.Signature || signature.Kind == SignatureKind.NestedSignature)
                {
                    sigs.Add(new AuthenticodeSignature(signature));
                    var nestedSig = signature.GetNestedSignatures()
                                             .Where(s => s.Kind == SignatureKind.NestedSignature)
                                             .FirstOrDefault();

                    if (nestedSig != null)
                    {
                        sigs.Add(new AuthenticodeSignature(nestedSig));
                    }
                }
            }
            
            return sigs;
        }

        private AuthenticodeSignature(ISignature signature)
        {
            _signature = signature ?? throw new ArgumentNullException(nameof(signature));
            
            PopulatePublisherInfo();
            PopulateTimestamp();
        }

        private void PopulatePublisherInfo()
        {
            foreach (var attribute in _signature.SignedAttributes)
            {
                if (attribute.Oid.Value == KnownOids.OpusInfo)
                {
                    PublisherInformation = new PublisherInformation(attribute.Values[0]);
                    break;
                }
            }
        }

        private void PopulateTimestamp()
        {
            var tsSig = _signature.GetNestedSignatures()
                                  .Where(s => s.Kind == SignatureKind.Rfc3161Timestamp || s.Kind == SignatureKind.AuthenticodeTimestamp)
                                  .FirstOrDefault();

            if (tsSig != null)
            {
                foreach (var attribute in tsSig.SignedAttributes)
                {
                    if (attribute.Oid.Value == KnownOids.SigningTime)
                    {
                        Timestamp = new SigningTime(attribute.Values[0]);
                        break;
                    }
                }
            }
        }

        public PublisherInformation PublisherInformation { get; private set; }

        public SigningTime Timestamp { get; private set; }
    }
}
