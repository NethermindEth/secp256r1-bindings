// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Security.Cryptography;
using NUnit.Framework;

namespace Nethermind.Crypto.Tests;

public class Secp256r1Tests
{
    [Theory]
    // https://github.com/paradigmxyz/alphanet/blob/7bbdcc0bbb28230b7be6d15f32552fcd885654d5/crates/precompile/src/secp256r1.rs#L154
    [TestCase(
        "4cee90eb86eaa050036147a12d49004b6b9c72bd725d39d4785011fe190f0b4da73bd4903f0ce3b639bbbf6e8e80d16931ff4bcf5993d58468e8fb19086e8cac36dbcd03009df8c59286b162af3bd7fcc0450c9aa81be5d10d312af6c66b1d604aebd3099c618202fcfe16ae7770b0c49ab5eadf74b754204a3bb6060e44eff37618b065f9832de4ca6ca971a7a1adc826d0f7c00181a5fb2ddf79ae00b4e10e",
        true
    )]
    [TestCase(
        "3fec5769b5cf4e310a7d150508e82fb8e3eda1c2c94c61492d3bd8aea99e06c9e22466e928fdccef0de49e3503d2657d00494a00e764fd437bdafa05f5922b1fbbb77c6817ccf50748419477e843d5bac67e6a70e97dde5a57e0c983b777e1ad31a80482dadf89de6302b1988c82c29544c9c07bb910596158f6062517eb089a2f54c9a0f348752950094d3228d3b940258c75fe2a413cb70baa21dc2e352fc5",
        true
    )]
    [TestCase(
        "e775723953ead4a90411a02908fd1a629db584bc600664c609061f221ef6bf7c440066c8626b49daaa7bf2bcc0b74be4f7a1e3dcf0e869f1542fe821498cbf2de73ad398194129f635de4424a07ca715838aefe8fe69d1a391cfa70470795a80dd056866e6e1125aff94413921880c437c9e2570a28ced7267c8beef7e9b2d8d1547d76dfcf4bee592f5fefe10ddfb6aeb0991c5b9dbbee6ec80d11b17c0eb1a",
        true
    )]
    [TestCase(
        "b5a77e7a90aa14e0bf5f337f06f597148676424fae26e175c6e5621c34351955289f319789da424845c9eac935245fcddd805950e2f02506d09be7e411199556d262144475b1fa46ad85250728c600c53dfd10f8b3f4adf140e27241aec3c2da3a81046703fccf468b48b145f939efdbb96c3786db712b3113bb2488ef286cdcef8afe82d200a5bb36b5462166e8ce77f2d831a52ef2135b2af188110beaefb1",
        true
    )]
    [TestCase(
        "858b991cfd78f16537fe6d1f4afd10273384db08bdfc843562a22b0626766686f6aec8247599f40bfe01bec0e0ecf17b4319559022d4d9bf007fe929943004eb4866760dedf31b7c691f5ce665f8aae0bda895c23595c834fecc2390a5bcc203b04afcacbb4280713287a2d0c37e23f7513fab898f2c1fefa00ec09a924c335d9b629f1d4fb71901c3e59611afbfea354d101324e894c788d1c01f00b3c251b2",
        true
    )]
    [TestCase(
        "3cee90eb86eaa050036147a12d49004b6b9c72bd725d39d4785011fe190f0b4da73bd4903f0ce3b639bbbf6e8e80d16931ff4bcf5993d58468e8fb19086e8cac36dbcd03009df8c59286b162af3bd7fcc0450c9aa81be5d10d312af6c66b1d604aebd3099c618202fcfe16ae7770b0c49ab5eadf74b754204a3bb6060e44eff37618b065f9832de4ca6ca971a7a1adc826d0f7c00181a5fb2ddf79ae00b4e10e",
        false
    )]
    [TestCase(
        "afec5769b5cf4e310a7d150508e82fb8e3eda1c2c94c61492d3bd8aea99e06c9e22466e928fdccef0de49e3503d2657d00494a00e764fd437bdafa05f5922b1fbbb77c6817ccf50748419477e843d5bac67e6a70e97dde5a57e0c983b777e1ad31a80482dadf89de6302b1988c82c29544c9c07bb910596158f6062517eb089a2f54c9a0f348752950094d3228d3b940258c75fe2a413cb70baa21dc2e352fc5",
        false
    )]
    [TestCase(
        "f775723953ead4a90411a02908fd1a629db584bc600664c609061f221ef6bf7c440066c8626b49daaa7bf2bcc0b74be4f7a1e3dcf0e869f1542fe821498cbf2de73ad398194129f635de4424a07ca715838aefe8fe69d1a391cfa70470795a80dd056866e6e1125aff94413921880c437c9e2570a28ced7267c8beef7e9b2d8d1547d76dfcf4bee592f5fefe10ddfb6aeb0991c5b9dbbee6ec80d11b17c0eb1a",
        false
    )]
    [TestCase(
        "c5a77e7a90aa14e0bf5f337f06f597148676424fae26e175c6e5621c34351955289f319789da424845c9eac935245fcddd805950e2f02506d09be7e411199556d262144475b1fa46ad85250728c600c53dfd10f8b3f4adf140e27241aec3c2da3a81046703fccf468b48b145f939efdbb96c3786db712b3113bb2488ef286cdcef8afe82d200a5bb36b5462166e8ce77f2d831a52ef2135b2af188110beaefb1",
        false
    )]
    [TestCase(
        "958b991cfd78f16537fe6d1f4afd10273384db08bdfc843562a22b0626766686f6aec8247599f40bfe01bec0e0ecf17b4319559022d4d9bf007fe929943004eb4866760dedf31b7c691f5ce665f8aae0bda895c23595c834fecc2390a5bcc203b04afcacbb4280713287a2d0c37e23f7513fab898f2c1fefa00ec09a924c335d9b629f1d4fb71901c3e59611afbfea354d101324e894c788d1c01f00b3c251b2",
        false
    )]
    public void Verifies_static_signature(string input, bool isValid)
    {
        var bytes = Convert.FromHexString(input);

        Assert.That(Secp256r1.VerifySignature(bytes), Is.EqualTo(isValid));
    }

