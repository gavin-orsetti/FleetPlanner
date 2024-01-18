using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static FleetPlanner.Helpers.Constants;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ViewModelBase : BaseViewModel, IQueryAttributable
    {
        private AsyncCommand backCommand;
        public AsyncCommand BackCommand => backCommand ??= new AsyncCommand( Back );

        public virtual async Task Back()
        {
            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        public virtual async void ApplyQueryAttributes( IDictionary<string, object> query )
        {
            foreach( KeyValuePair<string, object> kvp in query )
            {
                await EvaluateQueryParams( kvp );
                EvaluateQueryParams( kvp.Key, kvp.Value );
            }
        }


        /// <summary>
        /// Asyncronously evaluate query parameters and do work when arriving at a new view. Returns null unless overridden.
        /// </summary>
        /// <param name="kvp">A <see cref="KeyValuePair"/> representing the query parameter name with the key, and the value in the value. Good for evaluating query parameters with switch statements.</param>
        /// <returns><see cref="Task"/> representing whatever work the passed in parameter triggers.</returns>
        private protected virtual Task EvaluateQueryParams( KeyValuePair<string, object> kvp ) { return null; }

        /// <summary>
        /// Synchronously evaluate query parameters when arriving at a new view. Empty unless overriden.
        /// </summary>
        /// <param name="key">The parameter name</param>
        /// <param name="value">The parameter value</param>
        private protected virtual void EvaluateQueryParams( string key, object value ) { }
    }
}
