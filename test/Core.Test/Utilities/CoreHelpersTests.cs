﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Core.Utilities;
using Xunit;
using Bit.Core.Test.AutoFixture.Attributes;
using Bit.Core.Test.AutoFixture.UserFixtures;
using IdentityModel;
using Bit.Core.Enums.Provider;
using Bit.Core.Models.Table;
using Bit.Core.Context;
using AutoFixture;
using Bit.Core.Test.AutoFixture;
using Bit.Core.Enums;

namespace Bit.Core.Test.Utilities
{
    public class CoreHelpersTests
    {
        public static IEnumerable<object[]> _epochTestCases = new[]
        {
            new object[] {new DateTime(2020, 12, 30, 11, 49, 12, DateTimeKind.Utc), 1609328952000L},
        };

        [Fact]
        public void GenerateComb_Success()
        {
            // Arrange & Act
            var comb = CoreHelpers.GenerateComb();

            // Assert
            Assert.NotEqual(Guid.Empty, comb);
            // TODO: Add more asserts to make sure important aspects of
            // the comb are working properly
        }

        [Theory]
        [InlineData(2, 5, new[] { 1, 2, 3, 4, 5, 6, 7, 8 , 9, 0 })]
        [InlineData(2, 3, new[] { 1, 2, 3, 4, 5 })]
        [InlineData(2, 1, new[] { 1, 2 })]
        [InlineData(1, 1, new[] { 1 })]
        [InlineData(2, 2, new[] { 1, 2, 3 })]
        public void Batch_Success(int batchSize, int totalBatches, int[] collection)
        {
            // Arrange
            var remainder = collection.Length % batchSize;

            // Act
            var batches = collection.Batch(batchSize);

            // Assert
            Assert.Equal(totalBatches, batches.Count());

            foreach (var batch in batches.Take(totalBatches - 1))
            {
                Assert.Equal(batchSize, batch.Count());
            }

            Assert.Equal(batches.Last().Count(), remainder == 0 ? batchSize : remainder);
        }

        [Fact]
        public void ToGuidIdArrayTVP_Success()
        {
            // Arrange
            var item0 = Guid.NewGuid();
            var item1 = Guid.NewGuid();

            var ids = new[] { item0, item1 };

            // Act
            var dt = ids.ToGuidIdArrayTVP();

            // Assert
            Assert.Single(dt.Columns);
            Assert.Equal("GuidId", dt.Columns[0].ColumnName);
            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal(item0, dt.Rows[0][0]);
            Assert.Equal(item1, dt.Rows[1][0]);
        }

        // TODO: Test the other ToArrayTVP Methods

        [Theory]
        [InlineData("12345&6789", "123456789")]
        [InlineData("abcdef", "ABCDEF")]
        [InlineData("1!@#$%&*()_+", "1")]
        [InlineData("\u00C6123abc\u00C7", "123ABC")]
        [InlineData("123\u00C6ABC", "123ABC")]
        [InlineData("\r\nHello", "E")]
        [InlineData("\tdef", "DEF")]
        [InlineData("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUV1234567890", "ABCDEFABCDEF1234567890")]
        public void CleanCertificateThumbprint_Success(string input, string output)
        {
            // Arrange & Act
            var sanitizedInput = CoreHelpers.CleanCertificateThumbprint(input);

            // Assert
            Assert.Equal(output, sanitizedInput);
        }

        // TODO: Add more tests
        [Theory]
        [MemberData(nameof(_epochTestCases))]
        public void ToEpocMilliseconds_Success(DateTime date, long milliseconds)
        {
            // Act & Assert
            Assert.Equal(milliseconds, CoreHelpers.ToEpocMilliseconds(date));
        }

        [Theory]
        [MemberData(nameof(_epochTestCases))]
        public void FromEpocMilliseconds(DateTime date, long milliseconds)
        {
            // Act & Assert
            Assert.Equal(date, CoreHelpers.FromEpocMilliseconds(milliseconds));
        }

        [Fact]
        public void SecureRandomString_Success()
        {
            // Arrange & Act
            var @string = CoreHelpers.SecureRandomString(8);

            // Assert
            // TODO: Should probably add more Asserts down the line
            Assert.Equal(8, @string.Length);
        }

