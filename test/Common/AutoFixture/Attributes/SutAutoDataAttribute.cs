using System;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;

namespace Bit.Test.Common.AutoFixture.Attributes
{
    public class SutAutoDataAttribute : CustomAutoDataAttribute
    {
        public SutAutoDataAttribute(params Type[] iCustomizationTypes) : base(
            iCustomizationTypes.Append(typeof(SutProviderCustomization)).ToArray())
        { }
    }
}
