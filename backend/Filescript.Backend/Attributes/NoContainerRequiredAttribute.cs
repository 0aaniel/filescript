using System;

namespace Filescript.Backend.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class NoContainerRequiredAttribute : Attribute
    {
    }
}