        [Theory]
        [InlineData(1, "1 Bytes")]
        [InlineData(-5L, "-5 Bytes")]
        [InlineData(1023L, "1023 Bytes")]
        [InlineData(1024L, "1 KB")]
        [InlineData(1025L, "1 KB")]
        [InlineData(-1023L, "-1023 Bytes")]
        [InlineData(-1024L, "-1 KB")]
        [InlineData(-1025L, "-1 KB")]
        [InlineData(1048575L, "1024 KB")]
        [InlineData(1048576L, "1 MB")]
        [InlineData(1048577L, "1 MB")]
        [InlineData(-1048575L, "-1024 KB")]
        [InlineData(-1048576L, "-1 MB")]
        [InlineData(-1048577L, "-1 MB")]
        [InlineData(1073741823L, "1024 MB")]
        [InlineData(1073741824L, "1 GB")]
        [InlineData(1073741825L, "1 GB")]
        [InlineData(-1073741823L, "-1024 MB")]
        [InlineData(-1073741824L, "-1 GB")]
        [InlineData(-1073741825L, "-1 GB")]
        [InlineData(long.MaxValue, "8589934592 GB")]
        public void ReadableBytesSize_Success(long size, string readable)
        {
            // Act & Assert
            Assert.Equal(readable, CoreHelpers.ReadableBytesSize(size));
        }

        [Fact]
        public void CloneObject_Success()
        {
            var original = new { Message = "Message" };

            var copy = CoreHelpers.CloneObject(original);

            Assert.Equal(original.Message, copy.Message);
        }

        [Fact]
        public void ExtendQuery_AddNewParameter_Success()
        {
            // Arrange
            var uri = new Uri("https://bitwarden.com/?param1=value1");

            // Act
            var newUri = CoreHelpers.ExtendQuery(uri,
                new Dictionary<string, string> { { "param2", "value2" } });

            // Assert
            Assert.Equal("https://bitwarden.com/?param1=value1&param2=value2", newUri.ToString());
        }

        [Fact]
        public void ExtendQuery_AddTwoNewParameters_Success()
        {
            // Arrange
            var uri = new Uri("https://bitwarden.com/?param1=value1");

            // Act
            var newUri = CoreHelpers.ExtendQuery(uri,
                new Dictionary<string, string>
                {
                    { "param2", "value2" },
                    { "param3", "value3" }
                });

            // Assert
            Assert.Equal("https://bitwarden.com/?param1=value1&param2=value2&param3=value3", newUri.ToString());
        }

        [Fact]
        public void ExtendQuery_AddExistingParameter_Success()
        {
            // Arrange
            var uri = new Uri("https://bitwarden.com/?param1=value1&param2=value2");

            // Act
            var newUri = CoreHelpers.ExtendQuery(uri,
                new Dictionary<string, string> { { "param1", "test_value" } });

            // Assert
            Assert.Equal("https://bitwarden.com/?param1=test_value&param2=value2", newUri.ToString());
        }

        [Fact]
        public void ExtendQuery_AddNoParameters_Success()
        {
            // Arrange
            const string startingUri = "https://bitwarden.com/?param1=value1";

            var uri = new Uri(startingUri);

            // Act
            var newUri = CoreHelpers.ExtendQuery(uri, new Dictionary<string, string>());

            // Assert
            Assert.Equal(startingUri, newUri.ToString());
        }

