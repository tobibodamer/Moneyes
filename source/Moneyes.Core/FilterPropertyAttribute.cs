using System;

namespace Moneyes.Core
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class FilterPropertyAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _descriptiveName;

        public FilterPropertyAttribute(string descriptiveName)
        {
            _descriptiveName = descriptiveName;
        }

        public string DescriptiveName
        {
            get { return _descriptiveName; }
        }
    }
}