    [Test]
    // Generated signatures will be still be non-deterministic due to k randomness.
    [TestCase("619A6C9ABD7985F2175EF0EDF3DBC7CFB9FCB3DAE83991CCFB00D10DFF23E956", 100)]
    [TestCase("747B0D580A8FCE53AE3166FCBDB2123E4488B4BAAD007357D8F60626D95C0914", 100)]
    [TestCase("71FE3CF83CCDE61933CA269715D8AEC760F6B7FA5C13D357E2D2F5AD0E8FCA29", 100)]
    [TestCase("E10D2BD764C3E07B049E313BD38783D4060EBD36D988CE8B25A2B8646D54E0FC", 100)]
    [TestCase("4224A93AF42729459D6C366AD24800259948A94717E4178FC7CDB709596F5ACA", 100)]
    public void Verifies_ecdsa_generated_signatures(string d, int msgCount)
    {
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Convert.FromHexString(d)
        });
        ECParameters parameters = ecdsa.ExportParameters(false);

        var hashes = Enumerable.Range(1, msgCount).Select(i => SHA256.HashData(BitConverter.GetBytes(i))).ToArray();
        for (var i = 0; i < hashes.Length; i++)
        {
            var hash = hashes[i];
            var sig = ecdsa.SignHash(hash);
            var input = Enumerable.Empty<byte>()
                .Concat(hash)
                .Concat(sig)
                .Concat(parameters.Q.X!)
                .Concat(parameters.Q.Y!)
                .ToArray();

            using var temp = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = { X = parameters.Q.X, Y = parameters.Q.Y }
            });

            Assert.That(Secp256r1.VerifySignature(input), Is.True, $"input #{i} is 0x{Convert.ToHexString(input)}.");
        }
    }

    [Theory]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(159)]
    [TestCase(161)]
    [TestCase(320)]
    public void Returns_false_on_invalid_length(int length)
    {
        var bytes = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();

        Assert.That(Secp256r1.VerifySignature(bytes), Is.False);
    }
}
