using System;
using Cornerstone.Presentation.Collections;

namespace Cornerstone.Presentation.Controls.Templates
{
    /// <summary>
    /// A collection of <see cref="IDataTemplate"/>s.
    /// </summary>
    public class DataTemplates : OldPresentationList<IDataTemplate>, IOldPresentationListItemValidator<IDataTemplate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplates"/> class.
        /// </summary>
        public DataTemplates()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validator = this;
        }

        void IOldPresentationListItemValidator<IDataTemplate>.Validate(IDataTemplate item)
        {
            var valid = item switch
            {
                ITypedDataTemplate typed => typed.DataType is not null,
                _ => true
            };
            
            if (!valid)
            {
                throw new InvalidOperationException("DataTemplate inside of DataTemplates must have a DataType set. Set DataType property or use ItemTemplate with single template instead.");
            }
        }
    }
}