        [Theory]
        [InlineData("bücher.com", "xn--bcher-kva.com")]
        [InlineData("bücher.cömé", "xn--bcher-kva.xn--cm-cja4c")]
        [InlineData("hello@bücher.com", "hello@xn--bcher-kva.com")]
        [InlineData("hello@world.cömé", "hello@world.xn--cm-cja4c")]
        [InlineData("hello@bücher.cömé", "hello@xn--bcher-kva.xn--cm-cja4c")]
        [InlineData("ascii.com", "ascii.com")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void PunyEncode_Success(string text, string expected)
        {
            var actual = CoreHelpers.PunyEncode(text);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetEmbeddedResourceContentsAsync_Success()
        {
            var fileContents = CoreHelpers.GetEmbeddedResourceContentsAsync("data.embeddedResource.txt");
            Assert.Equal("Contents of embeddedResource.txt\n", fileContents);
        }

        [Theory, CustomAutoData(typeof(UserFixture))]
        public void BuildIdentityClaims_BaseClaims_Success(User user, bool isPremium)
        {
            var expected = new Dictionary<string, string>
            {
                { "premium", isPremium ? "true" : "false" },
                { JwtClaimTypes.Email, user.Email },
                { JwtClaimTypes.EmailVerified, user.EmailVerified ? "true" : "false" },
                { JwtClaimTypes.Name, user.Name },
                { "sstamp", user.SecurityStamp },
            }.ToList();

            var actual = CoreHelpers.BuildIdentityClaims(user, Array.Empty<CurrentContentOrganization>(),
                Array.Empty<CurrentContentProvider>(), isPremium);

            foreach (var claim in expected)
            {
                Assert.Contains(claim, actual);
            }
            Assert.Equal(expected.Count, actual.Count);
        }

        [Theory, CustomAutoData(typeof(UserFixture))]
        public void BuildIdentityClaims_OrganizationClaims_Success(User user)
        {
            var fixture = new Fixture().WithAutoNSubstitutions();
            var orgs = new List<CurrentContentOrganization>();
            foreach (var providerUserType in Enum.GetValues<OrganizationUserType>())
            {
                var org = fixture.Create<CurrentContentOrganization>();
                org.Type = providerUserType;
                orgs.Add(org);
            }

            var claims = new List<KeyValuePair<string, string>>();

            // Orgs that this user belongs to
            if (orgs.Any())
            {
                foreach (var group in orgs.GroupBy(o => o.Type))
                {
                    switch (group.Key)
                    {
                        case Enums.OrganizationUserType.Owner:
                            foreach (var org in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("orgowner", org.Id.ToString()));
                            }
                            break;
                        case Enums.OrganizationUserType.Admin:
                            foreach (var org in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("orgadmin", org.Id.ToString()));
                            }
                            break;
                        case Enums.OrganizationUserType.Manager:
                            foreach (var org in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("orgmanager", org.Id.ToString()));
                            }
                            break;
                        case Enums.OrganizationUserType.User:
                            foreach (var org in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("orguser", org.Id.ToString()));
                            }
                            break;
                        case Enums.OrganizationUserType.Custom:
                            foreach (var org in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("orgcustom", org.Id.ToString()));

                                if (org.Permissions.AccessBusinessPortal)
                                {
                                    claims.Add(new KeyValuePair<string, string>("accessbusinessportal", org.Id.ToString()));
                                }

                                if (org.Permissions.AccessEventLogs)
                                {
                                    claims.Add(new KeyValuePair<string, string>("accesseventlogs", org.Id.ToString()));
                                }

                                if (org.Permissions.AccessImportExport)
                                {
                                    claims.Add(new KeyValuePair<string, string>("accessimportexport", org.Id.ToString()));
                                }

                                if (org.Permissions.AccessReports)
                                {
                                    claims.Add(new KeyValuePair<string, string>("accessreports", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageAllCollections)
                                {
                                    claims.Add(new KeyValuePair<string, string>("manageallcollections", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageAssignedCollections)
                                {
                                    claims.Add(new KeyValuePair<string, string>("manageassignedcollections", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageGroups)
                                {
                                    claims.Add(new KeyValuePair<string, string>("managegroups", org.Id.ToString()));
                                }

                                if (org.Permissions.ManagePolicies)
                                {
                                    claims.Add(new KeyValuePair<string, string>("managepolicies", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageSso)
                                {
                                    claims.Add(new KeyValuePair<string, string>("managesso", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageUsers)
                                {
                                    claims.Add(new KeyValuePair<string, string>("manageusers", org.Id.ToString()));
                                }

                                if (org.Permissions.ManageResetPassword)
                                {
                                    claims.Add(new KeyValuePair<string, string>("manageresetpassword", org.Id.ToString()));
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            var actual = CoreHelpers.BuildIdentityClaims(user, orgs, Array.Empty<CurrentContentProvider>(), false);
            foreach (var claim in claims)
            {
                Assert.Contains(claim, actual);
            }
        }

        [Theory, CustomAutoData(typeof(UserFixture))]
        public void BuildIdentityClaims_ProviderClaims_Success(User user)
        {
            var fixture = new Fixture().WithAutoNSubstitutions();
            var providers = new List<CurrentContentProvider>();
            foreach (var providerUserType in Enum.GetValues<ProviderUserType>())
            {
                var provider = fixture.Create<CurrentContentProvider>();
                provider.Type = providerUserType;
                providers.Add(provider);
            }

            var claims = new List<KeyValuePair<string, string>>();

            if (providers.Any())
            {
                foreach (var group in providers.GroupBy(o => o.Type))
                {
                    switch (group.Key)
                    {
                        case ProviderUserType.ProviderAdmin:
                            foreach (var provider in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("providerprovideradmin", provider.Id.ToString()));
                            }
                            break;
                        case ProviderUserType.ServiceUser:
                            foreach (var provider in group)
                            {
                                claims.Add(new KeyValuePair<string, string>("providerserviceuser", provider.Id.ToString()));
                            }
                            break;
                    }
                }
            }

            var actual = CoreHelpers.BuildIdentityClaims(user, Array.Empty<CurrentContentOrganization>(), providers, false);
            foreach (var claim in claims)
            {
                Assert.Contains(claim, actual);
            }
        }

    }
}
